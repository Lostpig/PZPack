using System.Diagnostics;
using System.Text;

namespace PZPack.Core
{
    public class PZPacker
    {
        private static List<FileInfo> ScanDirectory(string path)
        {
            DirectoryInfo root = new(path);
            if (!root.Exists)
            {
                throw new DirectoryNotFoundException($"Directory not found: {path}");
            }

            return scan(root);

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
        private static void WritePackHead(FileStream stream, PZCrypto crypto)
        {
            byte[] versionBi = BitConverter.GetBytes(Common.Version);
            byte[] signBi = PZCrypto.Hash(Common.Sign);
            byte[] pwHashBi = crypto.GetPwCheckHash();

            stream.Write(versionBi, 0, versionBi.Length);
            stream.Write(signBi, 0, signBi.Length);
            stream.Write(pwHashBi, 0, pwHashBi.Length);
        }
        private static byte[] EncodePackInfo(string description, PZCrypto crypto)
        {
            using MemoryStream ms = new();
            int descLength = 0;
            int thumbnailLength = 0;
            byte[]? descBi = null;
            if (!string.IsNullOrWhiteSpace(description))
            {
                descBi = Encoding.UTF8.GetBytes(description);
                descLength = descBi.Length;
            }
            ms.Write(BitConverter.GetBytes(descLength));
            ms.Write(BitConverter.GetBytes(thumbnailLength));
            if (descBi != null)
            {
                ms.Write(descBi);
            }

            return crypto.Encrypt(ms.ToArray());
        }
        public static async Task<long> Pack(string source, string output, PackOption option, IProgress<(int, int, long, long)>? progress = default, CancellationToken? cancelToken = null)
        {
            if (File.Exists(output))
            {
                throw new Exception($"Output file {output} is already exists");
            }
            List<FileInfo> files = ScanDirectory(source);
            if (files.Count == 0)
            {
                throw new Exception("Pack directory is have no files");
            }

            PZCrypto crypto = new(option.Password);
            IndexEncoder idxEncoder = new();

            FileStream stream = File.Create(output);
            bool packError = false;
            long totalLength = 0;

            try
            {
                stream.Seek(0, SeekOrigin.Begin);
                WritePackHead(stream, crypto);
                Debug.Assert(stream.Position == 68);

                byte[] infoBi = EncodePackInfo(option.Description, crypto);
                stream.Write(BitConverter.GetBytes(infoBi.Length));
                stream.Write(infoBi);
                Debug.Assert(stream.Position == 72 + infoBi.Length);

                stream.Seek(8, SeekOrigin.Current);
                int count = 0;

                ProgressReporter<(long, long)> innerProgress = new((n) =>
                {
                    (long fReaded, long fSize) = n;
                    progress?.Report((count, files.Count, fReaded, fSize));
                });

                foreach (FileInfo file in files)
                {
                    if (cancelToken?.IsCancellationRequested == true)
                    {
                        cancelToken?.ThrowIfCancellationRequested();
                    }

                    string relativePath = Path.GetRelativePath(source, file.FullName);
                    long size = 0;
                    using (FileStream fs = file.OpenRead())
                    {
                        long offset = stream.Position;
                        size = await crypto.EncryptStream(fs, stream, innerProgress, cancelToken);
                        idxEncoder.AppendFile(relativePath, offset, size);
                    }

                    count++;
                    progress?.Report((count, files.Count, size, size));
                }

                long indexOffset = stream.Position;
                byte[] indexBytes = idxEncoder.Encode();
                using (MemoryStream indexStream = new(indexBytes))
                {
                    await crypto.EncryptStream(indexStream, stream);
                }

                stream.Seek(72 + infoBi.Length, SeekOrigin.Begin);
                stream.Write(BitConverter.GetBytes(indexOffset));

                stream.Flush();
                totalLength = stream.Length;
            }
            catch
            {
                packError = true;
                throw;
            }
            finally
            {
                stream.Close();
                stream.Dispose();

                if (packError && File.Exists(output))
                {
                    File.Delete(output);
                }
            }

            return totalLength;
        }
    }
}
