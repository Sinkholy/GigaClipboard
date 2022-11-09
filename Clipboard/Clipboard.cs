using System.Collections.Specialized;

using WPFClipboard = System.Windows.Clipboard;

using System.IO;
using System.Windows.Media.Imaging;
using Clipboard.Native;

using API;
using static API.IClipboard;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;

namespace Clipboard
{
	public sealed class Clipboard : IClipboard, IDisposable
	{
		readonly Win32Window clipboardWindow;
		readonly Win32Clipboard systemClipboard;
		readonly Win32ClipboardListener clipboardListener;
		readonly MemoryLocker memoryLocker;
		bool isDisposed;
		ClipboardData lastObtainedData;

		public Clipboard()
		{
			isDisposed = false;
			SuppressDuplicates = true;

			clipboardWindow = new MessageOnlyWin32Window();
			systemClipboard = new Win32Clipboard(clipboardWindow);
			clipboardListener = new Win32ClipboardListener(clipboardWindow);
			clipboardListener.ClipboardUpdated += OnClipboardUpdated;
			memoryLocker = new MemoryLocker();
		}

		private void OnClipboardUpdated()
		{
			bool shouldRiseEvent = true;

			if (SuppressDuplicates)
			{
				var newClipboardData = GetData();
				var comparer = new ClipboardDataEqualityComparer();
				bool isDuplicate = comparer.Equals(lastObtainedData, newClipboardData);
				shouldRiseEvent = !isDuplicate;
				lastObtainedData = newClipboardData;
			}

			if (shouldRiseEvent)
			{
				NewClipboardDataObtained();
			}
		}
		/// <summary>
		/// Сигнализирует о том, что в буфер обмена попали новые данные.
		/// </summary>
		public event Action NewClipboardDataObtained = delegate { };
		public bool SuppressDuplicates { get; set; }

		/// <summary>
		/// Опрашивает системный буфер обмена на тип в котором хранятся данные и возвращает
		/// управляющей стороне соответствующий внутренний тип.
		/// </summary>
		/// <returns>Тип в котором данные находятся в буфере обмена.</returns>
		public DataType? GetDataType()
		{
			DataType? dataType = null;
			// В зависимости от данных находящихся в буфере обмена установить тип данных.
			if (IsDataTextFormated())
			{
				dataType = DataType.Text;
			}
			else if (WPFClipboard.ContainsImage())
			{
				dataType = DataType.Image;
			}
			else if (WPFClipboard.ContainsFileDropList())
			{
				dataType = DataType.FileDrop;
			}
			else if (WPFClipboard.ContainsAudio())
			{
				dataType = DataType.Audio;
			}

			return dataType;

			static bool IsDataTextFormated() // TODO: небоходимо проверить последовательность форматов.
			{
				// Текстовые данные могут храниться в системном буфере обмена в разных форматах, разные форматы
				// имеют разную полноту описания.
				// Здесь данные расположены от наиболее описательного к наименее описательному.
				// https://docs.microsoft.com/en-us/windows/win32/dataxchg/clipboard-formats#multiple-clipboard-formats
				return WPFClipboard.ContainsText(System.Windows.TextDataFormat.Xaml) ||
					   WPFClipboard.ContainsText(System.Windows.TextDataFormat.CommaSeparatedValue) ||
					   WPFClipboard.ContainsText(System.Windows.TextDataFormat.Html) ||
					   WPFClipboard.ContainsText(System.Windows.TextDataFormat.Rtf) ||
					   WPFClipboard.ContainsText(System.Windows.TextDataFormat.UnicodeText) ||
					   WPFClipboard.ContainsText(System.Windows.TextDataFormat.Text);
			}
		}

