namespace PZPack.Core.Index;

public interface IPZFile
{
    public string Name { get; }
    public int Id { get; }
    public int Pid { get; }
    public long Size { get; }
    public string Extension { get; }
}
public interface IPZFolder
{
    public string Name { get; }
    public int Id { get; }
    public int Pid { get; }
}

public record PZFile : IPZFile
{
    public PZFile(string Name, int Id, int Pid, long Offset, long Size, long OriginSize)
    {
        this.Name = Name;
        this.Id = Id;
        this.Pid = Pid;
        this.Offset = Offset;
        this.Size = Size;
        this.OriginSize = OriginSize;

        Extension = Path.GetExtension(Name);
    }

    public string Name { get; init; }
    public int Id { get; init; }
    public int Pid { get; init; }
    public long Offset { get; init; }
    public long Size { get; init; }
    public long OriginSize { get; init; }

    public string Extension { get; init; }
}

public record PZFolder : IPZFolder
{
    public string Name { get; init; }
    public int Id { get; init; }
    public int Pid { get; init; }

    public PZFolder(string Name, int Id, int Pid)
    {
        this.Name = Name;
        this.Id = Id;
        this.Pid = Pid;
    }
}

public record PZDesigningFolder : IPZFolder
{
    public string Name { get; internal set; }
    public int Id { get; internal set; }
    public int Pid { get; internal set; }

    public PZDesigningFolder(string Name, int Id, int Pid)
    {
        this.Name = Name;
        this.Id = Id;
        this.Pid = Pid;
    }
}

public record PZDesigningFile : IPZFile
{
    public string Name { get; internal set; }
    public int Id { get; internal set; }
    public int Pid { get; internal set; }
    public string Source { get; internal set; }
    public long Size { get; internal set; }
    public string Extension { get; init; }

    public PZDesigningFile(string Name, int Id, int Pid, string Source, long Size)
    {
        this.Name = Name;
        this.Id = Id;
        this.Pid = Pid;
        this.Source = Source;
        this.Size = Size;

        Extension = Path.GetExtension(Name).ToLower();
    }
}
