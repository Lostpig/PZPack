using System.Security.Cryptography;
using System.Text;

namespace PZPack.Core
{
    internal static class PZHash
    {
        public static byte[] Hash(string text)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            return Hash(bytes);
        }
        public static byte[] Hash(byte[] source)
        {
            SHA256 sHA256 = SHA256.Create();
            return sHA256.ComputeHash(source);
        }
        public static string HashHex(string text)
        {
            byte[] hashBytes = Hash(text);
            return Convert.ToHexString(hashBytes);
        }
        public static string HashHex(byte[] source)
        {
            byte[] hashBytes = Hash(source);
            return Convert.ToHexString(hashBytes);
        }

    }
}
