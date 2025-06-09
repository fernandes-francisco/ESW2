using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using ESW2.Context; // Your DbContext namespace
using ESW2.Entities; // Your Entities namespace where estado_ativo is defined
using Npgsql; // <--- Add this for NpgsqlDataSourceBuilder and MapEnum
Npgsql.NpgsqlConnection.GlobalTypeMapper.EnableUnmappedTypes();

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);

builder.Services.AddDbContext<MyDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions =>
        {
            npgsqlOptions.MapEnum<estado_ativo>("estado_ativo");
        });
});

var dataSource = dataSourceBuilder.Build();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.Name = "UserAuthCookie";
        options.Events = new CookieAuthenticationEvents
        {
            OnRedirectToLogin = context =>
            {
                if (context.Request.Path.StartsWithSegments("/Home") ||
                    context.Request.Path.Value == "/" ||
                    context.Request.Path.Value == "/ESW2") 
                {
                    
                    context.Response.Redirect(options.LoginPath);
                }
                else
                {
                    context.Response.Redirect(context.RedirectUri);
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddDbContext<MyDbContext>(options =>
    options.UseNpgsql(dataSource) ); 

builder.Services.AddControllersWithViews();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=AtivoFinanceiro}/{action=Index}/{id?}");


app.Run();