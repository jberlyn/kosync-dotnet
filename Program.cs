var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSingleton<KosyncDb, KosyncDb>();

// Make connection to the database then dispose of it.
// This is just to ensure the database is created if it doesn't exist.
var db = new KosyncDb();
db.Context.Dispose();

var app = builder.Build();

app.MapControllers();

app.Run();
