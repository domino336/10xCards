using _10xCards.Application;
using _10xCards.Components;
using _10xCards.Persistance;
using _10xCards.Middleware;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;

// Configure Serilog BEFORE building the app
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        path: "logs/10xcards-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Starting 10xCards application...");

    var builder = WebApplication.CreateBuilder(args);

    // Replace default logging with Serilog
    builder.Host.UseSerilog();

// Add services to the container.
var services = builder.Services;
services.AddRazorComponents()
    .AddInteractiveServerComponents();
services.AddPersistance();
services.AddApplication();

// Add HttpClient for API calls
services.AddHttpClient();

// Add memory cache for admin metrics
services.AddMemoryCache();

// Identity configuration
services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 8;

    // User settings
    options.User.RequireUniqueEmail = true;

    // SignIn settings
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<CardsDbContext>()
.AddDefaultTokenProviders();

// Configure cookie for Blazor Server
services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = true;
});

    var app = builder.Build();

    // Use Serilog request logging
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent);
        };
    });

    // Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

    app.UseHttpsRedirection();

    // Add custom exception logging middleware
    app.UseExceptionLogging();

    app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// API endpoints for authentication (to avoid Blazor Server headers issue)
app.MapPost("/api/auth/login", async (
    LoginRequest request,
    SignInManager<IdentityUser> signInManager,
    ILogger<Program> logger,
    HttpContext httpContext) =>
{
    logger.LogInformation("Login attempt for {Email}", request.Email);
    
    var result = await signInManager.PasswordSignInAsync(
        request.Email, 
        request.Password, 
        request.RememberMe, 
        lockoutOnFailure: false);
    
    if (result.Succeeded)
    {
        logger.LogInformation("Login successful for {Email}, Cookie set: {HasCookie}", 
            request.Email, 
            httpContext.Response.Headers.ContainsKey("Set-Cookie"));
        return Results.Ok(new { success = true });
    }
    else if (result.IsLockedOut)
    {
        return Results.BadRequest(new { success = false, error = "Your account has been locked out." });
    }
    else if (result.RequiresTwoFactor)
    {
        return Results.BadRequest(new { success = false, error = "Two-factor authentication is required." });
    }
    else
    {
        logger.LogWarning("Login failed for {Email}", request.Email);
        return Results.BadRequest(new { success = false, error = "Invalid email or password." });
    }
})
.AllowAnonymous();

// Form-based login endpoint (for traditional HTML form submission)
app.MapPost("/api/auth/login-form", async (
    HttpContext httpContext,
    SignInManager<IdentityUser> signInManager,
    ILogger<Program> logger) =>
{
    var form = await httpContext.Request.ReadFormAsync();
    var email = form["email"].ToString();
    var password = form["password"].ToString();
    var rememberMe = form["rememberMe"].ToString() == "on";
    var returnUrl = form["returnUrl"].ToString();
    
    if (string.IsNullOrEmpty(returnUrl))
        returnUrl = "/cards";
    
    logger.LogInformation("Form login attempt for {Email}", email);
    
    var result = await signInManager.PasswordSignInAsync(email, password, rememberMe, lockoutOnFailure: false);
    
    if (result.Succeeded)
    {
        logger.LogInformation("Form login successful for {Email}", email);
        return Results.Redirect(returnUrl);
    }
    else
    {
        logger.LogWarning("Form login failed for {Email}", email);
        var error = result.IsLockedOut ? "Account locked out" : "Invalid email or password";
        return Results.Redirect($"/Account/Login?error={Uri.EscapeDataString(error)}");
    }
})
.AllowAnonymous();

app.MapPost("/api/auth/register", async (
    RegisterRequest request,
    UserManager<IdentityUser> userManager,
    SignInManager<IdentityUser> signInManager) =>
{
    var user = new IdentityUser
    {
        UserName = request.Email,
        Email = request.Email
    };

    var result = await userManager.CreateAsync(user, request.Password);

    if (result.Succeeded)
    {
        await signInManager.SignInAsync(user, isPersistent: false);
        return Results.Ok(new { success = true });
    }
    else
    {
        return Results.BadRequest(new { success = false, errors = result.Errors.Select(e => e.Description) });
    }
})
.AllowAnonymous();

// Apply migrations and seed test user for development
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        try
        {
            var context = scope.ServiceProvider.GetRequiredService<CardsDbContext>();
            
            // Apply pending migrations automatically
            await context.Database.MigrateAsync();
            Log.Information("Database migrations applied successfully.");
            
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            
            // Create Admin role if it doesn't exist
            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new IdentityRole("Admin"));
                Log.Information("Admin role created.");
            }
            
            // Create test user
            var testUser = await userManager.FindByEmailAsync("test@10xcards.local");
            
            if (testUser == null)
            {
                testUser = new IdentityUser
                {
                    UserName = "test@10xcards.local",
                    Email = "test@10xcards.local",
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(testUser, "Test123!");
                
                if (result.Succeeded)
                {
                    Log.Information("Test user created: test@10xcards.local / Test123!");
                }
                else
                {
                    Log.Error("Failed to create test user: {Errors}", 
                        string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                Log.Information("Test user already exists.");
            }
            
            // Create admin user
            var adminUser = await userManager.FindByEmailAsync("admin@10xcards.local");
            
            if (adminUser == null)
            {
                adminUser = new IdentityUser
                {
                    UserName = "admin@10xcards.local",
                    Email = "admin@10xcards.local",
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(adminUser, "Admin123!");
                
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                    Log.Information("Admin user created: admin@10xcards.local / Admin123!");
                }
                else
                {
                    Log.Error("Failed to create admin user: {Errors}", 
                        string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                // Ensure admin user has Admin role
                if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                    Log.Information("Admin role assigned to admin user.");
                }
                else
                {
                    Log.Information("Admin user already exists with Admin role.");
                }
            }
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "An error occurred while migrating or seeding the database.");
            throw;
        }
    }
}

app.Run();
Log.Information("10xCards application stopped.");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}

// Request DTOs for authentication API endpoints
record LoginRequest(string Email, string Password, bool RememberMe);
record RegisterRequest(string Email, string Password);
