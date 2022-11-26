using Clipboard.Exceptions;
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

		internal void SetClipboardData(uint format, GlobalHandle handle)
		{
			SetClipboardData(new[] { (format, handle) });
		}
		internal void SetClipboardData((uint format, GlobalHandle handle)[] dataset)
		{
			using (var accessToken = GetExclusiveAccess())
			{
				LocalClearClipboard(accessToken);

				foreach (var data in dataset)
				{
					LocalSetClipboardData(accessToken, data.format, data.handle);
				}
			}
		}
		void LocalSetClipboardData(ClipboardExclusiveAccessToken token, uint format, GlobalHandle handle)
		{
			const int RetryCount = 5;

			var currentTry = 0;
			while (!NativeMethodsWrapper.TrySetClipboardData(format, handle.Pointer, out var errorCode))
			{
				HandleError(errorCode.Value);

				bool callsLimitReached = ++currentTry == RetryCount;
				if (callsLimitReached)
				{
					throw new CallsLimitException(RetryCount);
				}
			}

			void HandleError(int errorCode)
			{
				switch (errorCode) // TODO: собрать данные о возможноых ошибках.
				{
					case NativeErrorsHelper.ERROR_CLIPBOARD_NOT_OPEN:
						// Метод был вызван без предварительного открытия буфера обмена.
						// Такого быть не должно так как этот аспект должен быть явно указан.
						throw new AssertException("При вызове метода который требует получение эксклюзивного доступа не был получен эксклюзивный доступ.");
					default:
						throw new UnhandledNativeErrorException(NativeErrorsHelper.CreateNativeErrorFromCode(errorCode), $"Attempted format to set in clipboard: {format}");
				}
			}
		}
		internal GlobalHandle GetClipboardData(uint formatId)
		{
			using (GetExclusiveAccess())
			{
				const int RetryCount = 5;

				int currentTry = 0;

				IntPtr? dataPtr;
				while (!NativeMethodsWrapper.TryToGetClipboardData(formatId, out dataPtr, out var errorCode))
				{
					HandleError(errorCode.Value);

					bool callsLimitReached = ++currentTry < RetryCount;
					if (callsLimitReached)
					{
						throw new CallsLimitException(RetryCount);
					}
				}

				return new GlobalHandle() { Pointer = dataPtr.Value };
			}

			void HandleError(int errorCode)
			{
				switch (errorCode) // TODO: собрать данные о потенциальных ошибках.
				{
					default:
						throw new UnhandledNativeErrorException(NativeErrorsHelper.CreateNativeErrorFromCode(errorCode), $"Requested data format was: {formatId}");
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

			using (GetExclusiveAccess())
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
						HandleError(errorCode.Value);
						// Ошибка обработана, ничего не меняя делаем тот же самый запрос.
						// TODO: необходимо сделать ограничитель на количество попыток.
						continue;
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

			void HandleError(int errorCode)
			{
				switch (errorCode) // TODO: собрать данные о потенциальных ошибках.
				{
					default:
						throw new UnhandledNativeErrorException(NativeErrorsHelper.CreateNativeErrorFromCode(errorCode));
				}
			}
		}
		internal int GetClipboardFormatsCount()
		{
			const int RetryCount = 5;

			int currentTry = 0;
			int? formatsCount;
			while (!NativeMethodsWrapper.TryToCountPresentedFormats(out formatsCount, out int? errorCode))
			{
				HandleError(errorCode.Value);

				bool callsLimitReached = ++currentTry < RetryCount;
				if (callsLimitReached)
				{
					throw new CallsLimitException(RetryCount);
				}
			}

			return formatsCount.Value;

			void HandleError(int errorCode)
			{
				switch (errorCode) // TODO: собрать данные о потенциальных ошибках.
				{
					default:
						throw new UnhandledNativeErrorException(NativeErrorsHelper.CreateNativeErrorFromCode(errorCode));
				}
			}
		}
		internal void ClearClipboard()
		{
			using (var access = GetExclusiveAccess())
			{
				LocalClearClipboard(access);
			}
		}

		void LocalClearClipboard(ClipboardExclusiveAccessToken token)
		{
			const int RetryCount = 5;

			var currentTry = 0;
			while (!NativeMethodsWrapper.TryToClearClipboard(out var errorCode))
			{
				HandleError(errorCode.Value);

				bool callsLimitReached = ++currentTry == RetryCount;
				if (callsLimitReached)
				{
					throw new CallsLimitException(RetryCount);
				}
			}

			void HandleError(int errorCode)
			{
				switch (errorCode) // TODO: собрать данные о возможноых ошибках.
				{
					default:
						throw new UnhandledNativeErrorException(NativeErrorsHelper.CreateNativeErrorFromCode(errorCode));
				}
			}
		}
		ClipboardExclusiveAccessToken GetExclusiveAccess()
		{
			const int RetryCount = 5;

			int currentTry = 0;
			while (!NativeMethodsWrapper.TryToGetExclusiveClipboardControl(clipboardWindow.Handle, out int? errorCode))
			{
				HandleError(errorCode.Value);

				bool callsLimitReached = ++currentTry < RetryCount;
				if (callsLimitReached)
				{
					throw new CallsLimitException(RetryCount);
				}
			}

			return new ClipboardExclusiveAccessToken(this);



			void HandleError(int errorCode)
			{
				switch (errorCode) // TODO: собрать данные о потенциальных ошибках
				{
					case NativeErrorsHelper.ERROR_ACCESS_DENIED:
						// При отказе в получении контроля вероятнее всего этот самый контроль занят.
						// Просто подождём и повторим попытку.
						Thread.Sleep(100); // TODO: время ожидания.
						break;
					default:
						throw new UnhandledNativeErrorException(NativeErrorsHelper.CreateNativeErrorFromCode(errorCode));
				}
			}
		}
		void ReturnExclusiveAccess()
		{
			const int RetryCount = 5;

			int currentTry = 0;
			bool controlReturned = NativeMethodsWrapper.TryToReturnExclusiveClipboardControl(out int? errorCode);
			while (!controlReturned)
			{
				HandleError(errorCode.Value);

				if (controlReturned)
				{
					// Некоторые ошибки можно трактовать как положительный результат операции.
					// Допустим при ошибке гласящей о том, что контроль не был получен
					// пытаться возвращать контроль ещё раз не имеет никого смысла.
					break;
				}

				bool callsLimitReached = ++currentTry < RetryCount;
				if (callsLimitReached)
				{
					throw new CallsLimitException(RetryCount);
				}

				controlReturned = NativeMethodsWrapper.TryToReturnExclusiveClipboardControl(out errorCode);
			}



			void HandleError(int errorCode)
			{
				switch (errorCode) // TODO: собрать информацию о возможных ошибках.
				{
					case NativeErrorsHelper.ERROR_CLIPBOARD_NOT_OPEN:
						// Контроль не был получен, нет возможности вернуть контроль.
						// Т.к. это никак не отражается на работе приложения ограничимся
						// лишь записью в лог.
						controlReturned = true;
						break;
					default:
						throw new UnhandledNativeErrorException(NativeErrorsHelper.CreateNativeErrorFromCode(errorCode));
				}
			}
		}
		#region Disposing
		bool disposed = false;
		protected virtual void Dispose(bool disposing)
		{
			if (disposed)
			{
				return;
			}

			ReturnExclusiveAccess();
			// TODO: оно здесь действительно нужно? 
			// т.к. доступ к запросу эксклюзивного контроля имеется только у private методов
			// а они, гарантировано, будут его возвращать т.к. токен доступа будет собран раньше чем этот класс.
			disposed = true;
		}
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		~Win32Clipboard()
			=> Dispose(false);
		#endregion



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
