using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;

using static API.IClipboard;

namespace CipboardDataExtensions
{
	public static class ClipboardDataConvertersExtension
	{
		public static bool TryConvertToBitmapImage(this ClipboardData clipboardData, out BitmapImage? image)
		{
			image = null;

			if (IsSuitableClipboardData(out var bitmapBinaryData))
			{
				var containsFileHeader = BitmapHelper.ContainsFileHeader(bitmapBinaryData);

				var suitableBinaryData = BitmapHelper.ContainsInfoHeader(bitmapBinaryData, containsFileHeader);
				if (suitableBinaryData)
				{
					var needToGenerateFileHeader = !containsFileHeader;
					if (needToGenerateFileHeader)
					{
						var fileHeader = BitmapHelper.GenerateFileHeader(bitmapBinaryData);
						bitmapBinaryData = BitmapHelper.AppendFileHeaderToDIB(bitmapBinaryData, fileHeader);
					}

					image = TryToCreateImage(bitmapBinaryData);
				}
			}

			return image is not null;

			bool IsSuitableClipboardData(out byte[]? bitmapBinaryData)
			{
				bool isSutableData = false;
				bitmapBinaryData = null;

				var isSutableRecordType = clipboardData.DataType is DataType.Image && clipboardData is ClipboardData<BinaryData>;
				if (isSutableRecordType)
				{
					bitmapBinaryData = (clipboardData as ClipboardData<BinaryData>).Data.GetBytes();
					if (bitmapBinaryData.Length is not 0)
					{
						isSutableData = true;
					}
				}

				return isSutableData;
			}
			BitmapImage? TryToCreateImage(byte[] bitmapFile)
			{
				var image = new BitmapImage();

				try
				{
					image.BeginInit();
					image.StreamSource = new MemoryStream(bitmapFile);
					image.EndInit();
				}
				catch (NotSupportedException ex)
				{
					// По какой-то причине не удалось создать изображение.
					image = null;
				}

				return image;
			}
		}

		internal static class BitmapHelper
		{
			const byte BitmapInfoHeaderSize = 40;
			const byte BitmapV5HeaderSize = 124;
			const byte FileHeaderSizeBytes = 14;

