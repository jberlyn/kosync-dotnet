using Microsoft.AspNetCore.Mvc;

namespace Kosync.Controllers;

[ApiController]
public class ManagementController : ControllerBase
{
    private KosyncDb _db;

    private UserService _userService;

    private ILogger<ManagementController> _logger;

    public ManagementController(KosyncDb db, UserService userService, ILogger<ManagementController> logger)
    {
        _db = db;
        _userService = userService;
        _logger = logger;
    }

    [HttpGet("/manage/users")]
    public ObjectResult GetUsers()
    {
        if (!_userService.IsAuthenticated)
        {
            _logger?.Log(LogLevel.Warning, "Unauthenticated GET request to /manage/users.");

            return StatusCode(401, new
            {
                message = "Unauthorized"
            });
        }

        if (!_userService.IsAdmin)
        {
            _logger?.Log(LogLevel.Warning, $"Unauthorized GET request to /manage/users from user [{_userService.Username}].");

            return StatusCode(401, new
            {
                message = "Unauthorized"
            });
        }

        if (!_userService.IsActive)
        {
            _logger?.Log(LogLevel.Warning, $"GET request to /manage/users received from inactive user [{_userService.Username}].");

            return StatusCode(401, new
            {
                message = "Unauthorized"
            });
        }

        _logger?.Log(LogLevel.Information, $"User [{_userService.Username}] requested /manage/users");

        var userCollection = _db.Context.GetCollection<User>("users");

        var users = userCollection.FindAll().Select(i => new
        {
            id = i.Id,
            username = i.Username,
            isAdministrator = i.IsAdministrator,
            isActive = i.IsActive,
            documentCount = i.Documents.Count()
        });

        return StatusCode(200, users);
    }

    [HttpPost("/manage/users")]
    public ObjectResult CreateUser(UserCreateRequest payload)
    {
        if (!_userService.IsAuthenticated)
        {
            _logger?.Log(LogLevel.Warning, "Unauthenticated POST request to /manage/users.");

            return StatusCode(401, new
            {
                message = "Unauthorized"
            });
        }

        if (!_userService.IsAdmin)
        {
            _logger?.Log(LogLevel.Warning, $"Unauthorized POST request to /manage/users from user [{_userService.Username}].");

            return StatusCode(401, new
            {
                message = "Unauthorized"
            });
        }

        if (!_userService.IsActive)
        {
            _logger?.Log(LogLevel.Warning, $"POST request to /manage/users received from inactive user [{_userService.Username}].");

            return StatusCode(401, new
            {
                message = "Unauthorized"
            });
        }

        var userCollection = _db.Context.GetCollection<User>("users");

        var existingUser = userCollection.FindOne(i => i.Username == payload.username);
        if (existingUser is not null)
        {
            return StatusCode(400, new
            {
                message = "User already exists"
            });
        }

        var passwordHash = Utility.HashPassword(payload.password);

        var user = new User()
        {
            Username = payload.username,
            PasswordHash = passwordHash,
            IsAdministrator = false
        };

        userCollection.Insert(user);
        userCollection.EnsureIndex(u => u.Username);

        _logger?.Log(LogLevel.Information, $"User [{payload.username}] created by user [{_userService.Username}]");

        return StatusCode(200, new
        {
            message = "User created successfully"
        });
    }

    [HttpGet("/manage/users/documents")]
    public ObjectResult GetDocuments(string username)
    {
        if (!_userService.IsAuthenticated)
        {
            _logger?.Log(LogLevel.Warning, "Unauthenticated GET request to /manage/users/documents.");

            return StatusCode(401, new
            {
                message = "Unauthorized"
            });
        }


        if (!_userService.IsAdmin &&
            !username.Equals(_userService.Username, StringComparison.OrdinalIgnoreCase))
            // allow a user to request their own docs
        {
            _logger?.Log(LogLevel.Warning, $"Unauthorized GET request to /manage/users/documents from user [{_userService.Username}].");

            return StatusCode(401, new
            {
                message = "Unauthorized"
            });
        }

        if (!_userService.IsActive)
        {
            _logger?.Log(LogLevel.Warning, $"GET request to /manage/users/documents received from inactive user [{_userService.Username}].");

            return StatusCode(401, new
            {
                message = "Unauthorized"
            });
        }

        _logger?.Log(LogLevel.Information, $"User [{username}]'s documents requested by [{_userService.Username}]");

        var userCollection = _db.Context.GetCollection<User>("users");

        var user = userCollection.FindOne(i => i.Username == username);
        if (user is null)
        {
            return StatusCode(400, new
            {
                message = "User does not exist"
            });
        }

        return StatusCode(200, user.Documents);
    }

