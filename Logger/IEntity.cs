namespace Logger;
/// <summary>
/// Implementations should provide an init-only Guid Id and a read-only Name.
/// Typically the Id is implemented once in an abstract base (implicit implementation) and Name is provided by each concrete type as a calculated property (explicit override) so there is no backing field.
/// </summary>
public interface IEntity
{
    /// <summary>
    /// Init-only setter so implementations can set it during object initialization.
    /// Provide this implicitly in an abstract base class to keep Id implementation centralized.
    /// </summary>
    Guid Id { get; init; }

    /// <summary>
    /// Implement this as a calculated property.
    /// </summary>
    string Name { get; }

}
