namespace API
{
	/// <summary>
	/// Класс реализующий этот интерфейс должен предоставлять
	/// доступ к системному буферу обмена.
	/// </summary>
	public interface IClipboard
	{
		/// <summary>
		/// Сигнализирует о том, что в буфер обмена попали новые данные.
		/// </summary>
		public event Action NewClipboardDataObtained;

		public DataType? GetDataType();
		public ClipboardData? GetData();

		public void SetData(object data, DataType dataType);

		public void ClearClipboard();

		public enum DataType
		{
			Text,
			Image,
			Audio,
			FileDrop
		}
		public abstract class ClipboardData
		{
			public ClipboardData(DataType dataType)
			{
				DataType = dataType;
			}

			public DataType DataType { get; init; }
		}
		public class ClipboardData<T> : ClipboardData
		{
			public ClipboardData(T data, DataType dataType)
				: base(dataType)
			{
				Data = data;
			}

			public T Data { get; init; }
		}
		public class BinaryData
		{
			readonly byte[] binaryBytes;

			public BinaryData(byte[] binaryBytes)
			{
				this.binaryBytes = binaryBytes;
			}

			public byte[] GetBytes()
			{
				return binaryBytes;
			}
			public MemoryStream GetStream()
			{
				return new MemoryStream(binaryBytes);
			}
		}
	}
}
