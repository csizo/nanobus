using System.Security.Cryptography;
using System.Text;

namespace System.Security
{
    internal class RngTokenProvider : ITokenProvider
    {
        static readonly char[] AlphaNumericCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890+-=/_".ToCharArray();
        public string GetToken(int length)
        {
            byte[] data = new byte[1];
            RNGCryptoServiceProvider crypto = new RNGCryptoServiceProvider();
            crypto.GetNonZeroBytes(data);
            data = new byte[length];
            crypto.GetNonZeroBytes(data);
            StringBuilder result = new StringBuilder(length);
            foreach (byte b in data)
            {
                result.Append(AlphaNumericCharacters[b % (AlphaNumericCharacters.Length)]);
            }
            return result.ToString();
        }
    }
}
