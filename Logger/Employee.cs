namespace Logger;

/// <summary>
/// Employee entity. Uses FullName and has an EmployeeNumber.
/// Name is calculated from FullName.
/// </summary>
public sealed record Employee(FullName FullName, string EmployeeNumber) : PersonEntity(FullName)
{
    // Id: implicit via EntityBase (through PersonEntity) to keep Id implementation centralized.
    // Name: implicit via PersonEntity override (calculated from FullName) so Employee shares the same Name behavior as Student.
    // EmployeeNumber: specific to Employee and part of its identity but intentionally not used as the Name.
}
