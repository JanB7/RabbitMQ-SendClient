using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static RabbitMQ_SendClient.SystemVariables;

namespace RabbitMQ_SendClient.General_Classes
{
    using System.Net;
    using UI;

    public static class GlocalTCPFunctions
    {
        public struct IPAddressTable
        {
            public IPAddress IpAddress {get;set;}
            public Guid UidGuid { get; set; }
            public ushort Port { get; set; }
        }

        public static bool? UserConfigureIp(Guid uidGuid)
        {
            var ipAddressTables = IpAddressTables;
            Array.Resize(ref ipAddressTables, ipAddressTables.Length +1 );
            ipAddressTables[ipAddressTables.Length - 1].UidGuid = uidGuid;

            var setIpAddress = new ModbusIpConfig
            {
                Port = 503,
                IpAddress = IPAddress.Parse("127.0.0.1")
            };
            var result = setIpAddress.ShowDialog();

            switch (result)
            {
                case null:
                    return null;
                case true:
                    ipAddressTables[ipAddressTables.Length - 1].IpAddress = setIpAddress.IpAddress;
                    ipAddressTables[ipAddressTables.Length - 1].Port = setIpAddress.Port;
                    IpAddressTables = ipAddressTables;
                    return true;
                default:
                    Array.Resize(ref ipAddressTables, ipAddressTables.Length -1);
                    return false;
            }
        }
    }
}