		#region Data getter's
		public ClipboardData? GetData()
		{
			var dataType = GetDataType();
			return dataType switch
			{
				DataType.Text => GetText(),
				DataType.Image => GetImage(),
				DataType.Audio => GetAudio(),
				DataType.FileDrop => GetFileDrop(),
				_ => null // TODO: ????
			};
		}
		/// <summary>
		/// Опрашивает буфер обмена на формат текста в котором хранятся данные затем 
		/// запрашивает данные в соответствующем формате и возвращает их управляющей стороне.
		/// </summary>
		/// <remarks>
		/// Для проверки типа данных находящихся в системному буфере обмена используйте
		/// <see cref="GetCurrentDataType"/>
		/// </remarks>
		/// <returns>
		/// Данные находящиеся в буфере обмена если они могут быть представлены в формате текста,
		/// иначе <see langword="null"/>.
		/// </returns>
		public ClipboardData<string>? GetText()
		{
			string text = null;

			if (WPFClipboard.ContainsText(System.Windows.TextDataFormat.Xaml))
			{
				text = WPFClipboard.GetText(System.Windows.TextDataFormat.Xaml);
			}
			else if (WPFClipboard.ContainsText(System.Windows.TextDataFormat.CommaSeparatedValue))
			{
				text = WPFClipboard.GetText(System.Windows.TextDataFormat.CommaSeparatedValue);
			}
			else if (WPFClipboard.ContainsText(System.Windows.TextDataFormat.Html))
			{
				text = WPFClipboard.GetText(System.Windows.TextDataFormat.Html);
			}
			else if (WPFClipboard.ContainsText(System.Windows.TextDataFormat.Rtf))
			{
				text = WPFClipboard.GetText(System.Windows.TextDataFormat.Rtf);
			}
			else if (WPFClipboard.ContainsText(System.Windows.TextDataFormat.UnicodeText))
			{
				text = WPFClipboard.GetText(System.Windows.TextDataFormat.UnicodeText);
			}
			else if (WPFClipboard.ContainsText(System.Windows.TextDataFormat.Text))
			{
				text = WPFClipboard.GetText(System.Windows.TextDataFormat.Text);
			}

			return text is not null
					? new ClipboardData<string>(text, DataType.Text)
					: null;
		}
		/// <summary>
		/// Запрашивает данные из буфера обмена в формате аудио-потока и возвращает их управляющей стороне.
		/// </summary>
		/// <remarks>
		/// Для проверки типа данных находящихся в системному буфере обмена используйте
		/// <see cref="GetCurrentDataType"/>
		/// </remarks>
		/// <returns>
		/// Данные находящиеся в буфере обмена если они могут быть представлены в формате аудио-потока,
		/// иначе <see langword="null"/>.
		/// </returns>
		public ClipboardData<Stream>? GetAudio()
		{
			return WPFClipboard.GetAudioStream() is Stream data
					? new ClipboardData<Stream>(data, DataType.Audio)
					: null;
		}
		/// <summary>
		/// Запрашивает данные из буфера обмена в формате изображения и возвращает их управляющей стороне.
		/// </summary>
		/// <remarks>
		/// Для проверки типа данных находящихся в системному буфере обмена используйте
		/// <see cref="GetCurrentDataType"/>
		/// </remarks>
		/// <returns>
		/// Данные находящиеся в буфере обмена если они могут быть представлены в формате изображения,
		/// иначе <see langword="null"/>.
		/// </returns>
		public ClipboardData<BinaryData>? GetImage()
		{
			const ushort CF_DIBV5 = 17;

			var imageFormatId = CF_DIBV5;
			byte[] imageBinaryData;
			if (systemClipboard.TryGetClipboardData(imageFormatId, out var dataPtr)
			 && memoryLocker.TryToLockMemory(dataPtr.Value, out var lockedMemory))
			{
				using (lockedMemory)
				{
					imageBinaryData = CopyBinaryFromUnmanagedMemory(lockedMemory.Pointer);
				}
			}
			else
			{
				imageBinaryData = Array.Empty<byte>();
				// TODO: логирование ошибки.
			}

			return imageBinaryData.Length != 0
											? new ClipboardData<BinaryData>(new BinaryData(imageBinaryData), DataType.Image)
											: null;

			byte[] CopyBinaryFromUnmanagedMemory(IntPtr unmanagedMemoryPointer)
			{
				if (NativeMethodsWrapper.TryToGetGlobalSize(unmanagedMemoryPointer, out var size, out var errorCode))
				{
					var buffer = new byte[size.Value];
					Marshal.Copy(unmanagedMemoryPointer, buffer, 0, (int)size.Value);
					return buffer;
				}
				else
				{
					return Array.Empty<byte>(); // TODO: затычка.
												// TODO: ошибка
				}
			}
		}
		/// <summary>
		/// Запрашивает данные из буфера обмена в формате коллекции путей к расположению файлов на дисковой системе 
		/// и возвращает их управляющей стороне.
		/// </summary>
		/// <remarks>
		/// Для проверки типа данных находящихся в системному буфере обмена используйте
		/// <see cref="GetCurrentDataType"/>
		/// </remarks>
		/// <returns>
		/// Данные находящиеся в буфере обмена если они могут быть представлены в коллекции путей к расположению файлов,
		/// иначе <see langword="null"/>.
		/// </returns>
		public ClipboardData<IReadOnlyCollection<string>>? GetFileDrop()
		{
			var rawFileDrop = WPFClipboard.GetFileDropList();
			return rawFileDrop is not null
								? new ClipboardData<IReadOnlyCollection<string>>(ConvertFromRaw(rawFileDrop), DataType.FileDrop)
								: null;

			static IReadOnlyCollection<string> ConvertFromRaw(StringCollection rawFileDrop)
			{
				var converted = new List<string>(rawFileDrop.Count);
				foreach (var file in rawFileDrop)
				{
					converted.Add(file);
				}

				return converted;
			}
		}
		#endregion

