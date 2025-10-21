namespace Logger;

/// <summary>
/// Student entity. Uses FullName and has a StudentId.
/// Name is calculated from FullName.
/// </summary>
public sealed record Student(FullName FullName, string StudentId) : PersonEntity(FullName)
{
	// Id: implicit via EntityBase (through PersonEntity) so Student doesn't duplicate Id initialization logic.
	// Name: implicit via PersonEntity override (calculated from FullName). Student intentionally reuses that implementation so the displayed Name is consistent across person-like entities.
    // StudentId: specific to Student and part of its identity but not used as the Name; this keeps Name focused on the person's full name.
}
