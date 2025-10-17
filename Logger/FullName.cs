namespace Logger;

/// <summary>
/// Full name record. First and Last are required; Middle is optional (nullable).
/// </summary>
public sealed record FullName(string First, string Last, string? Middle = null);

// Reference record: simple and avoids unexpected copying that can cause issues with memory.
// Immutable by design: the values are set at creation and don't change.

