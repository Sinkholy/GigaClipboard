using System.Runtime.InteropServices;

namespace Clipboard.Native
{
	/// <summary>
	/// Класс-обертка упрощяющий доступ к методам импортируемым классом <see cref="NativeMethods"/>.
	/// </summary>
	internal static class NativeMethodsWrapper
	{
		/// <summary>
		/// Подписывает обработчик окна на получение уведомлений об обновлении содержимого системного буфера обмена.
		/// </summary>
		/// <remarks>
		/// Дополнительную информацию можно найти в описании <see cref="NativeMethods.AddClipboardFormatListener(IntPtr)"/>.
		/// </remarks>
		/// <param name="windowHandler">Обработчик окна которое будет получать уведомления.</param>
		/// <param name="errorCode">Код ошибки. Если операция прошла успешно, будет равен <see langword="null"/>.</param>
		/// <returns>
		///		<see langword="true"/> если окно было подписано на уведомления, иначе <see langword="false"/>.
		///	</returns>
		internal static bool SubscribeWindowToClipboardUpdates(IntPtr windowHandler, out int? errorCode)
		{
			bool subscribed = NativeMethods.AddClipboardFormatListener(windowHandler);
			errorCode = subscribed
					? null
					: GetLastNativeError();

			return subscribed;
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
		internal static bool UnsubscribeWindowFromClipboardUpdates(IntPtr windowHandler, out int? errorCode) // TODO: документация.
		{
			bool unsubscribed = NativeMethods.RemoveClipboardFormatListener(windowHandler);
			errorCode = unsubscribed
					? null
					: GetLastNativeError();

			return unsubscribed;
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
		internal static bool GetExclusiveClipboardControl(IntPtr windowHandler, out int? errorCode)
		{
			bool controlGranted = NativeMethods.OpenClipboard(windowHandler);
			errorCode = controlGranted
					? null
					: GetLastNativeError();

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
		internal static bool ReturnExclusiveClipboardControl(out int? errorCode)
		{
			bool controlReturned = NativeMethods.CloseClipboard();
			errorCode = controlReturned
					? null
					: GetLastNativeError();

			return controlReturned;
		}
		/// <summary>
		/// Возвращает количество форматов в которых представлены данные находящиеся в буфере обмена.
		/// </summary>
		/// <returns>Количество форматов данных.</returns>
		internal static bool CountPresentedFormats(out int formatsCount, out int? errorCode)
		{
			//https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-countclipboardformats#return-value
			const int ZeroFormats = 0;
			const int ErrorId = 0;

			bool result;
			formatsCount = NativeMethods.CountClipboardFormats();
			if (formatsCount is ZeroFormats or ErrorId) // TODO: стоит ли инвертировать этот If-Else?
														// Эта мысль возникла в контексте "Первый вариант - верный и ожидаемый".
			{
				// Следуя документации Microsoft метод CountClipboardFormats
				// возвращает 0 в двух случаях:
				// a) Количество форматов равно нулю, то есть данные в буфере отсутствуют.
				// b) Произошла ошибка при обработке запроса.
				// Так как оба случая идентифицируются одним и тем же значением
				// приходится явно выяснять у системы произошла ли ошибка в случае получения этого значения.
				if (IsErrorOccured(out errorCode))
				{
					// Произошла ошибка.
					result = false;
				}
				else
				{
					// Форматов действительно 0.
					result = true;
				}
			}
			else
			{
				result = true;
				errorCode = null;
			}

			return result;
		}
		/// <summary>
		/// Получает коллекцию имён форматов в которых представлены данные в буфере обмена.
		/// </summary>
		/// <remarks>
		/// Перед вызовом этой функции необходимо получить эксклюзивный доступ к системному буферу обмену с помощью <see cref="NativeMethodsWrapper.GetExclusiveClipboardControl(IntPtr, uint)"/>,
		/// иначе результатом выполнения функции будет пустая коллекция;
		/// </remarks>
		/// <returns>Имена форматов данных или пустая коллекция при ошибке.</returns>
		internal static bool GetPresentedFormats(out IReadOnlyCollection<string>? formats, out int? errorCode)
		{
			const int DefaultFormatId = 0; // https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-enumclipboardformats#parameters
			const int LastFormatId = 0; // https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-enumclipboardformats#return-value
			const int ErrorId = 0; // https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-enumclipboardformats#return-value

			List<string> formatsFound = new(10);
			bool result;
			uint currentFormatId = DefaultFormatId;
			while (true)
			{
				currentFormatId = GetNextFormatId(currentFormatId);
				// Следуя документации Microsoft метод EnumClipboardFormats
				// возвращает 0 в случаях:
				// a) Больше нет форматов для перечисления.
				// b) Произошла ошибка при обработке запроса.
				// Так как оба случая идентифицируются одним и тем же значением
				// приходится явно выяснять у системы произошла ли ошибка в случае получения этого значения.
				if (currentFormatId is LastFormatId or ErrorId)
				{
					if (IsErrorOccured(out errorCode))
					{
						// Произошла ошибка.
						result = false;
					}
					else
					{
						// Больше нет форматов для перечисления.
						result = true;
					}
					break;
				}
				else
				{
					if (TryGetFormatName(currentFormatId, out string? formatName, out int? formatNameSearchErrorCode))
					{
						formatsFound.Add(formatName);
					}
					else
					{
						// Мы попадаем сюда с ошибкой - нужно ли её логировать?
						// TODO: нужно решить как поступать в случае если имя формато не было найдено.
						// В данный момент в результат просто добавляется числовой идентификатор формата,
						// но это никак не отражено ни в сигнатуре метода ни в его заголовочном комментарии.
						formatsFound.Add(currentFormatId.ToString());
					}
				}
			}

			formats = result
				? formatsFound
				: null;
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
		internal static bool TryGetFormatName(uint formatId, out string? formatName, out int? errorCode)
		{
			const int FormatNameErrorId = 0;
			const char EmptyChar = '\0';
			const int MaxFormatNameLength = 50; // TODO: я даже не знаю сколько запаса здесь стоит брать.

			errorCode = null;
			bool result;

			// Документация метода GetClipboardFormatName гласит, что параметром представляющим
			// идентификатор формата ([in] format) не должны передаваться идентификаторы предопреленных
			// форматов. https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getclipboardformatnamea#parameters
			// Здесь проверяется является ли формат предпоределенным системой.
			if (PredefinedFormats.TryGetFormatById(formatId, out formatName))
			{
				result = true;
			}
			else
			{
				// При проблемах с производительностью стоит рассмотреть вариант замены инициализации массива символов
				// на получение этого же массива из пула объектов ArrayPool<T>. (Доступно только в >net core 1.0)
				var buffer = new char[MaxFormatNameLength];

				var foundSymbolsCount = NativeMethods.GetClipboardFormatName(formatId, buffer, buffer.Length);
				if (foundSymbolsCount is not FormatNameErrorId)
				{
					result = true;
					formatName = new string(buffer).Trim(EmptyChar);
				}
				else
				{
					if (IsErrorOccured(out errorCode))
					{
						result = false;
						formatName = null;
					}
					else
					{

						result = true;
						formatName = string.Empty;
					}
				}
			}

			return result;
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
		internal static bool ClearClipboard(out int? errorCode)
		{
			bool isCleared = NativeMethods.EmptyClipboard();
			errorCode = isCleared
				? null
				: GetLastNativeError();
			return isCleared;
		}
		/// <summary>
		/// Опрашивает системный буфер обмена для получения идентификатора окна обладающего эксклюзивным доступом
		/// к нему.
		/// </summary>
		/// <returns>Идентификатор окна обладающего эксклюзивным доступом к системному буферу обмена.</returns>
		internal static bool GetWindowWithExclusiveControl(out IntPtr? window, out int? errorCode)
		{
			IntPtr NoWindow = IntPtr.Zero;
			IntPtr EmptyWindow = IntPtr.Zero;

			errorCode = null;
			bool windowFound;
			window = NativeMethods.GetOpenClipboardWindow();
			// https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getopenclipboardwindow#return-value
			// В документации Microsoft указано, что функция GetOpenClipboardWindow возвращает null в случаях:
			// a) Контроль свободен - ни одно окно не обладает эксклюзивным доступом к буферу обмена.
			// b) TaskOwnedControl - окно получившее эксклюзивный доступ к буферу обмена в параметрах
			// вызова функции OpenClipboard указало Null как параметр-идентификатор. https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-openclipboard#parameters
			bool taskOwnedControl = window == EmptyWindow;
			bool controlIsFree = window == NoWindow;
			if (taskOwnedControl || controlIsFree)
			{
				// Т.к. 0 так же может означать возникновение ошибки то проверяем этот случай явно.
				IsErrorOccured(out errorCode);
				windowFound = false;
				window = null;
			}
			else
			{
				windowFound = true;
			}
			return windowFound;
		}
		internal static bool TryGetClipboardData(UInt16 formatId, out IntPtr? dataPtr, out int? errorCode)
		{
			errorCode = null;

			bool dataRetrieved = true;
			dataPtr = NativeMethods.GetClipboardData(formatId);
			if (dataPtr == IntPtr.Zero)
			{
				dataRetrieved = !IsErrorOccured(out errorCode);
			}

			return dataRetrieved;
		}
		internal static bool TryGetGlobalSize(IntPtr memPtr, out uint? size, out int? errorCode)
		{
			errorCode = null;
			size = 0;

			bool successed = true;
			var sizePtr = NativeMethods.GlobalSize(memPtr);
			if (sizePtr != UIntPtr.Zero)
			{
				size = (uint)sizePtr;
			}
			else
			{
				successed = !IsErrorOccured(out errorCode);
			}

			return successed;
		}
		internal static bool TryToGlobalLock(IntPtr memPtr, out IntPtr? lockedMemPtr, out int? errorCode)
		{
			errorCode = null;

			bool locked = true;
			lockedMemPtr = NativeMethods.GlobalLock(memPtr);
			if (lockedMemPtr == IntPtr.Zero)
			{
				locked = !IsErrorOccured(out errorCode);
			}

			return locked;
		}
		internal static bool TryToGlobalUnlock(IntPtr lockedMemPtr, out int? errorCode)
		{
			errorCode = null;

			bool unlocked = NativeMethods.GlobalUnlock(lockedMemPtr);
			if (!unlocked)
			{
				unlocked = !IsErrorOccured(out errorCode);
			}

			return unlocked;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="errorCode"></param>
		/// <returns></returns>
		static bool IsErrorOccured(out int? errorCode)
		{
			errorCode = Marshal.GetLastWin32Error();
			return errorCode != NativeErrorsHelper.ERROR_SUCCESS;
		}
		static int GetLastNativeError()
		{
			return Marshal.GetLastWin32Error();
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

			internal static bool TryGetFormatById(uint formatId, out string? formatName)
			{
				return SystemPredefinedClipboardFormats.TryGetValue(formatId, out formatName);
			}
		}
	}
}
