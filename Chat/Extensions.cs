using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chat
{
    public static class Extensions
    {
        public static string Truncate(this string inputString, int maxLength)
        {
            if (string.IsNullOrEmpty(inputString))
            {
                return inputString;
            }

            if (inputString.Length <= maxLength)
            {
                return inputString;
            }

            return inputString.Substring(0, maxLength) + "...";
        }
    }
}
