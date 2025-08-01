using CopticDictionarynew1.Services;
using DEPI_Project1.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DEPI_Project1.Services;
using DEPI_Project1.Utilities;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(1);  // Session timeout duration
    options.Cookie.HttpOnly = true;               // Ensures the cookie is accessible only through HTTP requests
    options.Cookie.IsEssential = true;            // Marks the cookie as essential for GDPR compliance
});

// Configure authentication with cookie scheme
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.LoginPath = "/Account/Login";        // Redirect to Login when user is not authenticated
    options.LogoutPath = "/Account/Logout";      // Path for logging out
    options.ExpireTimeSpan = TimeSpan.FromHours(1);  // Cookie expiration time
    options.SlidingExpiration = true;            // Refreshes the cookie before expiration
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;  // Ensures the cookie is only sent over HTTPS
    options.Cookie.SameSite = SameSiteMode.Strict;  // Ensures strict SameSite enforcement
});

// Add this line to register HttpContextAccessor
builder.Services.AddHttpContextAccessor();

// Configure the DbContext to use SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("ConStr1"));
});

// Configure Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 4;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.User.AllowedUserNameCharacters = null; // Allow any characters in the username
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Services.AddScoped<IGoogleDriveService, LocalFileService>();
builder.Services.AddScoped<INormalizationService, NormalizationService>();

var app = builder.Build();

#region Perform database seeding and import Excel data

//using (var scope = app.Services.CreateScope())
//{
//    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
//    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
//    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

//    // Seed roles/users
//    //var dbInitializer = new DbInitializer(dbContext, roleManager, userManager);
//    //dbInitializer.Seed().Wait();

//    // Import Excel data
//    var excelImporter = new ExcelImporter();
//    excelImporter.ImportDataFromExcel("G:\\Coptic website\\result7 - 01.xlsx");

//    var dbService = new DatabaseService(dbContext);
//    dbService.InsertAllData(excelImporter);
//    Console.WriteLine("All data imported successfully!");
//}

#endregion
#region Database Initialization with Admin Account

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        // Ensure database is created and migrated
        await context.Database.MigrateAsync();
        logger.LogInformation("Database migrated successfully.");

        // Initialize database with roles and admin user
        await InitializeDatabaseAsync(userManager, roleManager, logger);
        
        logger.LogInformation("Database initialization completed successfully.");
    }
    catch (Exception ex)
    {
        var logger2 = services.GetRequiredService<ILogger<Program>>();
        logger2.LogError(ex, "An error occurred while initializing the database.");
    }
}

#endregion
// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error"); // Generic error handler in non-development environments
    app.UseHsts();                          // Use HSTS (Strict Transport Security)
}

app.UseHttpsRedirection();       // Redirects HTTP to HTTPS
app.UseStaticFiles();            // Serve static files
app.UseRouting();                // Routing middleware
app.UseSession();                // Enable session support
app.UseAuthentication();         // Enable authentication
app.UseAuthorization();          // Enable authorization

// Route configuration
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Exception handling for application startup
try
{
    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine($"Application startup error: {ex.Message}");
}
#region Database Initialization Methods

static async Task InitializeDatabaseAsync(
    UserManager<ApplicationUser> userManager, 
    RoleManager<IdentityRole> roleManager, 
    ILogger logger)
{
    try
    {
        // Create roles
        await CreateRolesAsync(roleManager, logger);
        
        // Create admin user
        await CreateAdminUserAsync(userManager, logger);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error during database initialization.");
        throw;
    }
}

