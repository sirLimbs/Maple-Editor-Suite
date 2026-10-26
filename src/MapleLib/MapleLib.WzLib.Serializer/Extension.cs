using System;
using System.IO;

namespace MapleLib.WzLib.Serializer;

internal static class Extension
{
	public static void EnsureMultiple(this Stream s, int multiple)
	{
		int num = (int)(multiple - s.Position % multiple);
		if (num != multiple)
		{
			s.Write(new byte[num], 0, num);
		}
	}

	public static T[] SubArray<T>(this T[] array, int offset, int length)
	{
		T[] array2 = new T[length];
		Array.Copy(array, offset, array2, 0, length);
		return array2;
	}
}
