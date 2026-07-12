using System.Text.Json.Serialization;
using GolfClub.Api.Filters;
using GolfClub.Api.Middleware;
using GolfClub.Application;
using GolfClub.Application.Interfaces;
using GolfClub.Infrastructure;
using GolfClub.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

const string CorsPolicyName = "FrontendClients";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddScoped<AdminAuthFilter>();

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<GolfClubDbContext>();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    await DbSeeder.SeedAsync(context, passwordHasher);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors(CorsPolicyName);
app.UseAuthorization();
app.MapControllers();

app.Run();
