using System.IO;
using System.Windows.Media.Imaging;

namespace API
{
	/// <summary>
	/// Класс реализующий этот интерфейс должен предоставлять
	/// доступ к системному буферу обмена.
	/// </summary>
	public interface IClipboard
	{
		/// <summary>
		/// Сигнализирует о том, что в буфер обмена попали новые данные.
		/// </summary>
		public event Action NewClipboardDataObtained;

		public DataType GetCurrentDataType();
		public ClipboardTextData? GetText();
		public ClipboardAudioStreamData? GetAudioStream();
		public ClipboardImageData? GetImage();
		public ClipboardFilesPathesData? GetFileDrop();

		public void SetText(ClipboardTextData data);
		public void SetText(string value, ClipboardTextData.TextFormats format);
		public void SetAudio(ClipboardAudioStreamData data);
		public void SetAudio(Stream audioStream);
		public void SetAudio(byte[] audioBytes);
		public void SetImage(ClipboardImageData data);
		public void SetImage(BitmapSource image);
		public void SetFilesPathes(ClipboardFilesPathesData data);
		public void SetFilesPathes(IReadOnlyCollection<string> pathes);

		public enum DataType
		{
			Text,
			Image,
			AudioStream,
			FileDrop,
			Unknown
		}
		public class ClipboardData<T>
		{
			public ClipboardData(T data, DataType dataType)
			{
				Data = data;
				DataType = dataType;
			}

			public T Data { get; init; }
			public DataType DataType { get; init; }
		}
		public class ClipboardTextData : ClipboardData<string>
		{
			public ClipboardTextData(string text, TextFormats format)
				: base(text, DataType.Text)
			{
				TextFormat = format;
			}

			public TextFormats TextFormat { get; init; }

			public enum TextFormats
			{
				Text,
				UnicodeText,
				RTF,
				Html,
				CSV,
				Xaml
			}
		}
		public class ClipboardAudioStreamData : ClipboardData<Stream>
		{
			public ClipboardAudioStreamData(Stream data, DataType dataType)
				: base(data, dataType)
			{
			}
		}
		public class ClipboardImageData : ClipboardData<BitmapSource>
		{
			public ClipboardImageData(BitmapSource data, DataType dataType)
				: base(data, dataType)
			{
			}
		}
		public class ClipboardFilesPathesData : ClipboardData<IReadOnlyCollection<string>>
		{
			public ClipboardFilesPathesData(IReadOnlyCollection<string> data, DataType dataType)
				: base(data, dataType)
			{
			}

			public class FileData
			{
				public FileData(string filePath, string fileExtension, string fileName)
				{
					FilePath = filePath;
					FileExtension = fileExtension;
					FileName = fileName;
				}

				public string FilePath { get; init; }
				public string FileName { get; init; }
				public string FileExtension { get; init; }
			}
		}
	}
}
