using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System;
using System.Collections.Generic;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using System.Collections.ObjectModel;
using Microsoft.Data.Sqlite;
using System.Printing;

namespace industrial_monitoring_platform
{

    public partial class MainWindow : Window
    {
        private List<SensorReading> _readings = new();
        private int _currentIndex = 0;
        private DispatcherTimer _timer = new();

        private string Status { get; set; } = "OK";
        private List<SensorReading> _visibleReadings = new();

        private ObservableCollection<double> _temperatureValues = new();
        private LineSeries<double> _temperatureSeries;
        private double? _observedMin = null;
        private double? _observedMax = null;


        public MainWindow()
        {
            InitializeComponent();

            InitializeDatabase();
            /*
            MessageBox.Show(
            $"Loaded {LoadRecentReadingsFromDatabase(20).Count} most recent readings from the database.\n" + 
            $" Database contains {CountSensorReadings()} readings.");
            */
            TemperatureText.Text = "Temperature: 72.4 °C";
            PressureText.Text = "Pressure: 4.8 bar";
            FlowRateText.Text = "Flow Rate: 120 L/min";
            TankLevelText.Text = "Tank Level: 65 %";
            StatusText.Foreground = Brushes.Green;

            //graphing
            _temperatureSeries = new LineSeries<double>
            {
                Values = _temperatureValues,
                Name = "Temperature °C"
            };

            TemperatureChart.Series = new ISeries[]
            {
                _temperatureSeries
            };

            //incoming sensor data
            _readings = LoadCsv("data/sensor_readings.csv");

            _timer.Interval = TimeSpan.FromSeconds(1);//FromMilliseconds(100);
            _timer.Tick += Timer_Tick;
            _timer.Start();

        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (_readings.Count == 0)
                return;

            var reading = _readings[_currentIndex];

            InsertSensorReading(reading);


            //DB actions
            int rowCount = CountSensorReadings();
            Title = $"Industrial Monitoring Platform - DB rows: {rowCount}";

            //live readings
            TimestampText.Text = $"Time: {reading.Timestamp}";
            TemperatureText.Text = $"Temperature: {reading.TemperatureC} °C";
            PressureText.Text = $"Pressure: {reading.PressureBar} bar";
            FlowRateText.Text = $"Flow Rate: {reading.FlowRateLMin} L/min";
            TankLevelText.Text = $"Tank Level: {reading.TankLevelPercent} %";


            //chart
            _temperatureValues.Add(reading.TemperatureC);
            if(_temperatureValues.Count > 20)//30
            {
                _temperatureValues.RemoveAt(0);
            }
            if (_observedMin == null || reading.TemperatureC < _observedMin)
                _observedMin = reading.TemperatureC;

            if (_observedMax == null || reading.TemperatureC > _observedMax)
                _observedMax = reading.TemperatureC;
            TemperatureChart.YAxes = new Axis[]
            {
                new Axis
                    {
                        MinLimit = _observedMin - 0.2,
                        MaxLimit = _observedMax + 0.2
                    }
            };

            //data table
            _visibleReadings.Insert(0,reading);
            if (_visibleReadings.Count > 20)
                _visibleReadings.RemoveAt(_visibleReadings.Count - 1);
            ReadingsGrid.ItemsSource = null;
            ReadingsGrid.ItemsSource = _visibleReadings;


            Status = GetStatus(reading);
            StatusText.Text = $"System Status: {Status}";

            if (Status == "OK")
                StatusText.Foreground = Brushes.Green;
            else if (Status == "WARNING")
                StatusText.Foreground = Brushes.Orange;
            else StatusText.Foreground = Brushes.Red;

            _currentIndex++;
            if (_currentIndex >= _readings.Count)
                _currentIndex = 0;

        }

        private List<SensorReading> LoadCsv(string path)
        {
            var readings = new List<SensorReading>();

            var lines = File.ReadAllLines(path);

            for (int i = 1; i < lines.Length; i++) // skip header
            {
                var parts = lines[i].Split(',');

                readings.Add(new SensorReading
                {
                    Timestamp = DateTime.Parse(parts[0]),
                    TemperatureC = double.Parse(parts[1]),
                    PressureBar = double.Parse(parts[2]),
                    FlowRateLMin = double.Parse(parts[3]),
                    TankLevelPercent = double.Parse(parts[4])
                });
            }

            return readings;
        }

