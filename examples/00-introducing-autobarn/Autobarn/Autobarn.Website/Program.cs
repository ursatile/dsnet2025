using Autobarn.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var logger = LoggerFactory.Create(loggingBuilder => loggingBuilder.AddConsole()).CreateLogger<Program>();

logger.LogInformation("Using in-memory database");
SqliteConnection sqliteConnection = new($"Data Source=:memory:");
sqliteConnection.Open();
builder.Services.AddDbContext<AutobarnDbContext>(options => options.UseSqlite(sqliteConnection));

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.Configure<RouteOptions>(options => options.LowercaseUrls = true);

var app = builder.Build();

using var scope = app.Services.CreateScope();
await using var db = scope.ServiceProvider.GetRequiredService<AutobarnDbContext>();
db.Database.EnsureCreated();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
