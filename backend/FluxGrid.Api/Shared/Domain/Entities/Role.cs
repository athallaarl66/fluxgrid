namespace FluxGrid.Api.Shared.Domain.Entities;

public class Role
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public List<User> Users { get; set; } = [];
    public List<string> Permissions { get; set; } = [];
}
