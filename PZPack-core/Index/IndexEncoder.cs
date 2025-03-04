using System.Text;

namespace PZPack.Core.Index;

internal record PZEncodingInfo
{
    public int Id { get; init; }
    public long Offset { get; init; }
    public long EncryptedSize { get; init; }

    public PZEncodingInfo(int id, long offset, long encryptedSize)
    {
        Id = id;
        Offset = offset;
        EncryptedSize = encryptedSize;
    }
}

internal class IndexEncoder
{
    readonly Dictionary<int, PZEncodingInfo> _encodingInfos = new();
    readonly Dictionary<int, byte[]> _nameBuffers = new();
    readonly List<PZDesigningFile> _designingFiles;
    readonly List<PZDesigningFolder> _usedFolders;

    public IndexEncoder(IndexDesigner designer)
    {
        List<PZDesigningFile> designingFiles = designer.GetAllFiles();

        HashSet<int> usedFolderIds = new();
        foreach (var f in designingFiles)
        {
            var nameBuffer = Encoding.UTF8.GetBytes(f.Name);
            _nameBuffers.Add(f.Id, nameBuffer);

            var folder = designer.GetFolder(f.Pid);
            if (usedFolderIds.Contains(folder.Id)) continue;

            while (folder.Id != PZCommon.IndexRootId)
            {
                if (usedFolderIds.Contains(folder.Id)) break;
                usedFolderIds.Add(folder.Id);
                var folderNameBuffer = Encoding.UTF8.GetBytes(folder.Name);
                _nameBuffers.Add(folder.Id, folderNameBuffer);

                folder = designer.GetFolder(folder.Pid);
            }
        }

        _designingFiles = designingFiles;
        _usedFolders = usedFolderIds.Select(id => designer.GetFolder(id)).ToList();
    }

    public PZDesigningFile[] GetFiles()
    {
        return _designingFiles.ToArray();
    }
    public void WriteEncodingInfo(PZDesigningFile file, long offset, long encryptedSize)
    {
        PZEncodingInfo info = new(file.Id, offset, encryptedSize);
        _encodingInfos.Add(info.Id, info);
    }

    public void ReEncodeName(PZDesigningFile file, string newName)
    {
        if (_nameBuffers.ContainsKey(file.Id))
        {
            _nameBuffers[file.Id] = Encoding.UTF8.GetBytes(newName);
        }
    }

    public int GetEncodedIndexSize()
    {
        int size = 0;
        foreach (var f in _designingFiles)
        {
            var nameBuffer = _nameBuffers[f.Id];
            size += 36 + nameBuffer.Length;
        }
        foreach (var fl in _usedFolders)
        {
            var nameBuffer = _nameBuffers[fl.Id];
            size += 12 + nameBuffer.Length;
        }

        size += 8;
        return size;
    }

    public byte[] EncodeIndex()
    {
        int indexSize = GetEncodedIndexSize();
        using MemoryStream memStream = new(indexSize);
        using BinaryWriter bw = new(memStream);

        int folderPartSize = 0;
        memStream.Seek(8, SeekOrigin.Begin);
        foreach (var fl in _usedFolders)
        {
            byte[] nameBuffer = _nameBuffers[fl.Id];
            bw.Write(12 + nameBuffer.Length);
            bw.Write(fl.Id);
            bw.Write(fl.Pid);
            bw.Write(nameBuffer);
            folderPartSize += 12 + nameBuffer.Length;
        }

        int filePartSize = 0;
        foreach (var f in _designingFiles)
        {
            byte[] nameBuffer = _nameBuffers[f.Id];
            PZEncodingInfo info = _encodingInfos[f.Id];
            if (info == null)
            {
                throw new Exceptions.FileInIndexNotEncodeException(f.Name, f.Id);
            }

            bw.Write(36 + nameBuffer.Length);
            bw.Write(f.Id);
            bw.Write(f.Pid);
            bw.Write(info.Offset);
            bw.Write(info.EncryptedSize);
            bw.Write(f.Size);
            bw.Write(nameBuffer);
            filePartSize += 36 + nameBuffer.Length;
        }

        memStream.Seek(0, SeekOrigin.Begin);
        bw.Write(folderPartSize);
        bw.Write(filePartSize);

        return memStream.ToArray();
    }
}