        private List<SensorReading> LoadRecentReadingsFromDatabase(int count)
        {
            var readings = new List<SensorReading>();

            using var connection = new SqliteConnection("Data Source=data/industrial.db");
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = 
                """
                SELECT Timestamp,
                Temperature,
                Pressure,
                Flow,
                TankLevel
                FROM SensorReadings
                ORDER BY Id DESC
                LIMIT $count;
                """;

            command.Parameters.AddWithValue("$count", count);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                readings.Add(new SensorReading
                {
                    Timestamp = DateTime.Parse(reader.GetString(0)),
                    TemperatureC = reader.GetDouble (1),
                    PressureBar = reader.GetDouble (2),
                    FlowRateLMin = reader.GetDouble (3),
                    TankLevelPercent = reader.GetDouble (4)
                });
            }

            return readings;
        }

        private void InsertSensorReading(SensorReading reading)
        {
            using var connection = new SqliteConnection("Data Source=data/industrial.db");
            connection.Open();

            using var command = connection.CreateCommand();

            command.CommandText =
                """
                INSERT INTO SensorReadings
                (
                    Timestamp,
                    Temperature,
                    Pressure,
                    Flow,
                    TankLevel
                )
                VALUES
                (
                    $timestamp,
                    $temperature,
                    $pressure,
                    $flow,
                    $tankLevel
                );
                """;

            command.Parameters.AddWithValue("$timestamp", reading.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("$temperature", reading.TemperatureC);
            command.Parameters.AddWithValue("$pressure", reading.PressureBar);
            command.Parameters.AddWithValue("$flow", reading.FlowRateLMin);
            command.Parameters.AddWithValue("$tankLevel", reading.TankLevelPercent);

            command.ExecuteNonQuery();

        }

        private int CountSensorReadings()
        {
            using var connection = new SqliteConnection("Data Source=data/industrial.db");
            connection.Open();

            using var command = connection.CreateCommand();

            command.CommandText =
                """
                SELECT COUNT(*)
                FROM SensorReadings;
                """;

            return Convert.ToInt32(command.ExecuteScalar());

        }

        private void InitializeDatabase()
        {
            using var connection = new SqliteConnection("Data Source=data/industrial.db");
            connection.Open();

            using var command = connection.CreateCommand();

            command.CommandText =
                """
                CREATE TABLE IF NOT EXISTS SensorReadings
                (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Timestamp TEXT NOT NULL,
                    Temperature REAL NOT NULL,
                    Pressure REAL NOT NULL,
                    Flow REAL NOT NULL,
                    TankLevel REAL NOT NULL
                );
                """;

            command.ExecuteNonQuery();
        }

        private string GetStatus(SensorReading r)
        {
            if (r.TemperatureC > 72.5 ||//80
                r.PressureBar > 7 ||
                r.FlowRateLMin < 80 ||
                r.FlowRateLMin > 160 ||
                r.TankLevelPercent < 20 ||
                r.TankLevelPercent > 90)
            {
                SetLiveValueColor(TemperatureText, r.TemperatureC, 72.4, 72.5);
                return "ALARM";
            }
                
            if (r.TemperatureC > 72.4 ||//80
                r.PressureBar > 6 ||
                r.FlowRateLMin < 90 ||
                r.FlowRateLMin > 150 ||
                r.TankLevelPercent < 25 ||
                r.TankLevelPercent > 85)
            {
                SetLiveValueColor(TemperatureText, r.TemperatureC, 72.4, 72.5);
                return "WARNING";
            }

            TemperatureText.Foreground = Brushes.Black;
            PressureText.Foreground = Brushes.Black;
            FlowRateText.Foreground = Brushes.Black;
            TankLevelText.Foreground = Brushes.Black;
            return "OK";
        }

        private void SetLiveValueColor(TextBlock text, double value, double warningThreshold, double alarmThreshold)
        {

            if (value > alarmThreshold)
            {
                text.Foreground = Brushes.Red;
                return;
            }

            if (value > warningThreshold)
            {
                text.Foreground = Brushes.Orange;
                return;
            }

            text.Foreground = Brushes.Black;
            return;

        }

    }
        
}