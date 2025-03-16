var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers();

builder.Services.AddScoped<KosyncDb, KosyncDb>();
builder.Services.AddScoped<UserService, UserService>();

var app = builder.Build();

app.MapControllers();

app.Run();
