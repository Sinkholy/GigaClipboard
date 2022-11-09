using Clipboard.Native;

namespace Clipboard
{
	internal class MemoryLocker
	{
		internal bool TryToLockMemory(IntPtr memoryPointer, out LockedMemory? lockedMemory)
		{
			bool memoryLocked = NativeMethodsWrapper.TryToGlobalLock(memoryPointer, out var lockedMemoryPtr, out var errorCode);
			var currentTry = 0;
			while (!memoryLocked)
			{
				const int RetryCount = 5;

				HandleError(errorCode.Value, out var errorHandled, out var errorExpected);
				RecordError(errorCode.Value, errorHandled, errorExpected);

				bool callsLimitReached = ++currentTry == RetryCount;
				if (errorHandled is false ||
					callsLimitReached)
				{
					break;
				}

				memoryLocked = NativeMethodsWrapper.TryToGlobalLock(memoryPointer, out lockedMemoryPtr, out errorCode);
			}

			lockedMemory = memoryLocked
						 ? new LockedMemory(lockedMemoryPtr.Value, this)
						 : null;

			return memoryLocked;

			void HandleError(int errorCode, out bool errorHandled, out bool errorExpected)
			{
				errorHandled = true;
				errorExpected = true;
				switch (errorCode) // TODO: собрать данные о возможноых ошибках.
				{
					default:
						errorHandled = false;
						errorExpected = false;
						break;
				}
			}
		}
		internal bool TryToUnlockMemory(IntPtr memoryPointer)
		{
			bool memoryUnlocked = NativeMethodsWrapper.TryToGlobalUnlock(memoryPointer, out var errorCode);
			var currentTry = 0;
			while (!memoryUnlocked)
			{
				const int RetryCount = 5;

				HandleError(errorCode.Value, out var errorHandled, out var errorExpected);
				RecordError(errorCode.Value, errorHandled, errorExpected);

				bool callsLimitReached = ++currentTry == RetryCount;
				if (errorHandled is false ||
					callsLimitReached)
				{
					break;
				}

				memoryUnlocked = NativeMethodsWrapper.TryToGlobalUnlock(memoryPointer, out errorCode);
			}

			return memoryUnlocked;

			void HandleError(int errorCode, out bool errorHandled, out bool errorExpected)
			{
				errorHandled = true;
				errorExpected = true;
				switch (errorCode) // TODO: собрать данные о возможноых ошибках.
				{
					default:
						errorHandled = false;
						errorExpected = false;
						break;
				}
			}
		}
		internal bool TryToUnlockMemory(LockedMemory lockedMemory)
		{
			return TryToUnlockMemory(lockedMemory.Pointer);
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

			// TODO: логируем ошибку.
		}

		internal class LockedMemory : IDisposable
		{
			readonly MemoryLocker locker;
			readonly internal IntPtr Pointer;

			internal LockedMemory(IntPtr pointer, MemoryLocker locker)
			{
				Pointer = pointer;
				this.locker = locker;
			}

			public void Dispose()
			{
				locker.TryToUnlockMemory(Pointer);
			}
		}
	}
}
