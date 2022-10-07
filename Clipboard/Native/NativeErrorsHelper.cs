using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clipboard.Native
{
	internal static class NativeErrorsHelper
	{
		static readonly IDictionary<int, string> errorsDescriptionsByCode;
		internal const int ERROR_SUCCESS = 0;
		internal const int ERROR_ACCESS_DENIED = 5;
		internal const int ERROR_INVALID_PARAMETER = 87;
		internal const int ERROR_INVALID_WINDOW_HANDLE = 1400;
		internal const int ERROR_CLIPBOARD_NOT_OPEN = 1418;

		static NativeErrorsHelper()
		{
			errorsDescriptionsByCode = new Dictionary<int, string>()
				{
					{ 5, "Access is denied." },
					{ 87, "The parameter is incorrect." },
					{ 1400, "Invalid window handle." },
					{ 1418, "Thread does not have a clipboard open." }
				};
		}

		internal static NativeError CreateNativeErrorFromCode(int errorCode)
		{
			var documentedError = TryGetErrorDescription(errorCode, out string errorDesc);
			return documentedError
				? new NativeError()
				{
					Code = errorCode,
					Description = errorDesc,
					Attributes = NativeError.ErrorAttributes.None
				}
				: new NativeError()
				{
					Code = errorCode,
					Description = null,
					Attributes = NativeError.ErrorAttributes.UnDocumented
				};
		}
		internal static bool TryGetErrorDescription(int errorId, out string errorDesc)
		{
			return errorsDescriptionsByCode.TryGetValue(errorId, out errorDesc);
		}
	}
}