static async Task CreateRolesAsync(RoleManager<IdentityRole> roleManager, ILogger logger)
{
    string[] roleNames = { "Admin", "User", "Moderator" };

    foreach (var roleName in roleNames)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            var result = await roleManager.CreateAsync(new IdentityRole(roleName));
            if (result.Succeeded)
            {
                logger.LogInformation($"✅ Role '{roleName}' created successfully.");
            }
            else
            {
                logger.LogError($"❌ Failed to create role '{roleName}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }
        else
        {
            logger.LogInformation($"Role '{roleName}' already exists.");
        }
    }
}

static async Task CreateAdminUserAsync(UserManager<ApplicationUser> userManager, ILogger logger)
{
    const string adminEmail = "fr.arsany@gmail.com";
    const string adminPassword = "Fr.arsany123#";

    // Check if admin user already exists
    var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
    if (existingAdmin != null)
    {
        logger.LogInformation($"⚠️  Admin user already exists: {adminEmail}");
        
        // Ensure admin has the Admin role
        if (!await userManager.IsInRoleAsync(existingAdmin, "Admin"))
        {
            await userManager.AddToRoleAsync(existingAdmin, "Admin");
            logger.LogInformation($"✅ Added Admin role to existing user: {adminEmail}");
        }
        return;
    }

    // Create new admin user
    var adminUser = new ApplicationUser
    {
        UserName = adminEmail,
        Email = adminEmail,
        EmailConfirmed = true
    };

    var result = await userManager.CreateAsync(adminUser, adminPassword);

    if (result.Succeeded)
    {
        // Add user to Admin role
        await userManager.AddToRoleAsync(adminUser, "Admin");
        
        logger.LogInformation($"✅ Admin user created successfully!");
        logger.LogInformation($"   Email: {adminEmail}");
        logger.LogInformation($"   Role: Admin");
        logger.LogInformation($"   Password: {adminPassword}");
    }
    else
    {
        logger.LogError($"❌ Error creating admin user:");
        foreach (var error in result.Errors)
        {
            logger.LogError($"   - {error.Description}");
        }
    }
}

#endregion

//using DEPI_Project1.Models;
//using Microsoft.AspNetCore.Authentication.Cookies;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.EntityFrameworkCore;

//using (var context = new ApplicationDbContext(optionsBuilder.Options))
//{
//    var excelImporter = new ExcelImporter();
//    excelImporter.ImportDataFromExcel("C:\\Users\\minam\\result6 - 01.xlsx");

//    var dbService = new DatabaseService(context);
//    dbService.InsertAllData(excelImporter);
//}

//Console.WriteLine("All data imported successfully!");

//var builder = WebApplication.CreateBuilder(args);

//// Add services to the container.
//builder.Services.AddControllersWithViews();

//// Configure session
//builder.Services.AddSession(options =>
//{
//    options.IdleTimeout = TimeSpan.FromHours(1);  // Session timeout duration
//    options.Cookie.HttpOnly = true;               // Ensures the cookie is accessible only through HTTP requests
//    options.Cookie.IsEssential = true;            // Marks the cookie as essential for GDPR compliance
//});

//// Configure authentication with cookie scheme
//builder.Services.AddAuthentication(options =>
//{
//    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
//    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
//})
//.AddCookie(options =>
//{
//    options.LoginPath = "/Account/Login";        // Redirect to Login when user is not authenticated
//    options.LogoutPath = "/Account/Logout";      // Path for logging out
//    options.ExpireTimeSpan = TimeSpan.FromHours(1);  // Cookie expiration time
//    options.SlidingExpiration = true;            // Refreshes the cookie before expiration
//    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;  // Ensures the cookie is only sent over HTTPS
//    options.Cookie.SameSite = SameSiteMode.Strict;  // Ensures strict SameSite enforcement
//});

//// Configure the DbContext to use SQL Server
//builder.Services.AddDbContext<ApplicationDbContext>(options =>
//{
//    options.UseSqlServer(builder.Configuration.GetConnectionString("ConStr1"));
//});

//// Add logging service
//builder.Logging.ClearProviders();  // Clear default logging providers (optional)
//builder.Logging.AddConsole();      // Add console logging
//builder.Logging.AddDebug();        // Add debug logging

////builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
////    .AddEntityFrameworkStores<ApplicationDbContext>();

//builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
//{
//    options.User.RequireUniqueEmail = true;
//    options.Password.RequireDigit = false;
//    options.Password.RequiredLength = 4;
//    options.Password.RequireDigit = false;
//    options.Password.RequireLowercase = false;
//    options.Password.RequireUppercase = false;
//    options.User.AllowedUserNameCharacters = null; // Allow any characters in the username
//})

//.AddEntityFrameworkStores<ApplicationDbContext>()
//.AddDefaultTokenProviders();
//var app = builder.Build();

//#region  Perform database seeding

//using (var scope = app.Services.CreateScope())
//{
//    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
//    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
//    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
//    var dbInitializer = new DbInitializer(dbContext,roleManager,userManager);
//    dbInitializer.Seed().Wait();  // Call the seeding logic
//}


//// Configure the HTTP request pipeline
//if (!app.Environment.IsDevelopment())
//{
//    app.UseExceptionHandler("/Home/Error"); // Generic error handler in non-development environments
//    app.UseHsts();                          // Use HSTS (Strict Transport Security)
//}

//app.UseHttpsRedirection();       // Redirects HTTP to HTTPS
//app.UseStaticFiles();            // Serve static files
//app.UseRouting();                // Routing middleware
//app.UseSession();                // Enable session support
//app.UseAuthentication();         // Enable authentication
//app.UseAuthorization();          // Enable authorization

//// Route configuration
//app.MapControllerRoute(
//    name: "default",
//    pattern: "{controller=Home}/{action=Index}/{id?}");

//// Exception handling for application startup
//try
//{
//    app.Run();
//}
//catch (Exception ex)
//{
//    // Log any unhandled exception at the application level
//    Console.WriteLine($"Application startup error: {ex.Message}");
//}


/////*****************************************************

//using Microsoft.EntityFrameworkCore;
//using Microsoft.AspNetCore.Session;
//using Microsoft.AspNetCore.Authentication.Cookies;

//var builder = WebApplication.CreateBuilder(args);

//// Add services to the container.
//builder.Services.AddControllersWithViews();
//builder.Services.AddSession(options =>
//{
//    options.IdleTimeout = TimeSpan.FromHours(1);
//    options.Cookie.HttpOnly = true;
//    options.Cookie.IsEssential = true;
//});

//builder.Services.AddAuthentication(options =>
//{
//    options.DefaultScheme = "Cookies";
//    options.DefaultChallengeScheme = "Cookies";
//})
//.AddCookie("Cookies", options =>
//{
//    options.LoginPath = "/Account/Login";
//    options.LogoutPath = "/Account/Logout";
//});

//builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("ConStr1")));

//var app = builder.Build();

//// Configure the HTTP request pipeline.
//if (!app.Environment.IsDevelopment())
//{
//    app.UseExceptionHandler("/Home/Error");
//    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
//    app.UseHsts();
//}

//app.UseHttpsRedirection();
//app.UseStaticFiles();

//app.UseRouting();
//app.UseSession();
//app.UseAuthentication(); // Add this line
//app.UseAuthorization();

//try
//{
//    app.MapControllerRoute(
//        name: "default", 
//        pattern: "{controller=Home}/{action=Index}/{id?}");
//}
//catch (Exception ex)
//{
//    // Log the exception
//    Console.WriteLine($"Error configuring routes: {ex.Message}");
//}

//app.Run();