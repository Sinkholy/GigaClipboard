namespace Clipboard
{
	internal abstract class Win32Window : IDisposable
	{
		public abstract IntPtr Handle { get; }
		public Action<int> NewWindowMessageReceived = delegate { };

		#region Disposing
		bool disposed = false;
		protected virtual void Dispose(bool disposing) 
		{
			if (disposed)
			{
				return;
			}

			if (disposing)
			{
				// Очищаем список подписчиков события.
				NewWindowMessageReceived = null;
			}

			disposed = true;
		}
		public void Dispose() 
			=> Dispose(true);
		#endregion
	}
}
