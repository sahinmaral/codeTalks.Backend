namespace Core.Persistence.Repositories;

public class Entity
{
    public string Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Entity()
    {
        Id = Guid.NewGuid().ToString();
    }


    public Entity(string id) : this()
    {
        Id = id;
    }

    public Entity(string id, DateTime createdAt) : this()
    {
        Id = id;
        CreatedAt = createdAt;
    }

    public Entity(string id, DateTime createdAt, DateTime? updatedAt) : this()
    {
        Id = id;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

}