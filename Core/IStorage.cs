using API;

namespace Core
{
	public interface IStorage
	{
		ulong SizeInBytes { get; }
		ulong RecordsCount { get; }

		IReadOnlyCollection<Record> GetRecords();
		void AddRecord(Record record);
		void UpdateRecord(Record record);
		void RemoveRecord(Record.Identifier identifier);
	}
}
