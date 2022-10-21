using System.Runtime.InteropServices;

namespace Clipboard.Native
{
	/// <summary>
	/// Класс инкапсулирующий набор методов предоставленных извне.
	/// </summary>
	/// <remarks>
	/// Для более простой работы с методами импортируемыми этим классом обратитесь к <see cref="NativeMethodsWrapper"/>.
	/// </remarks>
	internal static class NativeMethods
	{
		// TODO: углубиться в атрибут DllImport.
		/// <summary>
		/// Добавляет окно в список получателей уведомлений обновления системного буфера обмена.
		/// </summary>
		/// <remarks>
		///		<para>
		///			<seealso href="https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-addclipboardformatlistener">Документация.</seealso>
		///		</para>
		///		<para>
		///			<seealso href="https://pinvoke.net/default.aspx/user32/AddClipboardFormatListener.html">Дополнительные данные и пример использования.</seealso>
		///		</para>
		/// </remarks>
		/// <param name="hwnd">Обработчик окна которое будет добавление в список получателей уведомлений.</param>
		/// <returns>
		///		<see langword="true"/> если окно было подписано на уведомления, иначе <see langword="false"/>.
		///	</returns>
		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool AddClipboardFormatListener(IntPtr hwnd);

		/// <summary>
		/// Удаляет окно из списка получателей уведомлений обновления системного буфера обмена.
		/// </summary>
		/// <remarks>
		///		<para>
		///			<seealso href="https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-removeclipboardformatlistener">Документация.</seealso>
		///		</para>
		///		<para>
		///			<seealso href="https://pinvoke.net/default.aspx/user32/RemoveClipboardFormatListener.html">Дополнительные данные и пример использования.</seealso>
		///		</para>
		/// </remarks>
		/// <param name="hwnd">Обработчик окна которое будет удалено в список получателей уведомлений.</param>
		/// <returns>
		///		<see langword="true"/> если окно было подписано на уведомления, иначе <see langword="false"/>.
		///	</returns>
		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern int CountClipboardFormats();

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern uint EnumClipboardFormats(uint first);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern int GetClipboardFormatName(uint formatId,
															[Out] char[] buffer,
															int bufferSize);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern uint RegisterClipboardFormat(string withName);

		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern uint GetLastError();

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool OpenClipboard(IntPtr hwnd);

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool CloseClipboard();

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool EmptyClipboard();

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern IntPtr GetOpenClipboardWindow();
		[DllImport("kernel32.dll")]
		internal static extern IntPtr GlobalLock(IntPtr hMem);
		[DllImport("kernel32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool GlobalUnlock(IntPtr hMem);
		[DllImport("kernel32.dll")]
		internal static extern UIntPtr GlobalSize(IntPtr hMem);
		[DllImport("user32.dll")]
		internal static extern IntPtr GetClipboardData(uint uFormat);
	}
}
