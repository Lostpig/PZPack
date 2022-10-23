using PZPack.Core.Crypto;
using PZPack.Core.Exceptions;
using PZPack.Core.Index;
using System.Diagnostics;
using System.Text;

namespace PZPack.Core;

public record PZFileInfo
{
    public readonly int Version;
    public readonly string Sign;
    public readonly string PasswordHash;
    public readonly DateTime CreateTime;
    public readonly long FileSize;
    public readonly int BlockSize;
    public readonly int IndexSize;
    public readonly long IndexOffset;

    private PZFileInfo(int version, string sign, string passwordHash, long createTime, long fileSize, int blockSize, int indexSize, long indexOffset)
    {
        Version = version;
        Sign = sign;
        PasswordHash = passwordHash;
        CreateTime = new DateTime(createTime);
        FileSize = fileSize;
        BlockSize = blockSize;
        IndexSize = indexSize;
        IndexOffset = indexOffset;
    }
    public static PZFileInfo Load(FileStream stream)
    {
        using BinaryReader br = new(stream);
        stream.Seek(0, SeekOrigin.Begin);
        int version = br.ReadInt32();

        return version switch
        {
            1 or 2 or 4 => LoadInfoV2(stream),
            11 => LoadInfoV11(stream),
            _ => throw new Exceptions.FileVersionNotCompatiblityException(version)
        };
    }

    private static PZFileInfo LoadInfoV2(FileStream stream)
    {
        using BinaryReader br = new(stream);
        stream.Seek(0, SeekOrigin.Begin);
        int version = br.ReadInt32();
        byte[] sign = br.ReadBytes(32);
        byte[] passwordHash = br.ReadBytes(32);

        int infoLength = br.ReadInt32();
        br.ReadBytes(infoLength);
        long indexOffset = br.ReadInt64();

        return new PZFileInfo(
            version,
            Convert.ToHexString(sign),
            Convert.ToHexString(passwordHash),
            DateTime.MinValue.Ticks,
            stream.Length,
            0,
            (int)(stream.Length - indexOffset),
            indexOffset
        );
    }
    private static PZFileInfo LoadInfoV11(FileStream stream)
    {
        using BinaryReader br = new(stream);
        stream.Seek(0, SeekOrigin.Begin);
        int version = br.ReadInt32();
        byte[] sign = br.ReadBytes(32);
        byte[] passwordHash = br.ReadBytes(32);
        long createTime = br.ReadInt64();
        long fullSize = br.ReadInt64();
        int blockSize = br.ReadInt32();
        int indexSize = br.ReadInt32();

        return new PZFileInfo(
            version,
            Convert.ToHexString(sign),
            Convert.ToHexString(passwordHash),
            createTime,
            fullSize,
            blockSize,
            indexSize,
            92L
        );
    }
}
public record ExtractProgressArg
{
    /**
     * 总文件个数
     */
    public int TotalFileCount { get; internal set; }
    /**
     * 已处理文件个数
     */
    public int ProcessedFileCount { get; internal set; }

    /**
     * 总字节数
     */
    public long TotalBytes { get; internal set; }
    /**
     * 已处理字节数
     */
    public long TotalProcessedBytes { get; internal set; }

    /**
     * 当前文件的字节数
     */
    public long CurrentBytes { get; internal set; }
    /**
     * 当前文件已处理的字节数
     */
    public long CurrentProcessedBytes { get; internal set; }
}

