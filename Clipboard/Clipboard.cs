using System.Collections.Specialized;
using System.Windows.Interop;

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
		readonly ClipboardWindow clipboardWindow;
		bool isDisposed;
		ClipboardData lastObtainedData;

		public Clipboard()
		{
			isDisposed = false;
			SuppressDuplicates = true;
			clipboardWindow = new ClipboardWindow();
			clipboardWindow.NewClipboardDataObtained += OnNewClipboardDataObtained;

			// Подписание окна-слушателя на получение необходимых сообщений.
			var subscribed = clipboardWindow.SubscribeToClipboardUpdates(out var errors);
			if (!subscribed)
			{
				var exceptionMessage = "Не удалось подписаться на уведомления обновления содержимого системного буфера обмена.";
				throw new InicializationException(exceptionMessage);
			}
		}

		private void OnNewClipboardDataObtained()
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
			if (NativeMethodsWrapper.TryToGetClipboardData(clipboardWindow.Handler, imageFormatId, out var dataPtr, out var errorCode)
			 && NativeMethodsWrapper.TryToGlobalLock(dataPtr.Value, out var lockedMemory, out errorCode))
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
			if (!NativeMethodsWrapper.TryToClearClipboard(clipboardWindow.Handler, out var errorCode))
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
			if (!NativeMethodsWrapper.TryToGetPresentedFormats(clipboardWindow.Handler, out var formats, out int? errorCode))
			{
				formats = Array.Empty<string>();
			}

			return formats;
		}
		int GetClipboardFormatsCount()
		{
			var formatsCounted = NativeMethodsWrapper.TryToCountPresentedFormats(out int formatsCount, out int? errorCode);
			if (!formatsCounted)
			{
				// TODO: логировать ошибку.
			}
			return formatsCount;
		}
		bool IsFormatPresented(string formatName)
		{
			return WPFClipboard.ContainsData(formatName);
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
					subscribed = NativeMethodsWrapper.TryToSubscribeWindowToClipboardUpdates(windowHandlerSource.Handle, out int? errorCode);
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
					unsubscribed = NativeMethodsWrapper.TryToUnsubscribeWindowFromClipboardUpdates(windowHandlerSource.Handle, out int? errorCode);
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
			internal bool GetExclusiveAccess(out NativeMethodsWrapper.ClipboardExclusiveAccessToken accessToken, out ICollection<NativeError>? errors)
			{
				const int RetryCount = 5;

				var errorsLazy = new Lazy<List<NativeError>>();
				int currentTry = 0;
				bool controlGranted;
				while (true)
				{
					controlGranted = NativeMethodsWrapper.TryToGetExclusiveClipboardControl(Handler, out accessToken, out int? errorCode);
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
					controlReturned = NativeMethodsWrapper.TryToReturnExclusiveClipboardControl(out int? errorCode);
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