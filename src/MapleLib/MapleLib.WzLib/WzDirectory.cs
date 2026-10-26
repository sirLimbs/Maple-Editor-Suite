using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MapleLib.WzLib.Util;
using MapleLib.WzLib.WzStructure.Enums;

namespace MapleLib.WzLib;

public class WzDirectory : WzObject
{
	private List<WzImage> images = new List<WzImage>();

	internal List<WzDirectory> subDirs = new List<WzDirectory>();

	internal WzBinaryReader reader;

	internal long offset = 0L;

	internal string name;

	internal uint hash;

	internal int size;

	internal int checksum;

	internal int offsetSize;

	internal byte[] WzIv;

	internal WzObject parent;

	internal WzFile wzFile;

	public override WzObject Parent
	{
		get
		{
			return parent;
		}
		internal set
		{
			parent = value;
		}
	}

	public override string Name
	{
		get
		{
			return name;
		}
		set
		{
			name = value;
		}
	}

	public override WzObjectType ObjectType => WzObjectType.Directory;

	public override WzFile WzFileParent => wzFile;

	public int BlockSize
	{
		get
		{
			return size;
		}
		set
		{
			size = value;
		}
	}

	public int Checksum
	{
		get
		{
			return checksum;
		}
		set
		{
			checksum = value;
		}
	}

	public virtual List<WzImage> WzImages
	{
		get
		{
			return images;
		}
		private set
		{
		}
	}

	public virtual List<WzDirectory> WzDirectories => subDirs;

	public long Offset
	{
		get
		{
			return offset;
		}
		set
		{
			offset = value;
		}
	}

	public new virtual WzObject this[string name]
	{
		get
		{
			string text = name.ToLower();
			foreach (WzImage image in images)
			{
				if (image.Name.ToLower() == text)
				{
					return image;
				}
			}
			foreach (WzDirectory subDir in subDirs)
			{
				if (subDir.Name.ToLower() == text)
				{
					return subDir;
				}
			}
			return null;
		}
		set
		{
			if (value == null)
			{
				return;
			}
			value.Name = name;
			if (value is WzDirectory dir)
			{
				AddDirectory(dir);
				return;
			}
			if (value is WzImage img)
			{
				AddImage(img);
				return;
			}
			throw new ArgumentException("Value must be a Directory or Image");
		}
	}

	public override void Dispose()
	{
		name = null;
		reader = null;
		foreach (WzImage image in images)
		{
			image.Dispose();
		}
		foreach (WzDirectory subDir in subDirs)
		{
			subDir.Dispose();
		}
		images.Clear();
		subDirs.Clear();
		images = null;
		subDirs = null;
	}

	public WzDirectory()
	{
	}

	public WzDirectory(string dirName)
	{
		name = dirName;
	}

	public WzDirectory(string dirName, WzFile parentWzFileIvVerHashCloneSource)
	{
		name = dirName;
		hash = parentWzFileIvVerHashCloneSource.versionHash;
		WzIv = parentWzFileIvVerHashCloneSource.WzIv;
		wzFile = parentWzFileIvVerHashCloneSource;
	}

	internal WzDirectory(WzBinaryReader reader, string dirName, uint verHash, byte[] WzIv, WzFile wzFile)
	{
		this.reader = reader;
		name = dirName;
		hash = verHash;
		this.WzIv = WzIv;
		this.wzFile = wzFile;
	}

	internal void ParseDirectory(bool lazyParse = false)
	{
		long num = reader.Available();
		if (num == 0)
		{
			return;
		}
		int num2 = reader.ReadCompressedInt();
		if (num2 < 0 || num2 > 100000)
		{
			throw new Exception("Invalid wz version used for decryption, try parsing other version numbers.");
		}
		for (int i = 0; i < num2; i++)
		{
			byte b = reader.ReadByte();
			string text = null;
			long num3 = 0L;
			int blockSize;
			int num5;
			long num6;
			switch (b)
			{
			case 1:
			{
				int num7 = reader.ReadInt32();
				reader.ReadInt16();
				long num8 = reader.ReadOffset();
				continue;
			}
			case 2:
			{
				int num4 = reader.ReadInt32();
				num3 = reader.BaseStream.Position;
				reader.BaseStream.Position = reader.Header.FStart + num4;
				b = reader.ReadByte();
				text = reader.ReadString();
				Console.WriteLine("EntryCount: {0}, type: {1}, fname: {2}", num2, b, text);
				goto IL_0178;
			}
			case 3:
			case 4:
				text = reader.ReadString();
				num3 = reader.BaseStream.Position;
				goto IL_0178;
			default:
				{
					reader.PrintHexBytes(20);
					throw new Exception("[WzDirectory] Unknown directory. type = " + b);
				}
				IL_0178:
				reader.BaseStream.Position = num3;
				blockSize = reader.ReadCompressedInt();
				num5 = reader.ReadCompressedInt();
				num6 = reader.ReadOffset();
				if (b == 3)
				{
					WzDirectory item = new WzDirectory(reader, text, hash, WzIv, wzFile)
					{
						BlockSize = blockSize,
						Checksum = num5,
						Offset = num6,
						Parent = this
					};
					subDirs.Add(item);
					if (!lazyParse)
					{
						continue;
					}
				}
				else
				{
					WzImage item2 = new WzImage(text, reader, num5)
					{
						BlockSize = blockSize,
						Offset = num6,
						Parent = this
					};
					images.Add(item2);
					if (!lazyParse)
					{
						continue;
					}
				}
				break;
			}
			break;
		}
		foreach (WzDirectory subDir in subDirs)
		{
			reader.BaseStream.Position = subDir.offset;
			subDir.ParseDirectory();
		}
	}

