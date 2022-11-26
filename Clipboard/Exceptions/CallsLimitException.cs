using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clipboard.Exceptions
{
	public class CallsLimitException : Exception
	{
		public readonly int Limit;

		public CallsLimitException(int limit)
		{
			Limit = limit;
		}
	}
}
