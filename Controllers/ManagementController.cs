using Microsoft.AspNetCore.Mvc;

namespace Kosync.Controllers;

[ApiController]
public class ManagementController : ControllerBase
{
    private KosyncDb _db;

    private UserService _userService;

    public ManagementController(KosyncDb db, UserService userService)
    {
        _db = db;
        _userService = userService;
    }

    [HttpGet("/manage/users")]
    public ObjectResult GetUsers()
    {
        if (_userService.IsAdminUser(Request) == false)
        {
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

        return StatusCode(200, users);
    }

    [HttpPost("/manage/users")]
    public ObjectResult CreateUser(UserCreateRequest payload)
    {
        if (_userService.IsAdminUser(Request) == false)
        {
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

        return StatusCode(200, new
        {
            message = "User created successfully"
        });
    }

    [HttpGet("/manage/users/documents")]
    public ObjectResult GetDocuments(string username)
    {
        if (_userService.IsAdminUser(Request) == false)
        {
            return StatusCode(401, new
            {
                message = "Unauthorized"
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

        return StatusCode(200, user.Documents);
    }

    [HttpPut("/manage/users/active")]
    public ObjectResult UpdateUserActive(string username)
    {
        if (_userService.IsAdminUser(Request) == false)
        {
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

        return StatusCode(200, new
        {
            message = user.IsActive ? "User marked as active" : "User marked as inactive"
        });
    }

    [HttpPut("/manage/users/password")]
    public ObjectResult UpdatePassword(string username, PasswordChangeRequest payload)
    {
        if (_userService.IsAdminUser(Request) == false)
        {
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

        user.PasswordHash = Utility.HashPassword(payload.password);
        userCollection.Update(user);

        return StatusCode(200, new
        {
            message = "Password changed successfully"
        });
    }
}