			internal static bool ContainsFileHeader(byte[] dibBinaryData)
			{
				// Структуру BitmapFileHeader можно определить по
				// значению первого UInt16 поля bfType (BitmapFileHeader.FileType)
				// UInt16 = 16 бит = 2 байта.
				// Следовательно мы читаем первые два байта битовой последовательности
				// Преобразуем их в UInt16 и сверяемся с константным числом идентифицирующим 
				// структуру BitmapFileHeader.
				ushort fileType = BitConverter.ToUInt16(new ReadOnlySpan<byte>(dibBinaryData, 0, 2));
				return fileType == BitmapFileHeader.BM;
			}
			internal static bool ContainsInfoHeader(byte[] dibBinaryData, bool binaryDataContainsFileHeader)
			{
				// TODO: распихай всё по константам.
				var span = binaryDataContainsFileHeader
						 ? new ReadOnlySpan<byte>(dibBinaryData, FileHeaderSizeBytes, 4)
						 : new ReadOnlySpan<byte>(dibBinaryData, 0, 4);

				uint headerSize = BitConverter.ToUInt32(span);
				return headerSize is BitmapInfoHeaderSize
								  or BitmapV5HeaderSize;
			}
			internal static BitmapFileHeader GenerateFileHeader(byte[] dibBinaryData)
			{
				if (ContainsFileHeader(dibBinaryData))
				{
					return ExtractFileHeader(dibBinaryData);
				}
				else
				{
					var bitmapHeader = ExtractBitmapHeader(dibBinaryData, false);
					var fileSize = FileHeaderSizeBytes + bitmapHeader.HeaderSizeBytes + bitmapHeader.ImageSizeBytes;
					var imageOffsetInFile = FileHeaderSizeBytes + bitmapHeader.HeaderSizeBytes + (bitmapHeader.BitmapClrUsed * 4);

					return new BitmapFileHeader
					{
						FileType = BitmapFileHeader.BM,
						FileSizeBytes = fileSize,
						FileReserved1 = 0,
						FileReserved2 = 0,
						BitmapBitsOffsetInBytes = imageOffsetInFile
					};
				}
			}
			internal static BitmapFileHeader ExtractFileHeader(byte[] dibBinaryData)
			{
				return ExtractFileHeader(new MemoryStream(dibBinaryData));
			}
			internal static BitmapFileHeader ExtractFileHeader(MemoryStream dibBinaryStream)
			{
				UInt16 fileType;
				UInt32 fileSizeBytes;
				UInt16 fileReserved1;
				UInt16 fileReserved2;
				UInt32 bitmapBitsOffsetInBytes;

				using (var binaryReader = new BinaryReader(dibBinaryStream))
				{
					fileType = binaryReader.ReadUInt16();
					fileSizeBytes = binaryReader.ReadUInt32();
					fileReserved1 = binaryReader.ReadUInt16();
					fileReserved2 = binaryReader.ReadUInt16();
					bitmapBitsOffsetInBytes = binaryReader.ReadUInt32();
				}

				return new BitmapFileHeader()
				{
					FileType = fileType,
					FileSizeBytes = fileSizeBytes,
					BitmapBitsOffsetInBytes = bitmapBitsOffsetInBytes,
					FileReserved1 = fileReserved1,
					FileReserved2 = fileReserved2
				};
			}
			internal static BitmapHeader ExtractBitmapHeader(byte[] dibBinaryData, bool containsFileHeader)
			{
				return ExtractBitmapHeader(new MemoryStream(dibBinaryData), containsFileHeader);
			}
			internal static BitmapHeader ExtractBitmapHeader(MemoryStream dibBinaryStream, bool containsFileHeader)
			{
				const long ImageSizeOffset = 20;
				const long ImageClrUsedOffset = 32;

				// TODO: в этом методе требуется либо дописать детальный комментарий
				// либо переделать всё с использованием BitConverter
				uint headerSize;
				uint imageSize;
				uint imageClrUsed;

				using (var binaryReader = new BinaryReader(dibBinaryStream))
				{
					if (containsFileHeader)
					{
						binaryReader.BaseStream.Position = FileHeaderSizeBytes;
					}

					headerSize = binaryReader.ReadUInt32();

					binaryReader.BaseStream.Position += ImageSizeOffset;
					imageSize = binaryReader.ReadUInt32();

					binaryReader.BaseStream.Position += ImageClrUsedOffset;
					imageClrUsed = binaryReader.ReadUInt32();
				}

				return new BitmapHeader()
				{
					HeaderSizeBytes = headerSize,
					ImageSizeBytes = imageSize,
					BitmapClrUsed = imageClrUsed
				};
			}
			internal static byte[] AppendFileHeaderToDIB(byte[] dibBinaryData, BitmapFileHeader fileHeader)
			{
				// TODO: я бы дополнил этот метод комментарием.
				// TODO: Как мне кажется в этом методе вполне можно избежать второго копирования.
				byte[] bitmapFileBinary = new byte[dibBinaryData.Length + FileHeaderSizeBytes];
				var fileHeaderBytes = ConvertFileHeaderToBinaryData(fileHeader);
				Buffer.BlockCopy(fileHeaderBytes, 0, bitmapFileBinary, 0, fileHeaderBytes.Length);
				Buffer.BlockCopy(dibBinaryData, 0, bitmapFileBinary, FileHeaderSizeBytes, dibBinaryData.Length);
				return bitmapFileBinary;
			}
			static byte[] ConvertFileHeaderToBinaryData(BitmapFileHeader header)
			{
				var binaryDataStream = new MemoryStream(FileHeaderSizeBytes);
				using (var bw = new BinaryWriter(binaryDataStream))
				{
					bw.Write(header.FileType);
					bw.Write(header.FileSizeBytes);
					bw.Write(header.FileReserved1);
					bw.Write(header.FileReserved2);
					bw.Write(header.BitmapBitsOffsetInBytes);
				}

				return binaryDataStream.ToArray();
			}

			[StructLayout(LayoutKind.Sequential, Pack = 2)]
			internal struct BitmapFileHeader
			{
				public const UInt16 BM = 0x4d42; // TODO: описание

				public UInt16 FileType;
				public UInt32 FileSizeBytes;
				public UInt16 FileReserved1;
				public UInt16 FileReserved2;
				public UInt32 BitmapBitsOffsetInBytes; // TODO: нейминг получше?	
			}
			/// <summary>
			/// Я выяснил, что все заголовки имеют одинаковое смещение для нужных 
			/// </summary>
			[StructLayout(LayoutKind.Explicit)]
			internal struct BitmapHeader
			{
				[FieldOffset(0)] public UInt32 HeaderSizeBytes;
				[FieldOffset(20)] public UInt32 ImageSizeBytes;
				[FieldOffset(32)] public UInt32 BitmapClrUsed; // TODO: нейминг
			}
		}
	}
}