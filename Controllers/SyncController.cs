using Microsoft.AspNetCore.Mvc;

namespace Kosync.Controllers;

[ApiController]
public class SyncController : ControllerBase
{
    private ILogger<SyncController> _logger;

    private ProxyService _proxyService;
    private IPService _ipService;
    private KosyncDb _db;
    private UserService _userService;


    public SyncController(ILogger<SyncController> logger, ProxyService proxyService, IPService ipService, KosyncDb db, UserService userService)
    {
        _logger = logger;
        _proxyService = proxyService;
        _ipService = ipService;
        _db = db;
        _userService = userService;
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
            LogWarning("Request to /users/auth without credentials");

            return StatusCode(401, new
            {
                message = "Invalid credentials"
            });
        }

        if (!_userService.IsAuthenticated)
        {
            LogWarning($"Login to [{username}] attempted with invalid credentials.");

            return StatusCode(401, new
            {
                message = "User could not be found"
            });
        }

        if (!_userService.IsActive)
        {
            LogWarning($"Login to inactive account [{username}] attempted.");

            return StatusCode(401, new
            {
                message = "User is inactive"
            });
        }

        LogInfo($"User [{username}] logged in.");
        return StatusCode(200, new
        {
            username = _userService.Username
        });
    }

    [HttpPost("/users/create")]
    public ObjectResult CreateUser(UserCreateRequest payload)
    {
        if (Environment.GetEnvironmentVariable("REGISTRATION_DISABLED") == "true")
        {
            LogWarning("Account creation attempted but registration is disabled.");
            return StatusCode(402, new
            {
                message = "User registration is disabled"
            });
        }

        var userCollection = _db.Context.GetCollection<User>("users");

        var existing = userCollection.FindOne(u => u.Username == payload.username);
        if (existing is not null)
        {
            LogInfo($"Account creation attempted with existing username - [{payload.username}].");
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


        LogInfo($"User [{payload.username}] created.");
        return StatusCode(201, new
        {
            username = payload.username
        });
    }

    [HttpPut("/syncs/progress")]
    public ObjectResult SyncProgress(DocumentRequest payload)
    {
        if (!_userService.IsAuthenticated)
        {
            if (string.IsNullOrEmpty(_userService.Username))
            {
                LogWarning("Unauthenticated progress update received.");
            }
            else
            {
                LogWarning($"Unauthenticated progress update for user [{_userService.Username}] received.");
            }

            return StatusCode(401, new
            {
                message = "Unauthorized"
            });
        }

        if (!_userService.IsActive)
        {
            LogWarning($"Progress update from inactive user [{_userService.Username}] received.");

            return StatusCode(401, new
            {
                message = "Unauthorized"
            });
        }

        var userCollection = _db.Context.GetCollection<User>("users").Include(i => i.Documents);

        var user = userCollection.FindOne(i => i.Username == _userService.Username);

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

        LogInfo($"Received progress update for user [{_userService.Username}] from device [{payload.device}] with document hash [{payload.document}].");
        return StatusCode(200, new
        {
            document = document.DocumentHash,
            timestamp = document.Timestamp
        });
    }

    [HttpGet("/syncs/progress/{documentHash}")]
    public IActionResult GetProgress(string documentHash)
    {
        if (!_userService.IsAuthenticated)
        {
            if (string.IsNullOrEmpty(_userService.Username))
            {
                LogWarning("Unauthenticated progress request received.");
            }
            else
            {
                LogWarning($"Unauthenticated progress request for user [{_userService.Username}] received.");
            }

            return StatusCode(401, new
            {
                message = "Unauthorized"
            });
        }

        if (!_userService.IsActive)
        {
            LogWarning($"Progress request from inactive user [{_userService.Username}] received.");

            return StatusCode(401, new
            {
                message = "Unauthorized"
            });
        }

        var userCollection = _db.Context.GetCollection<User>("users").Include(i => i.Documents);

        var user = userCollection.FindOne(i => i.Username == _userService.Username);

        var document = user.Documents.SingleOrDefault(i => i.DocumentHash == documentHash);

        if (document is null)
        {
            LogInfo($"Document hash [{documentHash}] not found for user [{_userService.Username}].");
            return StatusCode(502, new
            {
                message = "Document not found on server"
            });
        }

        LogInfo($"Received progress request for user [{_userService.Username}] with document hash [{documentHash}].");

        var time = new DateTimeOffset(document.Timestamp);

        var result = new
        {
            device = document.Device,
            device_id = document.DeviceId,
            document = document.DocumentHash,
            percentage = document.Percentage,
            progress = document.Progress,
            timestamp = time.ToUnixTimeSeconds()
        };

        string json = System.Text.Json.JsonSerializer.Serialize(result);

        return new ContentResult()
        {
            Content = json,
            ContentType = "application/json",
            StatusCode = 200
        };
    }

    private void LogInfo(string text)
    {
        Log(LogLevel.Information, text);
    }

    private void LogWarning(string text)
    {
        Log(LogLevel.Warning, text);
    }

    private void Log(LogLevel level, string text)
    {
        string logMsg = $"[{DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")}] [{_ipService.ClientIP}]";


        // If trusted proxies are set but this request comes from another address, mark it
        if (_proxyService.TrustedProxies.Length > 0 &&
            !_ipService.TrustedProxy)
        {
            logMsg += "*";
        }

        logMsg += $" {text}";

        _logger?.Log(level, logMsg);
    }
}
