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
		readonly ClipboardWindow clipboardWindow;
		bool isDisposed;

		public Clipboard()
		{
			isDisposed = false;
			clipboardWindow = new ClipboardWindow();
			clipboardWindow.NewClipboardDataObtained += NewClipboardDataObtained;

			// Подписание окна-слушателя на получение необходимых сообщений.
			var subscribed = clipboardWindow.SubscribeToClipboardUpdates();
			if (!subscribed)
			{
				var exceptionMessage = "Не удалось подписаться на уведомления обновления содержимого системного буфера обмена.";
				throw new InicializationException(exceptionMessage);
			}
		}

		/// <summary>
		/// Сигнализирует о том, что в буфер обмена попали новые данные.
		/// </summary>
		public event Action NewClipboardDataObtained = delegate { };

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

		#region Data getter's
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
		#endregion

		#region Data setter's

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
		#endregion

		/// <summary>
		/// Опрашивает системный буфер для получения форматов в которых представленны данных хранящиеся в нём и
		/// возвращает форматы управляющей стороне.
		/// </summary>
		/// <returns>Коллекцию имён форматов в которых представленны данные в системном буфере обмена.</returns>
		/// <exception cref="ExclusiveControlException">Если не удалось получить или вернуть эксклюзивный доступ к системному буферу обмена.</exception>
		IReadOnlyCollection<string> GetPresentedFormats()
		{
			// Для опроса буфера обмена необходимо получить эксклюзивный доступ.
			var exclusiveControlGranted = clipboardWindow.GetExclusiveAccess();
			if (!exclusiveControlGranted)
			{
				throw new ExclusiveControlException("Не удалось получить эксклюзивный контроль " +
					"над системным буфером обмена.");
			}

			if (!NativeMethodsWrapper.GetPresentedFormats(out var formats, out int? errorCode))
			{
				// TODO: логирование появления ошибки.
				formats = Array.Empty<string>();
			}

			var exclisiveControlReturned = clipboardWindow.ReturnExclusiveAccess();
			if (!exclisiveControlReturned)
			{
				throw new ExclusiveControlException("Не удалось вернуть эксклюзивный контроль " +
					"над системным буфером обмена.");
			}

			return formats;
		}
		int GetClipboardFormatsCount()
		{
			var formatsCounted = NativeMethodsWrapper.CountPresentedFormats(out int formatsCount, out int? errorCode);
			if (!formatsCounted)
			{
				// TODO: логировать ошибку.
			}
			return formatsCount;
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
			var unsubscribed = clipboardWindow.UnsubscribeFromClipboardUpdates();
			if (!unsubscribed)
			{
				// TODO: ну и что делать если не получилось выписать себя из списка получателей сообщения? 
				// Как корректно завершить работу приложения?
				var exceptionMessage = "Не удалось отписаться от уведомлений обновления содержимого системного буфера обмена.";
				throw new InicializationException(exceptionMessage); // CA1065 гласит о том, что нельзя выбрасывать исключения из Dispose методов.
			}

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

		class ClipboardWindow : IDisposable
		{
			const int WM_CLIPBOARDUPDATE = 0x031D; // https://docs.microsoft.com/en-us/windows/win32/dataxchg/wm-clipboardupdate
			const int ClipboardUpdatedMessageIdentifier = WM_CLIPBOARDUPDATE;

			const int HWND_MESSAGE = -3; // https://docs.microsoft.com/en-us/windows/win32/winmsg/window-features#message-only-windows
			const int MessageOnlyWindowHandlerId = HWND_MESSAGE;

			readonly HwndSource windowHandlerSource;

			internal IntPtr Handler => windowHandlerSource.Handle;

			internal ClipboardWindow()
			{
				// Создание окна-слушателя и добавление хука для обработки необходимых сообщений.
				var windowHandlerSourceConfig = new HwndSourceParameters
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
				windowHandlerSourceConfig.ParentWindow = new IntPtr(MessageOnlyWindowHandlerId);
				windowHandlerSource = new HwndSource(windowHandlerSourceConfig);
				windowHandlerSource.AddHook(WindowsMessagesInterceptor);
				// ^^^
				// Есть ещё множество способов создать собственное msg-only окно
				// если надумаешь попробовать\потестировать, то материал можно найти здесь:
				// https://www.cyberforum.ru/visual-cpp/thread241794.html
				// https://docs.microsoft.com/en-us/windows/win32/winmsg/window-features#message-only-windows
			}

			public event Action NewClipboardDataObtained = delegate { };

			internal bool SubscribeToClipboardUpdates()
			{
				const int RetryCount = 5;

				byte currentTry = 0;
				bool subscribed;
				while (true)
				{
					subscribed = NativeMethodsWrapper.SubscribeWindowToClipboardUpdates(windowHandlerSource.Handle, out int? errorCode);
					if (subscribed)
					{
						break;
					}
					else
					{
						HandleError(errorCode, out bool errorHandled);
						if (!errorHandled)
						{
							break;
						}
						currentTry++;
					}

					if (currentTry == RetryCount)
					{
						break;
					}
				}

				return subscribed;

				void HandleError(int? errorCode, out bool errorHandled)
				{
					errorHandled = false; // TODO: здесь true
					switch (errorCode)
					{
						case NativeErrorsHelper.ERROR_INVALID_PARAMETER:
							// Эта ошибка возникает при попытке повторного подписания одного и того же окна на уведомления.
							// Мне неизвестно может ли эта ошибка возникать в следствии других действий.

							// Такого происходить не должно, следовательно Assert?
							break;
						case NativeErrorsHelper.ERROR_INVALID_WINDOW_HANDLE:
						// Эта ошибка возникала при попытке подписать на уведомления несуществующее окно.
						// Мне неизвестно может ли эта ошибка возникать в следствии других действий.

						// Попытаться его пересоздать или просто уведомить об исключении?
						default:
							errorHandled = true; // TODO: здесь false.
							break;
					}
				}
			}
			internal bool UnsubscribeFromClipboardUpdates()
			{
				const int RetryCount = 5;

				byte currentTry = 0;
				bool unsubscribed;
				while (true)
				{
					unsubscribed = NativeMethodsWrapper.UnsubscribeWindowFromClipboardUpdates(windowHandlerSource.Handle, out int? errorCode);
					if (unsubscribed)
					{
						break;
					}
					else
					{
						HandleError(errorCode, out bool errorHandled);
						if (!errorHandled)
						{
							break;
						}
						currentTry++;
					}

					if (currentTry == RetryCount)
					{
						break;
					}
				}

				return unsubscribed;

				void HandleError(int? errorCode, out bool errorHandled)
				{
					errorHandled = false;
					switch (errorCode)
					{
						case NativeErrorsHelper.ERROR_INVALID_PARAMETER:
							// Эта ошибка возникала при попытке повторного отписания одного и того же окна от уведомлений.
							// Мне неизвестно может ли эта ошибка возникать в следствии других действий.

							// Такого происходить не должно, следовательно Assert?
							break;
						case NativeErrorsHelper.ERROR_INVALID_WINDOW_HANDLE:
						// Эта ошибка возникала при попытке отписать от уведомлений несуществующее окно.
						// Мне неизвестно может ли эта ошибка возникать в следствии других действий.

						// Попытаться его пересоздать или просто уведомить об исключении?
						default:
							errorHandled = true;
							break;
					}
				}
			}
			internal bool GetExclusiveAccess()
			{
				const int RetryCount = 5;

				int currentTry = 0;
				bool controlGranted;
				while (true)
				{
					controlGranted = NativeMethodsWrapper.GetExclusiveClipboardControl(Handler, out int? errorCode);
					if (controlGranted)
					{
						break;
					}
					else
					{
						HandleError(errorCode, out bool errorHandled);
						if (!errorHandled)
						{
							break;
						}
						currentTry++;
					}

					if (currentTry == RetryCount)
					{
						break;
					}
				}

				return controlGranted;

				void HandleError(int? errorCode, out bool errorHandled)
				{
					switch (errorCode)
					{
						case NativeErrorsHelper.ERROR_ACCESS_DENIED:
							// При отказе в получении контроля вероятнее всего этот самый контроль занят.
							// Просто подождём и повторим попытку.
							Thread.Sleep(100); // TODO: время ожидания.
							errorHandled = true;
							break;
						default:
							errorHandled = false;
							break;
					}
				}
			}
			internal bool ReturnExclusiveAccess()
			{
				const int RetryCount = 5;

				int currentTry = 0;
				bool controlReturned;
				while (true)
				{
					controlReturned = NativeMethodsWrapper.ReturnExclusiveClipboardControl(out int? errorCode);
					if (controlReturned)
					{
						break;
					}
					else
					{
						HandleError(errorCode, out bool errorHandled);
						if (!errorHandled)
						{
							break;
						}
						else
						{
							if (controlReturned)
							{
								break;
							}
						}
						currentTry++;
					}

					if (currentTry == RetryCount)
					{
						break;
					}
				}

				return controlReturned;

				void HandleError(int? errorCode, out bool errorHandled)
				{
					switch (errorCode) // TODO: собрать информацию о возможных ошибках.
					{
						case NativeErrorsHelper.ERROR_CLIPBOARD_NOT_OPEN:
							// Контроль не был получен, нет возможности вернуть контроль.
							// Т.к. это никак не отражается на работе приложения ограничимся
							// лишь записью в лог.
							controlReturned = true;
							errorHandled = true;
							break;
						default:
							errorHandled = false;
							break;
					}
				}
			}

			/// <summary>
			/// Используется как перехватчик системных сообщений получаемых окном <see cref="windowHandlerSource"/>
			/// для обработки сообщений типа <see cref="ClipboardUpdatedMessageIdentifier"/> которые сигнализируют об обновлении
			/// системного буфера обмена.
			/// </summary>
			IntPtr WindowsMessagesInterceptor(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
			{
				// TAI: необходимо подробнее разобраться в механизме перехвата сообщений.
				// https://docs.microsoft.com/en-us/windows/win32/winmsg/about-hooks
				// https://habr.com/ru/company/icl_services/blog/324718/
				if (msg == ClipboardUpdatedMessageIdentifier)
				{
					NewClipboardDataObtained();
				}

				return IntPtr.Zero;
			}

			public void Dispose()
			{
				windowHandlerSource.Dispose();
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