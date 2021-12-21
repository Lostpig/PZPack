using System.Diagnostics;
using System.Text;
using PZPack.Core.Compatible;
using PZPack.Core.Exceptions;

namespace PZPack.Core
{
    public class PZPacker
    {
        private static FileInfo[] ScanDirectory(string path)
        {
            DirectoryInfo root = new(path);
            if (!root.Exists)
            {
                throw new DirectoryNotFoundException(path);
            }

            List<FileInfo> files = scan(root);
            if (files.Count == 0)
            {
                throw new SourceDirectoryIsEmptyException(path);
            }
            return files.ToArray();

            static List<FileInfo> scan(DirectoryInfo dir)
            {
                var files = dir.GetFiles().ToList();
                DirectoryInfo[] subDirs = dir.GetDirectories();
                foreach (DirectoryInfo subDir in subDirs)
                {
                    files.AddRange(scan(subDir));
                }
                return files;
            }
        }
        private void WritePackHead()
        {
            byte[] versionBi = BitConverter.GetBytes(Common.Version);
            byte[] signBi = PZCrypto.Hash(Common.Sign);
            byte[] pwHashBi = Crypto.GetPwCheckHash();

            Writer.Write(versionBi, 0, versionBi.Length);
            Writer.Write(signBi, 0, signBi.Length);
            Writer.Write(pwHashBi, 0, pwHashBi.Length);

            Debug.Assert(Writer.Position == 68);
        }
        private void WritePackInfo()
        {
            using MemoryStream ms = new();
            int descLength = 0;
            int thumbnailLength = 0;
            byte[]? descBi = null;
            if (!string.IsNullOrWhiteSpace(Description))
            {
                descBi = Encoding.UTF8.GetBytes(Description);
                descLength = descBi.Length;
            }
            ms.Write(BitConverter.GetBytes(descLength));
            ms.Write(BitConverter.GetBytes(thumbnailLength));
            if (descBi != null)
            {
                ms.Write(descBi);
            }

            byte[] infoBi = Crypto.Encrypt(ms.ToArray());
            Writer.Write(BitConverter.GetBytes(infoBi.Length));
            Writer.Write(infoBi);
            Debug.Assert(Writer.Position == 72 + infoBi.Length);
        }

        private readonly string Source;
        private readonly string Output;
        private readonly string Description;
        private readonly PZCrypto Crypto;

        private bool IsComplete = false;
        private bool HasError = false;
        private readonly FileStream Writer;
        private readonly FileInfo[] Files;
        private readonly IndexEncoder Index;
        public bool Useabel { get => !IsComplete && !HasError; }

        public PZPacker(string source, string output, PZPackInfo info)
        {
            Source = source;
            Output = output;
            Description = info.Description;
            Crypto = new(info.Password);

            if (File.Exists(Output))
            {
                throw new OutputFileAlreadyExistsException(Output);
            }
            Writer = File.Create(Output);
            Files = ScanDirectory(Source);
            Index = new IndexEncoder();
        }
        public event ProgressChangedHandler<(int, int, long, long)>? ProgressChanged;
        public async Task<long> Start(CancellationToken? cancelToken = null)
        {
            try
            {
                Writer.Seek(0, SeekOrigin.Begin);
                WritePackHead();
                WritePackInfo();
                long idxOffsetOffset = Writer.Position;
                Writer.Seek(8, SeekOrigin.Current);
                int count = 0;
                int length = Files.Length;

                ProgressReporter<(long, long)> innerProgress = new((n) =>
                {
                    (long fReaded, long fSize) = n;
                    ProgressChanged?.Invoke((count, length, fReaded, fSize));
                });

                foreach (FileInfo file in Files)
                {
                    if (cancelToken?.IsCancellationRequested == true)
                    {
                        cancelToken?.ThrowIfCancellationRequested();
                    }
                    string relativePath = Path.GetRelativePath(Source, file.FullName);
                    long size = 0;
                    using (FileStream fs = file.OpenRead())
                    {
                        long offset = Writer.Position;
                        size = await Crypto.EncryptStream(fs, Writer, innerProgress, cancelToken);
                        Index.AppendFile(relativePath, offset, size);
                        Debug.Assert(size == Writer.Position - offset);
                    }

                    count++;
                    ProgressChanged?.Invoke((count, length, size, size));
                }

                long indexOffset = Writer.Position;
                byte[] indexBi = Crypto.Encrypt(PZCodec.EncodeIndexData(Index));
                Writer.Write(indexBi);

                Writer.Seek(idxOffsetOffset, SeekOrigin.Begin);
                Writer.Write(BitConverter.GetBytes(indexOffset));
                Writer.Flush();

                return Writer.Length;
            } catch
            {
                HasError = true;
                throw;
            }
            finally
            {

                Writer.Close();
                Writer.Dispose();
                IsComplete = true;

                if (HasError && File.Exists(Output))
                {
                    File.Delete(Output);
                }
            }
        }

        public static async Task<long> Pack(string source, string output, PZPackInfo info, IProgress<(int, int, long, long)>? progress = default, CancellationToken? cancelToken = null)
        {
            var instance = new PZPacker(source, output, info);
            instance.ProgressChanged += progressChanged;
            long result = await instance.Start(cancelToken);
            instance.ProgressChanged -= progressChanged;

            return result;

            void progressChanged((int, int, long, long) value)
            {
                progress?.Report(value);
            }
        }
    }
}
