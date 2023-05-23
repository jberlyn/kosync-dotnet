namespace Kosync.Database.Entities;

public class User
{
    public int Id { get; set; }

    public string Username { get; set; } = default!;

    public string PasswordHash { get; set; } = default!;

    public bool IsActive { get; set; } = true;

    public bool IsAdministrator { get; set; } = false;

    public List<Document> Documents { get; set; } = new();
}