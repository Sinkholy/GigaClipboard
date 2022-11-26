using System.Collections.Specialized;

using WPFClipboard = System.Windows.Clipboard;

using System.IO;
using System.Windows.Media.Imaging;
using Clipboard.Native;

using API;
using static API.IClipboard;
using System.Runtime.InteropServices;
using System.Text;
using Clipboard.Native.Memory;

namespace Clipboard
{
	public sealed class Clipboard : IClipboard, IDisposable
	{
		readonly Win32Window clipboardWindow;
		readonly Win32Clipboard systemClipboard;
		readonly Win32ClipboardListener clipboardListener;
		readonly HandleableFormats formatsToHandle;
		readonly DuplicationPreventer duplicationPreventer;

		ClipboardData? cachedData;

		public Clipboard()
		{
			clipboardWindow = new MessageOnlyWin32Window();

			systemClipboard = new Win32Clipboard(clipboardWindow);

			clipboardListener = new Win32ClipboardListener(clipboardWindow);
			clipboardListener.ClipboardUpdated += OnClipboardUpdated;

			duplicationPreventer = new DuplicationPreventer();
			SuppressDuplicates = true;

			formatsToHandle = new HandleableFormats();
			formatsToHandle.RegisterPredefinedFormat();
		}

		/// <summary>
		/// Сигнализирует о том, что в буфер обмена попали новые данные.
		/// </summary>
		public event Action NewClipboardDataObtained = delegate { };
		public bool SuppressDuplicates
		{
			get => duplicationPreventer.SuppressDuplicates;
			set => duplicationPreventer.SuppressDuplicates = value;
		}

		void OnClipboardUpdated()
		{
			if (DataWasSetByThisClipboard())
			{
				return;
			}

			uint? formatToBeHanled = null;
			foreach (var format in systemClipboard.EnumerateDataFormats())
			{
				if (formatsToHandle.IsHandleableFormat(format))
				{
					formatToBeHanled = format;
					break;
				}
			}

			bool dataCannotBeHandled = !formatToBeHanled.HasValue;
			if (dataCannotBeHandled)
			{
				return;
			}

			var dataGHandle = systemClipboard.GetClipboardData(formatToBeHanled.Value);

			var dataCopy = NativeMemoryManager.CopyUnmanagedFromGHandle(dataGHandle);

			if (duplicationPreventer.IsDuplicate(dataCopy))
			{
				return;
			}

			var clipboardData = HandleClipboardData(formatToBeHanled.Value, dataCopy);
			cachedData = clipboardData;

			NewClipboardDataObtained();



			bool DataWasSetByThisClipboard()
			{
				return NativeMethodsWrapper.IsClipboardFormatAvailable(formatsToHandle.PredefinedFormat, out _);
			}
			ClipboardData HandleClipboardData(uint format, NativeMemoryManager.NativeMemorySegment memory)
			{
				ClipboardData handledData;
				if (IsTextData())
				{
					handledData = HandleTextData();
				}
				else if (IsFileDropData())
				{
					handledData = HandleFileDropData();
				}
				else if (IsImageData())
				{
					handledData = HandleImageData();
				}
				else
				{
					handledData = null;
					// TODO: тут нужен Assert, ведь формат был предварительно проверен.
				}
				return handledData;



				bool IsTextData()
				{
					return format is HandleableFormats.ASCIIText
								  or HandleableFormats.OemText
								  or HandleableFormats.UnicodeText;
				}
				bool IsImageData()
				{
					return format is HandleableFormats.Dib
								  or HandleableFormats.DibV5;
				}
				bool IsFileDropData()
				{
					return format is HandleableFormats.FileDrop;
				}
				ClipboardData HandleTextData()
				{
					var text = format switch
					{
						HandleableFormats.UnicodeText => Encoding.Unicode.GetString(memory.AsSpan()),
						HandleableFormats.ASCIIText => Encoding.ASCII.GetString(memory.AsSpan()),
						HandleableFormats.OemText => Encoding.UTF8.GetString(memory.AsSpan())
					};

					return new ClipboardData<string>(text, DataType.Text);
				}
				ClipboardData HandleFileDropData()
				{

					var filesCount = GetFilesCount(memory.AsIntPtr());
					var fileDrop = new List<string>((int)filesCount);
					for (int i = 0; i < filesCount; i++)
					{
						var fileNameBytes = new byte[GetFileNameLength(memory.AsIntPtr(), i) + 1];
						var result = DragQueryFile(memory.AsIntPtr(), i, fileNameBytes, fileNameBytes.Length);
						var span = new ReadOnlySpan<byte>(fileNameBytes, 0, fileNameBytes.Length - 1);
						fileDrop.Add(Encoding.Default.GetString(span));
					}

					return new ClipboardData<IReadOnlyCollection<string>>(fileDrop, DataType.FileDrop);



					uint GetFileNameLength(IntPtr memPtr, int fileIndex)
					{
						return DragQueryFile(memPtr, fileIndex, null, 0);
					}
					uint GetFilesCount(IntPtr memPtr)
					{
						return DragQueryFile(memPtr, -1, null, 0);
					}					
					[DllImport("Shell32.dll", SetLastError = true)]
					static extern uint DragQueryFile(IntPtr hMem, int iFile, [Out] byte[] buffer, int bufferSize);
				}
				ClipboardData HandleImageData()
				{
					var binaryData = new BinaryData(memory.AsSpan().ToArray()); // TODO: изменить апи?
					return new ClipboardData<BinaryData>(binaryData, DataType.Image);
				}
			}
		}
		public ClipboardData? GetData()
		{
			return cachedData;
		}
		public DataType? GetDataType()
		{
			return cachedData?.DataType;
		}

