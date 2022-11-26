namespace Clipboard.Native
{
	public class NativeError
	{
		public int Code { get; init; }
		public string? Description { get; init; }
		public ErrorAttributes Attributes { get; set; }

		[Flags]
		public enum ErrorAttributes : byte
		{
			None = 0, // CA1008: Enums should have zero value.
			UnDocumented = 1,
			UnExpected = 2,
			UnHandled = 4
		}
	}
}