		#region Data setter's
		/// <summary>
		/// Инкапсулирует обращение к системному буферу обмена для формирования абстракции и
		/// возможности использовать другой подход к обращению к системному буферу без больших
		/// изменений исходного кода.
		/// </summary>
		public void SetText(string text)
		{
			VerifyParameterIsNotNull(text, nameof(text));

			WPFClipboard.SetText(text, System.Windows.TextDataFormat.UnicodeText);
		}

		/// <summary>
		/// Инкапсулирует обращение к системному буферу обмена для формирования абстракции и
		/// возможности использовать другой подход к обращению к системному буферу без больших
		/// изменений исходного кода.
		/// </summary>
		public void SetAudio(Stream audioStream)
		{
			VerifyParameterIsNotNull(audioStream, nameof(audioStream));

			WPFClipboard.SetAudio(audioStream);
		}

		/// <summary>
		/// Помещает данные в буфер обмена в формате изображения. <see cref="ClipboardTextData"/>.
		/// </summary>
		/// <param name="image">Изображение которое необходимо установить в буфер обмена.</param>
		public void SetImage(BinaryData imageData)
		{
			VerifyParameterIsNotNull(imageData, nameof(imageData));

			WPFClipboard.SetImage(ConvertBinaryDataToBitmapSource(imageData)); // TODO: заменить заменить на низкоуровневый вызов.

			static BitmapSource ConvertBinaryDataToBitmapSource(BinaryData binaryData)
			{
				var stream = binaryData.GetStream();
				return BitmapFrame.Create(stream);
			}
		}

		/// <summary>
		/// Инкапсулирует обращение к системному буферу обмена для формирования абстракции и
		/// возможности использовать другой подход к обращению к системному буферу без больших
		/// изменений исходного кода.
		/// </summary>
		public void SetFileDrop(IReadOnlyCollection<string> pathes)
		{
			VerifyParameterIsNotNull(pathes, nameof(pathes));

			WPFClipboard.SetFileDropList(ConvertCollectionToSpecialized(pathes)); // TODO: заменить на низкоуровневый вызов.

			static StringCollection ConvertCollectionToSpecialized(IEnumerable<string> enumerable)
			{
				var specialized = new StringCollection();
				foreach (var path in enumerable)
				{
					specialized.Add(path);
				}
				return specialized;
			}
		}
		#endregion

		public void ClearClipboard()
		{
			systemClipboard.TryClearClipboard();
		}

		static void VerifyParameterIsNotNull<T>(T paramValue, string paramName)
		{
			if (paramValue is null)
			{
				throw new ArgumentNullException(paramName);
			}
		}

		#region Disposing
		public void Dispose() // TODO: разобраться с dispose-паттерном в C#.
		{
			clipboardListener.Dispose();
			systemClipboard.Dispose();
			clipboardWindow.Dispose();
			GC.SuppressFinalize(this);
			isDisposed = true;
		}
		~Clipboard()
		{
			if (isDisposed)
			{
				return;
			}
			Dispose();
		}
		#endregion


