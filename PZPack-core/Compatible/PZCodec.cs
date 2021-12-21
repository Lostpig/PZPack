using System.Diagnostics;
using System.Text;

namespace PZPack.Core.Compatible
{
    internal class PZCodec
    {
        private static Dictionary<int, List<PZFile>> DecodeFileList(byte[] bytes, int version)
        {
            Dictionary<int, List<PZFile>> filesMap = new();
            using (MemoryStream memStream = new(bytes))
            {
                while (memStream.Position < memStream.Length)
                {
                    byte[] lenBi = new byte[4];
                    memStream.Read(lenBi, 0, 4);
                    int len = BitConverter.ToInt32(lenBi);

                    byte[] fileBi = new byte[len];
                    memStream.Read(fileBi, 0, len);
                    PZFile file = DecodePZFile(fileBi, version);

                    if (!filesMap.ContainsKey(file.FolderId))
                    {
                        List<PZFile> list = new();
                        filesMap.Add(file.FolderId, list);
                    }
                    filesMap[file.FolderId].Add(file);
                }
            }

            return filesMap;
        }
        private static IFolderNode DecodeFolderNode(byte[] bytes)
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

                    PZFolder folder = PZCodec.DeocdePZFolder(folderBi);
                    folderMap.Add(folder.Id, folder);
                }
            }

            folderMap.Remove(FolderIdCreator.RootId);
            FolderTree.Node root = FolderTree.Node.CreateRoot();
            foreach (var folder in folderMap.Values)
            {
                PZFolder temp = folder;
                List<PZFolder> tempList = new();
                while (true)
                {
                    tempList.Add(temp);
                    if (temp.ParentId == FolderIdCreator.RootId) { break; }
                    temp = folderMap[temp.ParentId];
                }

                PZFolder[] arr = tempList.Reverse<PZFolder>().ToArray();
                root.ReAdd(arr, 0);
            }

            return root;
        }

        private static PZFile DecodePZFile(byte[] bytes, int version)
        {
            return version switch
            {
                1 => PZFileFromByteV1(bytes),
                _ => DecodePZFileVCurrent(bytes)
            };
        }
        private static PZFile DecodePZFileVCurrent (byte[] bytes)
        {
            int folderId = BitConverter.ToInt32(bytes, 0);
            long offset = BitConverter.ToInt64(bytes, 4);
            long size = BitConverter.ToInt64(bytes, 12);
            string name = Encoding.UTF8.GetString(bytes, 20, bytes.Length - 20);

            return new PZFile(name, folderId, offset, size);
        }
        private static PZFile PZFileFromByteV1(byte[] bytes)
        {
            int folderId = BitConverter.ToInt32(bytes, 0);
            long offset = BitConverter.ToInt64(bytes, 4);
            int size = BitConverter.ToInt32(bytes, 12);
            string name = Encoding.UTF8.GetString(bytes, 16, bytes.Length - 16);

            return new PZFile(name, folderId, offset, size);
        }
    
        private static byte[] EncodePZFile(PZFile file)
        {
            byte[] nameBi = Encoding.UTF8.GetBytes(file.Name);
            byte[] offsetBi = BitConverter.GetBytes(file.Offset);
            byte[] sizeBi = BitConverter.GetBytes(file.Size);
            byte[] fIdBi = BitConverter.GetBytes(file.FolderId);

            Debug.Assert(offsetBi.Length + sizeBi.Length + fIdBi.Length == 20);

            int fullLength = nameBi.Length + offsetBi.Length + sizeBi.Length + fIdBi.Length;
            byte[] lenBi = BitConverter.GetBytes(fullLength);

            return lenBi.Concat(fIdBi).Concat(offsetBi).Concat(sizeBi).Concat(nameBi).ToArray();
        }
        private static byte[] EncodeFileList(List<PZFile> list)
        {
            using MemoryStream memStream = new();
            memStream.Seek(0, SeekOrigin.Begin);
            foreach (PZFile file in list)
            {
                memStream.Write(EncodePZFile(file));
            }
            memStream.Flush();

            return memStream.ToArray();
        }

        private static PZFolder DeocdePZFolder(byte[] bytes)
        {
            int id = BitConverter.ToInt32(bytes, 0);
            int parentId = BitConverter.ToInt32(bytes, 4);
            string name = Encoding.UTF8.GetString(bytes, 8, bytes.Length - 8);
            return new PZFolder(name, id, parentId);
        }
        private static byte[] EncodePZFolder(PZFolder folder)
        {
            byte[] nameBi = Encoding.UTF8.GetBytes(folder.Name);
            byte[] idBi = BitConverter.GetBytes(folder.Id);
            byte[] pIdBi = BitConverter.GetBytes(folder.ParentId);
            int fullLength = nameBi.Length + idBi.Length + pIdBi.Length;
            byte[] lenBi = BitConverter.GetBytes(fullLength);

            return lenBi.Concat(idBi).Concat(pIdBi).Concat(nameBi).ToArray();
        }
        private static byte[] EncodePZFolderRoot(IFolderNode root)
        {
            byte[] result;

            using (MemoryStream stream = new(Common.MemInitLength))
            {
                FolderTree.EachNodes(root, (node) =>
                {
                    PZFolder folder = new(node.Name, node.Id, node.ParentId);
                    stream.Write(EncodePZFolder(folder));
                });

                stream.Flush();
                result = stream.ToArray();
            }

            return result;
        }

        public static IndexDecoder DecodeIndexData(byte[] bytes, int version)
        {
            using MemoryStream memStream = new(bytes);
            using BinaryReader br = new(memStream);
            int folderLen = br.ReadInt32();
            int fileLen = br.ReadInt32();

            byte[] folderBi = br.ReadBytes(folderLen);
            byte[] fileBi = br.ReadBytes(fileLen);

            Dictionary<int, List<PZFile>> filesMap = DecodeFileList(fileBi, version);
            IFolderNode root = DecodeFolderNode(folderBi);

            return new IndexDecoder(filesMap, root);
        }
        public static byte[] EncodeIndexData(IndexEncoder encoder)
        {
            byte[] fileBytes = EncodeFileList(encoder.Files);
            byte[] folderBytes = EncodePZFolderRoot(encoder.Tree.GetRoot());

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
