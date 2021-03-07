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
