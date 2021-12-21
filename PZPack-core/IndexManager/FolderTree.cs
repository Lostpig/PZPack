using System.Text;

namespace PZPack.Core
{
    internal class FolderTree
    {
        static public FolderTree Create()
        {
            return new FolderTree();
        }

        static public void EachNodes(IFolderNode node, Action<IFolderNode> func)
        {
            func(node);
            foreach (var item in node.GetChildren())
            {
                EachNodes(item, func);
            }
        }
        static public Dictionary<int, IFolderNode> Flatten(IFolderNode node)
        {
            Dictionary<int, IFolderNode> map = new();
            EachNodes(node, n => map.Add(n.Id, n));
            return map;
        }

        internal class Node : IFolderNode
        {
            public static Node CreateRoot()
            {
                return new Node();
            }

            public string Name { get; }
            public string FullName { get; }
            public int Id { get; }
            public int ParentId { get; }
            private readonly Dictionary<string, Node> children;

            private Node()
            {
                Id = FolderIdCreator.RootId;
                ParentId = 0;
                Name = "";
                FullName = "";
                children = new Dictionary<string, Node>();
            }
            private Node(string name, int id, Node parent)
            {
                Id = id;
                ParentId = parent.Id;
                Name = name;
                FullName = Path.Join(parent.FullName, Name);
                children = new Dictionary<string, Node>();
            }
            public Node Add(string[] folders, int index, FolderIdCreator idc)
            {
                if (index == folders.Length) { return this; }

                string current = folders[index];
                if (!children.ContainsKey(current))
                {
                    Node child = new(current, idc.Next(), this);
                    children.Add(current, child);
                }
                return children[current].Add(folders, index + 1, idc);
            }
            public Node ReAdd(PZFolder[] folders, int index)
            {
                if (index == folders.Length) { return this; }
                PZFolder current = folders[index];
                if (!children.ContainsKey(current.Name))
                {
                    Node child = new(current.Name, current.Id, this);
                    children.Add(current.Name, child);
                }
                return children[current.Name].ReAdd(folders, index + 1);
            }
            public IEnumerable<IFolderNode> GetChildren()
            {
                return children.Values.AsEnumerable();
            }
        }

        private readonly FolderIdCreator Idc;
        private readonly Node Root;
        private readonly Dictionary<string, int> cache;
        private FolderTree()
        {
            cache = new Dictionary<string, int>();
            Idc = new FolderIdCreator();
            Root = Node.CreateRoot();
            cache.Add(Root.Name, Root.Id);
        }

        public IFolderNode GetRoot()
        {
            return Root;
        }
        public int EnsureFolder(string path)
        {
            string? folderPath = Path.GetDirectoryName(path);
            if (String.IsNullOrEmpty(folderPath))
            {
                folderPath = "";
            }

            if (cache.ContainsKey(folderPath))
            {
                return cache[folderPath];
            }

            string[] folders = folderPath.Split(Path.DirectorySeparatorChar);
            Node node = Root.Add(folders, 0, Idc);
            cache.Add(folderPath, node.Id);
            return node.Id;
        }
    }
}
