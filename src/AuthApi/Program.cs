using AuthApi;
using AuthApi.Settings;
using Microsoft.Extensions.Options;

var builder = WebApplication
    .CreateBuilder(args);

var startup = new Startup(builder.Configuration);

startup.ConfigureServices(builder.Services);

var app = builder.Build();

startup.Configure(app, app.Environment);

// Check if email settings are correctly loaded
var emailSettings = app.Services.GetService<IOptions<EmailSettings>>();
if (emailSettings?.Value != null)
{
    app.Logger.LogInformation("Email settings successfully loaded");
}
else
{
    app.Logger.LogWarning("Email settings could not be loaded");
}

app.Run();