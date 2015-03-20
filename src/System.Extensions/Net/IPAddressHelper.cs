using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace System.Net
{
    public static class IPAddressHelper
    {
        /// <summary>
        ///     hexternal ipddress server
        /// </summary>
        public const string ExternalIpAddressServer = "http://bot.whatismyipaddress.com";

        /// <summary>
        ///     he lazy internal ipddress list
        /// </summary>
        private static readonly Lazy<IEnumerable<IPAddress>> LazyInternalIpAddressList =
            new Lazy<IEnumerable<IPAddress>>(() =>
            {
                var networkInterfaceTypes = new List<NetworkInterfaceType>
                {
                    NetworkInterfaceType.Wireless80211,
                    NetworkInterfaceType.Ethernet,
                };
                return
                    from i in
                        NetworkInterface.GetAllNetworkInterfaces()
                            .Where(
                                i =>
                                    i.OperationalStatus == OperationalStatus.Up &&
                                    networkInterfaceTypes.Contains(i.NetworkInterfaceType))
                    from ip in i.GetIPProperties().UnicastAddresses
                    where ip.Address.AddressFamily == AddressFamily.InterNetwork
                    select ip.Address;
            });

        /// <summary>
        ///     he lazyxternal ipddress
        /// </summary>
        private static readonly Lazy<IPAddress> LazyExternalIpAddress = new Lazy<IPAddress>(() =>
        {
            string ipString = new WebClient().DownloadString(ExternalIpAddressServer);
            return IPAddress.Parse(ipString);
        });

        /// <summary>
        ///     Getshe internal ipddress list.
        /// </summary>
        /// <value>
        ///     he internal ipddress list.
        /// </value>
        public static IEnumerable<IPAddress> InternalIpAddressList
        {
            get { return LazyInternalIpAddressList.Value; }
        }

        /// <summary>
        ///     Getshexternal ipddress.
        /// </summary>
        /// <value>
        ///     hexternal ipddress.
        /// </value>
        public static IPAddress ExternalIpAddress
        {
            get { return LazyExternalIpAddress.Value; }
        }
    }
}