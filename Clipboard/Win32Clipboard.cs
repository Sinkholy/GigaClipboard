using Clipboard.Native;
using Clipboard.Native.Memory;

namespace Clipboard
{
	internal class Win32Clipboard : IDisposable
	{
		readonly Win32Window clipboardWindow;

		internal Win32Clipboard(Win32Window clipboardWindow)
		{
			// TODO: проверить параметр.
			this.clipboardWindow = clipboardWindow;
		}

		internal bool TrySetClipboardData(uint format, GlobalHandle handle)
		{
			if(!GetExclusiveAccess(out var token))
			{
				return false;
			}

			bool dataSuccessfullySet;
			using (token)
			{
				if (!UnsafeTryToClearClipboard())
				{
					dataSuccessfullySet = false;
				}
				else
				{
					dataSuccessfullySet = UnsafeTrySetClipboardData(format, handle);
				}
			}

			return dataSuccessfullySet;
		}
		internal bool TrySetClipboardData((uint format, GlobalHandle handle)[] dataset)
		{
			if (!GetExclusiveAccess(out var token))
			{
				return false;
			}

			bool dataSuccessfullySet;
			using (token)
			{
				if (!UnsafeTryToClearClipboard())
				{
					dataSuccessfullySet = false;
				}
				else
				{
					dataSuccessfullySet = false;
					foreach (var data in dataset)
					{
						dataSuccessfullySet = UnsafeTrySetClipboardData(data.format, data.handle);
					}
				}
			}

			return dataSuccessfullySet;
		}
		bool UnsafeTrySetClipboardData(uint format, GlobalHandle handle)
		{
			const int RetryCount = 5;

			var currentTry = 0;
			bool dataSuccessfullySet = NativeMethodsWrapper.TrySetClipboardData(format, handle.Pointer, out var errorCode);
			while (!dataSuccessfullySet)
			{
				HandleError(errorCode, out bool errorHandled, out bool expectedError);
				RecordError(errorCode.Value, errorHandled, expectedError);

				bool callsLimitReached = ++currentTry == RetryCount;
				if (errorHandled is false ||
					callsLimitReached)
				{
					break;
				}

				dataSuccessfullySet = NativeMethodsWrapper.TryToClearClipboard(out errorCode);
			}
			return dataSuccessfullySet;

			void HandleError(int? errorCode, out bool errorHandled, out bool expectedError)
			{
				errorHandled = true;
				expectedError = true;
				switch (errorCode) // TODO: собрать данные о возможноых ошибках.
				{
					default:
						errorHandled = false;
						expectedError = false;
						break;
				}
			}
		}
		internal bool TryGetClipboardData(uint formatId, out GlobalHandle? globalHandle)
		{
			bool accessGranted = GetExclusiveAccess(out var access);
			if (!accessGranted)
			{
				globalHandle = null;
				return false;
			}

			bool dataRetreived;
			using (access)
			{
				const int RetryCount = 5;

				int currentTry = 0;
				dataRetreived = NativeMethodsWrapper.TryToGetClipboardData(formatId, out var dataPtr, out var errorCode);
				while (!dataRetreived)
				{
					HandleError(errorCode, out bool errorHandled, out bool expectedError);
					RecordError(errorCode.Value, errorHandled, expectedError);

					bool callsLimitReached = ++currentTry < RetryCount;
					if (errorHandled is false ||
						callsLimitReached)
					{
						break;
					}

					dataRetreived = NativeMethodsWrapper.TryToGetClipboardData(formatId, out dataPtr, out errorCode);
				}

				globalHandle = dataRetreived 
							? new GlobalHandle() { Pointer = dataPtr.Value } 
							: null;
			}

			return dataRetreived;

			void HandleError(int? errorCode, out bool errorHandled, out bool expectedError)
			{
				errorHandled = true;
				expectedError = true;
				switch (errorCode) // TODO: собрать данные о потенциальных ошибках.
				{
					default:
						errorHandled = false;
						expectedError = false;
						break;
				}
			}
		}
		/// <summary>
		/// Опрашивает системный буфер для получения форматов в которых представленны данных хранящиеся в нём и
		/// возвращает форматы управляющей стороне.
		/// </summary>
		/// <returns>Коллекцию имён форматов в которых представленны данные в системном буфере обмена.</returns>
		/// <exception cref="ExclusiveControlException">Если не удалось получить или вернуть эксклюзивный доступ к системному буферу обмена.</exception>
		internal IEnumerable<uint> EnumerateDataFormats()
		{
			const int DefaultFormatId = 0; // https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-enumclipboardformats#parameters

			var accessGranted = GetExclusiveAccess(out var accessToken);
			if (!accessGranted)
			{
				yield break;
				// Ну и що тут делать?
			}

			using (accessToken)
			{
				uint currentFormatId = DefaultFormatId;

				while (true)
				{
					bool nextFormatRetreived = NativeMethodsWrapper.TryToEnumClipboardFormats(currentFormatId, out var nextFormatId, out var errorCode);

					// Если запрос вернул отрицательный результат, но при этом код ошибки не был
					// призначен то все форматы были перечислены и переданный currentFormatId 
					// является последним в последовательности.
					bool allFormatsEnumerated = !nextFormatRetreived && errorCode is null;
					if (allFormatsEnumerated)
					{
						break;
					}

					bool errorOccured = !nextFormatRetreived && errorCode is not null;
					if (errorOccured)
					{
						HandleError(errorCode.Value, out var errorHandled, out var errorExpected);
						RecordError(errorCode.Value, errorHandled, errorExpected);

						if (!errorHandled)
						{
							break;
						}
					}
					else
					{
						// Формат удачно получен, запрашиваем следующий формат.
						currentFormatId = nextFormatId.Value;
					}

					yield return currentFormatId;
				}

				yield break;
			}

			void HandleError(int? errorCode, out bool errorHandled, out bool expectedError)
			{
				errorHandled = true;
				expectedError = true;
				switch (errorCode) // TODO: собрать данные о потенциальных ошибках.
				{
					default:
						errorHandled = false;
						expectedError = false;
						break;
				}
			}
		}
		internal bool TryGetClipboardFormatsCount(out int? formatsCount)
		{
			const int RetryCount = 5;

			int currentTry = 0;
			bool formatsCounted = NativeMethodsWrapper.TryToCountPresentedFormats(out formatsCount, out int? errorCode);
			while (!formatsCounted)
			{
				HandleError(errorCode, out bool errorHandled, out bool expectedError);
				RecordError(errorCode.Value, errorHandled, expectedError);

				bool callsLimitReached = ++currentTry < RetryCount;
				if (errorHandled is false ||
					callsLimitReached)
				{
					break;
				}

				formatsCounted = NativeMethodsWrapper.TryToCountPresentedFormats(out formatsCount, out errorCode);
			}

			return formatsCounted;

			void HandleError(int? errorCode, out bool errorHandled, out bool expectedError)
			{
				errorHandled = true;
				expectedError = true;
				switch (errorCode) // TODO: собрать данные о потенциальных ошибках.
				{
					default:
						errorHandled = false;
						expectedError = false;
						break;
				}
			}
		}
		internal bool TryClearClipboard()
		{
			bool accessGranted = GetExclusiveAccess(out var access);
			if (!accessGranted)
			{
				return false;
			}

			bool clipboardCleared;
			using (access)
			{
				clipboardCleared = UnsafeTryToClearClipboard();
			}

			return clipboardCleared;
		}

