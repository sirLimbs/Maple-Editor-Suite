using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using LZ4;
using MapleLib.WzLib.WzProperties;

namespace MapleLib.WzLib.Serializer;

public class WzToNxSerializer : ProgressingWzSerializer, IWzFileSerializer
{
	private sealed class DumpState
	{
		public List<WzCanvasProperty> Canvases { get; }

		public Dictionary<string, uint> Strings { get; }

		public List<WzBinaryProperty> MP3s { get; }

		public Dictionary<WzUOLProperty, Action<BinaryWriter, byte[]>> UOLs { get; }

		public Dictionary<WzObject, uint> Nodes { get; }

		public DumpState()
		{
			Canvases = new List<WzCanvasProperty>();
			Strings = new Dictionary<string, uint>(StringComparer.Ordinal) { { "", 0u } };
			MP3s = new List<WzBinaryProperty>();
			UOLs = new Dictionary<WzUOLProperty, Action<BinaryWriter, byte[]>>();
			Nodes = new Dictionary<WzObject, uint>();
		}

		public uint AddCanvas(WzCanvasProperty node)
		{
			uint count = (uint)Canvases.Count;
			Canvases.Add(node);
			return count;
		}

		public uint AddMP3(WzBinaryProperty node)
		{
			uint count = (uint)MP3s.Count;
			MP3s.Add(node);
			return count;
		}

		public uint AddString(string str)
		{
			if (Strings.ContainsKey(str))
			{
				return Strings[str];
			}
			uint count = (uint)Strings.Count;
			Strings.Add(str, count);
			return count;
		}

		public void AddNode(WzObject node)
		{
			uint count = (uint)Nodes.Count;
			Nodes.Add(node, count);
		}

		public uint GetNodeID(WzObject node)
		{
			return Nodes[node];
		}

		public uint GetNextNodeID()
		{
			return (uint)Nodes.Count;
		}

		public void AddUOL(WzUOLProperty node, long currentPosition)
		{
			UOLs.Add(node, (BinaryWriter bw, byte[] data) =>
			{
				bw.BaseStream.Position = currentPosition;
				bw.Write(data);
			});
		}
	}

	private static readonly byte[] PKG4 = new byte[4] { 80, 75, 71, 52 };

	private static readonly bool _is64bit = IntPtr.Size == 8;

