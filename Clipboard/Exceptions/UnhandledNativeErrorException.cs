using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Clipboard.Native;

namespace Clipboard.Exceptions
{
	public class UnhandledNativeErrorException : Exception
	{
		public readonly NativeError Error;
		public readonly object[] ContextParams;

		public UnhandledNativeErrorException(NativeError error, params object[] contextParams)
		{
			Error = error;
			ContextParams = contextParams;
		}
	}
}
