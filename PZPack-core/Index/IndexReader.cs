using System.Text;

namespace PZPack.Core.Index;

public class IndexReader
{
    readonly Dictionary<int, PZFolder> _folders = new();
    readonly Dictionary<int, PZFile> _files = new();
    readonly Dictionary<int, (PZFolder[], PZFile[])> _cache = new();

    readonly PZFolder _root;
    public PZFolder Root { get => _root; }

    internal IndexReader(List<PZFolder> folders, List<PZFile> files)
    {
        _root = new PZFolder("", PZCommon.IndexRootId, 0);

        foreach(var folder in folders)
        {
            _folders.Add(folder.Id, folder);
        }
        foreach (var file in files)
        {
            _files.Add(file.Id, file);
        }
    }
    private (PZFolder[], PZFile[]) LoadCache(PZFolder parent)
    {
        if (_cache.ContainsKey(parent.Id))
        {
            return _cache[parent.Id];
        }

        var folders = _folders.Values.Where(f => f.Pid == parent.Id).ToArray();
        var files = _files.Values.Where(f => f.Pid == parent.Id).ToArray();
        _cache.Add(parent.Id, (folders, files));

        return _cache[parent.Id];
    }

    public PZFolder GetFolder(int id)
    {
        if (id == Root.Id) return Root;
        if (!_folders.ContainsKey(id))
        {
            throw new Exceptions.PZFolderNotFoundException("", id);
        }

        return _folders[id];
    }
    public PZFile GetFile(int id)
    {
        if (!_folders.ContainsKey(id))
        {
            throw new Exceptions.PZFileNotFoundException("", id);
        }

        return _files[id];
    }
    public void GetChildren(PZFolder parent, out PZFolder[] folders, out PZFile[] files)
    {
        var children = LoadCache(parent);
        folders = children.Item1;
        files = children.Item2;
    }
    public PZFile[] GetFilesRecursion(PZFolder folder)
    {
        List<PZFile> list = new();
        GetChildren(folder, out var folders, out var files);
        list.AddRange(files);
        foreach (var f in folders)
        {
            list.AddRange(GetFilesRecursion(f));
        }
        return list.ToArray();
    }

    public PZFolder[] GetFolderResolveList(PZFolder folder, PZFolder? resolveFolder)
    {
        List<PZFolder> list = new();
        PZFolder current = folder;

        while (current.Id != PZCommon.IndexRootId)
        {
            list.Add(current);
            if (resolveFolder?.Id == current.Id)
            {
                break;
            }

            current = GetFolder(current.Pid);
        }

        list.Reverse();
        return list.ToArray();
    }

    public string GetResolveName(PZFolder folder, PZFolder resolveFolder)
    {
        PZFolder[] list = GetFolderResolveList(folder, resolveFolder);
        StringBuilder sb = new();
        foreach(var f in list)
        {
            sb.Append("/" + f.Name);
        }
        return sb.ToString();
    }
    public string GetResolveName(PZFile file, PZFolder resolveFolder)
    {
        PZFolder parent = GetFolder(file.Pid);
        string folderNames = GetResolveName(parent, resolveFolder);
        return folderNames + "/" + file.Name;
    }
    public string GetFullName(PZFolder folder)
    {
        return GetResolveName(folder, Root);
    }
    public string GetFullName(PZFile file)
    {
        return GetResolveName(file, Root);
    }
}
