using System;
using System.Linq;
using System.Net;

namespace WinLLDPService
{

    class NetworkInfo
    {
        /// <summary>
        /// Convert network mask to CIDR notation
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        public static int GetCIDRFromIPMaskAddress(IPAddress ip)
        {
            return Convert.ToString(BitConverter.ToInt32(ip.GetAddressBytes(), 0), 2).ToCharArray().Count(x => x == '1');
        }

        /// <summary>
        /// Change for example adapter link speed into more human readable format
        /// </summary>
        /// <param name="size"></param>
        /// <param name="unit"></param>
        /// <returns></returns>
        public static string ReadableSize(double size, int unit = 0)
        {
            string[] units = { "b", "Kb", "Mb", "Gb", "Tb", "Pb", "Eb", "Zb", "Yb" };

            while (size >= 1000)
            {
                size /= 1000;
                ++unit;
            }

            return String.Format("{0:G4}{1}", size, units[unit]);
        }


    }
}