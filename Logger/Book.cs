namespace Logger;

/// <summary>
/// Book entity. Title required. Name derived from Title.
/// Implements IEntity via EntityBase. Name is calculated as a property.
/// </summary>
public sealed record Book(string Title, string? Author = null) : EntityBase
{
    // Id: implemented implicitly via EntityBase so all entities share the same init-only Guid Id logic.
    // Name: implemented explicitly here (override) as a calculated property from Title — there is no backing field so Name remains derived.
    public override string Name => Title;
}
