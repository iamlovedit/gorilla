namespace Gorilla.Domain.Models;

public class EntityBase<T> where T : IEquatable<T>
{
    public required string Name { get; set; }

    public T Id { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public T CreatorId { get; set; }
}