	public void SerializeFile(WzFile file, string path)
	{
		string path2 = file.Name.Replace(".wz", ".nx");
		using FileStream fileStream = new FileStream(Path.Combine(Path.GetDirectoryName(path), path2), FileMode.Create, FileAccess.ReadWrite, FileShare.None);
		using BinaryWriter binaryWriter = new BinaryWriter(fileStream);
		DumpState dumpState = new DumpState();
		binaryWriter.Write(PKG4);
		binaryWriter.Write(new byte[48]);
		fileStream.EnsureMultiple(4);
		ulong position = (ulong)binaryWriter.BaseStream.Position;
		List<WzObject> nodeLevel = new List<WzObject> { file.WzDirectory };
		while (nodeLevel.Count > 0)
		{
			WriteNodeLevel(ref nodeLevel, dumpState, binaryWriter);
		}
		uint count = (uint)dumpState.Strings.Count;
		Dictionary<uint, string> dictionary = dumpState.Strings.ToDictionary((KeyValuePair<string, uint> kvp) => kvp.Value, (KeyValuePair<string, uint> kvp) => kvp.Key);
		ulong[] array = new ulong[count];
		for (uint num = 0u; num < count; num++)
		{
			fileStream.EnsureMultiple(2);
			array[num] = (ulong)binaryWriter.BaseStream.Position;
			WriteString(dictionary[num], binaryWriter);
		}
		fileStream.EnsureMultiple(8);
		ulong position2 = (ulong)binaryWriter.BaseStream.Position;
		for (uint num2 = 0u; num2 < count; num2++)
		{
			binaryWriter.Write(array[num2]);
		}
		ulong value = 0uL;
		uint num3 = 0u;
		if (true)
		{
			num3 = (uint)dumpState.Canvases.Count;
			ulong[] array2 = new ulong[num3];
			long num4 = 0L;
			foreach (WzCanvasProperty canvase in dumpState.Canvases)
			{
				fileStream.EnsureMultiple(8);
				array2[num4++] = (ulong)binaryWriter.BaseStream.Position;
				WriteBitmap(canvase, binaryWriter);
			}
			fileStream.EnsureMultiple(8);
			value = (ulong)binaryWriter.BaseStream.Position;
			for (uint num5 = 0u; num5 < num3; num5++)
			{
				binaryWriter.Write(array2[num5]);
			}
		}
		ulong value2 = 0uL;
		uint num6 = 0u;
		if (true)
		{
			num6 = (uint)dumpState.MP3s.Count;
			ulong[] array3 = new ulong[num6];
			long num7 = 0L;
			foreach (WzBinaryProperty mP in dumpState.MP3s)
			{
				fileStream.EnsureMultiple(8);
				array3[num7++] = (ulong)binaryWriter.BaseStream.Position;
				WriteMP3(mP, binaryWriter);
			}
			fileStream.EnsureMultiple(8);
			value2 = (ulong)binaryWriter.BaseStream.Position;
			for (uint num8 = 0u; num8 < num6; num8++)
			{
				binaryWriter.Write(array3[num8]);
			}
		}
		byte[] array4 = new byte[16];
		foreach (KeyValuePair<WzUOLProperty, Action<BinaryWriter, byte[]>> uOL in dumpState.UOLs)
		{
			WzObject linkValue = uOL.Key.LinkValue;
			if (linkValue != null)
			{
				binaryWriter.BaseStream.Position = (long)(position + dumpState.GetNodeID(linkValue) * 20 + 4);
				binaryWriter.BaseStream.Read(array4, 0, 16);
				uOL.Value(binaryWriter, array4);
			}
		}
		binaryWriter.Seek(4, SeekOrigin.Begin);
		binaryWriter.Write((uint)dumpState.Nodes.Count);
		binaryWriter.Write(position);
		binaryWriter.Write(count);
		binaryWriter.Write(position2);
		binaryWriter.Write(num3);
		binaryWriter.Write(value);
		binaryWriter.Write(num6);
		binaryWriter.Write(value2);
	}

