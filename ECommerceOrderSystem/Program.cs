using System.Security.Claims;
using System.Text;
using ECommerceOrderSystem.Application;
using ECommerceOrderSystem.Common;
using ECommerceOrderSystem.Data;
using ECommerceOrderSystem.Infrastructure.SeedData;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);
#region Database connection
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
#endregion

#region Identity
builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireDigit = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 10;
})
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager();

#endregion
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT key is missing.");

#region JWT Bearer
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultForbidScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(1),
        NameClaimType = ClaimTypes.Name,
        RoleClaimType = ClaimTypes.Role
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            if(string.IsNullOrEmpty(context.Token) && context.Request.Cookies.TryGetValue("access_token", out var token)) context.Token = token;
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            if(!context.Request.Path.StartsWithSegments("/api"))
            {
                context.HandleResponse();
                context.Response.Redirect($"/Account/Login?returnUrl={Uri.EscapeDataString(context.Request.Path + context.Request.QueryString)}");
            }
            return Task.CompletedTask;
        },
        OnForbidden = context => { if(!context.Request.Path.StartsWithSegments("/api")) context.Response.Redirect("/Account/AccessDenied"); return Task.CompletedTask; }
    };
});

#endregion 
builder.Services.AddAuthorization();
#region Services Registration
builder.Services.RegisterApplicationServices();
#endregion

builder.Services.AddControllersWithViews();
builder.Host.UseSerilog((context, services, configuration) => configuration
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "ECommerceOrderSystem")
    .WriteTo.Console()
    .WriteTo.File(
        "Logs/ecommerce-.log",
        rollingInterval: RollingInterval.Day,
        restrictedToMinimumLevel: LogEventLevel.Information,
        retainedFileCountLimit: 14));

try
{
    Log.Information("Starting ECommerceOrderSystem web application.");
    var app = builder.Build();
    await SeedData.SeedRoles(app.Services);
    await SeedData.SeedAdminUser(app.Services, builder.Configuration);
    Log.Information("Identity roles and admin seed data verified.");

    if(!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error"); app.UseHsts();
    }
    app.UseSerilogRequestLogging(options =>
    {
        options.GetLevel = (httpContext, elapsed, exception) =>
            exception is not null || httpContext.Response.StatusCode >= 500
                ? LogEventLevel.Error
                : elapsed >= 1000
                    ? LogEventLevel.Warning
                    : LogEventLevel.Debug;
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    });
    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllerRoute(name: "default", pattern: "{controller=Products}/{action=Index}/{id?}");
    app.Run();
}
catch(Exception exception)
{
    Log.Fatal(exception, "ECommerceOrderSystem web application terminated unexpectedly.");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}

