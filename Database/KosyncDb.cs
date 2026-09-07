namespace Kosync.Database;

public class KosyncDb : IDisposable
{
    public LiteDatabase Context { get; } = default!;

    public KosyncDb()
    {
        if (!Directory.Exists("data"))
        {
            Directory.CreateDirectory("data");
        }

        Context = new LiteDatabase("Filename=data/Kosync.db;Connection=direct");
        CreateDefaults();
    }

    public void CreateDefaults()
    {
        var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD");
        if (adminPassword is null)
        {
            adminPassword = "admin";
        }

        var userCollection = Context.GetCollection<User>("users");

        var adminUser = userCollection.FindOne(i => i.Username == "admin");
        if (adminUser is null)
        {
            adminUser = new User()
            {
                Username = "admin",
                IsAdministrator = true,
            };
            userCollection.Insert(adminUser);
        }

        adminUser.PasswordHash = Utility.HashPassword(adminPassword);

        userCollection.Update(adminUser);
        userCollection.EnsureIndex(i => i.Username);
    }

    public void Dispose()
    {
        Context?.Dispose();
    }
}