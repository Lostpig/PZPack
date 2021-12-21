namespace PZPack.Core
{
    public class ProgressReporter<T> : IProgress<T>
    {
        readonly Action<T> action;
        public ProgressReporter(Action<T> progressAction) =>
            action = progressAction;

        public void Report(T value) => action(value);
    }
    public delegate void ProgressChangedHandler<T>(T value);
}
