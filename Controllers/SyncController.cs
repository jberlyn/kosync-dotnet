using Microsoft.AspNetCore.Mvc;

namespace Kosync.Controllers;

[ApiController]
public class SyncController : ControllerBase
{
    private KosyncDb _db;

    public SyncController(KosyncDb db)
    {
        _db = db;
    }

    [HttpGet("/users/auth")]
    public ObjectResult AuthoriseUser()
    {
        string? username = Request.Headers["x-auth-user"];
        string? passwordHash = Request.Headers["x-auth-key"];

        if (username is null || passwordHash is null)
        {
            return StatusCode(401, new ErrorResponse
            {
                message = "Username and password invalid"
            });
        }

        var users = _db.Context.GetCollection<User>("users");

        var existing = users.FindOne(u => u.Username == username && u.PasswordHash == passwordHash);
        if (existing is null)
        {
            return StatusCode(401, new ErrorResponse
            {
                message = "Account could not be found"
            });
        }

        return StatusCode(200, new { username });
    }

    [HttpPost("/users/create")]
    public ObjectResult CreateUser(UserCreateRequest payload)
    {
        if (Environment.GetEnvironmentVariable("REGISTRATION_DISABLED") is not null)
        {
            return StatusCode(402, new ErrorResponse()
            {
                message = "Account registration is disabled"
            });
        }

        var users = _db.Context.GetCollection<User>("users");

        var existing = users.FindOne(u => u.Username == payload.username);
        if (existing is not null)
        {
            return StatusCode(402, new ErrorResponse()
            {
                message = "User already exists"
            });
        }

        var user = new User()
        {
            Username = payload.username,
            PasswordHash = payload.password
        };

        users.Insert(user);
        users.EnsureIndex(u => u.Username);

        return StatusCode(201, new RegistrationResponse()
        {
            username = payload.username
        });
    }

    [HttpPut("/syncs/progress")]
    public ObjectResult SyncProgress(DocumentRequest payload)
    {
        if (AuthorizeUser() == false)
        {
            return StatusCode(401, new ErrorResponse()
            {
                message = "Unauthorized"
            });
        }

        string? username = GetCredentials().Username;

        var users = _db.Context.GetCollection<User>("users").Include(i => i.Documents);

        var user = users.FindOne(i => i.Username == username);

        var document = user.Documents.SingleOrDefault(i => i.DocumentHash == payload.document);
        if (document is null)
        {
            document = new Document();
            document.DocumentHash = payload.document;
            user.Documents.Add(document);
        }

        document.Progress = payload.progress;
        document.Percentage = payload.percentage;
        document.Device = payload.device;
        document.DeviceId = payload.device_id;
        document.Timestamp = DateTime.UtcNow;

        users.Update(user);

        return StatusCode(200, new DocumentResponse()
        {
            document = document.DocumentHash,
            timestamp = document.Timestamp
        });
    }

    [HttpGet("/syncs/progress/{documentHash}")]
    public ObjectResult GetProgress(string documentHash)
    {
        if (AuthorizeUser() == false)
        {
            return StatusCode(401, new ErrorResponse()
            {
                message = "Unauthorized"
            });
        }

        string? username = GetCredentials().Username;

        var users = _db.Context.GetCollection<User>("users").Include(i => i.Documents);

        var user = users.FindOne(i => i.Username == username);

        var document = user.Documents.SingleOrDefault(i => i.DocumentHash == documentHash);

        if (document is null)
        {
            return StatusCode(502, new
            {
                message = "Document not found on server"
            });
        }

        return StatusCode(200, new DocumentRequest()
        {
            device = document.Device,
            device_id = document.DeviceId,
            document = document.DocumentHash,
            percentage = document.Percentage,
            progress = document.Progress
        });
    }

    private (string? Username, string? PasswordHash) GetCredentials()
    {
        return (Request.Headers["x-auth-user"], Request.Headers["x-auth-key"]);
    }

    private bool AuthorizeUser()
    {
        (string? username, string? passwordHash) = GetCredentials();
        var users = _db.Context.GetCollection<User>("users");
        return (users.FindOne(u => u.Username == username && u.PasswordHash == passwordHash) is not null);
    }
}
