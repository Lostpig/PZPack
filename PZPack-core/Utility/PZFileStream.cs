using PZPack.Core.Crypto;
using PZPack.Core.Index;

namespace PZPack.Core.Utility
{
    public class PZFileStream : Stream
    {
        private readonly PZFile file;
        private readonly PZCryptoBase crypto;
        private readonly StreamBlockWrapper blockWrapper;
        private readonly int blockSize;

        internal PZFileStream(PZFile file, FileStream source, PZCryptoBase crypto, int blockSize)
        {
            this.file = file;
            this.crypto = crypto;
            this.blockSize = blockSize;

            int encryptedBlockSize = 16 - (blockSize % 16) + blockSize + 16;
            blockWrapper = new(source, file.Offset, file.Size, encryptedBlockSize);
        }
        private int GetStartBlock(long offset)
        {
            return (int)(offset / blockSize);
        }

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length => file.OriginSize;
        public override long Position { get; set; } = 0;

        public override void Flush()
        {
            throw new System.NotImplementedException();
        }
        public override long Seek(long offset, SeekOrigin origin)
        {
            long movePosition = origin switch
            {
                SeekOrigin.Begin => offset,
                SeekOrigin.Current => Position + offset,
                SeekOrigin.End => Length - 1 + offset,
                _ => throw new NotSupportedException()
            };
            if (movePosition > Length - 1)
            {
                movePosition = Length - 1;
            }
            if (movePosition < 0)
            {
                movePosition = 0;
            }

            Position = movePosition;
            return Position;
        }
        public override void SetLength(long value)
        {
            throw new System.NotImplementedException();
        }
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new System.NotImplementedException();
        }

        private void ReadStream(MemoryStream dest, int count)
        {
            byte[] temp = new byte[blockWrapper.BlockSize];
            byte[] IV = new byte[16];
            long byteDecrypt = 0;
            int bytesRead;

            int blockIndex = GetStartBlock(Position);
            long posIndex = Position - blockIndex * blockSize;

            while (blockIndex < blockWrapper.Count)
            {
                bytesRead = blockWrapper.ReadBlock(blockIndex, temp);
                using MemoryStream mem = new(temp, 0, bytesRead);
                mem.Read(IV, 0, IV.Length);

                byteDecrypt += crypto.DecryptStream(mem, dest, IV);
                blockIndex++;

                if (byteDecrypt >= count) break;
            }

            dest.Seek(posIndex, SeekOrigin.Begin);
        }
        public override int Read(byte[] buffer, int offset, int count)
        {
            var destLen = ((count / blockSize) + 1) * blockSize;
            using MemoryStream dest = new(destLen);
            ReadStream(dest, count);

            var result = dest.Read(buffer, offset, count);
            Position += result;

            return result;
        }
        public override int Read(Span<byte> buffer)
        {
            var count = buffer.Length;
            var destLen = ((count / blockSize) + 1) * blockSize;
            using MemoryStream dest = new(destLen);
            ReadStream(dest, count);

            var result = dest.Read(buffer);
            Position += result;

            return result;
        }
    }
}
