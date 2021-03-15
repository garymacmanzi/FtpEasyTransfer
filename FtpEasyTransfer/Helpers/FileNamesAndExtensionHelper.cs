using System.IO;

namespace FtpEasyTransfer.Helpers
{
    public static class FileNamesAndExtensionHelper
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

        public static string EnsurePathIsFile(this string toBeChecked, string targetPath)
        {
            string output = toBeChecked;
            

            if (string.IsNullOrEmpty(Path.GetExtension(toBeChecked)))
            {
                if (toBeChecked.EndsWith('/') || toBeChecked.EndsWith('\\'))
                {
                    toBeChecked = toBeChecked.Remove(toBeChecked.Length - 1, 1);
                }
                var fileName = Path.GetFileName(targetPath);
                output = $"{toBeChecked}/{fileName}";
            }

            return output;
        }

        public static string EnsurePathIsDir(this string toBeChecked)
        {
            string output = toBeChecked;

            if (!string.IsNullOrEmpty(Path.GetExtension(toBeChecked)))
            {
                output = Path.GetDirectoryName(toBeChecked);
            }

            return output;
        }
    }
}
