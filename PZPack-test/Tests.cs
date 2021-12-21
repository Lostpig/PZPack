using PZPack.Core;
using System.Diagnostics;

namespace PZPack.Test
{
    internal class Tests
    {
        private static bool CompareBytes(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            for(int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i]) return false;
            }

            return true;
        }
        public static async Task EncodeAndDocodeTest ()
        {
            string source = @"D:\Code\Test\Files\Resources";
            string output = @"D:\Code\Test\Files\Output.pzpk";
            string description = "test";
            string password = "12345678";

            if (File.Exists(output))
            {
                File.Delete(output);
            }

            Console.WriteLine("Encode test start");
            var updater = new ConsoleUpdater();
            await PZPacker.Pack(
                source,
                output,
                new PZPackInfo(password, description),
                new ProgressReporter<(int, int, long, long)>((v) => {
                    (int count, int total, long processed, long size) = v;
                    double prec = processed / size;
                    updater.Update($"Progress: ${prec:f1}% (${count} / ${total})");
                })
            );
            updater.End();
            Console.WriteLine("Encode test complete");

            Console.WriteLine("Decode test start");
            var reader = PZReader.Open(output, password);
            var decryptor = new PZCrypto(password);
            Debug.Assert(reader.Description == description);

            PZFile[] packfiles = reader.GetAllFiles();
            FileInfo[] files = new DirectoryInfo(source).GetFiles();
            foreach (FileInfo file in files)
            {
                PZFile include = packfiles.First(f => f.Name == file.Name);
                Debug.Assert(include != null);

                byte[] ibytes = await reader.ReadFile(include);
                byte[] obytes = File.ReadAllBytes(file.FullName);
                byte[] oebytes = decryptor.Encrypt(obytes);

                byte[] ebytes = reader.ReadOrigin(include.Offset, include.Size);
                byte[] dbytes = decryptor.Decrypt(ebytes);

                bool isEqual = CompareBytes(ibytes, obytes);
                bool isEqual2 = CompareBytes(dbytes, obytes);
                bool isEqualE = CompareBytes(oebytes, ebytes);
                Debug.Assert(isEqual);
                Debug.Assert(isEqual2);
                Debug.Assert(isEqualE);
            }
             
            reader.Dispose();
            Console.WriteLine("Decode test complete");
        }

        public static async Task TestBytesEncodeAndDecode()
        {
            PZCrypto crypto = new("123456");
            byte[] aa = new byte[15000];
            for (int i = 0; i < aa.Length; i++)
            {
                byte x = (byte)(Random.Shared.Next() % 256);
                aa[i] = x;
            }

            byte[] e1 = crypto.Encrypt(aa);
            byte[] e2;
            using (MemoryStream input = new(aa))
            using(MemoryStream output = new())
            {
                await crypto.EncryptStream(input, output);
                e2 = output.ToArray();
            }

            bool isEqualE = CompareBytes(e1, e2);
            Debug.Assert(isEqualE);
            Console.WriteLine($"origin size: {aa.Length}, e1 size: {e1.Length}, e2 size: {e2.Length}");

            byte[] d1 = crypto.Decrypt(e1);
            byte[] d2;
            using (MemoryStream input = new(e2))
            using (MemoryStream output = new())
            {
                await crypto.DecryptStream(input, output, 0, input.Length);
                d2 = output.ToArray();
            }

            bool isEqualD = CompareBytes(d1, d2);
            Debug.Assert(isEqualD);
            Console.WriteLine($"origin size: {aa.Length}, d1 size: {d1.Length}, d2 size: {d2.Length}");

            bool d1Right = CompareBytes(aa, d1);
            bool d2Right = CompareBytes(aa, d2);
            Console.WriteLine($"D1: {d1Right}, D2: {d2Right}");
        }
    }
}
