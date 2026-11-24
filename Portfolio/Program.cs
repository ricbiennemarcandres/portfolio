using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Npgsql;
using portfolio.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options =>
{
    // Only the [Required] attributes we write ourselves make a field required.
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
});

// ---------------------------------------------------------------------
//  Database: Supabase PostgreSQL through Npgsql.
//  The connection string (with the password) lives in user-secrets,
//  NOT in appsettings.json, so it never reaches GitHub. See README.md.
// ---------------------------------------------------------------------
var connectionString = builder.Configuration.GetConnectionString("Portfolio");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "The connection string 'Portfolio' is missing. Run:  " +
        "dotnet user-secrets set \"ConnectionStrings:Portfolio\" \"Host=...;Username=...;Password=...\"");
}
builder.Services.AddSingleton(NpgsqlDataSource.Create(connectionString));

// One repository class per table.
builder.Services.AddScoped<CourseRepository>();
builder.Services.AddScoped<ProjectRepository>();
builder.Services.AddScoped<ProjectImageRepository>();

// ---------------------------------------------------------------------
//  Editor sign-in (extra credit): a cookie, and a password kept in
//  user-secrets as "Editor:Password". Every editor controller has [Authorize].
// ---------------------------------------------------------------------
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        // Never send the sign-in cookie over plain http.
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    });
builder.Services.AddAuthorization();

// At most 5 sign-in attempts per minute from one address (AccountController.Login POST).
// Extra attempts get 429, shown by the friendly status page.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

var app = builder.Build();

// ---------------------------------------------------------------------
//  Errors: a friendly page for anything unexpected (500),
//  and a friendly page for 404 (e.g. an id that does not exist).
// ---------------------------------------------------------------------
app.UseExceptionHandler("/Home/Error");
app.UseStatusCodePagesWithReExecute("/Home/Status/{0}");

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

// /Editor/...  → Areas/Editor (signed-in side)
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

// everything else → viewer side
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
