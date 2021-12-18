namespace PZPack.Core
{
    public class Common
    {
        internal const int MemInitLength = 65536;
        internal const string Sign = "PZPACK";
        public const int Version = 2;
    }
    public record PackOption(string Password, string Description);
    public class ProgressReporter<T> : IProgress<T>
    {
        readonly Action<T> action;
        public ProgressReporter(Action<T> progressAction) =>
            action = progressAction;

        public void Report(T value) => action(value);
    }
}
