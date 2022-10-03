using API;

namespace Core
{
	/// <summary>
	/// Класс инкапсулирующий публичный основной набор функций для работы с записями 
	/// и буфером обмена.
	/// </summary>
	public class Core
	{
		Configuration configuration;

		/// <summary>
		/// Сигнгализирует о том, что была создана новая запись.
		/// </summary>
		public event Action NewRecordCreated = delegate { };

		/// <summary>
		/// Возвращает запись созданную из последних данных полученных из буфера обмена.
		/// </summary>
		/// <returns>Последнюю созданную запись.</returns>
		public Record GetLastRecord()
		{

		}
		/// <summary>
		/// Возвращает запись с <paramref name="id"/> идентификатором.
		/// </summary>
		/// <param name="id">Идентификатор записи которую необходимо вернуть.</param>
		/// <returns>Запись с <paramref name="id"/> идентификатором.</returns>
		public Record GetRecordById(Record.Identifier id)
		{

		}
		/// <summary>
		/// Возвращает последние N записей.
		/// </summary>
		/// <param name="count">Количество записей которые необходимо вернуть.</param>
		/// <returns><paramref name="count" /> количество последних записей.</returns>
		public IReadOnlyCollection<Record> GetLastNRecords(uint count)
		{

		}
		/// <summary>
		/// Возвращает записи за определенный день.
		/// </summary>
		/// <param name="day">День за который необходимо предоставить записи.</param>
		/// <returns>Записи которые были созданы в <paramref name="day"/></returns>
		public IReadOnlyCollection<Record> GetRecordsByDay(DateOnly day)
		{

		}
		/// <summary>
		/// Возвращает записи за определенный период.
		/// </summary>
		/// <param name="from">Дата с которой необходимо начать выборку.</param>
		/// <param name="to">Дата котороый необходимо закончить выборку.</param>
		/// <returns>Записи которые были созданы в промежутке между от <paramref name="from"/> до <paramref name="to"/></returns>
		public IReadOnlyCollection<Record> GetRecordsBetweenDates(DateTime from, DateTime to)
		{

		}
		/// <summary>
		/// Возвращает записи которые были созданы до <paramref name="date"/>
		/// </summary>
		/// <param name="date">Дата ограничивающая последнюю запись.</param>
		/// <returns>Записи которые были созданы до <paramref name="date"/></returns>
		public IReadOnlyCollection<Record> GetRecordsBeforeDate(DateTime date)
		{

		}
		/// <summary>
		/// Возвращает записи которые были созданы после <paramref name="date"/>
		/// </summary>
		/// <param name="date">Дата ограничивающая первую запись.</param> // TODO: wtf
		/// <returns>Записи которые были созданы после <paramref name="date"/></returns>
		public IReadOnlyCollection<Record> GetRecordsAfterDate(DateTime date)
		{

		}
		/// <summary>
		/// Возвращает записи содержащие данные определенного типа.
		/// </summary>
		/// <param name="dataType">Тип данных записи с которым необходимо вернуть.</param>
		/// <returns>Записи содержащие в себе данные типа <paramref name="dataType"/></returns>
		public IReadOnlyCollection<Record> GetRecordsWithDataOfType(IClipboard.DataType dataType) // TODO: этот метод нарушает принцип сокрытия.
		{

		}
		/// <summary>
		/// Устанавливает запись в буфер обмена.
		/// </summary>
		/// <param name="record">Запись которую необходимо установить в буфер обмена.</param>
		public void SetRecordToClipboard(Record record)
		{

		}
		/// <summary>
		/// Вставляет данные находящиеся в записи.
		/// </summary>
		/// <param name="record">Запись данные которой необходимо вставить.</param>
		public void PasteRecord(Record record)
		{

		}
		/// <summary>
		/// Удаляет запись с <paramref name="recordId"/> идентификатором.
		/// </summary>
		/// <param name="recordId">Идентификатор записи которую необходимо удалить.</param>
		public void RemoveRecord(Record.Identifier recordId)
		{

		}

		/// <summary>
		/// Инкапсулирует конфигурацию ядра.
		/// </summary>
		public class Configuration
		{
			/// <summary>
			/// Определяет метаданные которые будут сохранены в записи при её создании.
			/// </summary>
			public RecordMetadataType MetadataToCollect { get; }

			/// <summary>
			/// Определяет записи с какими типами данных будут сохраняться.
			/// </summary 
			public IClipboard.DataType[] DataTypesToBeStored { get; } // TODO: Здесь должен быть не массив, а битовое поле как мне кажется или похожее решение.
																	  // TODO: Это поле нарушает принцип сокрытия и выдает потенциальным пользователям интерфейс IClipboard
																	  // Одним из вариантов решения этой проблеммы я вижу в том, чтобы предоставить набор свойств типа bool по типу "bool StoreStrings"
																	  // Это лишит данный сегмент гибкости, но позволит сохранить его секреты. Планируются ли новые типы данных?

			/// <summary>
			/// Возвращает конфигурацию ядра в сериализованном виде.
			/// </summary>
			/// <returns>Сериализованная конфигурация ядра.</returns>
			/// <remarks>
			/// Необходим для возможности переноса\сохранения настроек приложения.
			/// </remarks>
			public object Export() // TODO: необходимо определить формат JSON\XML и библиотеку которая будет отвечать за (де)сериализацию.
			{

			}
			/// <summary>
			/// Применяет конфигурацию ядра полученную извне.
			/// </summary>
			/// <param name="serializedConfiguration">Сериализованная конфигурация ядра.</param>
			/// <remarks>
			/// Необходим для возможности переноса\сохранения настроек приложения.
			/// </remarks>
			public void Import(object serializedConfiguration) // TODO: читай пометку метода Export.
			{

			}

			/// <summary>
			/// Метаданные которые необходимо собирать при создании записи.
			/// </summary>
			/// <remarks>
			/// Отмеченно атрибутом Flags для более простого сравнения и хранения.
			/// </remarks>
			[Flags]
			public enum RecordMetadataType : byte
			{
				CreationDateTime,
				ApplicationId,
				ApplicationName,
				ApplicationDescription
			}
		}
	}
}