	internal void SaveImages(BinaryWriter wzWriter, FileStream fs)
	{
		foreach (WzImage image in images)
		{
			if (image.Changed)
			{
				fs.Position = image.tempFileStart;
				byte[] buffer = new byte[image.size];
				fs.Read(buffer, 0, image.size);
				wzWriter.Write(buffer);
			}
			else
			{
				image.reader.BaseStream.Position = image.tempFileStart;
				wzWriter.Write(image.reader.ReadBytes((int)(image.tempFileEnd - image.tempFileStart)));
			}
		}
		foreach (WzDirectory subDir in subDirs)
		{
			subDir.SaveImages(wzWriter, fs);
		}
	}

	internal int GenerateDataFile(byte[] useIv, bool bIsWzUserKeyDefault, FileStream prevOpenedStream)
	{
		bool flag = useIv != null;
		size = 0;
		int num = subDirs.Count + images.Count;
		if (num == 0)
		{
			offsetSize = 1;
			return size = 0;
		}
		size = WzTool.GetCompressedIntLength(num);
		offsetSize = WzTool.GetCompressedIntLength(num);
		foreach (WzImage image in images)
		{
			image.bIsImageChanged = (image.bIsImageChanged | flag) || !bIsWzUserKeyDefault;
			if (image.bIsImageChanged)
			{
				using MemoryStream memoryStream = new MemoryStream();
				using WzBinaryWriter writer = new WzBinaryWriter(memoryStream, flag ? useIv : WzIv);
				image.SaveImage(writer, bIsWzUserKeyDefault, flag);
				image.CalculateAndSetImageChecksum(memoryStream.ToArray());
				image.tempFileStart = prevOpenedStream.Position;
				prevOpenedStream.Write(memoryStream.ToArray(), 0, (int)memoryStream.Length);
				image.tempFileEnd = prevOpenedStream.Position;
			}
			else
			{
				image.tempFileStart = image.offset;
				image.tempFileEnd = image.offset + image.size;
			}
			int wzObjectValueLength = WzTool.GetWzObjectValueLength(image.name, 4);
			size += wzObjectValueLength;
			int num2 = image.size;
			size += WzTool.GetCompressedIntLength(num2);
			size += num2;
			size += WzTool.GetCompressedIntLength(image.Checksum);
			size += 4;
			offsetSize += wzObjectValueLength;
			offsetSize += WzTool.GetCompressedIntLength(num2);
			offsetSize += WzTool.GetCompressedIntLength(image.Checksum);
			offsetSize += 4;
			if (flag || !bIsWzUserKeyDefault)
			{
				GC.Collect();
				GC.WaitForPendingFinalizers();
			}
		}
		foreach (WzDirectory subDir in subDirs)
		{
			int wzObjectValueLength2 = WzTool.GetWzObjectValueLength(subDir.name, 3);
			size += wzObjectValueLength2;
			size += subDir.GenerateDataFile(useIv, bIsWzUserKeyDefault, prevOpenedStream);
			size += WzTool.GetCompressedIntLength(subDir.size);
			size += WzTool.GetCompressedIntLength(subDir.Checksum);
			size += 4;
			offsetSize += wzObjectValueLength2;
			offsetSize += WzTool.GetCompressedIntLength(subDir.size);
			offsetSize += WzTool.GetCompressedIntLength(subDir.Checksum);
			offsetSize += 4;
		}
		return size;
	}

