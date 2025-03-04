using System.Diagnostics;
using System.Text;
using PZPack.Core.Crypto;
using PZPack.Core.Index;
using PZPack.Core.Exceptions;
using PZPack.Core.Core;

namespace PZPack.Core;

public record PackProgressArg
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

public class PZPacker
{
    private readonly string Destination;
    private readonly IPZCrypto Crypto;
    private readonly IndexEncoder IndexEncoder;
    private readonly int _blockSize;
    private readonly ImageResizer? ImgResizer;

    public PZPacker(string destination, IndexDesigner designer, string password, int blockSize, ImageResizer? imageResizer)
    {
        Destination = destination;
        Crypto = PZCrypto.CreateCrypto(password, PZCommon.Version, blockSize);
        IndexEncoder = new(designer);
        _blockSize = blockSize;
        ImgResizer = imageResizer;
    }
    private void CheckDestFileExists()
    {
        if (File.Exists(Destination))
        {
            throw new OutputFileAlreadyExistsException(Destination);
        }
    }
    private void WritePackHead(Stream writer, long fullSize, long indexOffset)
    {
        using BinaryWriter bw = new(writer, Encoding.Default, true);
        writer.Seek(0, SeekOrigin.Begin);

        bw.Write(PZCommon.Version);
        bw.Write(PZCommon.HashSign(PZTypes.PZPACK));
        bw.Write(Crypto.Hash);
        bw.Write(DateTime.Now.Ticks);
        bw.Write(fullSize);
        bw.Write(_blockSize);
        bw.Write(indexOffset);

        Debug.Assert(writer.Position == 96);
    }
    private void WriteIndex(Stream writer)
    {
        byte[] indexBytes = IndexEncoder.EncodeIndex();
        byte[] encryptBytes = Crypto.Encrypt(indexBytes);
        writer.Write(encryptBytes);
    }

    private static long ComputeTotalSize(PZDesigningFile[] files)
    {
        long totalSize = 0;
        foreach(var file in files)
        {
            totalSize += file.Size;
        }
        return totalSize;
    }

    public event ProgressChangedHandler<PackProgressArg>? ProgressChanged;
    public async Task<long> Start(CancellationToken cancelToken)
    {
        CheckDestFileExists();
        long fullSize = 0;

        try
        {
            using var writer = File.OpenWrite(Destination);

            var files = IndexEncoder.GetFiles();
            long totalSize = ComputeTotalSize(files);

            PackProgressArg progressArg = new()
            {
                TotalFileCount = files.Length,
                ProcessedFileCount = 0,
                TotalBytes = totalSize,
                TotalProcessedBytes = 0,
                CurrentBytes = 0,
                CurrentProcessedBytes = 0
            };
            ProgressReporter<(long, long)> innerProgress = new((n) =>
            {
                (long fReaded, long fSize) = n;
                progressArg.CurrentProcessedBytes = fReaded;
                progressArg.CurrentBytes = fSize;
                ProgressChanged?.Invoke(progressArg);
            });

            writer.Seek(96, SeekOrigin.Begin);
            long tempSize = 0;
            long offset = 0;
            foreach (var file in files)
            {
                if (cancelToken.IsCancellationRequested == true) break;

                using var fs = File.OpenRead(file.Source);

                offset = writer.Position;
                if (ImgResizer != null && ImageResizer.IsImageFile(file))
                {
                    using var imgStream = ImgResizer.ResizeImage(fs);
                    tempSize = await Crypto.EncryptStreamAsync(imgStream, writer, innerProgress, cancelToken);

                    IndexEncoder.ReEncodeName(file, Path.ChangeExtension(file.Name, ImgResizer.Extension));
                    IndexEncoder.WriteEncodingInfo(file, offset, tempSize);
                }
                else
                {
                    tempSize = await Crypto.EncryptStreamAsync(fs, writer, innerProgress, cancelToken);
                    IndexEncoder.WriteEncodingInfo(file, offset, tempSize);
                }

                Debug.Assert(tempSize == writer.Position - offset);

                progressArg.ProcessedFileCount++;
                progressArg.TotalProcessedBytes += file.Size;
                ProgressChanged?.Invoke(progressArg);
            }

            if (!cancelToken.IsCancellationRequested)
            {
                long indexOffset = writer.Position;
                WriteIndex(writer);
                fullSize = writer.Length;

                WritePackHead(writer, fullSize, indexOffset);

                writer.Flush();
            }
        } 
        catch
        {
            throw;
        }

        if (cancelToken.IsCancellationRequested)
        {
            if (File.Exists(Destination))
            {
                File.Delete(Destination);
            }
            cancelToken.ThrowIfCancellationRequested();
        }

        return fullSize;
    }
    public Task<long> Start()
    {
        return Start(CancellationToken.None);
    }

    public static async Task<long> Pack(
        string destination, 
        IndexDesigner designer, 
        string password, 
        int blockSize,
        ImageResizer? imageResizer = null,
        IProgress<PackProgressArg>? progress = default, 
        CancellationToken? cancelToken = null)
    {
        var instance = new PZPacker(destination, designer, password, blockSize, imageResizer);
        instance.ProgressChanged += progressChanged;
        long result = await instance.Start(cancelToken ?? CancellationToken.None);
        instance.ProgressChanged -= progressChanged;

        return result;

        void progressChanged(PackProgressArg arg)
        {
            progress?.Report(arg);
        }
    }
}
