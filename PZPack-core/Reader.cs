using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PZPack.Core
{
    public class PZReader : IDisposable
    {
        public static PZReader Open(string source, string password)
        {
            if (!File.Exists(source))
            {
                throw new FileNotFoundException($"File not found: {source}");
            }
            FileStream sourceStream = File.OpenRead(source);
            PZCrypto crypto = new(password);

            int version = CheckFileHead(sourceStream, crypto);
            return new PZReader(source, sourceStream, crypto, version);
        }
        private static int CheckFileHead(FileStream stream, PZCrypto crypto)
        {
            stream.Seek(0, SeekOrigin.Begin);

            byte[] versionBi = new byte[4];
            stream.Read(versionBi);
            int version = BitConverter.ToInt32(versionBi);
            if (!Compatibles.IsCompatibleVersion(version))
            {
                throw new Exception($"PZPack file version {version} is not compatiblity (Current version is {Common.Version})");
            }
            Debug.WriteLineIf(version != Common.Version, $"Open file is old verison {version}");

            byte[] signBi = new byte[32];
            stream.Read(signBi);
            string fileSign = Convert.ToHexString(signBi);
            string signHex = PZCrypto.HashHex(Common.Sign);
            if (fileSign != signHex)
            {
                throw new Exception($"PZPack file sign check failed");
            }

            byte[] pwHashBi = new byte[32];
            stream.Read(pwHashBi);
            string filePwHash = Convert.ToHexString(pwHashBi);
            string pwHashHex = Convert.ToHexString(crypto.GetPwCheckHash());
            if (filePwHash != pwHashHex)
            {
                throw new Exception($"PZPack file password check failed");
            }

            return version;
        }
        private static void EnsureDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
        private static void CheckDirectoryEmpty(string path)
        {
            if (Directory.Exists(path))
            {
                DirectoryInfo root = new(path);
                bool isEmpty = scan(root);
                if (!isEmpty)
                {
                    throw new Exception($"Directory {path} is not empty");
                }
            }

            static bool scan(DirectoryInfo dir)
            {
                var files = dir.GetFiles();
                if (files.Length > 0)
                {
                    return false;
                }
                DirectoryInfo[] subDirs = dir.GetDirectories();
                foreach (DirectoryInfo subDir in subDirs)
                {
                    if (scan(subDir) == false)
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        public string Source { get; private set; }
        public string Description
        {
            get
            {
                if (desc == null)
                {
                    desc = GetDescription();
                }
                return desc;
            }
        }
        public int FileCount { get => Index.FileCount; }
        public int FolderCount { get => Index.FolderCount; }
        public long PackSize { get => stream.Length; }

        private readonly int fileVersion;
        private readonly PZCrypto crypto;
        private readonly FileStream stream;
        private IndexDecoder? idx;
        private string? desc;
        private IndexDecoder Index
        {
            get
            {
                if (idx == null)
                {
                    idx = GetIndex();
                }
                return idx;
            }
        }

        private PZReader(string source, FileStream stream, PZCrypto crypto, int version)
        {
            Source = source;
            this.crypto = crypto;
            this.stream = stream;
            fileVersion = version;
        }
        private IndexDecoder GetIndex()
        {
            byte[] infoLengthBi = new byte[4];
            stream.Seek(68, SeekOrigin.Begin);
            stream.Read(infoLengthBi);
            int infoLength = BitConverter.ToInt32(infoLengthBi);

            byte[] idxOffsetBi = new byte[8];
            stream.Seek(72 + infoLength, SeekOrigin.Begin);
            stream.Read(idxOffsetBi);

            long idxOffset = BitConverter.ToInt64(idxOffsetBi);
            int size = (int)(stream.Length - idxOffset);
            using MemoryStream ms = new();
            Task t = crypto.DecryptStream(stream, ms, idxOffset, size);
            t.Wait();
            byte[] idxBi = ms.ToArray();

            return Compatible.Decoder.DecodeIndexData(idxBi, fileVersion);
        }
        private string GetDescription()
        {
            byte[] infoLengthBi = new byte[4];
            stream.Seek(68, SeekOrigin.Begin);
            stream.Read(infoLengthBi);
            int infoLength = BitConverter.ToInt32(infoLengthBi);

            byte[] infoEncBuffer = new byte[infoLength];
            stream.Seek(72, SeekOrigin.Begin);
            stream.Read(infoEncBuffer);

            byte[] infoBuffer = crypto.Decrypt(infoEncBuffer);
            string result = "";
            using (MemoryStream ms = new(infoBuffer))
            using (BinaryReader reader = new(ms))
            {
                ms.Seek(0, SeekOrigin.Begin);
                int descLength = reader.ReadInt32();
                if (descLength > 0)
                {
                    ms.Seek(8, SeekOrigin.Begin);
                    byte[] descBi = reader.ReadBytes(descLength);
                    result = Encoding.UTF8.GetString(descBi);
                }
            }

            return result;
        }

        public IFolderNode GetFolderNode()
        {
            return Index.GetTree();
        }
        public IFolderNode[] GetAllFolders()
        {
            return Index.GetAllFolders();
        }
        public PZFile[] GetFiles(int folderId)
        {
            return Index.GetFiles(folderId);
        }
        public PZFile[] GetAllFiles()
        {
            return Index.GetAllFiles();
        }
        public Task<string> UnpackFile(PZFile file, string output, string rename, IProgress<(long, long)>? progress = default, CancellationToken? cancelToken = null)
        {
            string name = rename ?? file.Name;
            string fullName = Path.Join(output, name);
            return UnpackFile(file, fullName, progress, cancelToken);
        }
        public async Task<string> UnpackFile(PZFile file, string fullOutput, IProgress<(long, long)>? progress = default, CancellationToken? cancelToken = null)
        {
            if (File.Exists(fullOutput))
            {
                throw new Exception($"Unpack target file {fullOutput} is already exists");
            }
            string? dir = Path.GetDirectoryName(fullOutput);
            if (dir == null)
            {
                throw new Exception($"Unpack target file path {fullOutput} invalid");
            }

            EnsureDirectory(dir);
            using (FileStream fs = File.Create(fullOutput))
            {
                await crypto.DecryptStream(stream, fs, file.Offset, file.Size, progress, cancelToken);
            }

            return fullOutput;
        }
        public async Task<byte[]> ReadFile(PZFile file)
        {
            using MemoryStream ms = new();
            await crypto.DecryptStream(stream, ms, file.Offset, file.Size);
            return ms.ToArray();
        }
        public async Task<long> UnpackAll(string output, IProgress<(int, int, long, long)>? progress = default, CancellationToken? cancelToken = null)
        {
            CheckDirectoryEmpty(output);
            EnsureDirectory(output);

            int count = 0;
            int total = Index.FileCount;
            long size = 0;
            ProgressReporter<(long, long)> innerProgress = new((n) =>
            {
                (long fReaded, long fSize) = n;
                progress?.Report((count, total, fReaded, fSize));
            });

            foreach (PZFile file in Index.GetAllFiles())
            {
                if (cancelToken?.IsCancellationRequested == true)
                {
                    cancelToken?.ThrowIfCancellationRequested();
                }

                IFolderNode folder = Index.GetFolder(file.FolderId);
                string folderPath = Path.Join(output, folder.FullName);
                await UnpackFile(file, folderPath, file.Name, innerProgress, cancelToken);
                count++;
                size += file.Size;
                progress?.Report((count, total, file.Size, file.Size));
            }

            return size;
        }

        public void Dispose()
        {
            stream.Close();
            stream.Dispose();
            crypto.Dispose();

            GC.SuppressFinalize(this);
        }
    }



}
