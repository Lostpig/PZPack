using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PZPack.Core
{

    public record PZPackInfo(string Password, string Description = "");
    public record PZFile(string Name, int FolderId, long Offset, long Size)
    {
        public string Extension { get; init; } = Path.GetExtension(Name);
    };
    public record PZFolder(string Name, int Id, int ParentId);
}
