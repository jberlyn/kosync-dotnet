using Microsoft.AspNetCore.Mvc;

namespace Kosync.Controllers;

[ApiController]
public class SyncController : ControllerBase
{
    private KosyncDb _db;

    private UserService _userService;

    private ILogger<SyncController> logger;

    public SyncController(KosyncDb db, UserService userService, ILogger<SyncController> logger)
    {
        _db = db;
        _userService = userService;
        this.logger = logger;
    }

    [HttpGet("/")]
    public IActionResult Index()
    {
        return Ok("kosync-dotnet server is running.");
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
            logger?.Log(LogLevel.Warning, "Login with invalid credentials attempted.");
            return StatusCode(401, new
            {
                message = "User could not be found"
            });
        }

        if (user.IsActive == false)
        {
            logger?.Log(LogLevel.Warning, $"Login with inactive account [{username}] attempted.");

            return StatusCode(401, new
            {
                message = "User is inactive"
            });
        }

        logger?.Log(LogLevel.Information, $"User {username} logged in.");
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
            logger?.Log(LogLevel.Warning, "Account creation attempted.");
            return StatusCode(402, new
            {
                message = "User registration is disabled"
            });
        }

        var userCollection = _db.Context.GetCollection<User>("users");

        var existing = userCollection.FindOne(u => u.Username == payload.username);
        if (existing is not null)
        {
            logger?.Log(LogLevel.Information, $"Attempted to create user that already exists - [{payload.username}].");
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


        logger?.Log(LogLevel.Information, $"User [{payload.username}] created.");
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
            logger?.Log(LogLevel.Warning, "Unauthorized progress update received.");

            return StatusCode(401, new
            {
                message = "Unauthorized"
            });
        }

        string? username = _userService.GetCredentials(Request).Username;

        logger?.Log(LogLevel.Information, $"Received progress update for user [{username}] with document hash [{payload.document}].");

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
            logger?.Log(LogLevel.Warning, "Unauthorized progress request received.");

            return StatusCode(401, new
            {
                message = "Unauthorized"
            });
        }

        string? username = _userService.GetCredentials(Request).Username;

        logger?.Log(LogLevel.Information, $"Received progress request for user [{username}] with document hash [{documentHash}].");

        var userCollection = _db.Context.GetCollection<User>("users").Include(i => i.Documents);

        var user = userCollection.FindOne(i => i.Username == username);

        var document = user.Documents.SingleOrDefault(i => i.DocumentHash == documentHash);

        if (document is null)
        {
            logger?.Log(LogLevel.Information, $"Document hash [{documentHash}] not found for user [{username}].");
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
