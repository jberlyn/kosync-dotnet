using Microsoft.AspNetCore.Mvc;

namespace Kosync.Controllers;

[ApiController]
public class SyncController : ControllerBase
{
    private KosyncDb _db;

    private UserService _userService;

    public SyncController(KosyncDb db, UserService userService)
    {
        _db = db;
        _userService = userService;
    }

    [HttpGet("/healthcheck")]
    public ObjectResult HealthCheck()
    {
        return StatusCode(200, new
        {
            state = "OK"
        });
    }

    [HttpGet("/users/auth")]
    public ObjectResult AuthoriseUser()
    {
        string? username = Request.Headers["x-auth-user"];
        string? passwordHash = Request.Headers["x-auth-key"];

        if (username is null || passwordHash is null)
        {
            return StatusCode(401, new
            {
                message = "Username and password invalid"
            });
        }

        var userCollection = _db.Context.GetCollection<User>("users");

        var user = userCollection.FindOne(u => u.Username == username && u.PasswordHash == passwordHash);
        if (user is null)
        {
            return StatusCode(401, new
            {
                message = "User could not be found"
            });
        }

        if (user.IsActive == false)
        {
            return StatusCode(401, new
            {
                message = "User is inactive"
            });
        }

        return StatusCode(200, new
        {
            username = user.Username
        });
    }

    [HttpPost("/users/create")]
    public ObjectResult CreateUser(UserCreateRequest payload)
    {
        if (Environment.GetEnvironmentVariable("REGISTRATION_DISABLED") == "true")
        {
            return StatusCode(402, new
            {
                message = "User registration is disabled"
            });
        }

        var userCollection = _db.Context.GetCollection<User>("users");

        var existing = userCollection.FindOne(u => u.Username == payload.username);
        if (existing is not null)
        {
            return StatusCode(402, new
            {
                message = "User already exists"
            });
        }

        var user = new User()
        {
            Username = payload.username,
            PasswordHash = payload.password,
        };

        userCollection.Insert(user);
        userCollection.EnsureIndex(u => u.Username);

        return StatusCode(201, new
        {
            username = payload.username
        });
    }

    [HttpPut("/syncs/progress")]
    public ObjectResult SyncProgress(DocumentRequest payload)
    {
        if (_userService.IsAuthorised(Request) == false)
        {
            return StatusCode(401, new
            {
                message = "Unauthorized"
            });
        }

        string? username = _userService.GetCredentials(Request).Username;

        var userCollection = _db.Context.GetCollection<User>("users").Include(i => i.Documents);

        var user = userCollection.FindOne(i => i.Username == username);

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

        userCollection.Update(user);

        return StatusCode(200, new
        {
            document = document.DocumentHash,
            timestamp = document.Timestamp
        });
    }

    [HttpGet("/syncs/progress/{documentHash}")]
    public ObjectResult GetProgress(string documentHash)
    {
        if (_userService.IsAuthorised(Request) == false)
        {
            return StatusCode(401, new
            {
                message = "Unauthorized"
            });
        }

        string? username = _userService.GetCredentials(Request).Username;

        var userCollection = _db.Context.GetCollection<User>("users").Include(i => i.Documents);

        var user = userCollection.FindOne(i => i.Username == username);

        var document = user.Documents.SingleOrDefault(i => i.DocumentHash == documentHash);

        if (document is null)
        {
            return StatusCode(502, new
            {
                message = "Document not found on server"
            });
        }

        return StatusCode(200, new
        {
            device = document.Device,
            device_id = document.DeviceId,
            document = document.DocumentHash,
            percentage = document.Percentage,
            progress = document.Progress
        });
    }
}
