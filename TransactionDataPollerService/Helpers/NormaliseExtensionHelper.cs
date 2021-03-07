using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FtpEasyTransfer.Helpers
{
    public static class NormaliseExtensionHelper
    {
        public static string Normalise(this string input)
        {
            char[] chars = new char[]
            {
                '*',
                '.'
            };

            string output = input.TrimStart(chars);

            return output.ToLower();
        }
    }
}
