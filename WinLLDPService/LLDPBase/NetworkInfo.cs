namespace WinLLDPService
{
    class NetworkInfo
    {
        /// <summary>
        /// Change for example adapter link speed into more human readable format
        /// </summary>
        /// <param name="size"></param>
        /// <param name="unit"></param>
        /// <returns></returns>
        public static string ReadableSize(double size, int unit = 0)
        {
            string[] units = { "b", "K", "M", "G", "T", "P", "E", "Z", "Y" };

            while (size >= 1000)
            {
                size /= 1000;
                ++unit;
            }

            return string.Format("{0:G4}{1}", size, units[unit]);
        }
    }
}