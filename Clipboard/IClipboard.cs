namespace Core
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

		/// <summary>
		/// Возвращает данные находящиеся в буфере обмена в данный момент.
		/// </summary>
		/// <returns>Данные находящиеся в буфере обмена.</returns>
		public ClipboardData GetData();
		/// <summary>
		/// Устанавливает данные в буфер обмена.
		/// </summary>
		/// <param name="data">Данные которые необходимо установить в буфер обмена.</param>
		public void SetData(ClipboardData data);
		/// <summary>
		/// Устанавливает данные в буфер обмена.
		/// </summary>
		/// <param name="data">Данные которые необходимо установить в буфер обмена.</param>
		/// <param name="type">Тип данных.</param>
		public void SetData(object? data, DataType type);

		/// <summary>
		/// Тип данных полученных из буфера обмена.
		/// </summary>
		/// <remarks>
		/// Так как из буфера обмена данные приходят в упакованном в object виде
		/// это перечисление помогает понять какого типа данные были получены.
		/// </remarks>
		public enum DataType : byte
		{
			// TODO: заполнить в соответствии с возможными типами данных.
		}
		/// <summary>
		/// Представляет набор данных находящиеся в буфере обмена и тип этих данных.
		/// </summary>
		public struct ClipboardData
		{
			public object? Data;
			public DataType Type;
		}
	}
}
