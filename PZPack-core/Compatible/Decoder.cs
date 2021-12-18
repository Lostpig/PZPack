using System.Text;

namespace PZPack.Core.Compatible
{
    internal class Decoder
    {
        static private Dictionary<int, List<PZFile>> DecodeFileList(byte[] bytes, int version)
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
        static public IndexDecoder DecodeIndexData(byte[] bytes, int version)
        {
            using MemoryStream memStream = new(bytes);
            using BinaryReader br = new(memStream);
            int folderLen = br.ReadInt32();
            int fileLen = br.ReadInt32();

            byte[] folderBi = br.ReadBytes(folderLen);
            byte[] fileBi = br.ReadBytes(fileLen);

            Dictionary<int, List<PZFile>>  filesMap = DecodeFileList(fileBi, version);
            IFolderNode root = FolderTree.Decode(folderBi);

            return new IndexDecoder(filesMap, root);
        }

        private static PZFile DecodePZFile(byte[] bytes, int version)
        {
            return version switch
            {
                1 => PZFileFromByteV1(bytes),
                _ => PZFile.FromBytes(bytes)
            };
        }
        private static PZFile PZFileFromByteV1(byte[] bytes)
        {
            int folderId = BitConverter.ToInt32(bytes, 0);
            long offset = BitConverter.ToInt64(bytes, 4);
            int size = BitConverter.ToInt32(bytes, 12);
            string name = Encoding.UTF8.GetString(bytes, 16, bytes.Length - 16);

            return new PZFile(name, size, offset, folderId);
        }
    }
}
