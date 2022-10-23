using PZPack.Core.Utility;

namespace PZPack.Core;

public enum PZTypes
{
    PZPACK,
    PZVIDEO
}
public class PZCommon
{
    internal const int MemInitLength = 65536;
    internal const string PZPackSign = "PZPACK";
    internal const string PZVideoSign = "PZVIDEO";
    internal const int Version = 11;
    internal const int IndexRootId = 10000;

    static byte[]? pzpackHashCache;
    static string? pzpackHashHexCache;
    static byte[]? pzvideoHashCache;
    static string? pzvideoHashHexCache;
    public static byte[] HashSign(PZTypes type)
    {
        if (type == PZTypes.PZPACK)
        {
            pzpackHashCache ??= PZHash.Sha256(PZPackSign);
            return pzpackHashCache;
        }
        else
        {
            pzvideoHashCache ??= PZHash.Sha256(PZVideoSign);
            return pzvideoHashCache;
        }
    }
    public static string HashSignHex(PZTypes type)
    {
        if (type == PZTypes.PZPACK)
        {
            pzpackHashHexCache ??= Convert.ToHexString(HashSign(type));
            return pzpackHashHexCache;
        }
        else
        {
            pzvideoHashHexCache ??= Convert.ToHexString(HashSign(type));
            return pzvideoHashHexCache;
        }
    }
}
