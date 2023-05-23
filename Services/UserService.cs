namespace Kosync.Services;

public class UserService
{
    private KosyncDb _db;

    public UserService(KosyncDb db)
    {
        _db = db;
    }

    public (string? Username, string? PasswordHash) GetCredentials(HttpRequest request)
    {
        return (request.Headers["x-auth-user"], request.Headers["x-auth-key"]);
    }

    public bool IsAuthorised(HttpRequest request)
    {
        (string? username, string? passwordHash) = GetCredentials(request);

        var userCollection = _db.Context.GetCollection<User>("users");

        var user = userCollection.FindOne(i => i.Username == username && i.PasswordHash == passwordHash);

        return (user is not null && user.IsActive);
    }

    public bool IsAdminUser(HttpRequest request)
    {
        if (IsAuthorised(request) == false) return false;

        string? username = GetCredentials(request).Username;

        var userCollection = _db.Context.GetCollection<User>("users");

        var user = userCollection.FindOne(i => i.Username == username);

        return user?.IsAdministrator ?? false;
    }
}