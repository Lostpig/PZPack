using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PZPack.Test
{
    internal class Tests
    {
        public static async void EncodeAndDocodeTest ()
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
            await Core.PZPacker.Pack(
                source,
                output,
                new Core.PZPackInfo(password, description),
                new Core.ProgressReporter<(int, int, long, long)>((v) => {
                    (int count, int total, long processed, long size) = v;
                    double prec = processed / size;
                    updater.Update($"Progress: ${prec:f1}% (${count} / ${total})");
                })
            );
            Console.WriteLine("Encode test complete");

            Console.WriteLine("Decode test start");
            var reader = Core.PZReader.Open(output, password);
            Debug.Assert(reader.Description == description);
        }
    }
}
