using ItemInventory2.DataLayer.ApplicationUsers;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AppUsersContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("UsersConnString")
    ?? Environment.GetEnvironmentVariable("ConnectionStrings__UsersConnString")));
builder.Services.AddScoped<AppUsersContext>();


var app = builder.Build();


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Items}/{action=Upload}/{id?}");

app.Run();
