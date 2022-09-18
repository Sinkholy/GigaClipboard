using System.Collections.Specialized;
using System.Windows.Interop;

using SystemClipboard = System.Windows.Clipboard;

using Core;
using System.IO;
using System.Windows.Media.Imaging;
using static Core.IClipboard;
using Clipboard.Native;

namespace Clipboard
{
	public sealed class Clipboard : IClipboard, IDisposable
	{
		const int WM_CLIPBOARDUPDATE = 0x031D; // https://docs.microsoft.com/en-us/windows/win32/dataxchg/wm-clipboardupdate
		const int ClipboardUpdatedMessageIdentifier = WM_CLIPBOARDUPDATE;

		const int HWND_MESSAGE = -3; // https://docs.microsoft.com/en-us/windows/win32/winmsg/window-features#message-only-windows
		const int MessageOnlyWindowHandlerId = HWND_MESSAGE;

		readonly HwndSource messageOnlyWindow;
		bool isDisposed;

		public Clipboard()
		{
			isDisposed = false;

			// Создание окна-слушателя и добавление хука для обработки необходимых сообщений.
			var messageOnlyWindowConfig = new HwndSourceParameters
			{
				WindowClassStyle = default,
				ExtendedWindowStyle = default,
				WindowStyle = default,
				PositionX = default,
				PositionY = default,
				Height = default,
				Width = default,
				WindowName = string.Empty,
			};
			messageOnlyWindowConfig.ParentWindow = new IntPtr(MessageOnlyWindowHandlerId);
			messageOnlyWindow = new HwndSource(messageOnlyWindowConfig);
			messageOnlyWindow.AddHook(WindowsMessagesInterceptor);
			// ^^^
			// Есть ещё множество способов создать собственное msg-only окно
			// если надумаешь попробовать\потестировать, то материал можно найти здесь:
			// https://www.cyberforum.ru/visual-cpp/thread241794.html
			// https://docs.microsoft.com/en-us/windows/win32/winmsg/window-features#message-only-windows

			// Подписание окна-слушателя на получение необходимых сообщений.
			bool windowSubscribed = NativeMethodsWrapper.SubscribeWindowToClipboardUpdates(messageOnlyWindow.Handle);
			if (!windowSubscribed)
			{
				throw new InicializationException("Не удалось подписаться на событие обновления содержимого " +
					"системного буфера обмена.");
			}
		}

		/// <summary>
		/// Сигнализирует о том, что в буфер обмена попали новые данные.
		/// </summary>
		public event Action NewClipboardDataObtained = delegate { };

		/// <summary>
		/// Используется как перехватчик системных сообщений получаемых окном <see cref="messageOnlyWindow"/>
		/// для обработки сообщений типа <see cref="ClipboardUpdatedMessageIdentifier"/> которые сигнализируют об обновлении
		/// системного буфера обмена.
		/// </summary>
		IntPtr WindowsMessagesInterceptor(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			// TODO: необходимо подробнее разобраться в механизме перехвата сообщений.
			// https://docs.microsoft.com/en-us/windows/win32/winmsg/about-hooks
			// https://habr.com/ru/company/icl_services/blog/324718/
			if (msg == ClipboardUpdatedMessageIdentifier)
			{
				NewClipboardDataObtained();
			}

			return IntPtr.Zero;
		}

