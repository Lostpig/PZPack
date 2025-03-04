

namespace PZPack.View.Service
{
    class StartupWith
    {
        public static void ApplyStartArg(string? arg)
        {
            if (arg == null) return;
            if (!arg.EndsWith(".pzpk")) return;

            bool openSuccess = Reader.TryOpen(arg);
            if (openSuccess) return;

            Dialogs.OpenReadOptionWindow(arg);
        }
    }
}
