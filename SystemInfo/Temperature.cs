using System;
using System.Collections.Generic;
using System.Management;
using System.Runtime.Versioning;

namespace SystemInfo
{
    class Temperature
    {
        public double CurrentValue { get; set; }
        public string InstanceName { get; set; } = string.Empty;

        [SupportedOSPlatform("windows")]
        public static List<Temperature> Temperatures
        {
            get
            {
                var result = new List<Temperature>();
                try
                {
                    var searcher = new ManagementObjectSearcher(@"root\WMI", "SELECT * FROM MSAcpi_ThermalZoneTemperature");
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        double temperature = Convert.ToDouble(obj["CurrentTemperature"].ToString());
                        temperature = (temperature - 2732) / 10.0;
                        result.Add(new Temperature
                        {
                            CurrentValue = temperature,
                            InstanceName = obj["InstanceName"].ToString() ?? string.Empty
                        });
                    }
                }
                catch (ManagementException)
                {
                    // Temperatures unavailable — admin privileges required
                }
                return result;
            }
        }
    }
}
