namespace PZPack.Core
{
    public class ProgressReporter<T> : IProgress<T>
    {
        private readonly Action<T> action;
        public ProgressReporter(Action<T> progressAction) => action = progressAction;
        void IProgress<T>.Report(T value) => action(value);
    }
    public delegate void ProgressChangedHandler<T>(T value);
}
