using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using HotChocolate.AspNetCore;
using BizCore.ApiGateway.GraphQL;
using BizCore.ApiGateway.Services;
using BizCore.ApiGateway.Middleware;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Serilog;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
            ValidAudience = builder.Configuration["JwtSettings:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:SecretKey"] ?? "your-secret-key"))
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ApiAccess", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("scope", "api.access");
    });
});

// Orleans Client
builder.Services.AddSingleton<IClusterClient>(provider =>
{
    var client = new ClientBuilder()
        .UseLocalhostClustering()
        .ConfigureApplicationParts(parts =>
        {
            parts.AddApplicationPart(typeof(BizCore.Orleans.Contracts.Accounting.IAccountGrain).Assembly);
            parts.AddApplicationPart(typeof(BizCore.Orleans.Contracts.Inventory.IProductGrain).Assembly);
            parts.AddApplicationPart(typeof(BizCore.Orleans.Contracts.Sales.ICustomerGrain).Assembly);
            parts.AddApplicationPart(typeof(BizCore.Orleans.Contracts.Purchasing.ISupplierGrain).Assembly);
        })
        .ConfigureLogging(logging => logging.AddSerilog())
        .Build();
    
    return client;
});

// YARP Reverse Proxy
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// GraphQL
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddInMemorySubscriptions()
    .AddAuthorization()
    .AddFiltering()
    .AddSorting()
    .AddProjections()
    .ModifyRequestOptions(opt => opt.IncludeExceptionDetails = builder.Environment.IsDevelopment());

// Custom Services
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<IAccountingService, AccountingService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<ISalesService, SalesService>();
builder.Services.AddScoped<IPurchasingService, PurchasingService>();

// Health Checks
builder.Services.AddHealthChecks()
    .AddCheck<OrleansHealthCheck>("orleans")
    .AddCheck<DatabaseHealthCheck>("database");

// API Documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "BizCore ERP API Gateway", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new()
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme."
    });
    c.AddSecurityRequirement(new()
    {
        {
            new()
            {
                Reference = new()
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.AddPolicy("ApiPolicy", context =>
    {
        var tenantId = context.Request.Headers["X-Tenant-Id"].FirstOrDefault();
        return RateLimitPartition.CreateTokenBucketLimiter(tenantId ?? "anonymous", _ =>
            new TokenBucketRateLimiterOptions
            {
                TokenLimit = 100,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 10,
                ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                TokensPerPeriod = 100,
                AutoReplenishment = true
            });
    });
});

// Response Caching
builder.Services.AddResponseCaching();
builder.Services.AddMemoryCache();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseHttpsRedirection();

app.UseCors();

app.UseResponseCaching();

app.UseRateLimiter();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Custom Middleware
app.UseMiddleware<TenantMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

// GraphQL endpoint
app.MapGraphQL("/graphql")
    .RequireAuthorization("ApiAccess");

// Health checks
app.MapHealthChecks("/health");

// Reverse Proxy
app.MapReverseProxy();

// Start Orleans Client
var clusterClient = app.Services.GetRequiredService<IClusterClient>();
try
{
    await clusterClient.Connect();
}
catch (Exception ex)
{
    Log.Error(ex, "Failed to connect to Orleans cluster");
}

try
{
    await app.RunAsync();
}
finally
{
    await clusterClient.Close();
    Log.CloseAndFlush();
}

public class Query
{
    public string GetWelcome() => "Welcome to BizCore ERP API Gateway";
}

public class Mutation
{
    public string Ping() => "Pong";
}