    [HttpPut("/manage/users/active")]
    public ObjectResult UpdateUserActive(string username)
    {
        if (!_userService.IsAuthenticated)
        {
            _logger?.Log(LogLevel.Warning, "Unauthenticated PUT request to /manage/users/active.");

            return StatusCode(401, new
            {
                message = "Unauthorized"
            });
        }

        if (!_userService.IsAdmin)
        {
            _logger?.Log(LogLevel.Warning, $"Unauthorized PUT request to /manage/users/active from user [{_userService.Username}].");

            return StatusCode(401, new
            {
                message = "Unauthorized"
            });
        }

        if (!_userService.IsActive)
        {
            _logger?.Log(LogLevel.Warning, $"PUT request to /manage/users/active received from inactive user [{_userService.Username}].");

            return StatusCode(401, new
            {
                message = "Unauthorized"
            });
        }

        if (username == "admin")
        {
            return StatusCode(400, new
            {
                message = "Cannot update admin user"
            });
        }

        var userCollection = _db.Context.GetCollection<User>("users");

        var user = userCollection.FindOne(i => i.Username == username);
        if (user is null)
        {
            return StatusCode(400, new
            {
                message = "User does not exist"
            });
        }

        user.IsActive = !user.IsActive;
        userCollection.Update(user);

        _logger?.Log(LogLevel.Information, $"User [{username}] set to {(user.IsActive ? "active" : "inactive")} by user [{_userService.Username}]");

        return StatusCode(200, new
        {
            message = user.IsActive ? "User marked as active" : "User marked as inactive"
        });
    }

    [HttpPut("/manage/users/password")]
    public ObjectResult UpdatePassword(string username, PasswordChangeRequest payload)
    {
        if (!_userService.IsAuthenticated)
        {
            _logger?.Log(LogLevel.Warning, "Unauthenticated PUT request to /manage/users/password.");

            return StatusCode(401, new
            {
                message = "Unauthorized"
            });
        }

        if (!_userService.IsAdmin)
        {
            _logger?.Log(LogLevel.Warning, $"Unauthorized PUT request to /manage/users/password from user [{_userService.Username}].");

            return StatusCode(401, new
            {
                message = "Unauthorized"
            });
        }

        if (!_userService.IsActive)
        {
            _logger?.Log(LogLevel.Warning, $"PUT request to /manage/users/password received from inactive user [{_userService.Username}].");

            return StatusCode(401, new
            {
                message = "Unauthorized"
            });
        }

        // KOReader will literally not attempt to log in with a blank password field or with just whitespace
        if (string.IsNullOrWhiteSpace(payload.password))
        {
            return StatusCode(400, new
            {
                message = "Password cannot be empty or whitespace"
            });
        }

        if (username == "admin")
        {
            return StatusCode(400, new
            {
                message = "Cannot update admin user"
            });
        }

        var userCollection = _db.Context.GetCollection<User>("users");

        var user = userCollection.FindOne(i => i.Username == username);
        if (user is null)
        {
            return StatusCode(400, new
            {
                message = "User does not exist"
            });
        }

        user.PasswordHash = Utility.HashPassword(payload.password);
        userCollection.Update(user);

        _logger?.Log(LogLevel.Information, $"User [{username}]'s password updated by [{_userService.Username}].");

        return StatusCode(200, new
        {
            message = "Password changed successfully"
        });
    }
}