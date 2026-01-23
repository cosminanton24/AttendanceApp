using InsuranceApp.Application.Common.Behaviors;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using AttendanceApp.Application;
using AttendanceApp.Infrastructure.Persistence;
using MediatR;
using AttendanceApp.Application.Common.Behaviors;
using AttendanceApp.Domain.Repositories;
using AttendanceApp.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);
 
 
builder.Services.AddControllers();
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
        options.UseSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection")
        )
    );
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
 
var app = builder.Build();
 
 
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
 
app.UseHttpsRedirection();
 
 
app.UseAuthorization();
 
app.MapControllers();
 
await app.RunAsync();
 
 
public partial class Program { protected Program() {} }
 
 