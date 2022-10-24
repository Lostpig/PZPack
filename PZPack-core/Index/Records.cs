using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PZPack.Core.Index;

public record PZFile
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

public record PZFolder
{
    public readonly string Name;
    public readonly int Id;
    public readonly int Pid;

    public PZFolder(string Name, int Id, int Pid)
    {
        this.Name = Name;
        this.Id = Id;
        this.Pid = Pid;
    }
}