public class PZReader : IDisposable
{
    public static PZReader Open(string source, string password)
    {
        if (!File.Exists(source))
        {
            throw new FileNotFoundException($"File {source} not found", source);
        }
        FileStream sourceStream = File.OpenRead(source);
        PZFileInfo info = PZFileInfo.Load(sourceStream);

        PZTypes type = GetFileType(info);
        IPZCrypto crypto = PZCrypto.CreateCrypto(password, info.Version, 0);
        if (info.PasswordHash != Convert.ToHexString(crypto.Hash))
        {
            throw new PZPasswordIncorrectException();
        }

        return new PZReader(source, sourceStream, crypto, type, info);
    }
    private static PZTypes GetFileType(PZFileInfo info)
    {
        if (info.Sign == PZCommon.HashSignHex(PZTypes.PZPACK))
        {
            return PZTypes.PZPACK;
        }
        else if (info.Sign == PZCommon.HashSignHex(PZTypes.PZVIDEO))
        {
            return PZTypes.PZVIDEO;
        }
        else
        {
            throw new PZSignCheckedException();
        }
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

    private readonly IPZCrypto Crypto;
    private readonly FileStream Stream;

    public IndexReader Index { get; private set; }
    public string Source { get; private set; }
    public PZFileInfo Info { get; private set; }
    public int BlockSize { get => Info.BlockSize; }
    public int Version { get => Info.Version; }
    public PZTypes Type { get; private set; }

    private PZReader(string source, FileStream stream, IPZCrypto crypto, PZTypes type, PZFileInfo info)
    {
        Source = source;
        Crypto = crypto;
        Stream = stream;
        Type = type;
        Info = info;

        Index = LoadIndex();
    }
    private IndexReader LoadIndex()
    {
        byte[] indexBytes = new byte[Info.IndexSize];
        Stream.Seek(Info.IndexOffset, SeekOrigin.Begin);
        Stream.Read(indexBytes, 0, indexBytes.Length);

        return IndexDecoder.DecodeIndex(indexBytes, Version);
    }

    public async Task<long> ExtractFileAsync(PZFile file, string destination, IProgress<(long, long)>? progress = default, CancellationToken? cancelToken = null)
    {
        if (File.Exists(destination))
        {
            throw new OutputFileAlreadyExistsException(destination);
        }
        string? dir = Path.GetDirectoryName(destination);
        if (dir == null)
        {
            throw new DirectoryNotFoundException(destination);
        }

        EnsureDirectory(dir);
        long length;
        using FileStream fs = File.Create(destination);
        length = await Crypto.DecryptStreamAsync(Stream, fs, file.Offset, file.Size, progress, cancelToken);

        return length;
    }
    public async Task<long> ExtractBatchAsync(PZFolder folder, string destination, IProgress<ExtractProgressArg>? progress = default, CancellationToken? cancelToken = null)
    {
        if (File.Exists(destination))
        {
            throw new Exceptions.PathIsNotDirectoryException(destination);
        }
        EnsureDirectory(destination);

        var files = Index.GetFilesRecursion(folder);
        long totalBytes = files.Sum(f => f.Size);

        ExtractProgressArg progressArg = new()
        {
            TotalFileCount = files.Length,
            ProcessedFileCount = 0,
            TotalBytes = totalBytes,
            TotalProcessedBytes = 0,
            CurrentBytes = 0,
            CurrentProcessedBytes = 0,
        };
        ProgressReporter<(long, long)> innerProgress = new((args) =>
        {
            (long readed, long total) = args;
            progressArg.CurrentBytes = total;
            progressArg.CurrentProcessedBytes = readed;
            progress?.Report(progressArg);
        });

        foreach(var file in files)
        {
            string resolveFilePath = Index.GetResolveName(file, folder);
            string dest = Path.Combine(destination, resolveFilePath);
            progressArg.TotalProcessedBytes += await ExtractFileAsync(file, dest, innerProgress, cancelToken);
            progressArg.ProcessedFileCount++;

            progress?.Report(progressArg);
        }

        return progressArg.TotalProcessedBytes;
    }
    public async Task<byte[]> ReadFileAsync(PZFile file)
    {
        using MemoryStream ms = new();
        await Crypto.DecryptStreamAsync(Stream, ms, file.Offset, file.Size);
        return ms.ToArray();
    }
    public byte[] ReadFile(PZFile file)
    {
        using MemoryStream ms = new();
        Crypto.DecryptStream(Stream, ms, file.Offset, file.Size);
        return ms.ToArray();
    }
    public byte[] ReadOrigin(long offset, long size)
    {
        byte[] bytes = new byte[size];
        Stream.Seek(offset, SeekOrigin.Begin);
        Stream.Read(bytes, 0, bytes.Length);
        return bytes;
    }

    public void Dispose()
    {
        Stream.Close();
        Stream.Dispose();
        Crypto.Dispose();

        GC.SuppressFinalize(this);
    }
}
