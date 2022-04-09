using PZPack.Core.Exceptions;
using System.Diagnostics;
using System.Text;

namespace PZPack.Core
{
    public class PZReader : IDisposable
    {
        public static PZReader Open(string source, string password)
        {
            if (!File.Exists(source))
            {
                throw new FileNotFoundException($"File {source} not found", source);
            }
            FileStream sourceStream = File.OpenRead(source);

            int version = GetFileVersion(sourceStream);
            IPZCrypto crypto = PZCryptoCreater.CreateCrypto(password, version);

            PZTypes type = CheckFileHead(sourceStream, crypto);
            return new PZReader(source, sourceStream, crypto, type, version);
        }
        private static int GetFileVersion(FileStream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);
            byte[] versionBi = new byte[4];
            stream.Read(versionBi);
            int version = BitConverter.ToInt32(versionBi);
            if (!Compatibles.IsCompatibleVersion(version))
            {
                throw new FileVersionNotCompatiblityException(version);
            }
            Debug.WriteLineIf(version != Common.Version, $"Open file is old verison {version}");

            return version;
        }
        private static PZTypes CheckFileHead(FileStream stream, IPZCrypto crypto)
        {
            stream.Seek(4, SeekOrigin.Begin);
            byte[] signBi = new byte[32];
            stream.Read(signBi);
            string fileSign = Convert.ToHexString(signBi);

            PZTypes resultType;
            string pzpackSign = Common.HashSignHex(PZTypes.PZPACK);
            string pzvideoSign = Common.HashSignHex(PZTypes.PZVIDEO);
            if (fileSign == pzpackSign)
            {
                resultType = PZTypes.PZPACK;
            }
            else if (fileSign == pzvideoSign)
            {
                resultType = PZTypes.PZVIDEO;
            }
            else
            {
                throw new PZSignCheckedException();
            }

            byte[] pwHashBi = new byte[32];
            stream.Read(pwHashBi);
            string filePwHash = Convert.ToHexString(pwHashBi);
            string pwHashHex = Convert.ToHexString(crypto.GetPwCheckHash());
            if (filePwHash != pwHashHex)
            {
                throw new PZPasswordIncorrectException();
            }

            return resultType;
        }
        private static void EnsureDirectory(string path)
        {
            if (File.Exists(path))
            {
                throw new PathIsNotDirectoryException(path);
            }

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
                    throw new OutputDirectoryIsNotEmptyException(path);
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
        public int FileVersion { get => fileVersion; }
        public PZTypes PZType { get => pztype; }

        private readonly int fileVersion;
        private readonly IPZCrypto crypto;
        private readonly FileStream stream;
        private readonly PZTypes pztype;
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

        private PZReader(string source, FileStream stream, IPZCrypto crypto, PZTypes type, int version)
        {
            Source = source;
            this.crypto = crypto;
            this.stream = stream;
            pztype = type;
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
            crypto.DecryptStream(stream, ms, idxOffset, size);
            byte[] idxBi = ms.ToArray();

            return Compatible.PZCodec.DecodeIndexData(idxBi, fileVersion);
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
        public Task<long> UnpackFile(PZFile file, string output, string rename, IProgress<(long, long)>? progress = default, CancellationToken? cancelToken = null)
        {
            string name = rename ?? file.Name;
            string fullName = Path.Join(output, name);
            return UnpackFile(file, fullName, progress, cancelToken);
        }
        public async Task<long> UnpackFile(PZFile file, string fullOutput, IProgress<(long, long)>? progress = default, CancellationToken? cancelToken = null)
        {
            if (File.Exists(fullOutput))
            {
                throw new OutputFileAlreadyExistsException(fullOutput);
            }
            string? dir = Path.GetDirectoryName(fullOutput);
            if (dir == null)
            {
                throw new DirectoryNotFoundException(fullOutput);
            }

            EnsureDirectory(dir);
            long length = 0;
            using (FileStream fs = File.Create(fullOutput))
            {
                length = await crypto.DecryptStreamAsync(stream, fs, file.Offset, file.Size, progress, cancelToken);
            }

            return length;
        }
        public async Task<byte[]> ReadFile(PZFile file)
        {
            using MemoryStream ms = new();
            await crypto.DecryptStreamAsync(stream, ms, file.Offset, file.Size);
            return ms.ToArray();
        }
        public byte[] ReadFileSync(PZFile file)
        {
            using MemoryStream ms = new();
            crypto.DecryptStream(stream, ms, file.Offset, file.Size);
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
        public byte[] ReadOrigin(long offset, long size)
        {
            byte[] bytes = new byte[size];
            stream.Seek(offset, SeekOrigin.Begin);
            stream.Read(bytes, 0, bytes.Length);
            return bytes;
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
