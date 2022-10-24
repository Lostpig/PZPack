using PZPack.Core.Index;
using System;
using System.Diagnostics;

namespace PZPack.View.Service;

internal class ExPlayer
{
    public static void Play(PZFolder folder)
    {
        string expalyerPath = Config.Instance.ExternalPlayer;
        if (string.IsNullOrEmpty(expalyerPath))
        {
            Alert.ShowWarning(Translate.EX_ExternalPlayerNotSet);
            return;
        }
        if (!DashServer.Instance.Binded)
        {
            Alert.ShowWarning(Translate.EX_ServerNotBinding);
            return;
        }

        var server = DashServer.Instance;
        string url = $"http://localhost:{server.port}/{server.Hash!}/{folder.Id}/output.mpd";
        Process.Start(expalyerPath, url);
    }
    public static void PlayAll()
    {
        string expalyerPath = Config.Instance.ExternalPlayer;
        if (string.IsNullOrEmpty(expalyerPath))
        {
            Alert.ShowWarning(Translate.EX_ExternalPlayerNotSet);
            return;
        }
        if (!DashServer.Instance.Binded)
        {
            Alert.ShowWarning(Translate.EX_ServerNotBinding);
            return;
        }

        var server = DashServer.Instance;
        string url = $"http://localhost:{server.port}/{server.Hash!}/playlist.pls";
        Process.Start(expalyerPath, url);
    }
}
