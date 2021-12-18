using PZPack.Core;

namespace PZPack.Cli
{
    class ProgressReporter<T> : IProgress<T>
    {
        readonly Action<T> action;
        public ProgressReporter(Action<T> progressAction) =>
            action = progressAction;

        public void Report(T value) => action(value);
    }
    internal class Operates
    {
        static public void Pack()
        {
            string? inputPath = null;
            while (string.IsNullOrWhiteSpace(inputPath))
            {
                Console.WriteLine("Please enter source directory path:");
                inputPath = Console.ReadLine();
                
                if (!string.IsNullOrWhiteSpace(inputPath) && !Directory.Exists(inputPath))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Directory \"{inputPath}\" not found!");
                    Console.ResetColor();
                    inputPath = null;
                }
            }

            string? outputPath = null;
            while (string.IsNullOrWhiteSpace(outputPath))
            {
                Console.WriteLine("Please enter output file path:");
                outputPath = Console.ReadLine();

                if (!string.IsNullOrWhiteSpace(outputPath) && File.Exists(outputPath))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"File \"{outputPath}\" is already exists!");
                    Console.ResetColor();
                    outputPath = null;
                }
            }

            string? password = null;
            while (string.IsNullOrWhiteSpace(password))
            {
                Console.WriteLine("Please enter password:");
                password = Console.ReadLine();
            }

            Console.WriteLine("Please enter description (optional):");
            string? description = Console.ReadLine();

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
                PackOption option = new(password, description ?? "");
                len = PZPacker.Pack(inputPath, outputPath, option, progress);
            }
            catch (Exception ex)
            {
                updater.End();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.Clear();
                return;
            }
            var endTime = DateTime.Now;
            updater.End();

            var time = endTime - startTime;
            Console.ResetColor();
            Console.WriteLine("PZPack excute complete");
            Console.WriteLine($"source: {inputPath}");
            Console.WriteLine($"output: {outputPath}");
            Console.WriteLine($"Processed {len} bytes in time: {time:g}");
        }

        static public void Unpack()
        {
            string? inputPath = null;
            while (string.IsNullOrWhiteSpace(inputPath))
            {
                Console.WriteLine("Please enter source pzpk file path:");
                inputPath = Console.ReadLine();

                if (!string.IsNullOrWhiteSpace(inputPath) && !File.Exists(inputPath))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"File \"{inputPath}\" not found!");
                    Console.ResetColor();
                    inputPath = null;
                }
            }

            string? outputPath = null;
            while (string.IsNullOrWhiteSpace(outputPath))
            {
                Console.WriteLine("Please enter output directory path:");
                outputPath = Console.ReadLine();
            }

            string? password = null;
            while (string.IsNullOrWhiteSpace(password))
            {
                Console.WriteLine("Please enter password:");
                password = Console.ReadLine();
            }

            var updater = new ConsoleUpdater();
            var progress = new ProgressReporter<(int, int)>((tp) =>
            {
                (int count, int total) = tp;
                double prec = (count / Convert.ToDouble(total)) * 100;
                updater.Update($"{prec:F1}% ({count} / {total})");
            });

            updater.Begin();
            long len = 0;
            int files = 0;
            var startTime = DateTime.Now;
            try
            {
                PZReader reader = PZReader.Open(inputPath, password);
                reader.UnpackAll(outputPath, progress);
                len = reader.PackSize;
                files = reader.FileCount;
            }
            catch (Exception ex)
            {
                updater.End();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.Clear();
                return;
            }
            var endTime = DateTime.Now;
            updater.End();

            var time = endTime - startTime;
            Console.ResetColor();
            Console.WriteLine("PZPack excute complete");
            Console.WriteLine($"source: {inputPath}");
            Console.WriteLine($"output: {outputPath}");
            Console.WriteLine($"Processed {files} files ({len} bytes) in time: {time:g}");
        }
    
        static public void ViewInfo()
        {
            string? inputPath = null;
            while (string.IsNullOrWhiteSpace(inputPath))
            {
                Console.WriteLine("Please enter source pzpk file path:");
                inputPath = Console.ReadLine();

                if (!string.IsNullOrWhiteSpace(inputPath) && !File.Exists(inputPath))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"File \"{inputPath}\" not found!");
                    Console.ResetColor();
                    inputPath = null;
                }
            }

            string? password = null;
            while (string.IsNullOrWhiteSpace(password))
            {
                Console.WriteLine("Please enter password:");
                password = Console.ReadLine();
            }

            try
            {
                PZReader reader = PZReader.Open(inputPath, password);
                Console.WriteLine($"Load pzpk file {inputPath}");
                Console.WriteLine($"Description: {reader.Description}");
                Console.WriteLine($"Files count: {reader.FileCount}");
                Console.WriteLine($"Folders count: {reader.FolderCount}");
                Console.WriteLine($"Size: {reader.PackSize} bytes");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.Clear();
                return;
            }
        }
    }
}
