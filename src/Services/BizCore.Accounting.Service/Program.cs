using BizCore.Accounting.Grains;
using BizCore.Application.Common.Interfaces;
using BizCore.Infrastructure.Services;
using Orleans.Configuration;
using Orleans.Hosting;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add Serilog
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add application services
builder.Services.AddScoped<IDateTime, DateTimeService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// Add Orleans
builder.Host.UseOrleans((context, siloBuilder) =>
{
    if (builder.Environment.IsDevelopment())
    {
        siloBuilder
            .UseLocalhostClustering()
            .UseInMemoryReminderService()
            .AddMemoryGrainStorageAsDefault()
            .AddMemoryStreams("Default")
            .UseDashboard(options => { });
    }
    else
    {
        siloBuilder
            .UseAdoNetClustering(options =>
            {
                options.ConnectionString = context.Configuration.GetConnectionString("OrleansDatabase");
                options.Invariant = "System.Data.SqlClient";
            })
            .UseAdoNetReminderService(options =>
            {
                options.ConnectionString = context.Configuration.GetConnectionString("OrleansDatabase");
                options.Invariant = "System.Data.SqlClient";
            })
            .AddAdoNetGrainStorageAsDefault(options =>
            {
                options.ConnectionString = context.Configuration.GetConnectionString("OrleansDatabase");
                options.Invariant = "System.Data.SqlClient";
            })
            .AddAzureEventHubStreams("Default", configurator =>
            {
                configurator.ConfigureEventHub(builder => builder.Configure(options =>
                {
                    options.ConnectionString = context.Configuration.GetConnectionString("EventHub");
                    options.ConsumerGroup = "accounting-service";
                    options.Path = "accounting-events";
                }));
                configurator.UseAzureTableCheckpointer(builder => builder.Configure(options =>
                {
                    options.ConnectionString = context.Configuration.GetConnectionString("AzureStorage");
                    options.TableName = "AccountingCheckpoints";
                }));
            })
            .UseDashboard(options => 
            {
                options.Username = "admin";
                options.Password = context.Configuration["Dashboard:Password"] ?? "admin123";
            });
    }

    siloBuilder.ConfigureLogging(logging => logging.AddSerilog());
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseSerilogRequestLogging();
app.MapControllers();

// Health checks
app.MapGet("/health", () => "Healthy");
app.MapGet("/health/ready", () => "Ready");

app.Run();

// Minimal implementation of CurrentUserService for demonstration
public class CurrentUserService : ICurrentUserService
{
    public string? UserId => "demo-user";
    public string? UserName => "Demo User";
    public string? Email => "demo@bizcore.com";
    public Guid? TenantId => Guid.Parse("550e8400-e29b-41d4-a716-446655440000");
    public bool IsAuthenticated => true;
    public IEnumerable<string> Roles => new[] { "Admin" };
    public IEnumerable<string> Permissions => new[] { "accounting.read", "accounting.write" };

    public bool HasRole(string role) => Roles.Contains(role);
    public bool HasPermission(string permission) => Permissions.Contains(permission);
}