		class ClipboardDataEqualityComparer : IEqualityComparer<ClipboardData>
		{
			public bool Equals(ClipboardData? x, ClipboardData? y)
			{
				if (x is null || y is null)
				{
					return false;
				}

				if (x.DataType != y.DataType)
				{
					return false;
				}
				bool dataEqual = true;

				switch (x.DataType)
				{
					case DataType.Text:
						if (x is ClipboardData<string> xText &&
							y is ClipboardData<string> yText)
						{
							dataEqual = CompareText(xText, yText);
						}
						else
						{
							// TODO: стоит ли здесь каким-то образом уведомлять о том, что данные не могут быть преобразованы?
							dataEqual = false;
						}
						break;
					case DataType.Image:
						if (x is ClipboardData<BinaryData> xBinary &&
							y is ClipboardData<BinaryData> yBinary)
						{
							dataEqual = CompareImages(xBinary, yBinary);
						}
						else
						{
							// TODO: стоит ли здесь каким-то образом уведомлять о том, что данные не могут быть преобразованы?
							dataEqual = false;
						}
						break;
					case DataType.Audio:
						if (x is ClipboardData<Stream> xAudio &&
							y is ClipboardData<Stream> yAudio)
						{
							dataEqual = CompareAudio(xAudio, yAudio);
						}
						else
						{
							// TODO: стоит ли здесь каким-то образом уведомлять о том, что данные не могут быть преобразованы?
							dataEqual = false;
						}
						break;
					case DataType.FileDrop:
						if (x is ClipboardData<IReadOnlyCollection<string>> xFileDrop &&
							y is ClipboardData<IReadOnlyCollection<string>> yFileDrop)
						{
							dataEqual = CompareFileDrop(xFileDrop, yFileDrop);
						}
						else
						{
							// TODO: стоит ли здесь каким-то образом уведомлять о том, что данные не могут быть преобразованы?
							dataEqual = false;
						}
						break;
				}

				return dataEqual;
			}

			public int GetHashCode([DisallowNull] ClipboardData obj)
			{
				return obj.GetHashCode();
			}

			static bool CompareImages(ClipboardData<BinaryData> a, ClipboardData<BinaryData> b)
			{
				var aBinaryArray = a.Data.GetBytes();
				var bBinaryArray = b.Data.GetBytes();

				// TODO: возможно нужна проверка на пустые массивы?

				if (aBinaryArray.Length != bBinaryArray.Length)
				{
					return false;
				}

				bool distinctionFound = false;
				for (int i = 0; i < aBinaryArray.Length; i++)
				{
					distinctionFound = aBinaryArray[i] != bBinaryArray[i];

					if (distinctionFound)
					{
						break;
					}
				}
				return !distinctionFound;
			}
			static bool CompareFileDrop(ClipboardData<IReadOnlyCollection<string>> a,
										ClipboardData<IReadOnlyCollection<string>> b)
			{
				var aCollection = a.Data;
				var bCollection = b.Data;

				if (aCollection.Count != bCollection.Count)
				{
					return false;
				}

				bool distinctionFound = false;
				for (int i = 0; i < aCollection.Count; i++)
				{
					var aElement = aCollection.ElementAt(i);
					var bElement = bCollection.ElementAt(i);

					distinctionFound = !string.Equals(aElement, bElement, StringComparison.Ordinal);

					if (distinctionFound)
					{
						break;
					}
				}

				return !distinctionFound;
			}
			static bool CompareText(ClipboardData<string> a, ClipboardData<string> b)
			{
				var aText = a.Data;
				var bText = b.Data;

				return string.Equals(aText, bText, StringComparison.Ordinal);
			}
			static bool CompareAudio(ClipboardData<Stream> a, ClipboardData<Stream> b)
			{
				const int bufferSize = 128;

				var aStream = a.Data;
				var bStream = b.Data;

				if (aStream.Length != bStream.Length)
				{
					return false;
				}

				bool distinctionFound = false;

				var aBuffer = new byte[bufferSize];
				var bBuffer = new byte[bufferSize];
				while (aStream.Read(aBuffer) > 0)
				{
					bStream.Read(bBuffer);
					distinctionFound = !aBuffer.SequenceEqual(bBuffer);

					if (distinctionFound)
					{
						break;
					}
				}
				aStream.Seek(0, SeekOrigin.Begin);
				bStream.Seek(0, SeekOrigin.Begin);

				return !distinctionFound;
			}
		}
		#region Exceptions
		public abstract class ClipboardException : Exception
		{
			protected ClipboardException(string message)
				: base(message)
			{ }
		}
		public class ExclusiveControlException : ClipboardException
		{
			public ExclusiveControlException(string message)
				: base(message)
			{ }
		}
		public class InicializationException : ClipboardException
		{
			public InicializationException(string message)
				: base(message)
			{ }
		}
		#endregion
	}
}