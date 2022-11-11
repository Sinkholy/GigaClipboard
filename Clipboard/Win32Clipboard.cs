﻿using Clipboard.Native;

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
			bool accessGranted = GetExclusiveAccess(out var access);
			if (!accessGranted)
			{
				dataPtr = null;
				return false;
			}

			bool dataRetreived;
			using (access)
			{
				const int RetryCount = 5;

				int currentTry = 0;
				dataRetreived = NativeMethodsWrapper.TryToGetClipboardData(formatId, out dataPtr, out var errorCode);
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
		internal bool TryEnumerateDataFormats(out IReadOnlyCollection<uint>? enumeratedFormats)
		{
			var accessGranted = GetExclusiveAccess(out var accessToken);
			if (!accessGranted)
			{
				enumeratedFormats = null;
				return false;
			}

			bool formatsEnumerated = true;
			using (accessToken)
			{
				const int DefaultFormatsCount = 10;
				const int DefaultFormatId = 0; // https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-enumclipboardformats#parameters

				this.TryGetClipboardFormatsCount(out var formatsCount);
				var foundFormats = new List<uint>(formatsCount ?? DefaultFormatsCount);
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
				const int RetryCount = 5;

				var currentTry = 0;
				clipboardCleared = NativeMethodsWrapper.TryToClearClipboard(out var errorCode);
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