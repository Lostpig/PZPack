using System;

namespace PZPack.Core.Utility;

internal sealed class ProgressStream : Stream
{
    private readonly Stream _innerStream;
    private readonly IProgress<(long, long)>? _progress;
    private readonly long _length;
    private long _position;
    private long _startPosition;
    private long _endPosition;

    public Stream InnerStream { get => _innerStream; }

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => _length;
    public override long Position
    {
        get { return _position; }
        set { throw new System.NotImplementedException(); }
    }

    public ProgressStream(Stream innerStream, long startPosition, long length, IProgress<(long, long)>? progress = default)
    {
        _innerStream = innerStream;
        _progress = progress;
        _length = length;
        _startPosition = startPosition;
        _endPosition = _startPosition + _length;
        _position = 0;
        innerStream.Seek(startPosition, SeekOrigin.Begin);
    }
    public override void Flush()
    {
        throw new System.NotImplementedException();
    }
    public override long Seek(long offset, SeekOrigin origin)
    {
        long movePosition = origin switch
        {
            SeekOrigin.Begin => _startPosition + offset,
            SeekOrigin.Current => _innerStream.Position + offset,
            SeekOrigin.End => _endPosition + offset,
            _ => throw new NotSupportedException()
        };
        if (movePosition > _endPosition)
        {
            movePosition = _endPosition;
        }
        if (movePosition < _startPosition)
        {
            movePosition = _startPosition;
        }

        _innerStream.Seek(movePosition, SeekOrigin.Begin);
        _position = movePosition - _startPosition;

        return _position;
    }
    public override void SetLength(long value)
    {
        throw new System.NotImplementedException();
    }
    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new System.NotImplementedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        int n = _innerStream.Read(buffer, offset, count);
        if (_position + n > _length)
        {
            n = (int)(_length - _position);
        }

        _position += n;
        _progress?.Report((_position, _length));
        return n;
    }
    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        int n = await _innerStream.ReadAsync(buffer.AsMemory(offset, count), cancellationToken);
        if (_position + n > _length)
        {
            n = (int)(_length - _position);
        }

        _position += n;
        _progress?.Report((_position, _length));
        return n;
    }
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
    {
        int n = await _innerStream.ReadAsync(buffer, cancellationToken);
        if (_position + n > _length)
        {
            n = (int)(_length - _position);
        }

        _position += n;
        _progress?.Report((_position, _length));
        return n;
    }


}