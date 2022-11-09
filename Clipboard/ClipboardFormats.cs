using Clipboard.Native;

namespace Clipboard
{
	internal class ClipboardFormats
	{
		internal static bool TryGetFormatName(uint formatId, out string? formatName)
		{

			// Документация метода GetClipboardFormatName гласит, что параметром представляющим
			// идентификатор формата ([in] format) не должны передаваться идентификаторы предопреленных
			// форматов. https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getclipboardformatnamea#parameters
			// Здесь проверяется является ли формат предпоределенным системой.
			bool nameObtained;
			bool isPredefinedFormat = PredefinedFormats.Contains(formatId);
			if (isPredefinedFormat)
			{
				nameObtained = PredefinedFormats.TryGetFormatById(formatId, out formatName);
			}
			else
			{
				nameObtained = NativeMethodsWrapper.TryToGetClipboardFormatName(formatId, out formatName, out var errorCode);
				if (!nameObtained)
				{
					// TODO: логируем ошибку.
				}
			}

			return nameObtained;
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

			internal static bool TryGetFormatById(uint formatId, out string? formatName)
			{
				return SystemPredefinedClipboardFormats.TryGetValue(formatId, out formatName);
			}
		}
	}
}
