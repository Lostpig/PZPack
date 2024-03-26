using PZPack.Core;
using PZPack.Core.Index;
using System;
using System.IO;
using System.Net;
using System.Text;
using PZPack.View.Utils;
using System.Net.Http;

namespace PZPack.View.Service
{
    internal class PZPKHttp
    {
        readonly HttpListener listener = new();

        private string bindingHash;
        private PZReader bindingReader;
        private int port;

        public PZPKHttp(string hash, PZReader reader, int port = 42508)
        {
            this.bindingHash = hash;
            this.bindingReader = reader;
            this.port = port;
        }

        public void Start()
        {
            listener.Prefixes.Add(string.Format("http://localhost:{0}/", port));
            // listener.IgnoreWriteExceptions = true;
            listener.Start();
            WaitForRequest();
        }

        public void Stop()
        {
            listener.Stop();
        }

        public void Close()
        {
            listener.Close();
        }

        private async void WaitForRequest()
        {
            HttpListenerContext context = await listener.GetContextAsync();
            HandleRequest(context);
            WaitForRequest();
        }

        private void HandleRequest(HttpListenerContext context) 
        {
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            if (bindingHash == null || bindingReader == null)
            {
                ResponseError(response, (int)HttpStatusCode.InternalServerError, "pzpack file not opened");
                return;
            }

            string raw = request.RawUrl ?? "";
            string[] parts = raw.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 3 || parts[0] != "file") 
            {
                ResponseError(response, 403, "url valid failed");
                return;
            }
            if (parts[1] != bindingHash)
            {
                ResponseError(response, 403, "hash valid failed");
                return;
            }

            bool success = int.TryParse(parts[2], out var fileId);
            if (!success)
            {
                ResponseError(response, 403, "file id valid failed");
                return;
            }

            PZFile file;
            try
            {
                file = bindingReader.Index.GetFile(fileId);
            }
            catch
            {
                ResponseError(response, 404, "file id not found");
                return;
            }

            ResponseFile(request, response, file);
        }

        private void ResponseError(HttpListenerResponse response, int code = 500, string msg = "")
        {
            response.StatusCode = code;
            response.ContentType = "text/html; charset=utf-8";

            using Stream output = response.OutputStream;
            byte[] buffer = Encoding.UTF8.GetBytes(msg);
            output.Write(buffer, 0, buffer.Length);

            output.Close();
        }
    
        private (long, long) ParseRangeValue(string value, long total)
        {
            string[] range = value.Replace("bytes=", "").Split("-");

            long from = long.Parse(range[0]);
            long to = -1;

            if (range[1].Trim().Length > 0) _ = long.TryParse(range[1], out to);

            if (to == -1) to = total;
            else to = to + 1;

            return (from, to);
        }

        private void ResponseFile(HttpListenerRequest request, HttpListenerResponse response, PZFile file)
        {
            long from = 0;
            long to = file.OriginSize;
            bool reqRange = request.Headers.GetValues("Range") != null;

            if (reqRange)
            {
                string rangeValue = request.Headers.GetValues("Range")![0];
                (from, to) = ParseRangeValue(rangeValue, file.OriginSize);
            }

            var chunkSize = to - from;
            string mime = MimeMapping.GetMimeMapping(file.Extension);

            if (reqRange)
            {
                response.StatusCode = 206;
                response.Headers.Set("Content-Range", $"bytes ${from}-${to - 1}/${file.OriginSize}");
                response.Headers.Set("Accept-Ranges", "bytes");
                response.Headers.Set("Content-Length", chunkSize.ToString());
                response.Headers.Set("Content-Type", mime);
            }
            else
            {
                response.StatusCode = 200;
                response.Headers.Set("Content-Length", chunkSize.ToString());
                response.Headers.Set("Content-Type", mime);
            }

            var method = new HttpMethod(request.HttpMethod);
            if (method == HttpMethod.Head)
            {
                response.Close();
                return;
            }

            var offset = from;
            using var stream = bindingReader.GetFileStream(file);
            using Stream output = response.OutputStream;
            while (offset < to)
            {
                // stream.Read
            }
        }
    }
}
