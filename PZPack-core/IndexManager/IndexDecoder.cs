namespace PZPack.Core
{
    internal class IndexDecoder
    {
        private static List<PZFile> FlattenFiles(Dictionary<int, List<PZFile>> filesMap)
        {
            List<PZFile> fullList = new();
            foreach (var flist in filesMap.Values)
            {
                fullList.AddRange(flist);
            }
            return fullList;
        }

        private readonly List<PZFile> Files;
        private readonly Dictionary<int, List<PZFile>> FilesMap;
        private readonly Dictionary<int, IFolderNode> FoldersMap;
        private readonly IFolderNode Root;
        public int FolderCount
        {
            get
            {
                return FoldersMap.Count;
            }
        }
        public int FileCount
        {
            get
            {
                return Files.Count;
            }
        }

        public IndexDecoder(Dictionary<int, List<PZFile>> filesMap, IFolderNode rootFolder)
        {
            FilesMap = filesMap;
            Files = FlattenFiles(filesMap);
            Root = rootFolder;
            FoldersMap = FolderTree.Flatten(rootFolder);
        }

        public PZFile[] GetFiles(int folderId)
        {
            return FilesMap.ContainsKey(folderId) ? FilesMap[folderId].ToArray() : Array.Empty<PZFile>();
        }
        public PZFile[] GetAllFiles()
        {
            return Files.ToArray();
        }
        public IFolderNode GetTree()
        {
            return Root;
        }
        public IFolderNode GetFolder(int folderId)
        {
            return FoldersMap[folderId];
        }
        public IFolderNode[] GetAllFolders()
        {
            return FoldersMap.Values.ToArray();
        }
    }
}
