namespace Clipboard.Native
{
	internal class NativeError
	{
		internal int Code { get; init; }
		internal string? Description { get; init; }
		internal ErrorAttributes Attributes { get; set; }

		[Flags]
		internal enum ErrorAttributes : byte
		{
			None = 0, // CA1008: Enums should have zero value.
			UnDocumented = 1,
			UnExpected = 2,
			UnHandled = 4
		}
	}
}
