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
						var bitmapHeader = BitmapHelper.ExtractBitmapHeader(bitmapBinaryData);
						var fileHeader = BitmapHelper.GenerateFileHeader(bitmapHeader);
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
				ushort fileType = BitConverter.ToUInt16(dibBinaryData, 0);
				return fileType == BitmapFileHeader.BM;
			}
			internal static bool ContainsInfoHeader(byte[] dibBinaryData, bool binaryDataContainsFileHeader)
			{
				int offset = binaryDataContainsFileHeader 
							? FileHeaderSizeBytes 
							: 0;
				uint headerSize = BitConverter.ToUInt32(dibBinaryData, offset);
				return headerSize is BitmapInfoHeaderSize
								  or BitmapV5HeaderSize;
			}
			internal static BitmapFileHeader GenerateFileHeader(BitmapHeader bitmapHeader)
			{
				return new BitmapFileHeader
				{
					FileType = BitmapFileHeader.BM,
					FileSizeBytes = FileHeaderSizeBytes + bitmapHeader.HeaderSizeBytes + bitmapHeader.ImageSizeBytes,
					FileReserved1 = 0,
					FileReserved2 = 0,
					BitmapBitsOffsetInBytes = FileHeaderSizeBytes + bitmapHeader.HeaderSizeBytes + (bitmapHeader.BitmapClrUsed * 4)
				};
			}
			internal static BitmapFileHeader ExtractFileHeader(byte[] dibBinaryData)
			{
				// Здесь я использую BinaryReader т.к. нужно прочитать много
				// ПОСЛЕДОВАТЕЛЬНЫХ данных и куда проще инициализировать BinaryReader нежели
				// каждый раз выставлять оффсеты в, допустим, BitConverter.
				using (var binaryReader = new BinaryReader(new MemoryStream(dibBinaryData)))
				{
					var fileType = binaryReader.ReadUInt16();
					var fileSizeBytes = binaryReader.ReadUInt32();
					var fileReserved1 = binaryReader.ReadUInt16();
					var fileReserved2 = binaryReader.ReadUInt16();
					var bitmapBitsOffsetInBytes = binaryReader.ReadUInt32();

					return new BitmapFileHeader()
					{
						FileType = fileType,
						FileSizeBytes = fileSizeBytes,
						BitmapBitsOffsetInBytes = bitmapBitsOffsetInBytes,
						FileReserved1 = fileReserved1,
						FileReserved2 = fileReserved2
					};
				}
			}
			internal static BitmapHeader ExtractBitmapHeader(byte[] dibBinaryData)
			{
				uint headerSize = BitConverter.ToUInt32(dibBinaryData, BitmapHeader.HeaderSizeOffset);
				uint imageSize = BitConverter.ToUInt32(dibBinaryData, BitmapHeader.ImageSizeOffset);
				uint imageClrUsed = BitConverter.ToUInt32(dibBinaryData, BitmapHeader.BitmapClrUsedOffset);

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
				internal const int HeaderSizeOffset = 0;
				internal const int ImageSizeOffset = 20;
				internal const int BitmapClrUsedOffset = 32;

				[FieldOffset(HeaderSizeOffset)] public UInt32 HeaderSizeBytes;
				[FieldOffset(ImageSizeOffset)] public UInt32 ImageSizeBytes;
				[FieldOffset(BitmapClrUsedOffset)] public UInt32 BitmapClrUsed; // TODO: нейминг
			}
		}
	}
}