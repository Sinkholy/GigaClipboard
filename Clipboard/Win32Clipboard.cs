using Clipboard.Native;

using API;

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

		internal bool TryGetClipboardData(ushort formatId, out IntPtr? dataPtr)
		{
			return NativeMethodsWrapper.TryToGetClipboardData(clipboardWindow.Handle, formatId, out dataPtr, out var error);
		}
		/// <summary>
		/// Опрашивает системный буфер для получения форматов в которых представленны данных хранящиеся в нём и
		/// возвращает форматы управляющей стороне.
		/// </summary>
		/// <returns>Коллекцию имён форматов в которых представленны данные в системном буфере обмена.</returns>
		/// <exception cref="ExclusiveControlException">Если не удалось получить или вернуть эксклюзивный доступ к системному буферу обмена.</exception>
		internal bool TryToEnumerateDataFormats(out IReadOnlyCollection<uint>? enumeratedFormats)
		{
			var accessGranted = GetExclusiveAccess(out var accessToken, out _);
			if (!accessGranted)
			{
				enumeratedFormats = null;
				return false;
			}

			bool formatsEnumerated = true;
			using (accessToken)
			{
				const int DefaultFormatId = 0; // https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-enumclipboardformats#parameters

				
				var foundFormats = new List<uint>(GetClipboardFormatsCount());
				uint currentFormatId = DefaultFormatId;
				while (true)
				{
					bool nextFormatRetreived = NativeMethodsWrapper.TryToEnumClipboardFormats(currentFormatId, out var nextFormatId, out var errorCode);

					bool allFormatsEnumerated = !nextFormatRetreived || errorCode is null;
					if (allFormatsEnumerated)
					{
						break;
					}
					else if (errorCode is not null)
					{
						HandleError(errorCode.Value, out var errorHandled, out var errorExpected);
						RecordError(errorCode.Value, errorHandled, errorExpected);

						if (!errorHandled)
						{
							formatsEnumerated = false;
							// TAI: нужно ли здесь возвращать негативный результут?
							// Может будет достаточно 90% перечисленных форматов?
							// Сделать выбор в пользу отказоустойчивости, а не правдивости?
							break;
						}
					}

					foundFormats.Add(nextFormatId.Value);
					currentFormatId = nextFormatId.Value;
				}

				enumeratedFormats = foundFormats;
			}

			return formatsEnumerated;

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
		}
		internal int GetClipboardFormatsCount()
		{
			var formatsCounted = NativeMethodsWrapper.TryToCountPresentedFormats(out int formatsCount, out int? errorCode);
			if (!formatsCounted)
			{
				// TODO: логировать ошибку.
			}
			return formatsCount;
		}
		internal bool GetExclusiveAccess(out NativeMethodsWrapper.ClipboardExclusiveAccessToken accessToken, out ICollection<NativeError>? errors)
		{
			const int RetryCount = 5;

			var errorsLazy = new Lazy<List<NativeError>>();
			int currentTry = 0;
			bool controlGranted;
			while (true)
			{
				controlGranted = NativeMethodsWrapper.TryToGetExclusiveClipboardControl(clipboardWindow.Handle, out accessToken, out int? errorCode);
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
		internal void ClearClipboard()
		{
			if (!NativeMethodsWrapper.TryToClearClipboard(clipboardWindow.Handle, out var errorCode))
			{
				// TODO: логирование 
			}
		}

		public void Dispose()
		{
			// TODO: проверить есть ли подписка.
			ReturnExclusiveAccess(out var errors);
		}
	}
}