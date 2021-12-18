using System.Text;

namespace PZPack.Core
{
    internal class FolderTree
    {
        static public FolderTree Create()
        {
            return new FolderTree();
        }
        static public IFolderNode Decode(byte[] bytes)
        {
            Dictionary<int, PZFolder> folderMap = new();
            using (MemoryStream memStream = new(bytes))
            {
                memStream.Seek(0, SeekOrigin.Begin);
                while (memStream.Position < memStream.Length)
                {
                    byte[] lenBi = new byte[4];
                    memStream.Read(lenBi, 0, 4);
                    int len = BitConverter.ToInt32(lenBi);
                    byte[] folderBi = new byte[len];
                    memStream.Read(folderBi, 0, len);

                    PZFolder folder = PZFolder.FromBytes(folderBi);
                    folderMap.Add(folder.Id, folder);
                }
            }

            folderMap.Remove(IdCreator.RootId);
            Node root = Node.CreateRoot();
            foreach (var folder in folderMap.Values)
            {
                PZFolder temp = folder;
                List<PZFolder> tempList = new();
                while (true)
                {
                    tempList.Add(temp);
                    if (temp.ParentId == IdCreator.RootId) { break; }
                    temp = folderMap[temp.ParentId];
                }

                PZFolder[] arr = tempList.Reverse<PZFolder>().ToArray();
                root.ReAdd(arr, 0);
            }

            return root;
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

        class IdCreator
        {
            public const int RootId = 10000;
            private int Countor;
            public IdCreator()
            {
                Countor = RootId + 1;
            }
            public int Next()
            {
                Countor++;
                return Countor;
            }
        }
        class Node : IFolderNode
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
                Id = IdCreator.RootId;
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
            public Node Add(string[] folders, int index, IdCreator idc)
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

        private readonly IdCreator Idc;
        private readonly Node Root;
        private readonly Dictionary<string, int> cache;
        private FolderTree()
        {
            cache = new Dictionary<string, int>();
            Idc = new IdCreator();
            Root = Node.CreateRoot();
            cache.Add(Root.Name, Root.Id);
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
        public byte[] Encode()
        {
            byte[] result;

            using (MemoryStream stream = new(Common.MemInitLength))
            {
                EachNodes(Root, (node) =>
                {
                    PZFolder folder = new(node.Name, node.Id, node.ParentId);
                    stream.Write(folder.ToBytes());
                });

                stream.Flush();
                result = stream.ToArray();
            }

            return result;
        }
    }
}
