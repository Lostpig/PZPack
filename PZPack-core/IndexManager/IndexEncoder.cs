namespace PZPack.Core
{
    internal class IndexEncoder
    {
        public readonly List<PZFile> Files;
        public readonly FolderTree Tree;
        public IndexEncoder()
        {
            Files = new List<PZFile>();
            Tree = FolderTree.Create();
        }

        public void AppendFile(string fullName, long offset, long size)
        {
            int folderId = Tree.EnsureFolder(fullName);
            string name = Path.GetFileName(fullName);
            PZFile file = new(name, folderId, offset, size);
            Files.Add(file);
        }
    }
}
