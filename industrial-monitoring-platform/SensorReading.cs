using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace industrial_monitoring_platform
{
    public class SensorReading
    {
        public DateTime Timestamp { get; set; }
        public double TemperatureC { get; set; }
        public double PressureBar { get; set; }
        public double FlowRateLMin { get; set; }
        public double TankLevelPercent { get; set; }


    }

    }
