namespace Cleaner.RecyclingBin
{
	/// <summary>
	/// Инкапсулирует конфигурацию корзины.
	/// </summary>
	/// <remarks>
	/// Был помещен отдельно от <seealso cref="RecyclingBin"/> так как 
	/// должен быть публичным, а корзина будет внутренней.
	/// </remarks>
	public class Configuration
	{
		/// <summary>
		/// Определяет будут ли записи помещаться в корзину перед удалением. 
		/// </summary>
		public bool IsEnabled { get; set; }
		/// <summary>
		/// Определяет срок хранения записи в корзине перед окончательным удалением.
		/// </summary>
		public TimeSpan RecordsStoringPeriod { get; set; }
		/// <summary>
		/// Определяет максимальное число записей хранящихся в корзине.
		/// </summary>
		public uint MaxRecordsInBin { get; set; }
		/// <summary>
		/// Определяет максимальный объем памяти занимаемый корзиной на ПЗУ.
		/// </summary>
		public ulong MaxBinSizeInBytes { get; set; }
	}
}
