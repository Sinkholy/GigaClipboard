using System.Runtime.InteropServices;

namespace Clipboard.Native.Memory
{
	internal static class NativeMemoryManager
	{
		#region Global
		internal static NativeMemorySegment CopyUnmanagedFromGHandle(GlobalHandle gHandle)
		{
			NativeMethodsWrapper.TryToGetGlobalSize(gHandle.Pointer, out var ghandleSize, out _);
			using (var lockedMemory = GHandleLocker.LockGHandle(gHandle.Pointer))
			{
				var copiedMemory = Allocate(ghandleSize.Value);
				MemCopy(lockedMemory.Pointer, copiedMemory.AsIntPtr(), ghandleSize.Value);

				return copiedMemory;
			}
		}
		internal static byte[] CopyManagedFromGHandle(GlobalHandle gHandle)
		{
			NativeMethodsWrapper.TryToGetGlobalSize(gHandle.Pointer, out var ghandleSize, out _);
			using (var lockedMemory = GHandleLocker.LockGHandle(gHandle.Pointer))
			{
				byte[] bytes = new byte[ghandleSize.Value];
				unsafe
				{
					new ReadOnlySpan<byte>(lockedMemory.Pointer.ToPointer(), (int)ghandleSize.Value)
						.CopyTo(bytes);
				}

				return bytes;
			}
		}
		internal static GlobalHandle CreateGHandle(uint size)
		{
			const uint GHND = 0x0042;

			var allocatedGHandlePtr = GlobalAlloc(GHND, size);
			return new GlobalHandle() { Pointer = allocatedGHandlePtr };



			[DllImport("kernel32.dll")]
			static extern IntPtr GlobalAlloc(uint uFlags, uint dwBytes);
		}
		internal static GlobalHandle CreateEmptyGHandle()
		{
			return new GlobalHandle() { Pointer = IntPtr.Zero };
		}
		internal static void CopyToGHandle(GlobalHandle gHandle, NativeMemorySegment nativeMem)
		{
			NativeMethodsWrapper.TryToGetGlobalSize(gHandle.Pointer, out var globalSize, out _);

			if(globalSize < nativeMem.GetSize())
			{
				throw new Exception(); 
			}

			using (var lockedMemory = GHandleLocker.LockGHandle(gHandle.Pointer))
			{
				MemCopy(nativeMem.AsIntPtr(), lockedMemory.Pointer, globalSize.Value);
			}
		}
		internal static void CopyToGHandle(GlobalHandle gHandle, byte[] binaryData)
		{
			NativeMethodsWrapper.TryToGetGlobalSize(gHandle.Pointer, out var globalSize, out _);

			if(globalSize < binaryData.Length)
			{
				throw new Exception();
			}

			using (var lockedMemory = GHandleLocker.LockGHandle(gHandle.Pointer))
			{
				unsafe
				{
					var gHandleSpan = new Span<byte>(lockedMemory.Pointer.ToPointer(), (int)globalSize.Value);
					for (int i = 0; i < binaryData.Length; i++)
					{
						gHandleSpan[i] = binaryData[i];
					}
				}
			}
		}
		#endregion

		internal static NativeMemorySegment Allocate(uint size)
		{
			unsafe
			{
				var allocatedPtr = NativeMemory.Alloc(size);
				return new NativeMemorySegment(allocatedPtr, size);
			}
		}
		internal static void Free(NativeMemorySegment memorySegment)
		{
			unsafe
			{
				NativeMemory.Free(memorySegment.AsRawPointer());
			}
		}
		static void MemCopy(IntPtr source, IntPtr destination, long memSize)
		{
			unsafe
			{
				MemCopy(source.ToPointer(), destination.ToPointer(), memSize);
			}
		}
		static unsafe void MemCopy(void* source, void* destination, long memSize)
		{
			Buffer.MemoryCopy(source, destination, memSize, memSize);
		}
		


		internal class NativeMemorySegment : IDisposable
		{
			unsafe readonly void* memoryPtr;
			readonly uint memorySize;

			public unsafe NativeMemorySegment(void* memoryPtr, uint memorySize)
			{
				this.memoryPtr = memoryPtr;
				this.memorySize = memorySize;
			}

			internal uint GetSize()
			{
				return memorySize;
			}
			internal unsafe void* AsRawPointer()
			{
				return memoryPtr;
			}
			internal IntPtr AsIntPtr()
			{
				unsafe
				{
					return new IntPtr(memoryPtr);
				}
			}
			internal Span<byte> AsSpan()
			{
				unsafe
				{
					return new Span<byte>(memoryPtr, (int)memorySize);
				}
			}
			internal Memory<byte> AsMemory()
			{
				return default; // TODO: WTF
			}

			public void Dispose()
			{
				NativeMemoryManager.Free(this);
			}
		}
		static class GHandleLocker
		{
			internal static LockedMemory LockGHandle(IntPtr gHandlePtr)
			{
				bool memoryLocked = NativeMethodsWrapper.TryToGlobalLock(gHandlePtr, out var lockedMemoryPtr, out var errorCode);
				var currentTry = 0;
				while (!memoryLocked)
				{
					const int RetryCount = 5;

					HandleError(errorCode.Value, out var errorHandled, out var errorExpected);

					bool callsLimitReached = ++currentTry == RetryCount;
					if (errorHandled is false ||
						callsLimitReached)
					{
						break;
					}

					memoryLocked = NativeMethodsWrapper.TryToGlobalLock(gHandlePtr, out lockedMemoryPtr, out errorCode);
				}

				return new LockedMemory(lockedMemoryPtr.Value);

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
			static void UnlockGHandle(LockedMemory lockedMemory)
			{
				bool memoryUnlocked = NativeMethodsWrapper.TryToGlobalUnlock(lockedMemory.Pointer, out var errorCode);
				var currentTry = 0;
				while (!memoryUnlocked)
				{
					const int RetryCount = 5;

					HandleError(errorCode.Value, out var errorHandled, out var errorExpected);

					bool callsLimitReached = ++currentTry == RetryCount;
					if (errorHandled is false ||
						callsLimitReached)
					{
						break;
					}

					memoryUnlocked = NativeMethodsWrapper.TryToGlobalUnlock(lockedMemory.Pointer, out errorCode);
				}

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



			internal ref struct LockedMemory
			{
				readonly internal IntPtr Pointer;

				internal LockedMemory(IntPtr pointer)
				{
					Pointer = pointer;
				}

				public void Dispose()
				{
					GHandleLocker.UnlockGHandle(this);
				}
			}
		}
	}
}
