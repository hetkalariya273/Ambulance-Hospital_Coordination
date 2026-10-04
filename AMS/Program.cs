using AMS.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using AMS.Models;
using AMS.Hubs;

var builder = WebApplication.CreateBuilder(args);

//ADDED DATABASE CONNECTION STRING
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

//ADDED IDENTITY SERVICES
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

//ADDED SERVICES TO THE CONTAINER
builder.Services.AddControllersWithViews();

builder.Services.AddSignalR();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var roleManager =
        scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    var userManager =
        scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    var configuration =
        scope.ServiceProvider.GetRequiredService<IConfiguration>();

    await IdentitySeeder.SeedAsync(
        roleManager,
        userManager,
        configuration);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();//WHO IS USER?
app.UseAuthorization();//IS THIS USER ALLOWED TO DO THIS?

app.MapStaticAssets();

app.MapHub<AmbulanceHub>("/ambulanceHub");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();