namespace PZPack.Core.Utility;

internal class IdCounter
{
    private int Countor;
    public IdCounter(int startId = 10000)
    {
        Countor = startId + 1;
    }
    public int Next()
    {
        Countor++;
        return Countor;
    }
}
