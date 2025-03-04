namespace PZPack.Core;

internal class Compatibles
{
    public static bool IsCompatibleVersion(int version)
    {
        return version switch
        {
            1 or 2 or 4 => true,
            11 => true,
            12 => true,
            _ => false
        };
    }

}
