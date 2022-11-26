﻿using Clipboard.Native;
using Clipboard.Exceptions;

namespace Clipboard
{
	internal class ClipboardFormats
	{
		internal static uint[] GetAvailableClipboardFormats()
		{
			uint[] formats = new uint[GetFormatsCount()];
			// TODO: Здесь возможно потребуется закрепить (pin) массив для оптимизации.
			// TODO: лучше сделать это непосредственно в NMW.
			// https://learn.microsoft.com/en-us/dotnet/framework/interop/copying-and-pinning?source=recommendations
			int currentTry = 0;
			while (!NativeMethodsWrapper.TryGetUpdatedClipboardFormats(formats, formats.Length, out _, out var errorCode))
			{
				const int RetryCount = 5;

				HandleError(errorCode.Value);

				bool callsLimitReached = ++currentTry < RetryCount;
				if (callsLimitReached)
				{
					throw new CallsLimitException(RetryCount);
				}
			}

			return formats;



			int GetFormatsCount()
			{
				// TODO: посмотреть метод к переработке.
				const int DefaultFormatsCount = 30;

				var formatsCounted = NativeMethodsWrapper.TryToCountPresentedFormats(out int? formatsCount, out var errorCode);

				return formatsCounted
					? formatsCount.Value
					: DefaultFormatsCount;
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
		internal static string GetFormatName(uint formatId)
		{
			// Документация метода GetClipboardFormatName гласит, что параметром представляющим
			// идентификатор формата ([in] format) не должны передаваться идентификаторы предопреленных
			// форматов. https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getclipboardformatnamea#parameters
			// Здесь проверяется является ли формат предпоределенным системой.
			if (IsPredefinedFormat(formatId))
			{
				return PredefinedFormats.GetFormatName(formatId);
			}
			else
			{
				if(NativeMethodsWrapper.TryToGetClipboardFormatName(formatId, out var formatName, out var errorCode))
				{
					return formatName;
				}
				else
				{
					return string.Empty;
				}
			}
		}
		internal static bool IsPredefinedFormat(uint formatId)
		{
			return PredefinedFormats.Contains(formatId);
		}
		internal static bool IsPredefinedFormat(string formatName)
		{
			return PredefinedFormats.Contains(formatName);
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
			internal static bool Contains(string formatName)
			{
				return SystemPredefinedClipboardFormats.Values.Contains(formatName);
			}

			internal static string GetFormatName(uint formatId)
			{
				return SystemPredefinedClipboardFormats[formatId];
			}
			internal static bool TryGetFormatById(uint formatId, out string? formatName)
			{
				return SystemPredefinedClipboardFormats.TryGetValue(formatId, out formatName);
			}
		}
	}
}
