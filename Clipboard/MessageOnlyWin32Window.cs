using System.Windows.Interop;

namespace Clipboard
{
	internal class MessageOnlyWin32Window : Win32Window
	{
		// Есть ещё множество способов создать собственное msg-only окно
		// если надумаешь попробовать\потестировать, то материал можно найти здесь:
		// https://www.cyberforum.ru/visual-cpp/thread241794.html
		// https://docs.microsoft.com/en-us/windows/win32/winmsg/window-features#message-only-windows

		const int HWND_MESSAGE = -3; // https://docs.microsoft.com/en-us/windows/win32/winmsg/window-features#message-only-windows
		const int MessageOnlyWindowHandlerId = HWND_MESSAGE;

		readonly HwndSource hwndSource;

		public MessageOnlyWin32Window()
		{
			// Создание окна-слушателя и добавление хука для перехвата сообщений.
			var windowHandlerSourceConfig = new HwndSourceParameters
			{
				WindowClassStyle = default,
				ExtendedWindowStyle = default,
				WindowStyle = default,
				PositionX = default,
				PositionY = default,
				Height = default,
				Width = default,
				WindowName = string.Empty,
			};
			windowHandlerSourceConfig.ParentWindow = new IntPtr(MessageOnlyWindowHandlerId);
			hwndSource = new HwndSource(windowHandlerSourceConfig);
			hwndSource.AddHook(WindowsMessagesInterceptor);
		}

		public override IntPtr Handle
			=> hwndSource.Handle;

		/// <summary>
		/// Используется как перехватчик системных сообщений получаемых окном <see cref="windowHandlerSource"/>
		/// для обработки сообщений типа <see cref="ClipboardUpdatedMessageIdentifier"/> которые сигнализируют об обновлении
		/// системного буфера обмена.
		/// </summary>
		IntPtr WindowsMessagesInterceptor(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			// TAI: необходимо подробнее разобраться в механизме перехвата сообщений.
			// https://docs.microsoft.com/en-us/windows/win32/winmsg/about-hooks
			// https://habr.com/ru/company/icl_services/blog/324718/

			NewWindowMessageReceived(msg);

			return IntPtr.Zero;
		}

		#region Disposing
		bool disposed = false;
		protected override void Dispose(bool disposing)
		{
			if (!disposed)
			{
				hwndSource.RemoveHook(WindowsMessagesInterceptor);
				hwndSource.Dispose();

				disposed = true;
			}
			
			base.Dispose(disposing);
		}
		#endregion
	}
}
