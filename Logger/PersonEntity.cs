namespace Logger;

/// <summary>
/// Shared record for entities with a FullName.
/// This helps avoid duplicating code between Student and Employee.
/// </summary>
public abstract record PersonEntity(FullName FullName) : EntityBase
{
    // Id: inherited (implicit) from EntityBase so no duplication of the init-only Id implementation.
    // Name: implemented explicitly here (override) as a calculated property delegating to FullName.Name — avoids storing a separate backing field.
    public override string Name => FullName.Name;
}
