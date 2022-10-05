using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Core
{
	public static class WindowMetadataProvider
	{
		public static WindowMetadata? GetForegroundWindowMetadata()
		{
			var foregroundWindowHwnd = NativeMethodsWrapper.GetForegroundWindowHandler();
			if (foregroundWindowHwnd is null)
			{
				return null;
			}
			return new WindowMetadata(foregroundWindowHwnd.Value);
		}

		public class WindowMetadata
		{
			readonly IntPtr windowHandler;

			public WindowMetadata(IntPtr windowHandler)
			{
				this.windowHandler = windowHandler;
			}

			public string? GetWindowProcessDescription()
			{
				string? title = null;
				if (NativeMethodsWrapper.TryGetWindowThreadProcessId(windowHandler, out var processId))
				{
					if (TryGetProcessById(processId.Value, out var process))
					{
						title = process.MainModule?.FileVersionInfo.FileDescription;
					}
				}

				return title;
			}
			public string? GetWindowProcessName()
			{

				string? title = null;
				if (NativeMethodsWrapper.TryGetWindowThreadProcessId(windowHandler, out var processId))
				{
					if (TryGetProcessById(processId.Value, out var process))
					{
						title = process.ProcessName;
					}
				}

				return title;
			}
			public string? GetWindowTitle()
			{
				// TODO: здесь, возможно, потребуется по-другому получать заголовок окна.
				// https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getwindowtexta#remarks

				string? title = null;
				if (NativeMethodsWrapper.TryGetWindowTitleTextLength(windowHandler, out int? length))
				{
					var buffer = new char[length.Value];
					if (NativeMethodsWrapper.TryGetWindowTitle(windowHandler, buffer))
					{
						title = new string(buffer);
					}
				}

				return title;
			}

			static bool TryGetProcessById(int processId, out Process? process)
			{
				bool processFound = true;
				try
				{
					process = Process.GetProcessById(processId);
				}
				catch
				{
					processFound = false;
					process = null;
				}

				return processFound;
			}
		}
		static class NativeMethodsWrapper
		{
			internal static IntPtr? GetForegroundWindowHandler()
			{
				var handler = NativeMethods.GetForegroundWindow();
				return handler == IntPtr.Zero
								? null
								: handler;
			}
			internal static bool TryGetWindowTitle(IntPtr windowHandler, char[] buffer)
			{
				int returnedSymbolsCount = NativeMethods.GetWindowText(windowHandler, buffer, buffer.Length);
				if (returnedSymbolsCount == 0)
				{
					if (IsErrorOccured(out int? errorCode))
					{
						return false;
					}
				}
				return true;
			}
			internal static bool TryGetWindowTitleTextLength(IntPtr windowHandler, out int? length)
			{
				int lengthReturned = NativeMethods.GetWindowTextLength(windowHandler);
				if (lengthReturned == 0)
				{
					if (IsErrorOccured(out int? errorCode))
					{
						length = null;
						return false;
					}
				}
				length = lengthReturned;
				return true;
			}
			internal static bool TryGetWindowThreadProcessId(IntPtr windowHandler, out int? processId)
			{
				_ = NativeMethods.GetWindowThreadProcessId(windowHandler, out int returnedProcessId);
				if (returnedProcessId == 0)
				{
					processId = null;
					return false;
				}
				else
				{
					processId = returnedProcessId;
					return true;
				}
			}

			static bool IsErrorOccured(out int? errorCode)
			{
				errorCode = GetLastNativeError();
				return errorCode != 0;
			}
			static int GetLastNativeError()
			{
				return Marshal.GetLastWin32Error();
			}
		}
		static class NativeMethods
		{
			[DllImport("user32.dll", SetLastError = true)]
			internal static extern IntPtr GetForegroundWindow();

			[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
			internal static extern int GetWindowText(IntPtr hWnd,
													[Out] char[] buffer,
													int bufferSize);

			[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
			internal static extern uint GetWindowModuleFileName(IntPtr hwnd,
															   [Out] char[] buffer,
															   int bufferSize);

			[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
			internal static extern int GetWindowTextLength(IntPtr hWnd);

			[DllImport("user32.dll", SetLastError = true)]
			internal static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

			[DllImport("kernel32.dll", SetLastError = true)]
			internal static extern IntPtr OpenProcess(ulong processAccess, bool bInheritHandle, uint processId);
		}
	}
}
