using Clipboard.Native;

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
			var subscribed = SubscribeToClipboardUpdates(); // TODO: вынести в отдельный метод Start()? Это позволит безболезнено возвращать результат создания слушателя управляющей стороне. 
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
		bool SubscribeToClipboardUpdates()
		{
			const int RetryCount = 5;

			byte currentTry = 0;
			bool subscribed;
			while (true)
			{
				subscribed = NativeMethodsWrapper.TryToSubscribeWindowToClipboardUpdates(messagesReceiverWindow.Handle, out int? errorCode);
				if (subscribed)
				{
					break;
				}
				else
				{
					HandleError(errorCode, out bool errorHandled, out bool expectedError);
					RecordError(errorCode.Value, errorHandled, expectedError);
					if (!errorHandled)
					{
						break;
					}
					currentTry++;
				}

				if (currentTry == RetryCount)
				{
					break;
				}
			}

			return subscribed;

			void HandleError(int? errorCode, out bool errorHandled, out bool expectedError)
			{
				errorHandled = true;
				expectedError = true;
				switch (errorCode)
				{
					case NativeErrorsHelper.ERROR_INVALID_PARAMETER:
						// Эта ошибка возникает при попытке повторного подписания одного и того же окна на уведомления.
						// Мне неизвестно может ли эта ошибка возникать в следствии других действий.

						// Такого происходить не должно, следовательно Assert?
						break;
					case NativeErrorsHelper.ERROR_INVALID_WINDOW_HANDLE:
					// Эта ошибка возникала при попытке подписать на уведомления несуществующее окно.
					// Мне неизвестно может ли эта ошибка возникать в следствии других действий.

					// Попытаться его пересоздать или просто уведомить об исключении?
					default:
						expectedError = false;
						errorHandled = false;
						break;
				}
			}
		}
		bool UnsubscribeFromClipboardUpdates()
		{
			const int RetryCount = 5;

			byte currentTry = 0;
			bool unsubscribed;
			while (true)
			{
				unsubscribed = NativeMethodsWrapper.TryToUnsubscribeWindowFromClipboardUpdates(messagesReceiverWindow.Handle, out int? errorCode);
				if (unsubscribed)
				{
					break;
				}
				else
				{
					HandleError(errorCode, out bool errorHandled, out bool expectedError);
					RecordError(errorCode.Value, errorHandled, expectedError);
					if (!errorHandled)
					{
						break;
					}
					currentTry++;
				}

				if (currentTry == RetryCount)
				{
					break;
				}
			}

			return unsubscribed;

			void HandleError(int? errorCode, out bool errorHandled, out bool expectedError)
			{
				errorHandled = false;
				expectedError = true;
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

					// Попытаться его пересоздать или просто уведомить об исключении?
					default:
						errorHandled = true;
						expectedError = false;
						break;
				}
			}
		}
		void RecordError(int code, bool handled, bool expected)
		{
			var error = NativeErrorsHelper.CreateNativeErrorFromCode(code);
			if (!handled)
			{
				error.Attributes |= NativeError.ErrorAttributes.UnHandled;
			}
			if (!expected)
			{
				error.Attributes |= NativeError.ErrorAttributes.UnExpected;
			}

			// TODO: логировать ошибку.
		}

		public void Dispose()
		{
			UnsubscribeFromClipboardUpdates();
			messagesReceiverWindow.NewWindowMessageReceived -= OnNewWindowMessageReceived;
		}
	}
}
