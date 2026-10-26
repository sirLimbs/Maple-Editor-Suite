using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MapleLib.Helpers;

public static class ErrorLogger
{
	private static readonly object _lock = new object();

	private static readonly List<Error> _errorList = new List<Error>();

	public static void Log(ErrorLevel level, string message)
	{
		if (string.IsNullOrWhiteSpace(message))
		{
			throw new ArgumentNullException("message", "Error message cannot be null or empty.");
		}
		lock (_lock)
		{
			_errorList.Add(new Error(level, message, DateTime.UtcNow));
		}
	}

	public static int NumberOfErrorsPresent()
	{
		return _errorList.Count;
	}

	public static bool ErrorsPresent()
	{
		return _errorList.Any();
	}

	public static void ClearErrors()
	{
		lock (_lock)
		{
			_errorList.Clear();
		}
	}

	public static void SaveToFile(string filename)
	{
		if (string.IsNullOrWhiteSpace(filename))
		{
			throw new ArgumentNullException("filename", "Filename cannot be null or empty.");
		}
		if (!ErrorsPresent())
		{
			return;
		}
		List<Error> source;
		lock (_lock)
		{
			source = new List<Error>(_errorList);
			ClearErrors();
		}
		IOrderedEnumerable<IGrouping<ErrorLevel, Error>> orderedEnumerable = from e in source
			group e by e.Level into g
			orderby g.Key
			select g;
		StringBuilder stringBuilder = new StringBuilder();
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(38, 1, stringBuilder2);
		handler.AppendLiteral("----- Start of the error log. [");
		handler.AppendFormatted(DateTime.UtcNow, "yyyy-MM-dd HH:mm:ss.fff");
		handler.AppendLiteral("] -----");
		stringBuilder3.AppendLine(ref handler);
		foreach (IGrouping<ErrorLevel, Error> item in orderedEnumerable)
		{
			stringBuilder.AppendLine();
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder4 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(15, 1, stringBuilder2);
			handler.AppendLiteral("=== ");
			handler.AppendFormatted(item.Key);
			handler.AppendLiteral(" Errors ===");
			stringBuilder4.AppendLine(ref handler);
			foreach (Error item2 in item.OrderBy((Error e) => e.Timestamp))
			{
				stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder5 = stringBuilder2;
				handler = new StringBuilder.AppendInterpolatedStringHandler(5, 2, stringBuilder2);
				handler.AppendLiteral("[");
				handler.AppendFormatted(item2.Timestamp, "HH:mm:ss.fff");
				handler.AppendLiteral("] : ");
				handler.AppendFormatted(item2.Message);
				stringBuilder5.AppendLine(ref handler);
			}
		}
		stringBuilder.AppendLine();
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder6 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(36, 1, stringBuilder2);
		handler.AppendLiteral("----- End of the error log. [");
		handler.AppendFormatted(DateTime.UtcNow, "yyyy-MM-dd HH:mm:ss.fff");
		handler.AppendLiteral("] -----");
		stringBuilder6.AppendLine(ref handler);
		stringBuilder.AppendLine();
		using StreamWriter streamWriter = new StreamWriter(File.Open(filename, FileMode.Append, FileAccess.Write, FileShare.ReadWrite));
		streamWriter.Write(stringBuilder.ToString());
	}

	public static Dictionary<ErrorLevel, List<Error>> GetErrorSnapshot()
	{
		lock (_lock)
		{
			return (from e in _errorList
				group e by e.Level).ToDictionary((IGrouping<ErrorLevel, Error> g) => g.Key, (IGrouping<ErrorLevel, Error> g) => g.ToList());
		}
	}
}
