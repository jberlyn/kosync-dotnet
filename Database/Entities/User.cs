namespace Kosync.Database.Entities;

public class User
{
    public int Id { get; set; }
    
    public string Username { get; set; } = default!;

    public string PasswordHash { get; set; } = default!;

    public List<Document> Documents { get; set; } = new();
}