		bool UnsafeTryToClearClipboard()
		{
			const int RetryCount = 5;

			var currentTry = 0;
			bool clipboardCleared = NativeMethodsWrapper.TryToClearClipboard(out var errorCode);
			while (!clipboardCleared)
			{
				HandleError(errorCode, out bool errorHandled, out bool expectedError);
				RecordError(errorCode.Value, errorHandled, expectedError);

				bool callsLimitReached = ++currentTry == RetryCount;
				if (errorHandled is false ||
					callsLimitReached)
				{
					break;
				}

				clipboardCleared = NativeMethodsWrapper.TryToClearClipboard(out errorCode);
			}
			return clipboardCleared;

			void HandleError(int? errorCode, out bool errorHandled, out bool expectedError)
			{
				errorHandled = true;
				expectedError = true;
				switch (errorCode) // TODO: собрать данные о возможноых ошибках.
				{
					default:
						errorHandled = false;
						expectedError = false;
						break;
				}
			}
		}
		bool GetExclusiveAccess(out ClipboardExclusiveAccessToken? accessToken)
		{
			const int RetryCount = 5;

			int currentTry = 0;
			bool controlGranted = NativeMethodsWrapper.TryToGetExclusiveClipboardControl(clipboardWindow.Handle, out int? errorCode);
			while (!controlGranted)
			{
				HandleError(errorCode, out bool errorHandled, out bool expectedError);
				RecordError(errorCode.Value, errorHandled, expectedError);

				bool callsLimitReached = ++currentTry < RetryCount;
				if (errorHandled is false ||
					callsLimitReached)
				{
					break;
				}

				controlGranted = NativeMethodsWrapper.TryToGetExclusiveClipboardControl(clipboardWindow.Handle, out errorCode);
			}
			accessToken = controlGranted
						? new ClipboardExclusiveAccessToken(this)
						: null;

			return controlGranted;

			void HandleError(int? errorCode, out bool errorHandled, out bool expectedError)
			{
				errorHandled = true;
				expectedError = true;
				switch (errorCode) // TODO: собрать данные о потенциальных ошибках
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
		}
		bool ReturnExclusiveAccess()
		{
			const int RetryCount = 5;

			int currentTry = 0;
			bool controlReturned = NativeMethodsWrapper.TryToReturnExclusiveClipboardControl(out int? errorCode);
			while (!controlReturned)
			{
				HandleError(errorCode, out bool errorHandled, out bool expectedError);
				RecordError(errorCode.Value, errorHandled, expectedError);

				bool callsLimitReached = ++currentTry < RetryCount;
				if (errorHandled is false ||
					callsLimitReached)
				{
					break;
				}

				if (controlReturned)
				{
					// Некоторые ошибки можно трактовать как положительный результат оп=ерации.
					// Допустим при ошибке гласящей о том, что контроль не был получен
					// пытаться возвращать контроль ещё раз не имеет никого смысла.
					break;
				}

				controlReturned = NativeMethodsWrapper.TryToReturnExclusiveClipboardControl(out errorCode);
			}

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

			// TODO: логируем ошибку.
		}
		public void Dispose()
		{
			// TODO: проверить есть ли подписка.
			ReturnExclusiveAccess();
		}



		class ClipboardExclusiveAccessToken : IDisposable
		{
			readonly Win32Clipboard clipboard;

			public ClipboardExclusiveAccessToken(Win32Clipboard clipboard)
			{
				this.clipboard = clipboard;
			}

			public void Dispose()
			{
				clipboard.ReturnExclusiveAccess();
			}
		}
	}
}
