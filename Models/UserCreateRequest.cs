namespace Kosync.Models;

public class UserCreateRequest
{
    public string username { get; set; } = default!;

    public string password { get; set; } = default!;
}