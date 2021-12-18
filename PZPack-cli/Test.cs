using PZPack.Core;
using System.Text;
using System.Diagnostics;

namespace PZPack.Cli
{
    internal class Test
    {
        private class ProgressReporter<T> : IProgress<T>
        {
            readonly Action<T> action;
            public ProgressReporter(Action<T> progressAction) =>
                action = progressAction;

            public void Report(T value) => action(value);
        }

        public static void FullEncodeTest()
        {
            Console.WriteLine("PZPack version .Net console test: FullEncodeTest");

            string dir = @"D:\Media\Picture";
            string output = @"D:\Media\pictures2.pzpk";

            var updater = new ConsoleUpdater();
            var progress = new ProgressReporter<(int, int)>((tp) =>
            {
                (int count, int total) = tp;
                double prec = (count / Convert.ToDouble(total)) * 100;
                updater.Update($"{prec:F1}% ({count} / {total})");
            });

            updater.Begin();
            long len = 0;
            var startTime = DateTime.Now;
            try
            {
                PackOption option = new("4294967296", "this is a test file");
                len = PZPacker.Pack(dir, output, option, progress);
            }
            catch (Exception ex)
            {
                updater.End();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
            }
            var endTime = DateTime.Now;
            updater.End();

            var time = endTime - startTime;
            Console.ResetColor();
            Console.WriteLine("PZPack test excute complete");
            Console.WriteLine($"source: {dir}");
            Console.WriteLine($"target: {output}");
            Console.WriteLine($"Processed {len} bytes in time: { time:g}");
        }
        public static void FullDecodeTest()
        {
            Console.WriteLine("PZPack version .Net console test: FullEncodeTest");
            string source = @"D:\Media\pictures.pzpk";

            using var reader = PZReader.Open(source, "4294967296");
            try
            {
                Console.WriteLine($"this file's description is '{reader.Description}'");
                Console.WriteLine($"Folder count: {reader.FolderCount}");
                Console.WriteLine($"File count: {reader.FileCount}");
                Console.WriteLine("Folder list:");
                foreach (var f in reader.GetAllFolders())
                {
                    Console.WriteLine($">  {f.FullName}");
                }

                var temp = reader.GetAllFiles()[15];
                var outputName = reader.UnpackFile(temp, @"D:\Media");
                Console.WriteLine($"unpack file to: {outputName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Debug.Write(ex.StackTrace);
            }
        }
        public static void UnpackTest()
        {
            Console.WriteLine("PZPack version .Net console test: UnpackTest");
            string source = @"D:\Media\pictures2.pzpk";
            string output = @"D:\Media\pictures-test2";

            var updater = new ConsoleUpdater();
            var progress = new ProgressReporter<(int, int)>((tp) =>
            {
                (int count, int total) = tp;
                double prec = (count / Convert.ToDouble(total)) * 100;
                updater.Update($"{prec:F1}% ({count} / {total})");
            });


            using var reader = PZReader.Open(source, "4294967296");
            updater.Begin();
            var startTime = DateTime.Now;

            reader.UnpackAll(output, progress);

            var endTime = DateTime.Now;
            updater.End();

            var time = endTime - startTime;
            Console.ResetColor();
            Console.WriteLine("PZPack test excute complete");
            Console.WriteLine($"source: {source}");
            Console.WriteLine($"target: {output}");
            Console.WriteLine($"Processed {reader.FileCount} files in time: {time:g}");
        }

        public static void TestCrypto()
        {
            string password = "1234567";
            string text = "aabbcceedd";
            byte[] textBytes = Encoding.UTF8.GetBytes(text);
            Console.WriteLine($"text bytes length = {textBytes.Length}");

            PZCrypto cp = new(password);
            byte[] ds = cp.Encrypt(textBytes);
            string b64 = Convert.ToBase64String(ds);
            Console.WriteLine($"encrypt base64 string = {b64}");

            byte[] es = cp.Decrypt(ds);
            string esStr = Encoding.UTF8.GetString(es);
            Console.WriteLine($"decrypt string = {esStr}");
        }

        public static void TestStreamCrypto()
        {
            string password = "1234567";
            string text = "aabbcceedd";
            byte[] textBytes = Encoding.UTF8.GetBytes(text);
            Console.WriteLine($"Text bytes: { btostr(textBytes) }");

            PZCrypto cp = new(password);

            using MemoryStream enreader = new(textBytes);
            using MemoryStream enwriter = new();
            cp.EncryptStream(enreader, enwriter);
            byte[] encodeBytes = enwriter.ToArray();
            Console.WriteLine($"enwriter now write access is {enwriter.CanWrite}");

            byte[] atEncodeBytes = cp.Encrypt(textBytes);
            Console.WriteLine($"Stream version: { btostr(encodeBytes) }");
            Console.WriteLine($"Byte version: { btostr(atEncodeBytes) }");

            using MemoryStream dereader = new(encodeBytes);
            using MemoryStream dewriter = new();
            cp.DecryptStream(dereader, dewriter, 0, encodeBytes.Length);
            byte[] decodeBytes = dewriter.ToArray();
            Console.WriteLine($"dereader now read access is {dereader.CanRead}");

            Console.WriteLine($"Decode bytes: { btostr(decodeBytes) }");

            static string btostr(byte[] bts) => String.Join(',', bts.Select(x => x.ToString()).ToArray());
        }

        public static void TestFileEncrypt()
        {
            string password = "1234567";
            PZCrypto cp = new(password);

            var readStream = File.OpenRead(@"D:\Code\Test\Files\t2.mp4");
            var writeStream = File.Create(@"D:\Code\Test\Files\t2.mp4.pzpk");

            var startTime = DateTime.Now;
            var len = cp.EncryptStream(readStream, writeStream);
            var endTime = DateTime.Now;
            var timeSpan = endTime - startTime;

            Console.WriteLine($"encrypt {len} in {timeSpan.TotalSeconds}.{timeSpan.Milliseconds}");
            Console.WriteLine("File encrypt complete");
        }
        public static void TestFileDecrypt()
        {
            string password = "1234567";
            PZCrypto cp = new(password);

            var readStream = File.OpenRead(@"D:\Code\Test\Files\t2.mp4.pzpk");
            var writeStream = File.Create(@"D:\Code\Test\Files\t2-unpack.mp4");

            var startTime = DateTime.Now;
            var len = cp.DecryptStream(readStream, writeStream, 0, (int)readStream.Length);
            var endTime = DateTime.Now;
            var timeSpan = endTime - startTime;

            Console.WriteLine($"decrypt {len} in {timeSpan.TotalSeconds}.{timeSpan.Milliseconds}");
            writeStream.Flush();
            Console.WriteLine("File decrypt complete");
        }

        public static void TestMultiFileEncrypt()
        {
            string password = "1234567";
            string text1 = "qqqqwwwwqqqqwwww";
            string text2 = "z8z8z8z8z8z8z8z8z8z8";
            byte[] textBytes1 = Encoding.UTF8.GetBytes(text1);
            byte[] textBytes2 = Encoding.UTF8.GetBytes(text2);

            var writeStream = File.Create(@"D:\Code\Test\Files\ttst.pzpk");

            MemoryStream m1 = new(textBytes1);
            MemoryStream m2 = new(textBytes2);
            PZCrypto cp = new(password);

            try
            {
                long len1 = cp.EncryptStream(m1, writeStream);
                Console.WriteLine($"m1 complete {len1}");
                long len2 = cp.EncryptStream(m2, writeStream);
                Console.WriteLine($"m2 complete {len2}");

                writeStream.Flush();
            }
            catch
            {
                Console.WriteLine("Error");
            }
            finally
            {
                m1.Dispose();
                m2.Dispose();
                cp.Dispose();
            }
        }
        public static void TestMultiFileDecrypt()
        {
            string password = "1234567";
            var readStream = File.OpenRead(@"D:\Code\Test\Files\ttst.pzpk");

            MemoryStream m1 = new();
            MemoryStream m2 = new();
            PZCrypto cp = new(password);

            try
            {
                long len1 = cp.DecryptStream(readStream, m1, 0, 32);
                string m1s = Encoding.UTF8.GetString(m1.ToArray());
                Console.WriteLine($"m1 complete {m1s}");

                long len2 = cp.DecryptStream(readStream, m2, 32, 32);
                string m2s = Encoding.UTF8.GetString(m2.ToArray());
                Console.WriteLine($"m2 complete {m2s}");
            }
            catch
            {
                Console.WriteLine("Error");
            }
            finally
            {
                m1.Dispose();
                m2.Dispose();
                cp.Dispose();
            }
        }
    }
}