	internal void SaveDirectory(WzBinaryWriter writer)
	{
		offset = (uint)writer.BaseStream.Position;
		int num = subDirs.Count + images.Count;
		if (num == 0)
		{
			BlockSize = 0;
			return;
		}
		writer.WriteCompressedInt(num);
		foreach (WzImage image in images)
		{
			if (!writer.WriteWzObjectValue(image.name, WzDirectoryType.WzImage_4))
			{
			}
			writer.WriteCompressedInt(image.BlockSize);
			writer.WriteCompressedInt(image.Checksum);
			writer.WriteOffset(image.Offset);
		}
		foreach (WzDirectory subDir in subDirs)
		{
			if (!writer.WriteWzObjectValue(subDir.name, WzDirectoryType.WzDirectory_3))
			{
			}
			writer.WriteCompressedInt(subDir.BlockSize);
			writer.WriteCompressedInt(subDir.Checksum);
			writer.WriteOffset(subDir.Offset);
		}
		foreach (WzDirectory subDir2 in subDirs)
		{
			if (subDir2.BlockSize > 0)
			{
				subDir2.SaveDirectory(writer);
			}
			else
			{
				writer.Write((byte)0);
			}
		}
	}

	internal uint GetOffsets(uint curOffset)
	{
		offset = curOffset;
		curOffset += (uint)offsetSize;
		foreach (WzDirectory subDir in subDirs)
		{
			curOffset = subDir.GetOffsets(curOffset);
		}
		return curOffset;
	}

	internal uint GetImgOffsets(uint curOffset)
	{
		foreach (WzImage image in images)
		{
			image.Offset = curOffset;
			curOffset += (uint)image.BlockSize;
		}
		foreach (WzDirectory subDir in subDirs)
		{
			curOffset = subDir.GetImgOffsets(curOffset);
		}
		return curOffset;
	}

	internal void ExportXml(StreamWriter writer, bool oneFile, int level, bool isDirectory)
	{
		if (!oneFile)
		{
			return;
		}
		if (isDirectory)
		{
			writer.WriteLine(XmlUtil.Indentation(level) + XmlUtil.OpenNamedTag("WzDirectory", name, finish: true));
		}
		foreach (WzDirectory wzDirectory in WzDirectories)
		{
			wzDirectory.ExportXml(writer, oneFile, level + 1, isDirectory);
		}
		foreach (WzImage wzImage in WzImages)
		{
			wzImage.ExportXml(writer, oneFile, level + 1);
		}
		if (isDirectory)
		{
			writer.WriteLine(XmlUtil.Indentation(level) + XmlUtil.CloseTag("WzDirectory"));
		}
	}

	public void ParseImages()
	{
		foreach (WzImage image in images)
		{
			if (reader.BaseStream.Position != image.Offset)
			{
				reader.BaseStream.Position = image.Offset;
			}
			image.ParseImage();
		}
		foreach (WzDirectory subDir in subDirs)
		{
			if (reader.BaseStream.Position != subDir.Offset)
			{
				reader.BaseStream.Position = subDir.Offset;
			}
			subDir.ParseImages();
		}
	}

	internal void SetVersionHash(uint newHash)
	{
		hash = newHash;
		foreach (WzDirectory subDir in subDirs)
		{
			subDir.SetVersionHash(newHash);
		}
	}

	public void AddImage(WzImage img)
	{
		images.Add(img);
		img.Parent = this;
	}

	public void AddDirectory(WzDirectory dir)
	{
		subDirs.Add(dir);
		dir.wzFile = wzFile;
		dir.Parent = this;
	}

	public void ClearImages()
	{
		foreach (WzImage image in images)
		{
			image.Parent = null;
		}
		images.Clear();
	}

	public void ClearDirectories()
	{
		foreach (WzDirectory subDir in subDirs)
		{
			subDir.Parent = null;
		}
		subDirs.Clear();
	}

	public virtual WzImage GetImageByName(string name)
	{
		return images.FirstOrDefault((WzImage wzI) => wzI.Name.ToLower() == name.ToLower());
	}

	public virtual WzDirectory GetDirectoryByName(string name)
	{
		return subDirs.FirstOrDefault((WzDirectory dir) => dir.Name.ToLower() == name.ToLower());
	}

	public void RemoveImage(WzImage image)
	{
		images.Remove(image);
		image.Parent = null;
	}

	public void RemoveDirectory(WzDirectory dir)
	{
		subDirs.Remove(dir);
		dir.Parent = null;
	}

	public WzDirectory DeepClone()
	{
		WzDirectory wzDirectory = (WzDirectory)MemberwiseClone();
		wzDirectory.WzDirectories.Clear();
		wzDirectory.WzImages.Clear();
		foreach (WzDirectory wzDirectory2 in WzDirectories)
		{
			wzDirectory.WzDirectories.Add(wzDirectory2.DeepClone());
		}
		foreach (WzImage wzImage in WzImages)
		{
			wzDirectory.WzImages.Add(wzImage.DeepClone());
		}
		return wzDirectory;
	}

	public virtual int CountImages()
	{
		int num = images.Count;
		foreach (WzDirectory wzDirectory in WzDirectories)
		{
			num += wzDirectory.CountImages();
		}
		return num;
	}

	public override void Remove()
	{
		((WzDirectory)Parent).RemoveDirectory(this);
	}
}
