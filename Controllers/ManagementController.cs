using Microsoft.AspNetCore.Mvc;

namespace Kosync.Controllers;

[ApiController]
public class ManagementController : ControllerBase
{
    private ILogger<ManagementController> _logger;

    private ProxyService _proxyService;
    private IPService _ipService;
    private KosyncDb _db;
    private UserService _userService;


    public ManagementController(ILogger<ManagementController> logger, ProxyService proxyService, IPService ipService, KosyncDb db, UserService userService)
    {
        _logger = logger;
        _proxyService = proxyService;
        _ipService = ipService;
        _db = db;
        _userService = userService;
    }

    [HttpGet("/manage/users")]
    public ObjectResult GetUsers()
    {
        if (!_userService.IsAuthenticated)
        {
            if (string.IsNullOrEmpty(_userService.Username))
            {
                LogWarning("Unauthenticated GET request to /manage/users.");
            }
            else
            {
                LogWarning($"Unauthenticated GET request to /manage/users with username [{_userService.Username}].");
            }

            return StatusCode(401, new
            {
                message = "Unauthorized"
            });
        }

        if (!_userService.IsAdmin)
        {
            LogWarning($"Unauthorized GET request to /manage/users from user [{_userService.Username}].");

            return StatusCode(401, new
            {
                message = "Unauthorized"
            });
        }

        if (!_userService.IsActive)
        {
            LogWarning($"GET request to /manage/users received from inactive user [{_userService.Username}].");

            return StatusCode(401, new
            {
                message = "Unauthorized"
            });
        }


        var userCollection = _db.Context.GetCollection<User>("users");

        var users = userCollection.FindAll().Select(i => new
        {
            id = i.Id,
            username = i.Username,
            isAdministrator = i.IsAdministrator,
            isActive = i.IsActive,
            documentCount = i.Documents.Count()
        });

        LogInfo($"User [{_userService.Username}] requested /manage/users");
        return StatusCode(200, users);
    }

    [HttpPost("/manage/users")]
    public ObjectResult CreateUser(UserCreateRequest payload)
    {
        if (!_userService.IsAuthenticated)
        {
            if (string.IsNullOrEmpty(_userService.Username))
            {
                LogWarning("Unauthenticated POST request to /manage/users.");
            }
            else
            {
                LogWarning($"Unauthenticated POST request to /manage/users with username [{_userService.Username}].");
            }

            return StatusCode(401, new
            {
                message = "Unauthorized"
            });
        }

        if (!_userService.IsAdmin)
        {
            LogWarning($"Unauthorized POST request to /manage/users from user [{_userService.Username}].");

            return StatusCode(401, new
            {
                message = "Unauthorized"
            });
        }

        if (!_userService.IsActive)
        {
            LogWarning($"POST request to /manage/users received from inactive user [{_userService.Username}].");

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

        LogInfo($"User [{payload.username}] created by user [{_userService.Username}]");
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
            if (string.IsNullOrEmpty(_userService.Username))
            {
                LogWarning("Unauthenticated GET request to /manage/users/documents.");
            }
            else
            {
                LogWarning($"Unauthenticated GET request to /manage/users/documents with username [{_userService.Username}].");
            }

            return StatusCode(401, new
            {
                message = "Unauthorized"
            });
        }


        if (!_userService.IsAdmin &&
            !username.Equals(_userService.Username, StringComparison.OrdinalIgnoreCase))
            // allow a user to request their own docs
        {
            LogWarning($"Unauthorized GET request to /manage/users/documents from user [{_userService.Username}].");

            return StatusCode(401, new
            {
                message = "Unauthorized"
            });
        }

        if (!_userService.IsActive)
        {
            LogWarning($"GET request to /manage/users/documents received from inactive user [{_userService.Username}].");

            return StatusCode(401, new
            {
                message = "Unauthorized"
            });
        }

        LogInfo($"User [{username}]'s documents requested by [{_userService.Username}]");

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
            if (string.IsNullOrEmpty(_userService.Username))
            {
                LogWarning("Unauthenticated PUT request to /manage/users/active.");
            }
            else
            {
                LogWarning($"Unauthenticated PUT request to /manage/users/active with username [{_userService.Username}].");
            }

            return StatusCode(401, new
            {
                message = "Unauthorized"
            });
        }

        if (!_userService.IsAdmin)
        {
            LogWarning($"Unauthorized PUT request to /manage/users/active from user [{_userService.Username}].");

            return StatusCode(401, new
            {
                message = "Unauthorized"
            });
        }

        if (!_userService.IsActive)
        {
            LogWarning($"PUT request to /manage/users/active received from inactive user [{_userService.Username}].");

            return StatusCode(401, new
            {
                message = "Unauthorized"
            });
        }

        if (username == "admin")
        {
            LogWarning($"Attempt to toggle admin user active from user [{_userService.Username}].");

            return StatusCode(400, new
            {
                message = "Cannot update admin user"
            });
        }

        var userCollection = _db.Context.GetCollection<User>("users");

        var user = userCollection.FindOne(i => i.Username == username);
        if (user is null)
        {
            LogInfo($"PUT request to /manage/users/active received from [{_userService.Username}] but target username [{username}] does not exist.");

            return StatusCode(400, new
            {
                message = "User does not exist"
            });
        }

        user.IsActive = !user.IsActive;
        userCollection.Update(user);

        LogInfo($"User [{username}] set to {(user.IsActive ? "active" : "inactive")} by user [{_userService.Username}]");

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
            if (string.IsNullOrEmpty(_userService.Username))
            {
                LogWarning("Unauthenticated PUT request to /manage/users/password.");
            }
            else
            {
                LogWarning($"Unauthenticated PUT request to /manage/users/password with username [{_userService.Username}].");
            }

            return StatusCode(401, new
            {
                message = "Unauthorized"
            });
        }

        if (!_userService.IsAdmin)
        {
            LogWarning($"Unauthorized PUT request to /manage/users/password from user [{_userService.Username}].");

            return StatusCode(401, new
            {
                message = "Unauthorized"
            });
        }

        if (!_userService.IsActive)
        {
            LogWarning($"PUT request to /manage/users/password received from inactive user [{_userService.Username}].");

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
            LogWarning($"Attempt to change admin password from user [{_userService.Username}].");
            return StatusCode(400, new
            {
                message = "Cannot update admin user"
            });
        }

        var userCollection = _db.Context.GetCollection<User>("users");

        var user = userCollection.FindOne(i => i.Username == username);
        if (user is null)
        {
            LogWarning($"Password change request received from [{_userService.Username}] but target username [{username}] does not exist.");
            return StatusCode(400, new
            {
                message = "User does not exist"
            });
        }

        user.PasswordHash = Utility.HashPassword(payload.password);
        userCollection.Update(user);

        LogInfo($"User [{username}]'s password updated by [{_userService.Username}].");
        return StatusCode(200, new
        {
            message = "Password changed successfully"
        });
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