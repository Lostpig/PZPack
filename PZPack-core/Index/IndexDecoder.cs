using PZPack.Core.Utility;
using System.Text;

namespace PZPack.Core.Index;

internal class IndexDecoder
{
    internal static IndexReader DecodeIndex(ReadOnlySpan<byte> buffer, int version)
    {
        return version switch
        {
            1 => DecodeIndexV1(buffer),
            2 or 4 => DecodeIndexV2(buffer),
            11 => DecodeIndexV11(buffer),
            _ => throw new Exceptions.FileVersionNotCompatiblityException(version)
        };
    }

    private static IndexReader DecodeIndexV1(ReadOnlySpan<byte> buffer)
    {
        int foldersLen = BitConverter.ToInt32(buffer[..4]);
        int filesLen = BitConverter.ToInt32(buffer[4..8]);

        ReadOnlySpan<byte> foldersBuffer = buffer.Slice(8, foldersLen);
        ReadOnlySpan<byte> filesBuffer = buffer.Slice(8 + foldersLen, filesLen);

        var folders = DecodeFolders(foldersBuffer);
        var files = DecodeFilesV1(filesBuffer);

        return new IndexReader(folders, files);
    }
    private static IndexReader DecodeIndexV2(ReadOnlySpan<byte> buffer)
    {
        int foldersLen = BitConverter.ToInt32(buffer[..4]);
        int filesLen = BitConverter.ToInt32(buffer[4..8]);

        ReadOnlySpan<byte> foldersBuffer = buffer.Slice(8, foldersLen);
        ReadOnlySpan<byte> filesBuffer = buffer.Slice(8 + foldersLen, filesLen);

        var folders = DecodeFolders(foldersBuffer);
        var files = DecodeFilesV2(filesBuffer);

        return new IndexReader(folders, files);
    }
    private static IndexReader DecodeIndexV11(ReadOnlySpan<byte> buffer)
    {
        int foldersLen = BitConverter.ToInt32(buffer[..4]);
        int filesLen = BitConverter.ToInt32(buffer[4..8]);

        ReadOnlySpan<byte> foldersBuffer = buffer.Slice(8, foldersLen);
        ReadOnlySpan<byte> filesBuffer = buffer.Slice(8 + foldersLen, filesLen);

        var folders = DecodeFolders(foldersBuffer);
        var files = DecodeFilesV11(filesBuffer);

        return new IndexReader(folders, files);
    }

    private static List<PZFolder> DecodeFolders(ReadOnlySpan<byte> buffer)
    {
        List<PZFolder> folders = new();

        int position = 0;
        while (position < buffer.Length)
        {
            int partLength = BitConverter.ToInt32(buffer.Slice(position, 4));
            var partBuffer = buffer.Slice(position + 4, partLength);

            int id = BitConverter.ToInt32(partBuffer[..4]);
            int pid = BitConverter.ToInt32(partBuffer[4..8]);
            string name = Encoding.UTF8.GetString(partBuffer[8..]);
            PZFolder folder = new(name, id, pid);
            folders.Add(folder);

            position += partLength + 4;
        }

        return folders;
    }
    private static List<PZFile> DecodeFilesV1(ReadOnlySpan<byte> buffer)
    {
        List<PZFile> files = new();
        IdCounter idCounter = new(160000);

        int position = 0;
        while (position < buffer.Length)
        {
            int partLength = BitConverter.ToInt32(buffer.Slice(position, 4));
            var partBuffer = buffer.Slice(position + 4, partLength);

            int pid = BitConverter.ToInt32(partBuffer[..4]);
            long offset = BitConverter.ToInt64(partBuffer[4..12]);
            int size = BitConverter.ToInt32(partBuffer[12..16]);
            string name = Encoding.UTF8.GetString(partBuffer[16..]);
            PZFile file = new(name, idCounter.Next(), pid, offset, size, size);
            files.Add(file);

            position += partLength + 4;
        }

        return files;
    }
    private static List<PZFile> DecodeFilesV2(ReadOnlySpan<byte> buffer)
    {
        List<PZFile> files = new();
        IdCounter idCounter = new(180000);

        int position = 0;
        while (position < buffer.Length)
        {
            int partLength = BitConverter.ToInt32(buffer.Slice(position, 4));
            var partBuffer = buffer.Slice(position + 4, partLength);

            int pid = BitConverter.ToInt32(partBuffer[..4]);
            long offset = BitConverter.ToInt64(partBuffer[4..12]);
            long size = BitConverter.ToInt64(partBuffer[12..20]);
            string name = Encoding.UTF8.GetString(partBuffer[20..]);
            PZFile file = new(name, idCounter.Next(), pid, offset, size, size);
            files.Add(file);

            position += partLength + 4;
        }

        return files;
    }
    private static List<PZFile> DecodeFilesV11(ReadOnlySpan<byte> buffer)
    {
        List<PZFile> files = new();

        int position = 0;
        while (position < buffer.Length)
        {
            int partLength = BitConverter.ToInt32(buffer.Slice(position, 4));
            var partBuffer = buffer.Slice(position + 4, partLength);

            int id = BitConverter.ToInt32(partBuffer[..4]);
            int pid = BitConverter.ToInt32(partBuffer[4..8]);
            long offset = BitConverter.ToInt64(partBuffer[8..16]);
            long size = BitConverter.ToInt64(partBuffer[16..24]);
            long originSize = BitConverter.ToInt64(partBuffer[24..32]);
            string name = Encoding.UTF8.GetString(partBuffer[32..]);
            PZFile file = new(name, id, pid, offset, size, originSize);
            files.Add(file);

            position += partLength + 4;
        }

        return files;
    }
}
