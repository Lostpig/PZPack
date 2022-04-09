namespace PZPack.Core
{
    public enum PZTypes
    {
        PZPACK,
        PZVIDEO
    }
    public class Common
    {
        internal const int MemInitLength = 65536;
        internal const string PZPackSign = "PZPACK";
        internal const string PZVideoSign = "PZVIDEO";
        internal const int Version = 4;

        static byte[]? pzpackHashCache;
        static string? pzpackHashHexCache;
        static byte[]? pzvideoHashCache;
        static string? pzvideoHashHexCache;
        public static byte[] HashSign(PZTypes type)
        {
            if (type == PZTypes.PZPACK)
            {
                if (pzpackHashCache == null) pzpackHashCache = PZHash.Hash(PZPackSign);
                return pzpackHashCache;
            }
            else
            {
                if (pzvideoHashCache == null) pzvideoHashCache = PZHash.Hash(PZVideoSign);
                return pzvideoHashCache;
            }
        }
        public static string HashSignHex(PZTypes type)
        {
            if (type == PZTypes.PZPACK)
            {
                if (pzpackHashHexCache == null) pzpackHashHexCache = Convert.ToHexString(HashSign(type));
                return pzpackHashHexCache;
            }
            else
            {
                if (pzvideoHashHexCache == null) pzvideoHashHexCache = Convert.ToHexString(HashSign(type));
                return pzvideoHashHexCache;
            }
        }
    }
}
