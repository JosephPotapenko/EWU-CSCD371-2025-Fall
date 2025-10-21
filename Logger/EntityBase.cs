using System;

namespace Logger;

/// <summary>
/// Abstract base that implements IEntity.Id but leaves Name to be provided by derived classes.
/// </summary>
public abstract class EntityBase : IEntity
{
    // Id: implemented here implicitly to provide a single init-only Guid Id implementation for all derived entities.
    public Guid Id { get; init; }

    // Name: left abstract so derived classes must provide a calculated, backing-field-free implementation.
    public abstract string Name { get; }
}
