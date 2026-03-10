using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace SystemInfo
{
    class MachineIPAddress
    {
        public string Address { get; set; } = string.Empty;

        public static List<MachineIPAddress> IPaddresses
        {
            get
            {
                var results = new List<MachineIPAddress>();
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                        results.Add(new MachineIPAddress { Address = ip.ToString() });
                }
                return results;
            }
        }
    }
}
