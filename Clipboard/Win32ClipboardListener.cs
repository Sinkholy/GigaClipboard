using Clipboard.Native;
using Clipboard.Exceptions;

namespace Clipboard
{
	internal class Win32ClipboardListener : IDisposable
	{
		const int WM_CLIPBOARDUPDATE = 0x031D; // https://docs.microsoft.com/en-us/windows/win32/dataxchg/wm-clipboardupdate
		const int ClipboardUpdatedMessageIdentifier = WM_CLIPBOARDUPDATE;

		readonly Win32Window messagesReceiverWindow;

		internal Win32ClipboardListener(Win32Window messagesReceiverWindow)
		{
			// TODO: проверить параметр.
			this.messagesReceiverWindow = messagesReceiverWindow;
			this.messagesReceiverWindow.NewWindowMessageReceived += OnNewWindowMessageReceived;

			// Подписание окна-слушателя на получение необходимых сообщений.
			try
			{
				SubscribeToClipboardUpdates();
			}
			catch(ArgumentException ex)
			{
				throw new ArgumentException("Проблема с Win32 окном.", nameof(messagesReceiverWindow), ex);
			}
		}

		internal event Action ClipboardUpdated = delegate { };

		/// <summary>
		/// Используется как перехватчик системных сообщений получаемых окном <see cref="windowHandlerSource"/>
		/// для обработки сообщений типа <see cref="ClipboardUpdatedMessageIdentifier"/> которые сигнализируют об обновлении
		/// системного буфера обмена.
		/// </summary>
		void OnNewWindowMessageReceived(int msg)
		{
			if (msg is ClipboardUpdatedMessageIdentifier)
			{
				ClipboardUpdated();
			}
		}
		void SubscribeToClipboardUpdates()
		{
			const int RetryCount = 5;
				
			byte currentTry = 0;
			while (!NativeMethodsWrapper.TryToSubscribeWindowToClipboardUpdates(messagesReceiverWindow.Handle, out int? errorCode))
			{
				HandleError(errorCode.Value);

				bool callsLimitReached = ++currentTry == RetryCount;
				if (callsLimitReached)
				{
					throw new CallsLimitException(RetryCount);
				}
			}



			void HandleError(int errorCode)
			{
				switch (errorCode)
				{
					case NativeErrorsHelper.ERROR_INVALID_PARAMETER:
						// Эта ошибка возникает при попытке повторного подписания одного и того же окна на уведомления.
						// Мне неизвестно может ли эта ошибка возникать в следствии других действий.

						// TODO: здесь нужно подумотб.
						// Такого происходить не должно, следовательно Assert?
						break;
					case NativeErrorsHelper.ERROR_INVALID_WINDOW_HANDLE:
						// Эта ошибка возникала при попытке подписать на уведомления несуществующее окно.
						// Мне неизвестно может ли эта ошибка возникать в следствии других действий.

						throw new ArgumentException();
					default:
						throw new UnhandledNativeErrorException(NativeErrorsHelper.CreateNativeErrorFromCode(errorCode));
				}
			}
		}
		void UnsubscribeFromClipboardUpdates()
		{
			const int RetryCount = 5;

			byte currentTry = 0;
			while (!NativeMethodsWrapper.TryToUnsubscribeWindowFromClipboardUpdates(messagesReceiverWindow.Handle, out int? errorCode))
			{
				HandleError(errorCode.Value);

				bool callsLimitReached = ++currentTry == RetryCount;
				if (callsLimitReached)
				{
					throw new CallsLimitException(RetryCount);
				}
			}



			void HandleError(int errorCode)
			{
				switch (errorCode)
				{
					case NativeErrorsHelper.ERROR_INVALID_PARAMETER:
						// Эта ошибка возникала при попытке повторного отписания одного и того же окна от уведомлений.
						// Мне неизвестно может ли эта ошибка возникать в следствии других действий.

						// Такого происходить не должно, следовательно Assert?
						break;
					case NativeErrorsHelper.ERROR_INVALID_WINDOW_HANDLE:
						// Эта ошибка возникала при попытке отписать от уведомлений несуществующее окно.
						// Мне неизвестно может ли эта ошибка возникать в следствии других действий.
						throw new AssertException($"Произошла попытка отписать от сообщений об изменении содержимого буфера несуществующее окно. " +
												$"Оно должно было быть проверено при подписании. Window handle: {messagesReceiverWindow.Handle}");
					default:
						throw new UnhandledNativeErrorException(NativeErrorsHelper.CreateNativeErrorFromCode(errorCode));
				}
			}
		}
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
				ClipboardUpdated = null;
				if (messagesReceiverWindow is not null)
				{
					UnsubscribeFromClipboardUpdates(); // TODO: считать ли это неуправляемыми ресурсами?
					messagesReceiverWindow.NewWindowMessageReceived -= OnNewWindowMessageReceived;
				}
			}

			disposed = true;
		}
		public void Dispose()
			=> Dispose(true);
		#endregion
	}
}
