namespace Clipboard
{
	internal abstract class Win32Window : IDisposable
	{
		public abstract IntPtr Handle { get; }
		public Action<int> NewWindowMessageReceived = delegate { };

		public abstract void Dispose();
	}
}
