using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StarEvents.Data;
using Microsoft.AspNetCore.Identity.UI.Services;
using StarEvents.Services;

var builder = WebApplication.CreateBuilder(args);

// Add Dummy EmailSender
builder.Services.AddSingleton<IEmailSender, DummyEmailSender>();

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure SQL Server + Identity
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Identity with Roles
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure application cookie
builder.Services.ConfigureApplicationCookie(options =>
{
    // These apply only when trying to access restricted areas
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
});

// Add Razor Pages if using Identity UI
builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // Ensure static files are served
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Default route → HomeController.Index (your landing page)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

// Seed the database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await DbInitializer.SeedAsync(services);
}

await app.RunAsync();
