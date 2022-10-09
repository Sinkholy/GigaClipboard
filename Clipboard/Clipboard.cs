using System.Collections.Specialized;
using System.Windows.Interop;

using SystemClipboard = System.Windows.Clipboard;

using System.IO;
using System.Windows.Media.Imaging;
using Clipboard.Native;

using API;
using static API.IClipboard;

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
			var subscribed = clipboardWindow.SubscribeToClipboardUpdates(out var errors);
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
		public DataType? GetDataType()
		{
			DataType? dataType = null;
			// В зависимости от данных находящихся в буфере обмена установить тип данных.
			if (IsDataTextFormated())
			{
				dataType = DataType.Text;
			}
			else if (SystemClipboard.ContainsImage())
			{
				dataType = DataType.Image;
			}
			else if (SystemClipboard.ContainsFileDropList())
			{
				dataType = DataType.FileDrop;
			}
			else if (SystemClipboard.ContainsAudio())
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
				return SystemClipboard.ContainsText(System.Windows.TextDataFormat.Xaml) ||
					   SystemClipboard.ContainsText(System.Windows.TextDataFormat.CommaSeparatedValue) ||
					   SystemClipboard.ContainsText(System.Windows.TextDataFormat.Html) ||
					   SystemClipboard.ContainsText(System.Windows.TextDataFormat.Rtf) ||
					   SystemClipboard.ContainsText(System.Windows.TextDataFormat.UnicodeText) ||
					   SystemClipboard.ContainsText(System.Windows.TextDataFormat.Text);
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

			ClipboardData<string>? GetText()
			{
				var text = this.GetText();
				return text is null
							? null
							: new ClipboardData<string>(text, DataType.Text);
			}
			ClipboardData<BitmapSource>? GetImage()
			{
				var image = this.GetImage();
				return image is null
							? null
							: new ClipboardData<BitmapSource>(image, DataType.Image);
			}
			ClipboardData<Stream>? GetAudio()
			{
				var audioStream = this.GetAudio();
				return audioStream is null
									? null
									: new ClipboardData<Stream>(audioStream, DataType.Audio);
			}
			ClipboardData<StringCollection>? GetFileDrop()
			{
				var fileDrop = this.GetFileDrop();
				return fileDrop is null
								? null
								: new ClipboardData<StringCollection>(fileDrop, DataType.FileDrop);
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
		string? GetText()
		{
			string text = null;

			if (SystemClipboard.ContainsText(System.Windows.TextDataFormat.Xaml))
			{
				text = SystemClipboard.GetText(System.Windows.TextDataFormat.Xaml);
			}
			else if (SystemClipboard.ContainsText(System.Windows.TextDataFormat.CommaSeparatedValue))
			{
				text = SystemClipboard.GetText(System.Windows.TextDataFormat.CommaSeparatedValue);
			}
			else if (SystemClipboard.ContainsText(System.Windows.TextDataFormat.Html))
			{
				text = SystemClipboard.GetText(System.Windows.TextDataFormat.Html);
			}
			else if (SystemClipboard.ContainsText(System.Windows.TextDataFormat.Rtf))
			{
				text = SystemClipboard.GetText(System.Windows.TextDataFormat.Rtf);
			}
			else if (SystemClipboard.ContainsText(System.Windows.TextDataFormat.UnicodeText))
			{
				text = SystemClipboard.GetText(System.Windows.TextDataFormat.UnicodeText);
			}
			else if (SystemClipboard.ContainsText(System.Windows.TextDataFormat.Text))
			{
				text = SystemClipboard.GetText(System.Windows.TextDataFormat.Text);
			}

			return text;
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
		Stream? GetAudio()
		{
			return SystemClipboard.GetAudioStream();
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
		BitmapSource? GetImage()
		{
			return SystemClipboard.GetImage();
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
		StringCollection? GetFileDrop()
		{
			return SystemClipboard.GetFileDropList();
		}
		#endregion

		#region Data setter's
		public void SetData(object data, DataType dataType)
		{
			const string DataDoesNotMatchTypeExceptionMessage = $"Данные в параметре {nameof(data)}, не могут быть преобразованы к соответствующему типу переданому в параметре {nameof(dataType)}";

			VerifyParameterIsNotNull(data, nameof(data));

			switch (dataType)
			{
				case DataType.Text:
					SetText();
					break;
				case DataType.Image:
					SetImage();
					break;
				case DataType.Audio:
					SetAudio();
					break;
				case DataType.FileDrop:
					SetFileDrop();
					break;
				default: // TODO: что делать в default случае?
					break;
			}

			void SetText()
			{
				if (data is string text)
				{
					this.SetText(text, System.Windows.TextDataFormat.UnicodeText);
				}
				else
				{
					throw new ArgumentException(DataDoesNotMatchTypeExceptionMessage, nameof(data)); 
				}
			}
			void SetImage()
			{
				if (data is BitmapSource image)
				{
					this.SetImage(image);
				}
				else
				{
					throw new ArgumentException(DataDoesNotMatchTypeExceptionMessage, nameof(data));
				}
			}
			void SetAudio()
			{
				if (data is byte[] audioBytes)
				{
					this.SetAudio(audioBytes);
				}
				else if (data is Stream audioStream)
				{
					this.SetAudio(audioStream);
				}
				else
				{
					throw new ArgumentException(DataDoesNotMatchTypeExceptionMessage, nameof(data));
				}
			}
			void SetFileDrop()
			{
				if (data is StringCollection stringCollection)
				{
					this.SetFileDrop(stringCollection);
				}
				else if (data is IEnumerable<string> enumerable)
				{
					this.SetFileDrop(ConvertCollectionToSpecialized(enumerable));
				}
				else
				{
					throw new ArgumentException(DataDoesNotMatchTypeExceptionMessage, nameof(data)); 
				}

				StringCollection ConvertCollectionToSpecialized(IEnumerable<string> enumerable)
				{
					var specialized = new StringCollection();
					foreach (var path in enumerable)
					{
						specialized.Add(path);
					}
					return specialized;
				}
			}
		}

		/// <summary>
		/// Инкапсулирует обращение к системному буферу обмена для формирования абстракции и
		/// возможности использовать другой подход к обращению к системному буферу без больших
		/// изменений исходного кода.
		/// </summary>
		void SetText(string value, System.Windows.TextDataFormat format)
		{
			SystemClipboard.SetText(value, format);
		}

		/// <summary>
		/// Инкапсулирует обращение к системному буферу обмена для формирования абстракции и
		/// возможности использовать другой подход к обращению к системному буферу без больших
		/// изменений исходного кода.
		/// </summary>
		void SetAudio(Stream audioStream)
		{
			SystemClipboard.SetAudio(audioStream);
		}
		/// <summary>
		/// Инкапсулирует обращение к системному буферу обмена для формирования абстракции и
		/// возможности использовать другой подход к обращению к системному буферу без больших
		/// изменений исходного кода.
		/// </summary>
		void SetAudio(byte[] audioBytes)
		{
			SystemClipboard.SetAudio(audioBytes);
		}

		/// <summary>
		/// Помещает данные в буфер обмена в формате изображения. <see cref="ClipboardTextData"/>.
		/// </summary>
		/// <param name="image">Изображение которое необходимо установить в буфер обмена.</param>
		void SetImage(BitmapSource image)
		{
			SystemClipboard.SetImage(image);
		}

		/// <summary>
		/// Инкапсулирует обращение к системному буферу обмена для формирования абстракции и
		/// возможности использовать другой подход к обращению к системному буферу без больших
		/// изменений исходного кода.
		/// </summary>
		void SetFileDrop(StringCollection pathes)
		{
			SystemClipboard.SetFileDropList(pathes);
		}
		#endregion

		public void ClearClipboard()
		{
			if(!NativeMethodsWrapper.ClearClipboard(out var errorCode))
			{
				// TODO: логирование 
			}
		}

		/// <summary>
		/// Опрашивает системный буфер для получения форматов в которых представленны данных хранящиеся в нём и
		/// возвращает форматы управляющей стороне.
		/// </summary>
		/// <returns>Коллекцию имён форматов в которых представленны данные в системном буфере обмена.</returns>
		/// <exception cref="ExclusiveControlException">Если не удалось получить или вернуть эксклюзивный доступ к системному буферу обмена.</exception>
		IReadOnlyCollection<string> GetPresentedFormats()
		{
			// Для опроса буфера обмена необходимо получить эксклюзивный доступ.
			var exclusiveControlGranted = clipboardWindow.GetExclusiveAccess(out var errors);
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

			var exclisiveControlReturned = clipboardWindow.ReturnExclusiveAccess(out errors);
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
			var unsubscribed = clipboardWindow.UnsubscribeFromClipboardUpdates(out var errors);
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

			internal bool SubscribeToClipboardUpdates(out ICollection<NativeError>? errors)
			{
				const int RetryCount = 5;

				var errorsLazy = new Lazy<List<NativeError>>();
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
						HandleError(errorCode, out bool errorHandled, out bool expectedError);
						RecordError(errorCode.Value, errorHandled, expectedError);
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
				errors = errorsLazy.IsValueCreated
					? errorsLazy.Value
					: null;
				return subscribed;

				void HandleError(int? errorCode, out bool errorHandled, out bool expectedError)
				{
					errorHandled = true;
					expectedError = true;
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
							expectedError = false;
							errorHandled = false;
							break;
					}
				}
				void RecordError(int code, bool handled, bool expected)
				{
					var error = NativeErrorsHelper.CreateNativeErrorFromCode(code);
					if (!handled)
					{
						error.Attributes |= NativeError.ErrorAttributes.UnHandled;
					}
					if (!expected)
					{
						error.Attributes |= NativeError.ErrorAttributes.UnExpected;
					}

					errorsLazy.Value.Add(error);
				}
			}
			internal bool UnsubscribeFromClipboardUpdates(out ICollection<NativeError>? errors)
			{
				const int RetryCount = 5;

				var errorsLazy = new Lazy<List<NativeError>>();
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
						HandleError(errorCode, out bool errorHandled, out bool expectedError);
						RecordError(errorCode.Value, errorHandled, expectedError);
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

				errors = errorsLazy.IsValueCreated
					? errorsLazy.Value
					: null;
				return unsubscribed;

				void HandleError(int? errorCode, out bool errorHandled, out bool expectedError)
				{
					errorHandled = false;
					expectedError = true;
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
							expectedError = false;
							break;
					}
				}
				void RecordError(int code, bool handled, bool expected)
				{
					var error = NativeErrorsHelper.CreateNativeErrorFromCode(code);
					if (!handled)
					{
						error.Attributes |= NativeError.ErrorAttributes.UnHandled;
					}
					if (!expected)
					{
						error.Attributes |= NativeError.ErrorAttributes.UnExpected;
					}

					errorsLazy.Value.Add(error);
				}
			}
			internal bool GetExclusiveAccess(out ICollection<NativeError>? errors)
			{
				const int RetryCount = 5;

				var errorsLazy = new Lazy<List<NativeError>>();
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
						HandleError(errorCode, out bool errorHandled, out bool expectedError);
						RecordError(errorCode.Value, errorHandled, expectedError);
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

				errors = errorsLazy.IsValueCreated
					? errorsLazy.Value
					: null;
				return controlGranted;

				void HandleError(int? errorCode, out bool errorHandled, out bool expectedError)
				{
					errorHandled = true;
					expectedError = true;
					switch (errorCode)
					{
						case NativeErrorsHelper.ERROR_ACCESS_DENIED:
							// При отказе в получении контроля вероятнее всего этот самый контроль занят.
							// Просто подождём и повторим попытку.
							Thread.Sleep(100); // TODO: время ожидания.
							break;
						default:
							errorHandled = false;
							expectedError = false;
							break;
					}
				}
				void RecordError(int code, bool handled, bool expected)
				{
					var error = NativeErrorsHelper.CreateNativeErrorFromCode(code);
					if (!handled)
					{
						error.Attributes |= NativeError.ErrorAttributes.UnHandled;
					}
					if (!expected)
					{
						error.Attributes |= NativeError.ErrorAttributes.UnExpected;
					}

					errorsLazy.Value.Add(error);
				}
			}
			internal bool ReturnExclusiveAccess(out ICollection<NativeError>? errors)
			{
				const int RetryCount = 5;

				var errorsLazy = new Lazy<List<NativeError>>();
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
						HandleError(errorCode, out bool errorHandled, out bool expectedError);
						RecordError(errorCode.Value, errorHandled, expectedError);
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

				errors = errorsLazy.IsValueCreated
					? errorsLazy.Value
					: null;
				return controlReturned;

				void HandleError(int? errorCode, out bool errorHandled, out bool expectedError)
				{
					errorHandled = true;
					expectedError = true;
					switch (errorCode) // TODO: собрать информацию о возможных ошибках.
					{
						case NativeErrorsHelper.ERROR_CLIPBOARD_NOT_OPEN:
							// Контроль не был получен, нет возможности вернуть контроль.
							// Т.к. это никак не отражается на работе приложения ограничимся
							// лишь записью в лог.
							controlReturned = true;
							break;
						default:
							errorHandled = false;
							expectedError = false;
							break;
					}
				}
				void RecordError(int code, bool handled, bool expected)
				{
					var error = NativeErrorsHelper.CreateNativeErrorFromCode(code);
					if (!handled)
					{
						error.Attributes |= NativeError.ErrorAttributes.UnHandled;
					}
					if (!expected)
					{
						error.Attributes |= NativeError.ErrorAttributes.UnExpected;
					}

					errorsLazy.Value.Add(error);
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