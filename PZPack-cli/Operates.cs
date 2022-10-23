using PZPack.Core;
using PZPack.Core.Index;
using PZPack.Core.Exceptions;

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
        private static IndexDesigner ScanDirectory(string path)
        {
            DirectoryInfo root = new(path);
            if (!root.Exists)
            {
                throw new DirectoryNotFoundException(path);
            }

            IndexDesigner designer = new();

            scan(root, designer.Root);

            List<PZDesigningFile> files = designer.GetAllFiles();
            if (files.Count == 0)
            {
                throw new SourceDirectoryIsEmptyException(path);
            }
            return designer;

            void scan(DirectoryInfo dir, PZDesigningFolder parent)
            {
                var current = designer.AddFolder(dir.Name, parent);
                var files = dir.GetFiles();
                foreach(var f in files)
                {
                    designer.AddFile(f.FullName, f.Name, current);
                }

                DirectoryInfo[] subDirs = dir.GetDirectories();
                foreach (DirectoryInfo subDir in subDirs)
                {
                    scan(subDir, current);
                }
            }
        }

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

            var updater = new ConsoleUpdater();
            var progress = new ProgressReporter<PackProgressArg>((v) =>
            {
                double prec = v.CurrentBytes == 0 ? 0 : v.CurrentProcessedBytes / v.CurrentBytes * 100;
                updater.Update($"{prec:f1}% ({v.ProcessedFileCount} / {v.TotalFileCount})");
            });

            updater.Begin();
            long len = 0;
            var startTime = DateTime.Now;
            try
            {
                var designer = ScanDirectory(inputPath);
                var task = PZPacker.Pack(outputPath, designer, password, 65536, progress);
                task.Wait();
                len = task.Result;
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
            var progress = new ProgressReporter<ExtractProgressArg>((v) =>
            {
                double prec = v.CurrentBytes == 0 ? 0 : v.CurrentProcessedBytes / v.CurrentBytes * 100;
                updater.Update($"{prec:f1}% ({v.ProcessedFileCount} / {v.TotalFileCount})");
            });

            
            long len = 0;
            int files = 0;
            var startTime = DateTime.Now;
            try
            {
                PZReader reader = PZReader.Open(inputPath, password);
                Console.WriteLine($"File version: {reader.Version}");

                updater.Begin();
                Task<long> task = reader.ExtractBatchAsync(reader.Index.Root, outputPath, progress);
                task.Wait();
                len = task.Result;
                files = reader.Index.GetFilesRecursion(reader.Index.Root).Length;
            }
            catch (Exception ex)
            {
                updater.End();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error on unpack:");
                Console.WriteLine(ex.Message);
                Console.ResetColor();
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
                Console.WriteLine($"File version = {reader.Version}");
                Console.WriteLine($"BlockSize: {reader.BlockSize}");
                Console.WriteLine($"Size: {reader.Info.FileSize} bytes");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.ResetColor();
                return;
            }
        }
    }
}
