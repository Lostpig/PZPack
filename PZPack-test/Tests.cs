using PZPack.Core;
using System.Diagnostics;

namespace PZPack.Test
{
    internal class Tests
    {
        private static bool CompareBytes(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i]) return false;
            }

            return true;
        }

        public static void TestBytesEncodeAndDecode()
        {
            IPZCrypto crypto = PZCryptoCreater.CreateCrypto("123456", PZVersion.Current);
            byte[] testBuf = new byte[10000];
            for (int i = 0; i < testBuf.Length; i++)
            {
                byte x = (byte)(Random.Shared.Next() % 256);
                testBuf[i] = x;
            }

            byte[] e1 = crypto.Encrypt(testBuf);
            byte[] e2;
            using (MemoryStream input = new(testBuf))
            using (MemoryStream output = new())
            {
                crypto.EncryptStream(input, output);
                e2 = output.ToArray();
            }

            bool isEqualE = CompareBytes(e1, e2);
            Debug.WriteLineIf(isEqualE, "Error: e1 and e2 is equal");
            Console.WriteLine($"origin size: {testBuf.Length}, e1 size: {e1.Length}, e2 size: {e2.Length}");

            byte[] d1 = crypto.Decrypt(e1);
            byte[] d2;
            using (MemoryStream input = new(e2))
            using (MemoryStream output = new())
            {
                crypto.DecryptStream(input, output, 0, input.Length);
                d2 = output.ToArray();
            }

            bool isEqualD = CompareBytes(d1, d2);
            Debug.WriteLineIf(!isEqualD, "Error: d1 and d2 is not equal");
            Console.WriteLine($"origin size: {testBuf.Length}, d1 size: {d1.Length}, d2 size: {d2.Length}");

            bool d1Right = CompareBytes(testBuf, d1);
            bool d2Right = CompareBytes(testBuf, d2);
            Console.WriteLine($"D1: {d1Right}, D2: {d2Right}");
        }
    }
}
