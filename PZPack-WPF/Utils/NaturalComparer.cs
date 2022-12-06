using PZPack.Core.Index;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;

namespace PZPack.View.Utils;

[SuppressUnmanagedCodeSecurity]
internal static class SafeNativeMethods
{
    [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
    public static extern int StrCmpLogicalW(string psz1, string psz2);
}

internal sealed class NaturalComparer : IComparer<string>
{
    private static NaturalComparer? _instance;
    public static NaturalComparer Instance { get { return _instance ??= new NaturalComparer(); } }
    public int Compare(string? a, string? b)
    {
        return SafeNativeMethods.StrCmpLogicalW(a ?? "", b ?? "");
    }
}

internal sealed class NaturalPZFileComparer : IComparer<IPZFile>
{
    private static NaturalPZFileComparer? _instance;
    public static NaturalPZFileComparer Instance { get { return _instance ??= new NaturalPZFileComparer(); } }
    public int Compare(IPZFile? a, IPZFile? b)
    {
        return SafeNativeMethods.StrCmpLogicalW(a?.Name ?? "", b?.Name ?? "");
    }
}

internal sealed class NaturalPZFolderComparer : IComparer<IPZFolder>
{
    private static NaturalPZFolderComparer? _instance;
    public static NaturalPZFolderComparer Instance { get { return _instance ??= new NaturalPZFolderComparer(); } }
    public int Compare(IPZFolder? a, IPZFolder? b)
    {
        return SafeNativeMethods.StrCmpLogicalW(a?.Name ?? "", b?.Name ?? "");
    }
}
