namespace PZPack.Core
{
    public class PZVersion
    {
        public static int Current { get => PZCommon.Version; }
        public static bool CompatibleVersion(int version)
        {
            return Compatibles.IsCompatibleVersion(version);
        }
    }
}
