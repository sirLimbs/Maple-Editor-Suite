using System;
using System.IO;

namespace MapleLib.WzLib.Util;

public class PartialStream : Stream
{
	private Stream baseStream;

	private long offset;

	private long length;

	private bool leaveOpen;

	public Stream BaseStream => baseStream;

	public override bool CanRead => baseStream.CanRead;

	public override bool CanSeek => baseStream.CanSeek;

	public override bool CanWrite => baseStream.CanWrite;

	public override long Length => length;

	public virtual long Offset => offset;

	public override long Position
	{
		get
		{
			return baseStream.Position - offset;
		}
		set
		{
			baseStream.Position = value + offset;
		}
	}

	public override bool CanTimeout => baseStream.CanTimeout;

	public override int ReadTimeout
	{
		get
		{
			return baseStream.ReadTimeout;
		}
		set
		{
			baseStream.ReadTimeout = value;
		}
	}

	public override int WriteTimeout
	{
		get
		{
			return baseStream.WriteTimeout;
		}
		set
		{
			baseStream.WriteTimeout = value;
		}
	}

	public PartialStream(Stream baseStream, long offset, long length, bool leaveOpen = false)
	{
		if (baseStream == null)
		{
			throw new ArgumentNullException("baseStream", "BaseStream cannot be null.");
		}
		if (offset < 0)
		{
			throw new ArgumentOutOfRangeException("offset", "Offset cannot be negative.");
		}
		if (length < 0)
		{
			throw new ArgumentOutOfRangeException("offset", "Length cannot be negative.");
		}
		this.baseStream = baseStream;
		this.offset = offset;
		this.length = length;
		this.leaveOpen = leaveOpen;
	}

	public override void Flush()
	{
		baseStream.Flush();
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		long position = Position;
		if (position < 0)
		{
			return 0;
		}
		long num = length - position;
		if (num < 0)
		{
			return 0;
		}
		return baseStream.Read(buffer, offset, (int)Math.Min(count, num));
	}

	public override long Seek(long offset, SeekOrigin origin)
	{
		offset = origin switch
		{
			SeekOrigin.Begin => offset + this.offset, 
			SeekOrigin.Current => offset + Position, 
			SeekOrigin.End => offset + (this.offset + length), 
			_ => throw new ArgumentException("Unknown SeekOrigin.", "origin"), 
		};
		if (offset < this.offset)
		{
			throw new IOException("Attempt to seek front of the stream.");
		}
		return baseStream.Seek(offset, SeekOrigin.Begin) - this.offset;
	}

	public override void SetLength(long value)
	{
		throw new NotSupportedException("Cannot set the length of PartialStream.");
	}

	public override void Write(byte[] buffer, int offset, int count)
	{
		if (Position + count > length)
		{
			throw new IOException("Cannot write out of bound.");
		}
		baseStream.Write(buffer, offset, count);
	}

	public override void Close()
	{
		Dispose(disposing: true);
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && !leaveOpen)
		{
			baseStream.Dispose();
		}
	}

	public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
	{
		if (Position < 0)
		{
			throw new IOException("Cannot read out of bound.");
		}
		return baseStream.BeginRead(buffer, offset, count, callback, state);
	}

	public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
	{
		if (Position + count > length)
		{
			throw new IOException("Cannot write out of bound.");
		}
		return baseStream.BeginWrite(buffer, offset, count, callback, state);
	}

	public override int EndRead(IAsyncResult asyncResult)
	{
		return base.EndRead(asyncResult);
	}

	public override void EndWrite(IAsyncResult asyncResult)
	{
		baseStream.EndWrite(asyncResult);
	}

	public override int ReadByte()
	{
		long position = Position;
		if (position >= 0 && position < length)
		{
			return baseStream.ReadByte();
		}
		return -1;
	}

	public override void WriteByte(byte value)
	{
		if (Position >= 0 && Position < length)
		{
			baseStream.WriteByte(value);
		}
	}
}
