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

namespace industrial_monitoring_platform
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<SensorReading> _readings = new();
        private int _currentIndex = 0;
        private DispatcherTimer _timer = new();

        private string Status { get; set; } = "OK";
        private List<SensorReading> _visibleReadings = new();

        private ObservableCollection<double> _temperatureValues = new();
        private LineSeries<double> _temperatureSeries;


        public MainWindow()
        {
            InitializeComponent();

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

            _readings = LoadCsv("data/sensor_readings.csv");

            _timer.Interval = TimeSpan.FromSeconds(1);
            //_timer.Interval = TimeSpan.FromMilliseconds(200);
            _timer.Tick += Timer_Tick;
            _timer.Start();

        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (_readings.Count == 0)
                return;

            var reading = _readings[_currentIndex];

            //live readings
            TimestampText.Text = $"Time: {reading.Timestamp}";
            TemperatureText.Text = $"Temperature: {reading.TemperatureC} °C";
            PressureText.Text = $"Pressure: {reading.PressureBar} bar";
            FlowRateText.Text = $"Flow Rate: {reading.FlowRateLMin} L/min";
            TankLevelText.Text = $"Tank Level: {reading.TankLevelPercent} %";


            //manage the chart
            _temperatureValues.Add(reading.TemperatureC);
            if(_temperatureValues.Count > 20)//30
            {
                _temperatureValues.RemoveAt(0);
            }


            //running past data table
            _visibleReadings.Add(reading);
            if (_visibleReadings.Count > 20)
                _visibleReadings.RemoveAt(0);
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