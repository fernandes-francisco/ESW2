using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using ESW2.Context; // Your DbContext namespace
using ESW2.Entities; // Your Entities namespace where estado_ativo is defined
using Npgsql; // <--- Add this for NpgsqlDataSourceBuilder and MapEnum
Npgsql.NpgsqlConnection.GlobalTypeMapper.EnableUnmappedTypes();

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// 2. Create a DataSourceBuilder
var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);

builder.Services.AddDbContext<MyDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions =>
        {
            npgsqlOptions.MapEnum<estado_ativo>("estado_ativo");
        });
});

// 4. Build the data source
var dataSource = dataSourceBuilder.Build();
// --- End of DataSource Setup ---


// Configurar a autenticação com cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.Name = "UserAuthCookie";
        // Keep your custom OnRedirectToLogin logic if needed
        options.Events = new CookieAuthenticationEvents
        {
            OnRedirectToLogin = context =>
            {
                if (context.Request.Path.StartsWithSegments("/Home") ||
                    context.Request.Path.Value == "/" ||
                    context.Request.Path.Value == "/ESW2") // Consider if "/ESW2" is correct base path
                {
                    // Only redirect non-Home requests to login? Maybe adjust this logic.
                    // This current logic seems to *only* redirect Home/Root requests.
                    // Maybe you intended the opposite: context.Response.StatusCode = 401; return Task.CompletedTask;
                    // Or maybe redirect everything *except* specific public paths.
                    context.Response.Redirect(options.LoginPath);
                }
                else
                {
                    // Let ASP.NET Core handle the default redirect URI for other paths
                    context.Response.Redirect(context.RedirectUri);
                }
                return Task.CompletedTask;
            }
        };
    });

// Configurar a ligação à base de dados PostgreSQL
// MODIFY THIS line to use the dataSource
builder.Services.AddDbContext<MyDbContext>(options =>
    options.UseNpgsql(dataSource) ); // Optional: Add if your DB uses snake_case

// Adicionar controladores com vistas
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configuração do pipeline de processamento HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Consider carefully if HTTPS redirection is needed/configured
// app.UseHttpsRedirection(); // Commented out if not using HTTPS locally or if proxy handles it
app.UseStaticFiles();

app.UseRouting();

// Ativar a autenticação e autorização (Order matters: AuthN before AuthZ)
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=AtivoFinanceiro}/{action=Index}/{id?}");


app.Run();