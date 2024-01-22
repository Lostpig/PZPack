using PZPack.Core.Utility;
using System.Collections.Generic;

namespace PZPack.Core.Index;

public class IndexDesigner
{
    readonly IdCounter _idCounter = new(PZCommon.IndexRootId);
    readonly Dictionary<int, PZDesigningFolder> _folders = new();
    readonly Dictionary<int, PZDesigningFile> _files = new();
    readonly PZDesigningFolder _root;
    public PZDesigningFolder Root { get => _root; }
    public bool IsEmpty { get => _files.Count == 0; }

    public IndexDesigner()
    {
        _root = new PZDesigningFolder("", PZCommon.IndexRootId, 0);
    }

    public void CheckFolderExists(PZDesigningFolder folder)
    {
        if (!_folders.ContainsKey(folder.Id) && folder.Id != Root.Id)
        {
            throw new Exceptions.PZFolderNotFoundException(folder.Name, folder.Id);
        }
    }
    public void CheckDuplicateNameFile(string name, PZDesigningFolder parent)
    {
        var files = GetFiles(parent);
        if (files.Exists(f => f.Name == name))
        {
            throw new Exceptions.DuplicateNameException(name);
        }
    }
    public void CheckDuplicateNameFolder(string name, PZDesigningFolder parent)
    {
        var folders = GetFolders(parent);
        if (folders.Exists(f => f.Name == name))
        {
            throw new Exceptions.DuplicateNameException(name);
        }
    }

    public PZDesigningFolder AddFolder(string name, PZDesigningFolder parent)
    {
        CheckFolderExists(parent);
        CheckDuplicateNameFolder(name, parent);

        int id = _idCounter.Next();
        PZDesigningFolder folder = new(name, id, parent.Id);
        _folders.Add(id, folder);
        return folder;
    }
    public PZDesigningFile AddFile(string source, string name, PZDesigningFolder parent)
    {
        FileInfo sourceInfo = new(source);
        if (!sourceInfo.Exists)
        {
            throw new FileNotFoundException(string.Empty, source);
        }

        CheckFolderExists(parent);
        CheckDuplicateNameFile(name, parent);

        int id = _idCounter.Next();
        PZDesigningFile file = new(name, id, parent.Id, source, sourceInfo.Length);
        _files.Add(id, file);
        return file;
    }

    public bool RemoveFile(PZDesigningFile file)
    {
        return _files.Remove(file.Id);
    }
    public bool RemoveFolder(PZDesigningFolder folder)
    {
        if (folder.Id == PZCommon.IndexRootId)
        {
            throw new ArgumentException("Cannot remove root folder", nameof(folder));
        }

        List<PZDesigningFile> files = GetFiles(folder);
        foreach (var file in files)
        {
            RemoveFile(file);
        }
        List<PZDesigningFolder> children = GetFolders(folder);
        foreach (var child in children)
        {
            RemoveFolder(child);
        }

        return _folders.Remove(folder.Id);
    }
    public void MoveFile(PZDesigningFile file, PZDesigningFolder toFolder)
    {
        CheckDuplicateNameFile(file.Name, toFolder);
        file.Pid = toFolder.Id;
    }
    public void MoveFolder(PZDesigningFolder folder, PZDesigningFolder toParent)
    {
        if (folder.Id == PZCommon.IndexRootId)
        {
            throw new ArgumentException("Cannot move root folder", nameof(folder));
        }

        CheckDuplicateNameFolder(folder.Name, toParent);
        folder.Pid = toParent.Id;
    }
    public void RenameFile(PZDesigningFile file, string newName)
    {
        if (newName == file.Name) return;

        PZDesigningFolder folder = GetFolder(file.Pid);
        CheckDuplicateNameFile(newName, folder);

        file.Name = newName;
    }
    public void RenameFolder(PZDesigningFolder folder, string newName)
    {
        if (folder.Id == PZCommon.IndexRootId)
        {
            throw new ArgumentException("Cannot rename root folder", nameof(folder));
        }
        if (newName == folder.Name) return;

        PZDesigningFolder parent = GetFolder(folder.Pid);
        CheckDuplicateNameFolder(newName, parent);

        folder.Name = newName;
    }

    public PZDesigningFolder GetFolder(int id)
    {
        if (id == Root.Id) return Root;

        if (!_folders.ContainsKey(id))
        {
            throw new Exceptions.PZFolderNotFoundException("", id);
        }

        return _folders[id];
    }
    public PZDesigningFile GetFile(int id)
    {
        if (!_folders.ContainsKey(id))
        {
            throw new Exceptions.PZFileNotFoundException("", id);
        }

        return _files[id];
    }
    public List<PZDesigningFolder> GetFolders(PZDesigningFolder parent)
    {
        return _folders.Values.Where(f => f.Pid == parent.Id).ToList();
    }
    public List<PZDesigningFile> GetFiles(PZDesigningFolder folder)
    {
        return _files.Values.Where(f => f.Pid == folder.Id).ToList();
    }

    public List<PZDesigningFolder> GetFolderResolveList(PZDesigningFolder folder, PZDesigningFolder? resolveFolder)
    {
        List<PZDesigningFolder> list = new();
        PZDesigningFolder current = folder;

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
        return list;
    }

    public List<PZDesigningFile> GetAllFiles()
    {
        return _files.Values.ToList()!;
    }
}
