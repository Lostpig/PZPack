using PZPack.Core;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;


namespace PZPack.View.Utils
{
    [SuppressUnmanagedCodeSecurity]
    internal static class SafeNativeMethods
    {
        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
        public static extern int StrCmpLogicalW(string psz1, string psz2);
    }

    internal sealed class NaturalComparer : IComparer<string>
    {
        private static NaturalComparer? _instance;
        public static NaturalComparer Instance { get { return _instance ?? (_instance = new NaturalComparer()); } }
        public int Compare(string? a, string? b)
        {
            return SafeNativeMethods.StrCmpLogicalW(a ?? "", b ?? "");
        }
    }

    internal sealed class NaturalPZFileComparer : IComparer<PZFile>
    {
        private static NaturalPZFileComparer? _instance;
        public static NaturalPZFileComparer Instance { get { return _instance ?? (_instance = new NaturalPZFileComparer()); } }
        public int Compare(PZFile? a, PZFile? b)
        {
            return SafeNativeMethods.StrCmpLogicalW(a?.Name ?? "", b?.Name ?? "");
        }
    }

    internal sealed class NaturalPZFolderComparer : IComparer<IFolderNode>
    {
        private static NaturalPZFolderComparer? _instance;
        public static NaturalPZFolderComparer Instance { get { return _instance ?? (_instance = new NaturalPZFolderComparer()); } }
        public int Compare(IFolderNode? a, IFolderNode? b)
        {
            return SafeNativeMethods.StrCmpLogicalW(a?.Name ?? "", b?.Name ?? "");
        }
    }
}
