using System.Net;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();


builder.Services.AddSingleton<ProxyService, ProxyService>();
builder.Services.AddScoped<IPService, IPService>();
builder.Services.AddScoped<UserService, UserService>();
builder.Services.AddScoped<KosyncDb, KosyncDb>();


builder.Services.AddControllers();


var app = builder.Build();

app.UseForwardedHeaders();

app.MapControllers();



app.Run();
