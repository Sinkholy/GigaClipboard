namespace Core
{
	/// <summary>
	/// Представляет собой запись данных из буфера обмена.
	/// </summary>
	public class Record
	{
		/// <summary>
		/// Идентификатор записи.
		/// </summary>
		public Identifier ID { get; }
		/// <summary>
		/// Данные которые представляет эта запись.
		/// </summary>
		public object? Data { get; }
		/// <summary>
		/// Тип данных которые представляет эта запись.
		/// </summary>
		public IClipboard.DataType DataType { get; }
		/// <summary>
		/// Указывает на то содержит ли эта запись метаданные или нет.
		/// </summary>
		public bool ContainsMetadata { get; }
		/// <summary>
		/// Метаданные записи.
		/// </summary>
		public Metadata RecordMetadata { get; } // TODO: Поработай над неймингом.

		/// <summary>
		/// Инкапсулирует сведения об идентификаторе записи.
		/// </summary>
		public class Identifier
		{
			// TODO: необходимо продумать механизм получения идентификатора. Для простого поиска он должен быть инкрементным.
			// TODO: Необходимо продумать формат в котором будет представлен идентификатор записи.
			public override bool Equals(object? obj)
			{
				// TODO: Подумать над тем нужно ли здесь это сравнение.
			}
		}

		/// <summary>
		/// Представляет метаданные записи.
		/// </summary>
		public class Metadata
		{
			/// <summary>
			/// Дата и время когда были получены данные для этой записи.
			/// </summary>
			public DateTime CreatedAt { get; }
			/// <summary>
			/// Контекст в котором были полученны данные для этой записи.
			/// </summary>
			public ApplicationMetadata CreationContext { get; }

			/// <summary>
			/// Представляет метаданные приложения в котором были полученны данные.
			/// </summary>
			public class ApplicationMetadata
			{
				/// <summary>
				/// Идентификатор процесса в котором были получены данные.
				/// </summary>
				public object ProccessID { get; } // TODO: Необходимо узнать в каком формате хранить идентификатор процесса и что он вообще из себя представляет.
				/// <summary>
				/// Имя приложения в котором были получены данные.
				/// </summary>
				public string Name { get; }
				/// <summary>
				/// Описание приложения в котором были получены данные.
				/// </summary>
				public string Description { get; }
			}
		}
	}
}
