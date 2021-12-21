namespace PZPack.Core
{
    public class Version
    {
        public static int Current { get => Common.Version; }
        public static bool CompatibleVersion(int version)
        {
            return Common.Version >= version;
        }
    }
}