		/// <summary>
		/// Опрашивает системный буфер обмена на тип в котором хранятся данные и возвращает
		/// управляющей стороне соответствующий внутренний тип.
		/// </summary>
		/// <returns>Тип в котором данные находятся в буфере обмена.</returns>
		public DataType GetCurrentDataType()
		{
			var type = DataType.Unknown;

			// В зависимости от данных находящихся в буфере обмена установить тип данных.
			if (IsDataTextFormated())
			{
				type = DataType.Text;
			}
			else if (SystemClipboard.ContainsImage())
			{
				type = DataType.Image;
			}
			else if (SystemClipboard.ContainsFileDropList())
			{
				type = DataType.FileDrop;
			}
			else if (SystemClipboard.ContainsAudio())
			{
				type = DataType.AudioStream;
			}

			return type;

			static bool IsDataTextFormated() // TODO: небоходимо проверить последовательность форматов.
			{
				// Текстовые данные могут храниться в системном буфере обмена в разных форматах, разные форматы
				// имеют разную полноту описания.
				// Здесь данные расположены от наиболее описательного к наименее описательному.
				// https://docs.microsoft.com/en-us/windows/win32/dataxchg/clipboard-formats#multiple-clipboard-formats
				return SystemClipboard.ContainsText(System.Windows.TextDataFormat.Xaml) ||
					   SystemClipboard.ContainsText(System.Windows.TextDataFormat.CommaSeparatedValue) ||
					   SystemClipboard.ContainsText(System.Windows.TextDataFormat.Html) ||
					   SystemClipboard.ContainsText(System.Windows.TextDataFormat.Rtf) ||
					   SystemClipboard.ContainsText(System.Windows.TextDataFormat.UnicodeText) ||
					   SystemClipboard.ContainsText(System.Windows.TextDataFormat.Text);
			}
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
		public ClipboardTextData? GetText()
		{
			ClipboardTextData.TextFormats textFormat = default;
			string text = null;

			if (SystemClipboard.ContainsText(System.Windows.TextDataFormat.Xaml))
			{
				textFormat = ClipboardTextData.TextFormats.Xaml;
				text = SystemClipboard.GetText(System.Windows.TextDataFormat.Xaml);
			}
			else if (SystemClipboard.ContainsText(System.Windows.TextDataFormat.CommaSeparatedValue))
			{
				textFormat = ClipboardTextData.TextFormats.CSV;
				text = SystemClipboard.GetText(System.Windows.TextDataFormat.CommaSeparatedValue);
			}
			else if (SystemClipboard.ContainsText(System.Windows.TextDataFormat.Html))
			{
				textFormat = ClipboardTextData.TextFormats.Html;
				text = SystemClipboard.GetText(System.Windows.TextDataFormat.Html);
			}
			else if (SystemClipboard.ContainsText(System.Windows.TextDataFormat.Rtf))
			{
				textFormat = ClipboardTextData.TextFormats.RTF;
				text = SystemClipboard.GetText(System.Windows.TextDataFormat.Rtf);
			}
			else if (SystemClipboard.ContainsText(System.Windows.TextDataFormat.UnicodeText))
			{
				textFormat = ClipboardTextData.TextFormats.UnicodeText;
				text = SystemClipboard.GetText(System.Windows.TextDataFormat.UnicodeText);
			}
			else if (SystemClipboard.ContainsText(System.Windows.TextDataFormat.Text))
			{
				textFormat = ClipboardTextData.TextFormats.Text;
				text = SystemClipboard.GetText(System.Windows.TextDataFormat.Text);
			}

			return text is null
						? null
						: new ClipboardTextData(text, textFormat);
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
		public ClipboardAudioStreamData? GetAudioStream()
		{
			var stream = SystemClipboard.GetAudioStream();
			return stream is null
						  ? null
						  : new ClipboardAudioStreamData(stream, DataType.AudioStream);
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
		public ClipboardImageData? GetImage()
		{
			var image = SystemClipboard.GetImage();
			return image is null
						 ? null
						 : new ClipboardImageData(image, DataType.AudioStream);
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
		public ClipboardFilesPathesData? GetFileDrop()
		{
			var filesPathes = SystemClipboard.GetFileDropList();
			return filesPathes is null
								? null
								: new ClipboardFilesPathesData(filesPathes as IReadOnlyCollection<string>, DataType.FileDrop);

		}

		/// <summary>
		/// Помещает данные в буфер обмена в формате текста инкапсулированном в объекте типа \
		/// <see cref="ClipboardTextData"/>.
		/// </summary>
		/// <param name="data">Объект инкапсулирующий доступ к данным.</param>
		/// <exception cref="ArgumentNullException">Если <paramref name="data"/> равен null.</exception>
		public void SetText(ClipboardTextData data)
		{
			VerifyParameterIsNotNull(data, nameof(data));

			SetText(data.Data, data.TextFormat);
		}
		/// <summary>
		/// Помещает данные в буфер обмена в формате текста.
		/// </summary>
		/// <param name="value">Данные в формате текста.</param>
		/// <param name="format">Формат текста в котором данные будут помещены в буфер обмена.</param>
		/// <exception cref="ArgumentNullException">Если<paramref name="value"> равен null.</exception>
		public void SetText(string value, ClipboardTextData.TextFormats format)
		{
			VerifyParameterIsNotNull(value, nameof(value));

			var targetTextFormat = ConvertTextFormatToSystemType();
			SetTextInternal(value, targetTextFormat);

			System.Windows.TextDataFormat ConvertTextFormatToSystemType()
			{
				return format switch
				{
					ClipboardTextData.TextFormats.Xaml => System.Windows.TextDataFormat.Xaml,
					ClipboardTextData.TextFormats.CSV => System.Windows.TextDataFormat.CommaSeparatedValue,
					ClipboardTextData.TextFormats.Html => System.Windows.TextDataFormat.Html,
					ClipboardTextData.TextFormats.RTF => System.Windows.TextDataFormat.Rtf,
					ClipboardTextData.TextFormats.UnicodeText => System.Windows.TextDataFormat.UnicodeText,
					ClipboardTextData.TextFormats.Text => System.Windows.TextDataFormat.Text,
					_ => System.Windows.TextDataFormat.Text,
				};
			}
		}
		/// <summary>
		/// Инкапсулирует обращение к системному буферу обмена для формирования абстракции и
		/// возможности использовать другой подход к обращению к системному буферу без больших
		/// изменений исходного кода.
		/// </summary>
		void SetTextInternal(string value, System.Windows.TextDataFormat format)
		{
			SystemClipboard.SetText(value, format);
		}

		/// <summary>
		/// Помещает данные в буфер обмена в формате инкапсулированном в объекте типа <see cref="ClipboardAudioStreamData"/>.
		/// </summary>
		/// <param name="data">Объект представляющий данные.</param>
		/// <exception cref="ArgumentNullException">Если <paramref name="data"/> равен null.</exception>
		/// <exception cref="ArgumentException">Если данные инкапсулированные в <paramref name="data"/> равны null.</exception>
		public void SetAudio(ClipboardAudioStreamData data)
		{
			VerifyParameterIsNotNull(data, nameof(data));
			if (data.Data is null)
			{
				var errorDesc = $"Данные содержащиеся в {nameof(data)} равны null " +
					$"и не могут быть установлены в системный буфер обмена.";
				throw new ArgumentException(errorDesc, nameof(data));
			}

			SetAudioStreamInternal(data.Data);
		}
		/// <summary>
		/// Помещает данные в буфер обмена в формате аудио-потока.
		/// </summary> 
		/// <param name="audioStream">Данные в формате аудио-потока.</param>
		/// <exception cref="ArgumentNullException">Если <paramref name="audioStream"> равен null.</exception>
		public void SetAudio(Stream audioStream)
		{
			VerifyParameterIsNotNull(audioStream, nameof(audioStream));

			SetAudioStreamInternal(audioStream);
		}
		/// <summary>
		/// Инкапсулирует обращение к системному буферу обмена для формирования абстракции и
		/// возможности использовать другой подход к обращению к системному буферу без больших
		/// изменений исходного кода.
		/// </summary>
		void SetAudioStreamInternal(Stream audioStream)
		{
			SystemClipboard.SetAudio(audioStream);
		}
		/// <summary>
		/// Помещает данные в буфер обмена обмена в формате массива байтов представляющего собой аудиозапись.
		/// </summary>
		/// <param name="audioBytes">Данные в формате массива байтов представляющего собой аудиозапись.</param>
		/// <exception cref="ArgumentNullException">Если <paramref name="audioBytes"> равен null.</exception>
		public void SetAudio(byte[] audioBytes)
		{
			VerifyParameterIsNotNull(audioBytes, nameof(audioBytes));

			SetAudioBytesInternal(audioBytes);
		}
		/// <summary>
		/// Инкапсулирует обращение к системному буферу обмена для формирования абстракции и
		/// возможности использовать другой подход к обращению к системному буферу без больших
		/// изменений исходного кода.
		/// </summary>
		void SetAudioBytesInternal(byte[] audioBytes)
		{
			SystemClipboard.SetAudio(audioBytes);
		}

		/// <summary>
		/// Помещает данные в буфер обмена в формате инкапсулированном в объекте типа <see cref="ClipboardImageData"/>.
		/// </summary>
		/// <param name="data">Объект инкапсулирующий доступ к данным.</param>
		/// <exception cref="ArgumentNullException">Если <paramref name="data"/> равен null.</exception>
		public void SetImage(ClipboardImageData data)
		{
			VerifyParameterIsNotNull(data, nameof(data));

			SetImage(data.Data);
		}
		/// <summary>
		/// Помещает данные в буфер обмена в формате изображения. <see cref="ClipboardTextData"/>.
		/// </summary>
		/// <param name="image">Изображение которое необходимо установить в буфер обмена.</param>
		/// <exception cref="ArgumentNullException">Если <paramref name="image"/> равен null.</exception>
		public void SetImage(BitmapSource image)
		{
			VerifyParameterIsNotNull(image, nameof(image));

			SystemClipboard.SetImage(image);
		}

		/// <summary>
		/// Помещает данные в буфер обмена в формате инкапсулированном в объекте типа <see cref="ClipboardFilesPathesData"/>.
		/// </summary>
		/// <param name="data">Объект представляющий данные.</param>
		/// <exception cref="ArgumentNullException">Если <paramref name="data"> равен null.</exception>
		public void SetFilesPathes(ClipboardFilesPathesData data)
		{
			VerifyParameterIsNotNull(data, nameof(data));

			SetFilesPathes(data.Data);
		}
		/// <summary>
		/// Помещает данные в буфер обмена в формате набора путей к файлам. <see cref="ClipboardTextData"/>.
		/// </summary>
		/// <param name="pathes">Коллекция путей к файлам.</param>
		/// <exception cref="ArgumentNullException">Если <paramref name="pathes"/> равен null.</exception>
		public void SetFilesPathes(IReadOnlyCollection<string> pathes)
		{
			VerifyParameterIsNotNull(pathes, nameof(pathes));

			if (pathes is StringCollection specializedCollection)
			{
				SetFilesPathesInternal(specializedCollection);
			}
			else
			{
				specializedCollection = ConvertCollectionToSpecialized();
				SetFilesPathesInternal(specializedCollection);
			}

			StringCollection ConvertCollectionToSpecialized()
			{
				var specialized = new StringCollection();
				foreach (var path in pathes)
				{
					specialized.Add(path);
				}
				return specialized;
			}
		}
		/// <summary>
		/// Инкапсулирует обращение к системному буферу обмена для формирования абстракции и
		/// возможности использовать другой подход к обращению к системному буферу без больших
		/// изменений исходного кода.
		/// </summary>
		void SetFilesPathesInternal(StringCollection pathes)
		{
			SystemClipboard.SetFileDropList(pathes);
		}

		/// <summary>
		/// Опрашивает системный буфер для получения форматов в которых представленны данных хранящиеся в нём и
		/// возвращает форматы управляющей стороне.
		/// </summary>
		/// <returns>Коллекцию имён форматов в которых представленны данные в системном буфере обмена.</returns>
		/// <exception cref="ExclusiveControlException">Если не удалось получить эксклюзивный доступ к системному буферу обмена.</exception>
		IReadOnlyCollection<string> GetPresentedFormats() // TODO: Non-public
		{
			// Для опроса буфера обмена необходимо получить эксклюзивный доступ.
			var controlGranted = NativeMethodsWrapper.GetExclusiveClipboardControl(messageOnlyWindow.Handle);
			if (!controlGranted)
			{
				throw new ExclusiveControlException("Не удалось получить эксклюзивный " +
					"контроль над системным буфером обмена.");
			}
			var result = NativeMethodsWrapper.GetPresentedFormats();
			NativeMethodsWrapper.ReturnExclusiveClipboardControl();

			return result;
		}
		int GetClipboardFormatsCount()
		{
			return NativeMethodsWrapper.CountPresentedFormats();
		}
		bool IsFormatPresented(string formatName)
		{
			return SystemClipboard.ContainsData(formatName);
		}

		static void VerifyParameterIsNotNull(object param, string paramName)
		{
			if (param is null)
			{
				throw new ArgumentNullException(paramName);
			}
		}

		#region Disposing
		public void Dispose()
		{
			bool windowUnsubscribed = NativeMethodsWrapper.UnsubscribeWindowFromClipboardUpdates(messageOnlyWindow.Handle);
			if (!windowUnsubscribed)
			{
				// TODO: ну и что делать если не получилось выписать себя из списка получателей сообщения? 
				// Как корректно завершить работу приложения?
			}
			messageOnlyWindow.Dispose();
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