		public ClipboardData<BinaryData>? GetImage()
		{
			return cachedData is ClipboardData<BinaryData> castedData
							? castedData
							: null;
		}

		public ClipboardData<string>? GetText()
		{
			return cachedData is ClipboardData<string> castedData
							? castedData
							: null;
		}

		public ClipboardData<IReadOnlyCollection<string>>? GetFileDrop()
		{
			return cachedData is ClipboardData<IReadOnlyCollection<string>> castedData
				? castedData
				: null;
		}

		public ClipboardData<Stream>? GetAudio()
		{
			return cachedData is ClipboardData<Stream> castedData
							? castedData
							: null;
		}
		#region Data setter's
		/// <summary>
		/// Инкапсулирует обращение к системному буферу обмена для формирования абстракции и
		/// возможности использовать другой подход к обращению к системному буферу без больших
		/// изменений исходного кода.
		/// </summary>
		public void SetText(string text)
		{
			VerifyParameterIsNotNull(text, nameof(text));

			var dataSet = new (uint format, GlobalHandle handle)[]
			{
				new (HandleableFormats.UnicodeText, CreateUnicodeHandle()),
				new (formatsToHandle.PredefinedFormat, NativeMemoryManager.CreateEmptyGHandle())
			};
			// Мы устанавливаем текст лишь в одном формате (Unicode) потому что
			// система автоматически конвертирует (создает synthesized format)
			// текст в несколько разных форматов. 
			// В данном случае она автоматически конвертирует Unicode текст
			// в 4 формата: 13, 16, 1, 7
			systemClipboard.SetClipboardData(dataSet);

			GlobalHandle CreateUnicodeHandle()
			{
				var textBytes = Encoding.Unicode.GetBytes(text);
				var gHandle = NativeMemoryManager.CreateGHandle((uint)(textBytes.Length));
				NativeMemoryManager.CopyToGHandle(gHandle, textBytes);

				return gHandle;
			}
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
			systemClipboard.ClearClipboard();
		}

		static void VerifyParameterIsNotNull<T>(T paramValue, string paramName)
		{
			if (paramValue is null)
			{
				throw new ArgumentNullException(paramName);
			}
		}

		#region Disposing
		bool disposed = false;
		void Dispose(bool disposing)
		{
			if (disposed)
			{
				return;
			}

			if (disposing)
			{
				clipboardListener.Dispose();
				systemClipboard.Dispose();
				clipboardWindow.Dispose();
				duplicationPreventer.Dispose();
			}

			disposed = true;
		}
		public void Dispose()
			=> Dispose(true);
		#endregion

		internal class DuplicationPreventer : IDisposable
		{
			NativeMemoryManager.NativeMemorySegment prev;

			internal bool SuppressDuplicates { get; set; }

			internal bool IsDuplicate(NativeMemoryManager.NativeMemorySegment current)
			{
				if (!SuppressDuplicates)
				{
					return false;
				}

				bool isDuplicate = prev is not null &&
									prev.AsSpan().SequenceEqual(current.AsSpan());

				if (!isDuplicate)
				{
					prev?.Dispose();
					prev = current;
				}

				return isDuplicate;
			}

			#region Disposing
			bool disposed = false;
			void Dispose(bool disposing)
			{
				if (disposed)
				{
					return;
				}

				if (disposing)
				{
					prev?.Dispose();
					prev = null;
				}

				disposed = true;
			}
			public void Dispose()
				=> Dispose(true);
			#endregion
		}
		class HandleableFormats
		{
			const string PredefinedFormatName = "GigaClipboard_PredefinedFormat";

			internal const uint Dib = 8;
			internal const uint DibV5 = 17;

			internal const uint FileDrop = 15;

			internal const uint UnicodeText = 13;
			internal const uint OemText = 7;
			internal const uint ASCIIText = 1;

			readonly uint[] formats;

			public HandleableFormats()
			{
				formats = new uint[]
				{
					Dib,
					DibV5,
					FileDrop,
					UnicodeText,
					ASCIIText,
					OemText
				};
			}

			internal void RegisterPredefinedFormat()
			{
				PredefinedFormat = NativeMethods.RegisterClipboardFormat(PredefinedFormatName);
			}
			internal bool IsHandleableFormat(uint format)
			{
				return formats.Contains(format);
			}

			internal uint PredefinedFormat { get; private set; }
		}
	}
}
