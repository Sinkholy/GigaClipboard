using System.Runtime.InteropServices;

namespace Clipboard.Native
{
	/// <summary>
	/// Класс-обертка упрощяющий доступ к методам импортируемым классом <see cref="NativeMethods"/>.
	/// </summary>
	internal static class NativeMethodsWrapper
	{
		const int MillisecondsTimeoutBetweenTries = 100; // TAI: возможно стоит вынести таймаут в параметры каждого конкретного метода?

		/// <summary>
		/// Подписывает обработчик окна на получение уведомлений об обновлении содержимого системного буфера обмена.
		/// </summary>
		/// <remarks>
		/// Дополнительную информацию можно найти в описании <see cref="NativeMethods.AddClipboardFormatListener(IntPtr)"/>.
		/// </remarks>
		/// <param name="windowhandler">Обработчик окна которое будет получать уведомления.</param>
		/// <param name="retryCount">Количество попыток подписаться при неудаче.</param>
		/// <returns>
		///		<see langword="true"/> если окно было подписано на уведомления, иначе <see langword="false"/>.
		///	</returns>
		internal static bool SubscribeWindowToClipboardUpdates(IntPtr windowhandler, uint retryCount = 5)
		{
			byte currentTry = 0;
			bool isSubscribed;
			while (true)
			{
				isSubscribed = NativeMethods.AddClipboardFormatListener(windowhandler);
				if (isSubscribed)
				{
					break;
				}
				else if (currentTry == retryCount)
				{
					break;
				}
				Thread.Sleep(MillisecondsTimeoutBetweenTries);

				currentTry++;
			}
			return isSubscribed;
		}
		/// <summary>
		/// Отписывает обработчик окна от получения события обновления системного буфера обмена.
		/// </summary>
		/// <remarks>
		/// Дополнительную информацию можно найти в описании <see cref="NativeMethods.RemoveClipboardFormatListener(IntPtr)"/>.
		/// </remarks>
		/// <param name="windowHandler">Обработчик окна которое будет отписано от уведомлений.</param>
		/// <param name="retryCount">Количество попыток отписаться при неудаче.</param>
		/// <returns>
		///		<see langword="true"/> если окно было отписано, иначе <see langword="false"/>.
		///	</returns>
		internal static bool UnsubscribeWindowFromClipboardUpdates(IntPtr windowHandler, uint retryCount = 5)
		{
			byte currentTry = 0;
			bool isUnsubscribed;
			while (true)
			{
				isUnsubscribed = NativeMethods.RemoveClipboardFormatListener(windowHandler);
				if (isUnsubscribed)
				{
					break;
				}
				else if (currentTry == retryCount)
				{
					break;
				}
				Thread.Sleep(MillisecondsTimeoutBetweenTries);

				currentTry++;
			}
			return isUnsubscribed;
		}
		/// <summary>
		/// Получает эксклюзивный доступ к системному буферу обмена для окна представленного обработчиком <paramref name="windowHandler"/>.
		/// </summary>
		/// <remarks>
		/// <para>
		/// !!!Эксклюзивный доступ необходимо вернуть после произведения необходимых действий с буфером обмена посредством <see cref="NativeMethodsWrapper.ReturnExclusiveClipboardControl(uint)"/>.
		/// </para>
		/// <para>
		///		Дополнительную информацию можно найти в описании <see cref="NativeMethods.OpenClipboard(IntPtr)"/>.
		/// </para>
		/// </remarks>
		/// <param name="windowHandler">Обработчик окна которому будет предоставлен экслюзивный доступ.</param>
		/// <param name="retryCount">Количество попыток завладеть эксклюзивным доступом при неудаче.</param>
		/// <returns>
		///		<see langword="true"/> если эксклюзивный доступ получен, иначе <see langword="false"/>.
		/// </returns>
		internal static bool GetExclusiveClipboardControl(IntPtr windowHandler, uint retryCount = 5)
		{
			uint currentTry = 0;
			bool controlGranted;
			while (true)
			{
				controlGranted = NativeMethods.OpenClipboard(windowHandler);
				if (controlGranted)
				{
					break;
				}
				else if (currentTry == retryCount)
				{
					break;
				}

				Thread.Sleep(MillisecondsTimeoutBetweenTries);
				currentTry++;
			}
			return controlGranted;
		}
		/// <summary>
		/// Возвращает эксклюзивный доступ к системному буферу обмена.
		/// </summary>
		/// <remarks>
		/// Дополнительную информацию можно найти в описании <see cref="NativeMethods.CloseClipboard()"/>.
		/// </remarks>
		/// <param name="retryCount">Количество попыток отдать эксклюзивный доступ при неудаче.</param>
		/// <returns>
		///		<see langword="true"/> если эксклюзивный доступ возвращен, иначе <see langword="false"/>.
		/// </returns>
		internal static bool ReturnExclusiveClipboardControl(uint retryCount = 5)
		{
			uint currentTry = 0;
			bool controlReturned;
			while (true)
			{
				controlReturned = NativeMethods.CloseClipboard();
				if (controlReturned)
				{
					break;
				}
				else if (currentTry == retryCount)
				{
					break;
				}
				Thread.Sleep(MillisecondsTimeoutBetweenTries);

				currentTry++;
			}
			return controlReturned;
		}
		/// <summary>
		/// Возвращает количество форматов в которых представлены данные находящиеся в буфере обмена.
		/// </summary>
		/// <returns>Количество форматов данных.</returns>
		internal static int CountPresentedFormats()
		{
			return NativeMethods.CountClipboardFormats();
		}
		/// <summary>
		/// Получает коллекцию имён форматов в которых представлены данные в буфере обмена.
		/// </summary>
		/// <remarks>
		/// Перед вызовом этой функции необходимо получить эксклюзивный доступ к системному буферу обмену с помощью <see cref="NativeMethodsWrapper.GetExclusiveClipboardControl(IntPtr, uint)"/>,
		/// иначе результатом выполнения функции будет пустая коллекция;
		/// </remarks>
		/// <returns>Имена форматов данных или пустая коллекция при ошибке.</returns>
		internal static IReadOnlyCollection<string> GetPresentedFormats()
		{
			const int DefaultFormatId = 0; // https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-enumclipboardformats#parameters
			const int LastFormatId = 0; // https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-enumclipboardformats#return-value
			const int ErrorId = 0; // https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-enumclipboardformats#return-value

			List<string> result = new(10);

			uint currentFormatId = DefaultFormatId;
			while (true)
			{
				currentFormatId = GetNextFormatId(currentFormatId);
				if (currentFormatId is LastFormatId or ErrorId)
				{
					if (IsErrorOccured(out int errorId))
					{
						// TODO: заготовка под обработку ошибок.
						// Необходимо продумать то как обрабатывать и\или 
						// сообщать об ошибках и стоит ли вообще это делать.
					}
					break;
				}

				if (TryGetFormatName(currentFormatId, out string formatName))
				{
					result.Add(formatName);
				}
				else
				{
					// TODO: нужно решить как поступать в случае если имя формато не было найдено.
					// В данный момент в результат просто добавляется числовой идентификатор формата,
					// но это никак не отражено ни в сигнатуре метода ни в его заголовочном комментарии.
					result.Add(currentFormatId.ToString());
				}
			}

			return result;

			static uint GetNextFormatId(uint currentFormatId)
			{
				return NativeMethods.EnumClipboardFormats(currentFormatId);
			}
		}
		/// <summary>
		/// Запрашивает в системном буфере обмена имя формата данных основываясь на его идентификаторе.
		/// </summary>
		/// <param name="formatId">Идентификатор формата имя которого необходимо получить.</param>
		/// <param name="formatName">Имя формата.</param>
		/// <returns>
		/// <see langword="true"/> если имя формата было найдено, иначе <see langword="false"/>.
		/// </returns>
		internal static bool TryGetFormatName(uint formatId, out string formatName)
		{
			// При проблемах с производительностью стоит рассмотреть вариант замены инициализации массива символов
			// на получение этого же массива из пула объектов ArrayPool<T>.
			const char EmptyChar = '\0';
			const int MaxFormatNameLength = 50; // TODO: я даже не знаю сколько запаса здесь стоит брать.

			if (PredefinedFormats.TryGetFormatById(formatId, out string systemFormat))
			{
				formatName = systemFormat;
				return true;
			}
			else
			{
				var buffer = new char[MaxFormatNameLength];
				var foundSymbols = NativeMethods.GetClipboardFormatName(formatId, buffer, buffer.Length);
				var formatNameFound = foundSymbols > 0;
				if (formatNameFound)
				{
					formatName = new string(buffer).Trim(EmptyChar);
					return true;
				}
				else
				{
					formatName = string.Empty;
					return false;
				}
			}
		}
		/// <summary>
		/// Очищает системный буфер обмена от содержимого и освобождает ресурсы. 
		/// </summary>
		/// <remarks>
		/// Перед вызовом этой функции необходимо получить эксклюзивный доступ к системному буферу обмену с помощью <see cref="NativeMethodsWrapper.GetExclusiveClipboardControl(IntPtr, uint)"/>,
		/// иначе результатом выполнения функции будет ошибка;
		/// </remarks>
		/// <param name="retryCount">Количество попыток очищения буфера в случае неудачи.</param>
		/// <returns>
		/// <see langword="true"/> если очищение произведено, иначе <see langword="false"/>.
		/// </returns>
		internal static bool ClearClipboard(uint retryCount = 5)
		{
			uint currentTry = 0;
			bool isCleared;
			while (true)
			{
				isCleared = NativeMethods.EmptyClipboard();
				if (isCleared)
				{
					break;
				}
				else if (currentTry == retryCount)
				{
					break;
				}
				Thread.Sleep(MillisecondsTimeoutBetweenTries);

				currentTry++;
			}
			return isCleared;
		}
		/// <summary>
		/// Опрашивает системный буфер обмена для получения идентификатора окна обладающего эксклюзивным доступом
		/// к нему.
		/// </summary>
		/// <returns>Идентификатор окна обладающего эксклюзивным доступом к системному буферу обмена.</returns>
		internal static IntPtr GetWindowWithExclusiveControl()
		{
			return NativeMethods.GetOpenClipboardWindow();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="errorId"></param>
		/// <returns></returns>
		static bool IsErrorOccured(out int errorId)
		{
			const int ERROR_SUCCESS = 0;

			errorId = Marshal.GetLastWin32Error();
			return errorId != ERROR_SUCCESS;
		}
		static int GetHRForLastError()
		{
			return Marshal.GetHRForLastWin32Error();
		}
		static Exception? GetExceptionForLastError()
		{
			var hresult = Marshal.GetHRForLastWin32Error();
			return Marshal.GetExceptionForHR(hresult);
		}

		/// <summary>
		/// Класс инкапсулирует предопределенные форматы данных и методы доступа к ним.
		/// </summary>
		/// <remarks>
		/// <see href="https://docs.microsoft.com/en-us/windows/win32/dataxchg/standard-clipboard-formats">Документация касающаяся предопределенных форматов.</see>
		/// </remarks>
		static class PredefinedFormats
		{
			static readonly IDictionary<uint, string> SystemPredefinedClipboardFormats;

			static PredefinedFormats()
			{
				SystemPredefinedClipboardFormats = new Dictionary<uint, string>()
					{
						{ 1, "CF_TEXT" },
						{ 2, "CF_BITMAP" },
						{ 8, "CF_DIB" },
						{ 17, "CF_DIBV5" },
						{ 0x0082, "CF_DSPBITMAP" },
						{ 0x008E, "CF_DSPENHMETAFILE" },
						{ 0x0083, "CF_DSPMETAFILEPICT" },
						{ 0x0081, "CF_DSPTEXT" },
						{ 14, "CF_ENHMETAFILE" },
						{ 0x0300, "CF_GDIOBJFIRST" },
						{ 0x03FF, "CF_GDIOBJLAST" },
						{ 15, "CF_HDROP" },
						{ 16, "CF_LOCALE" },
						{ 3, "CF_METAFILEPICT" },
						{ 7, "CF_OEMTEXT" },
						{ 0x0080, "CF_OWNERDISPLAY" },
						{ 9, "CF_PALETTE" },
						{ 10, "CF_PENDATA" },
						{ 0x0200, "CF_PRIVATEFIRST" },
						{ 0x02FF, "CF_PRIVATELAST" },
						{ 11, "CF_RIFF" },
						{ 4, "CF_SYLK" },
						{ 6, "CF_TIFF" },
						{ 13, "CF_UNICODETEXT" },
						{ 12, "CF_WAVE" }
					};
			}

			/// <summary>
			/// Метод определяет является ли формат с предоставленным идентификатором <paramref name="formatId"/> предопределенным.
			/// </summary>
			/// <param name="formatId">Идентификатор формата.</param>
			/// <returns>
			/// <see langword="true"/> если формат предопределен, иначе <see langword="false"/>.
			/// </returns>
			internal static bool Contains(uint formatId)
			{
				return SystemPredefinedClipboardFormats.ContainsKey(formatId);
			}

			internal static bool TryGetFormatById(uint formatId, out string formatName)
			{
				return SystemPredefinedClipboardFormats.TryGetValue(formatId, out formatName);
			}
		}
	}
}
