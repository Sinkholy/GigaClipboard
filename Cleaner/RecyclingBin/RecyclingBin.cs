using Core;

namespace Cleaner.RecyclingBin
{
	/// <summary>
	/// Класс представляющий собой корзину для временного хранения
	/// записей перед их окончательным удалением.
	/// </summary>
	/// <remarks>
	/// Концептуально очень похож на системную корзину в ОС.
	/// </remarks>
	internal class RecyclingBin
	{
		/// <summary>
		/// Конфигурация корзины.
		/// </summary>
		internal Configuration Configuration { get; set; }
		/// <summary>
		/// Записи которые находятся в корзине.
		/// </summary>
		internal IReadOnlyCollection<Record> RecordsStoredInBin { get; }

		/// <summary>
		/// Окончательно удаляет все записи находящиеся в корзине.
		/// </summary>
		internal void Clear()
		{

		}
		/// <summary>
		/// Восстанавливает все записи из корзины.
		/// </summary>
		internal void RestoreAllRecords()
		{

		}
		/// <summary>
		/// Восстанавливает запись из корзины.
		/// </summary>
		/// <param name="record">Запись которую необходимо восстановить.</param>
		internal void RestoreRecord(Record.Identifier recordId)
		{

		}
		/// <summary>
		/// Перехватывает записи которые подлежат удалению и отбирает из них те которые будут перемещенны в корзину.
		/// </summary>
		/// <remarks>
		/// Должен быть вызван управляющей стороной после того как будут найдены записи подлежащие удалению.
		/// </remarks>
		/// <param name="records">Найденные записи подлежащие удалению.</param>
		/// <param name="interceptedRecords">Записи которые будут помещены в корзину.</param>
		internal void InterceptRecordsToBeDeleted(ICollection<Record> records, out ICollection<Record> interceptedRecords)
		{

		}
	}
}
