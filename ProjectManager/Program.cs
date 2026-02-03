using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Azure.Cosmos;
using ProjectManager.Data;
using ProjectManager.Models;
using ProjectManager.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Identity with SQLite
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=identity.db"));

builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddRazorPages();

// Cosmos config
string endpoint = Environment.GetEnvironmentVariable("EndpointUrl") ?? builder.Configuration["CosmosDb:EndpointUrl"];
string key = Environment.GetEnvironmentVariable("PrimaryKey") ?? builder.Configuration["CosmosDb:PrimaryKey"];
var cosmosConfig = builder.Configuration.GetSection("CosmosDb");
string dbName = cosmosConfig["DatabaseName"] ?? "ProjectDb";
string projectsContainer = cosmosConfig["ProjectsContainer"] ?? "Projects";
string tasksContainer = cosmosConfig["TasksContainer"] ?? "Tasks";

if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(key))
{
    throw new InvalidOperationException(
        "Cosmos DB configuration missing. Set EndpointUrl and PrimaryKey as environment variables or in appsettings.Development.json.");
}

var cosmosClient = new CosmosClient(endpoint, key);
builder.Services.AddSingleton(cosmosClient);

// Ensure DB and two containers exist
async Task EnsureCosmosSetupAsync()
{
    try
    {
        var dbResponse = await cosmosClient.CreateDatabaseIfNotExistsAsync(dbName);

        await dbResponse.Database.CreateContainerIfNotExistsAsync(new ContainerProperties(projectsContainer, "/id"));
        await dbResponse.Database.CreateContainerIfNotExistsAsync(new ContainerProperties(tasksContainer, "/ProjectId"));
    }
    catch (CosmosException ex) when ((int)ex.StatusCode == 400 && ex.SubStatusCode == 1028)
    {
        Console.WriteLine("Cosmos DB throughput limit reached. Containers were not created automatically.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error during Cosmos DB setup: {ex.Message}");
        throw;
    }
}

EnsureCosmosSetupAsync().GetAwaiter().GetResult();

// Register services
builder.Services.AddSingleton(typeof(CosmosDbService<Project>), sp =>
    new CosmosDbService<Project>(cosmosClient, dbName, projectsContainer));
builder.Services.AddSingleton(typeof(CosmosDbService<TaskItem>), sp =>
    new CosmosDbService<TaskItem>(cosmosClient, dbName, tasksContainer));
builder.Services.AddSingleton(typeof(CosmosDbService<TeamMember>), sp =>
    new CosmosDbService<TeamMember>(cosmosClient, dbName, projectsContainer));

var app = builder.Build();

// Seed roles and admin
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

    string[] roles = new[] { "Admin", "User" };
    foreach (var r in roles)
    {
        if (!await roleManager.RoleExistsAsync(r))
            await roleManager.CreateAsync(new IdentityRole(r));
    }

    var adminEmail = "admin@example.com";
    var admin = await userManager.FindByEmailAsync(adminEmail);
    if (admin == null)
    {
        admin = new IdentityUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
        var createResult = await userManager.CreateAsync(admin, "Admin123!");
        if (createResult.Succeeded)
            await userManager.AddToRoleAsync(admin, "Admin");
        else
            Console.WriteLine("Failed to create admin user: " + string.Join(", ", createResult.Errors.Select(e => e.Description)));
    }
}

// Configure middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
