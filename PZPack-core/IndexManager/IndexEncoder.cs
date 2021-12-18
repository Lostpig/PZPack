namespace PZPack.Core
{
    internal class IndexEncoder
    {
        static private byte[] EncodeFileList(List<PZFile> list)
        {
            using MemoryStream memStream = new();
            memStream.Seek(0, SeekOrigin.Begin);
            foreach (PZFile file in list)
            {
                memStream.Write(file.ToBytes());
            }
            memStream.Flush();

            return memStream.ToArray();
        }

        readonly List<PZFile> files;
        readonly FolderTree folderTree;
        public IndexEncoder()
        {
            files = new List<PZFile>();
            folderTree = FolderTree.Create();
        }

        public void AppendFile(string fullName, long offset, long size)
        {
            int folderId = folderTree.EnsureFolder(fullName);
            string name = Path.GetFileName(fullName);
            PZFile file = new(name, size, offset, folderId);
            files.Add(file);
        }
        public byte[] Encode()
        {
            byte[] fileBytes = EncodeFileList(files);
            byte[] folderBytes = folderTree.Encode();

            using MemoryStream ms = new();
            using BinaryWriter bw = new(ms);
            bw.Write(folderBytes.Length);
            bw.Write(fileBytes.Length);
            bw.Write(folderBytes);
            bw.Write(fileBytes);

            bw.Flush();
            return ms.ToArray();
        }
    }
}
