using InsuranceApp.Application.Common.Behaviors;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using AttendanceApp.Application;
using AttendanceApp.Infrastructure.Persistence;
using MediatR;
using AttendanceApp.Application.Common.Behaviors;
using AttendanceApp.Domain.Repositories;
using AttendanceApp.Infrastructure.Repositories;
using System.Text;
using DotNetEnv;
using Microsoft.OpenApi;
using System.Text.Json.Serialization;


Env.Load("../.env");

var builder = WebApplication.CreateBuilder(args); 
 
builder.Services.AddControllersWithViews()
    .AddApplicationPart(typeof(AttendanceApp.Web.Controllers.AuthController).Assembly);
    
builder.Services.AddControllers()
  .AddJsonOptions(o =>
    o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddHttpClient();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
 
//mediatr
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(IAssemblyMarker).Assembly)
);
 
//db
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddDbContext<AttendanceAppDbContext>(options =>
    {
        var envConn = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");

        var connString = !string.IsNullOrWhiteSpace(envConn)
            ? envConn
            : builder.Configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connString))
            throw new InvalidOperationException("No PostgreSQL connection string configured.");

        options.UseNpgsql(connString);
    });
}
 
builder.Services.AddValidatorsFromAssembly(typeof(IAssemblyMarker).Assembly);
 
//pipelines
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ExceptionToResultBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
 
//repos
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ILectureRepository, LectureRepository>();
builder.Services.AddScoped<ILectureAttendeeRepository, LectureAttendeeRepository>();
builder.Services.AddScoped<IUserFollowingsRepository, UserFollowingsRepository>();

// Add JWT authentication
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? "dev_only_jwt_key_at_least_32_chars_long!!";
var key = Encoding.UTF8.GetBytes(jwtSecret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            if (context.Request.Cookies.TryGetValue("AttendanceApp.Jwt", out var token))
            {
                context.Token = token;
            }
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            context.HandleResponse();

            var req = context.HttpContext.Request;
            var res = context.HttpContext.Response;

            if (IsBrowserHtmlRequest(req))
            {
                res.Redirect($"/auth/login");
            }
            else
            {
                res.StatusCode = StatusCodes.Status401Unauthorized;
            }

            return Task.CompletedTask;
        }
    };
});

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CampusEats API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter: Bearer {your JWT token}"
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

builder.Services.AddAuthorization();
 
var app = builder.Build();
 
 
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
 
app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

await app.RunAsync();
 
static bool IsBrowserHtmlRequest(HttpRequest request)
{
    if (request.Headers.TryGetValue("Accept", out var accept) && accept.Any(a => a!.Contains("text/html", StringComparison.OrdinalIgnoreCase)))
    {
        return true;
    }

    if (request.Headers.TryGetValue("X-Requested-With", out var xrw) && xrw.Any(v => v!.Equals("XMLHttpRequest", StringComparison.OrdinalIgnoreCase)))
    {
         return false;
    }

    if (request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        return false;

    return string.Equals(request.Method, HttpMethods.Get, StringComparison.OrdinalIgnoreCase);
}
namespace AttendanceApp.Web
{
    public partial class WebHostMarker { protected WebHostMarker() { } }
}


