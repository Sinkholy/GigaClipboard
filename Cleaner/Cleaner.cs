using API;

namespace Cleaner
{
	/// <summary>
	/// Класс отвечающий за удаление устаревших записей и инкапсулирующий в себе 
	/// функционал корзины.
	/// </summary>
	public class Cleaner
	{
		readonly RecyclingBin.RecyclingBin recyclingBin;

		/// <summary>
		/// Сигнализирует о том, что начался процесс поиска и удаления(или перемещение в корзину) устаревших записей.
		/// </summary>
		public event Action CleanupStarted = delegate { };
		/// <summary>
		/// Сигнализирует о том, что завершился процесс поиска и удаления(или перемещение в корзину) перемещение в корзину устаревших записей.
		/// </summary>
		public event Action<CleanupResult> CleanupCompleted = delegate { };

		/// <summary>
		/// Конфигурация корзины.
		/// </summary>
		public RecyclingBin.Configuration RecyclingBinConfiguration 
			=> recyclingBin.Configuration;
		/// <summary>
		/// Конфигурация очистителя.
		/// </summary>
		public Configuration CleanerConfiguration { get; }

		/// <summary>
		/// Принудительно начинает поиск и удаление(или перемещение в корзину) устаревших записей.
		/// </summary>
		public void ForceCleanup()
		{

		}
		/// <summary>
		/// Принудительно начинет процесс очистки корзины.
		/// </summary>
		public void CleanRecyclingBin()
		{

		}

		/// <summary>
		/// Инкапсулирует конфигурацию очистителя.
		/// </summary>
		public class Configuration
		{
			/// <summary>
			/// Определяет будет ли производиться очистка.
			/// </summary>
			public bool IsEnabled { get; set; }
			/// <summary>
			/// Определяет условия при которых запись будет удалена или пермещена в корзину.
			/// </summary>
			public CleaningCondition Condition { get; set; }
			/// <summary>
			/// Определяет интервал с которым будет происходить очистка.
			/// </summary>
			public CleaningInterval Interval { get; set; }
			/// <summary>
			/// Определяет срок хранения записи в хранилище перед удалением.
			/// </summary>
			public TimeSpan RecordsStoringPeriod { get; set; }
			/// <summary>
			/// Определяет максимальный объем памяти занимаемый хранилищем на ПЗУ.
			/// </summary>
			public ulong MaxStorageSizeInBytes { get; set; }

			/// <summary>
			/// Условия при которых запись будет удалена или пермещена в корзину.
			/// </summary>
			[Flags]
			public enum CleaningCondition : byte
			{
				ByStoringTime,
				ByStorageType
			}
			/// <summary>
			/// Интервал с которым будет происходить очистка.
			/// </summary>
			public enum CleaningInterval
			{
				Weekly,
				Daily,
				EveryHour
			}
		}
		/// <summary>
		/// Класс представляющий результат выполнения очистки хранилища.
		/// </summary>
		public class CleanupResult
		{
			/// <summary>
			/// Записи которые были удалены окончательно.
			/// </summary>
			public IReadOnlyCollection<Record> DeletedRecords { get; }
			/// <summary>
			/// Записи которые были перемещены в корзину.
			/// </summary>
			public IReadOnlyCollection<Record> RecordsMovedToRecyclingBin { get; }
		}
	}
}