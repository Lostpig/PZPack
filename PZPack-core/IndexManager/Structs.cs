using System.Diagnostics;
using System.Text;

namespace PZPack.Core
{
    public readonly struct PZFile
    {
        public readonly string Name { get; }
        public readonly long Size { get; }
        public readonly long Offset { get; }
        public readonly int FolderId { get; }
        public readonly string Extension { get; }
        public PZFile(string name, long size, long offset, int folderId)
        {
            Name = name;
            Size = size;
            Offset = offset;
            FolderId = folderId;

            Extension = Path.GetExtension(name);
        }
        public byte[] ToBytes()
        {
            byte[] nameBi = Encoding.UTF8.GetBytes(Name);
            byte[] offsetBi = BitConverter.GetBytes(Offset);
            byte[] sizeBi = BitConverter.GetBytes(Size);
            byte[] fIdBi = BitConverter.GetBytes(FolderId);

            Debug.Assert(offsetBi.Length + sizeBi.Length + fIdBi.Length == 20);

            int fullLength = nameBi.Length + offsetBi.Length + sizeBi.Length + fIdBi.Length;
            byte[] lenBi = BitConverter.GetBytes(fullLength);

            return lenBi.Concat(fIdBi).Concat(offsetBi).Concat(sizeBi).Concat(nameBi).ToArray();
        }
        public static PZFile FromBytes(byte[] bytes)
        {
            int folderId = BitConverter.ToInt32(bytes, 0);
            long offset = BitConverter.ToInt64(bytes, 4);
            long size = BitConverter.ToInt64(bytes, 12);
            string name = Encoding.UTF8.GetString(bytes, 20, bytes.Length - 20);

            return new PZFile(name, size, offset, folderId);
        }
    }
    public readonly struct PZFolder
    {
        public readonly string Name { get; }
        public readonly int Id { get; }
        public readonly int ParentId { get; }

        public PZFolder(string name, int id, int pid)
        {
            Name = name;
            Id = id;
            ParentId = pid;
        }
        public byte[] ToBytes()
        {
            byte[] nameBi = Encoding.UTF8.GetBytes(Name);
            byte[] idBi = BitConverter.GetBytes(Id);
            byte[] pIdBi = BitConverter.GetBytes(ParentId);
            int fullLength = nameBi.Length + idBi.Length + pIdBi.Length;
            byte[] lenBi = BitConverter.GetBytes(fullLength);

            return lenBi.Concat(idBi).Concat(pIdBi).Concat(nameBi).ToArray();
        }
        public static PZFolder FromBytes(byte[] bytes)
        {
            int id = BitConverter.ToInt32(bytes, 0);
            int parentId = BitConverter.ToInt32(bytes, 4);
            string name = Encoding.UTF8.GetString(bytes, 8, bytes.Length - 8);
            return new PZFolder(name, id, parentId);
        }
    }
}
