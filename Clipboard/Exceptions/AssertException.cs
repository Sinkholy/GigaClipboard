﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clipboard.Exceptions
{
	public class AssertException : Exception
	{
		public AssertException(string message)
			: base(message) { }
	}
}
