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

    public readonly string Name;
    public readonly int Id;
    public readonly int Pid;
    public readonly long Offset;
    public readonly long Size;
    public readonly long OriginSize;

    public readonly string Extension;
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
