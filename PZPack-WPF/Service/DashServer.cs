using System;
using System.Net;
using System.Text;
using System.IO;
using PZPack.Core;
using System.Diagnostics;

namespace PZPack.View.Service
{
    internal class DashServer
    {
        private static DashServer? _instance;
        public static DashServer Instance
        {
            get
            {
                if (_instance == null) _instance = new DashServer();
                return _instance;
            }
        }
        public static void Close()
        {
            if (_instance != null) _instance.CloseServer();
        }

        public readonly int port = 42508;
        readonly HttpListener listener = new();
        string? bindingHash;
        PZReader? bindingReader;
        public bool Binded
        {
            get
            {
                return bindingHash != null && bindingReader != null;
            }
        }
        public string? hash { get => bindingHash; }

        public void Start()
        {
            if (listener.IsListening) return;

            listener.Prefixes.Add(string.Format("http://localhost:{0}/", port));
            listener.IgnoreWriteExceptions = true;
            listener.Start();
            listener.BeginGetContext(new AsyncCallback(GetContext), listener);
        }
        public void CloseServer()
        {
            try
            {
                listener.Stop();
                listener.Close();
            }
            catch
            {
                // DO NOTHING
            }

        }
        public void Binding(PZReader? pzr)
        {
            bindingReader = pzr;

            if (pzr != null)
            {
                string hashOrg = pzr.Source + pzr.FileCount + pzr.FolderCount + pzr.PackSize + pzr.PZType;
                string hash = PZHash.HashHex(hashOrg);
                bindingHash = hash;
                Debug.WriteLine("Dash server binding hash:" + bindingHash);
            }
            else
            {
                bindingHash = null;
            }
        }

        void GetContext(IAsyncResult ar)
        {
            if (ar.AsyncState is HttpListener httpListener)
            {
                HttpListenerContext context = httpListener.EndGetContext(ar);
                if (!(httpListener.IsListening))
                {
                    return;
                }

                httpListener.BeginGetContext(new AsyncCallback(GetContext), httpListener);

                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;

                if (bindingHash == null || bindingReader == null)
                {
                    ResponseError(response, "pzpack file not opened");
                    return;
                }

                string raw = request.RawUrl ?? "";
                string[] parts = raw.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                string hash = parts[0];

                if (parts.Length == 2 && parts[1] == "playlist.pls")
                {
                    ResponsePlaylist(response);
                    return;
                }

                if (parts.Length == 3)
                {
                    bool success = int.TryParse(parts[1], out int fid);
                    if (!success)
                    {
                        ResponseError(response, "url invalid");
                        return;
                    }

                    string filename = parts[2] == "play.mpd" ? "output.mpd" : parts[2];
                    ResponseFile(response, fid, filename);
                    return;
                }

                ResponseError(response, "url invalid");
            }
        }

        static void AddXorsHeader(HttpListenerResponse response)
        {
            response.AddHeader("access-control-allow-headers", "Origin, X-Requested-With, Content-Type, Accept, Range");
            response.AddHeader("access-control-allow-origin", "*");
        }

        static void ResponseError(HttpListenerResponse response, string message)
        {
            response.StatusCode = 500;
            response.ContentType = "text/html; charset=utf-8";

            using Stream output = response.OutputStream;
            byte[] buffer = Encoding.UTF8.GetBytes(message);
            output.Write(buffer, 0, buffer.Length);
        }
        void ResponsePlaylist(HttpListenerResponse response)
        {
            PZReader pzr = bindingReader!;
            if (pzr.PZType != PZTypes.PZVIDEO)
            {
                ResponseError(response, "opened file is not a pzvideo file");
                return;
            }

            var videos = pzr.GetFolderNode().GetChildren();
            StringBuilder listText = new();
            listText.AppendLine("[playlist]");
            int count = 0;
            foreach (var video in videos)
            {
                listText.AppendLine($"File{count + 1}=http://localhost:{port}/{bindingHash}/{video.Id}/output.mpd");
                listText.AppendLine($"Title${count + 1}={ video.Name}");
                count++;
            }
            listText.AppendLine("")
                .AppendLine($"NumberOfEntries={count}")
                .AppendLine("Version=2");

            response.StatusCode = 200;
            response.ContentType = "application/text; charset=utf-8";
            response.AddHeader("cache-control", "no-store");
            AddXorsHeader(response);

            using Stream output = response.OutputStream;
            byte[] buffer = Encoding.UTF8.GetBytes(listText.ToString());
            output.Write(buffer, 0, buffer.Length);
        }
        void ResponseFile(HttpListenerResponse response, int fid, string filename)
        {
            PZReader pzr = bindingReader!;
            var files = pzr.GetFiles(fid);
            var file = Array.Find(files, (file) =>
            {
                return file.Name == filename;
            });
            if (file == null)
            {
                ResponseError(response, "file not found");
                return;
            }

            response.StatusCode = 200;
            response.AddHeader("cache-control", "max-age=3600");
            if (filename == "output.mpd")
            {
                response.ContentType = "application/dash+xml; charset=utf-8";
            }
            else
            {
                response.ContentType = "application/octet-stream; charset=utf-8";
            }
            AddXorsHeader(response);

            try
            {
                using Stream output = response.OutputStream;
                byte[] buffer = pzr.ReadFileSync(file);
                output.Write(buffer, 0, buffer.Length);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return;
            }
        }
    }
}
