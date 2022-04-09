using PZPack.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PZPack.View.Service
{
    internal class ExPlayer
    {
        public static void Play(IFolderNode folder)
        {
            string expalyerPath = Config.Instance.ExternalPlayer;
            if (String.IsNullOrEmpty(expalyerPath))
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
            string url = $"http://localhost:{server.port}/{server.hash!}/{folder.Id}/output.mpd";
            Process.Start(expalyerPath, url);
        }
        public static void PlayAll()
        {
            string expalyerPath = Config.Instance.ExternalPlayer;
            if (String.IsNullOrEmpty(expalyerPath))
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
            string url = $"http://localhost:{server.port}/{server.hash!}/playlist.pls";
            Process.Start(expalyerPath, url);
        }
    }
}
