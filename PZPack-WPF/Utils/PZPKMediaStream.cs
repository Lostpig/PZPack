using System;
using System.IO;
using Unosquare.FFME.Common;
using PZPack.Core;
using PZPack.Core.Utility;
using PZPack.Core.Index;
using FFmpeg.AutoGen;
using System.Runtime.InteropServices;

namespace PZPack.View.Utils
{
    internal sealed unsafe class PZPKMediaStream: IMediaInputStream
    {
        public static string Scheme => "pzpkfile://";

        private readonly PZFileStream BackingStream;
        private readonly object ReadLock = new object();
        private readonly byte[] ReadBuffer;

        public Uri StreamUri { get; }
        public bool CanSeek { get; }
        public int ReadBufferLength => 1024 * 16;
        public InputStreamInitializing OnInitializing { get; }
        public InputStreamInitialized OnInitialized { get; }

        public PZPKMediaStream(PZFile file, PZReader reader)
        {
            StreamUri = new Uri($"{Scheme}/{file.Pid}/{file.Id}/{file.Name}");
            CanSeek = true;
            ReadBuffer = new byte[ReadBufferLength];

            BackingStream = reader.GetFileStream(file);
        }

        public void Dispose()
        {
            BackingStream?.Dispose();
        }

        public int Read(void* opaque, byte* targetBuffer, int targetBufferLength)
        {
            lock (ReadLock)
            {
                try
                {
                    var readCount = BackingStream.Read(ReadBuffer, 0, ReadBuffer.Length);
                    if (readCount > 0)
                        Marshal.Copy(ReadBuffer, 0, (IntPtr)targetBuffer, readCount);

                    return readCount;
                }
                catch (Exception)
                {
                    return ffmpeg.AVERROR_EOF;
                }
            }
        }

        public long Seek(void* opaque, long offset, int whence)
        {
            lock (ReadLock)
            {
                try
                {
                    return whence == ffmpeg.AVSEEK_SIZE ?
                        BackingStream.Length : BackingStream.Seek(offset, SeekOrigin.Begin);
                }
                catch
                {
                    return ffmpeg.AVERROR_EOF;
                }
            }
        }
    }
}