	private byte[] GetCompressedBitmap(Bitmap b)
	{
		BitmapData bitmapData = b.LockBits(new Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
		int num = Math.Abs(bitmapData.Stride) * bitmapData.Height;
		byte[] array = new byte[num];
		Marshal.Copy(bitmapData.Scan0, array, 0, num);
		byte[] array2 = LZ4Codec.WrapHC(array);
		return array2.SubArray(8, array2.Length - 8);
	}

	private void WriteBitmap(WzCanvasProperty node, BinaryWriter bw)
	{
		Bitmap bitmap = node.PngProperty.GetBitmap();
		byte[] compressedBitmap = GetCompressedBitmap(bitmap);
		bw.Write((uint)compressedBitmap.Length);
		bw.Write(compressedBitmap);
	}

	private void WriteMP3(WzBinaryProperty node, BinaryWriter bw)
	{
		byte[] bytes = node.GetBytes();
		bw.Write(bytes);
	}

	private void WriteString(string s, BinaryWriter bw)
	{
		if (s.Any(char.IsControl))
		{
			Console.WriteLine("Warning; control character in string. Perhaps toggle /wzn?");
		}
		byte[] bytes = Encoding.UTF8.GetBytes(s);
		bw.Write((ushort)bytes.Length);
		bw.Write(bytes);
	}

	private void WriteNodeLevel(ref List<WzObject> nodeLevel, DumpState ds, BinaryWriter bw)
	{
		uint num = (uint)(ds.GetNextNodeID() + nodeLevel.Count);
		foreach (WzObject item in nodeLevel)
		{
			if (item is WzUOLProperty)
			{
				WriteUOL((WzUOLProperty)item, ds, bw);
			}
			else
			{
				WriteNode(item, ds, bw, num);
			}
			num += (uint)GetChildCount(item);
		}
		List<WzObject> list = new List<WzObject>();
		foreach (WzObject item2 in nodeLevel)
		{
			List<WzObject> childObjects = GetChildObjects(item2);
			list.AddRange(childObjects);
		}
		nodeLevel.Clear();
		nodeLevel = list;
	}

	private void WriteUOL(WzUOLProperty node, DumpState ds, BinaryWriter bw)
	{
		ds.AddNode(node);
		bw.Write(ds.AddString(node.Name));
		ds.AddUOL(node, bw.BaseStream.Position);
		bw.Write(0L);
		bw.Write(0L);
	}

	public List<WzObject> GetChildObjects(WzObject node)
	{
		List<WzObject> list = new List<WzObject>();
		if (node is WzDirectory)
		{
			list.AddRange(((WzDirectory)node).WzImages);
			list.AddRange(((WzDirectory)node).WzDirectories);
		}
		else if (node is WzImage)
		{
			list.AddRange(((WzImage)node).WzProperties);
		}
		else if (node is WzImageProperty && !(node is WzUOLProperty) && ((WzImageProperty)node).WzProperties != null)
		{
			list.AddRange(((WzImageProperty)node).WzProperties);
		}
		return list;
	}

	private int GetChildCount(WzObject node)
	{
		return GetChildObjects(node).Count();
	}

	private void WriteNode(WzObject node, DumpState ds, BinaryWriter bw, uint nextChildID)
	{
		ds.AddNode(node);
		bw.Write(ds.AddString(node.Name));
		bw.Write(nextChildID);
		bw.Write((ushort)GetChildCount(node));
		ushort num;
		if (node is WzDirectory || node is WzImage || node is WzSubProperty || node is WzConvexProperty || node is WzNullProperty)
		{
			num = 0;
		}
		else if (node is WzIntProperty || node is WzShortProperty || node is WzLongProperty)
		{
			num = 1;
		}
		else if (node is WzDoubleProperty || node is WzFloatProperty)
		{
			num = 2;
		}
		else if (node is WzStringProperty || node is WzLuaProperty)
		{
			num = 3;
		}
		else if (node is WzVectorProperty)
		{
			num = 4;
		}
		else if (node is WzCanvasProperty)
		{
			num = 5;
		}
		else
		{
			if (!(node is WzBinaryProperty))
			{
				throw new InvalidOperationException("Unhandled WZ node type [1]");
			}
			num = 6;
		}
		bw.Write(num);
		if (node is WzIntProperty)
		{
			bw.Write((long)((WzIntProperty)node).Value);
		}
		else if (node is WzShortProperty)
		{
			bw.Write((long)((WzShortProperty)node).Value);
		}
		else if (node is WzLongProperty)
		{
			bw.Write(((WzLongProperty)node).Value);
		}
		else if (node is WzFloatProperty)
		{
			bw.Write((double)((WzFloatProperty)node).Value);
		}
		else if (node is WzDoubleProperty)
		{
			bw.Write(((WzDoubleProperty)node).Value);
		}
		else if (node is WzStringProperty)
		{
			bw.Write(ds.AddString(((WzStringProperty)node).Value));
		}
		else if (node is WzVectorProperty)
		{
			Point pos = ((WzVectorProperty)node).Pos;
			bw.Write(pos.X);
			bw.Write(pos.Y);
		}
		else if (node is WzCanvasProperty)
		{
			WzCanvasProperty wzCanvasProperty = (WzCanvasProperty)node;
			bw.Write(ds.AddCanvas(wzCanvasProperty));
			if (true)
			{
				bw.Write((ushort)wzCanvasProperty.PngProperty.GetBitmap().Width);
				bw.Write((ushort)wzCanvasProperty.PngProperty.GetBitmap().Height);
			}
			else
			{
				bw.Write(0);
			}
		}
		else if (node is WzBinaryProperty)
		{
			WzBinaryProperty wzBinaryProperty = (WzBinaryProperty)node;
			bw.Write(ds.AddMP3(wzBinaryProperty));
			if (true)
			{
				bw.Write((uint)wzBinaryProperty.GetBytes().Length);
			}
			else
			{
				bw.Write(0);
			}
		}
		switch (num)
		{
		case 0:
			bw.Write(0L);
			break;
		case 3:
			bw.Write(0);
			break;
		}
	}
}
