using System;

namespace MapleLib.Helpers;

public class Error
{
	public ErrorLevel Level { get; }

	public string Message { get; }

	public DateTime Timestamp { get; }

	internal Error(ErrorLevel level, string message, DateTime timestamp)
	{
		Level = level;
		Message = message;
		Timestamp = timestamp;
	}

	public override string ToString()
	{
		return $"[{Level}] [{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] : {Message}";
	}
}
