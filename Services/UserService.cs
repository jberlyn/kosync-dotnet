namespace Kosync.Services;

public class UserService
{
    private KosyncDb _db;

    private IHttpContextAccessor contextAccessor;

    private bool userLoadAttempted = false;

    public string? Username { get; private set; }

    private bool _isAuthenticated = false;
    public bool IsAuthenticated
    {
        get
        {
            LoadUser();
            return _isAuthenticated;
        }
    }

    private bool _isActive = false;

    public bool IsActive
    {
        get
        {
            LoadUser();
            return _isActive;
        }
    }

    private bool _isAdmin = false;

    public bool IsAdmin
    {
        get
        {
            LoadUser();
            return _isAdmin;
        }
    }

    public UserService(KosyncDb db, IHttpContextAccessor contextAccessor)
    {
        _db = db;
        this.contextAccessor = contextAccessor;
    }

    private void LoadUser()
    {
        if (userLoadAttempted) { return; }

        userLoadAttempted = true;

        Username = contextAccessor?.HttpContext?.Request.Headers["x-auth-user"];
        string? passwordHash = contextAccessor?.HttpContext?.Request.Headers["x-auth-key"];

        var userCollection = _db.Context.GetCollection<User>("users");

        var user = userCollection.FindOne(i => i.Username == Username && i.PasswordHash == passwordHash);

        if (user is null) { return; }

        _isAuthenticated = true;

        _isActive = user.IsActive;

        _isAdmin = user.IsAdministrator;
    }
}