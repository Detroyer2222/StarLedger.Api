using System.Text.Json.Serialization;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Metrics;
using StarLedger.Api;
using StarLedger.Api.Extensions;
using StarLedger.Api.Models;

var builder = WebApplication.CreateBuilder(args);

#region Security Configuration

builder.Services.AddAuthentication().AddBearerToken(IdentityConstants.BearerScheme, options =>
{
    options.BearerTokenExpiration = TimeSpan.FromDays(1);
    options.RefreshTokenExpiration = TimeSpan.FromDays(30);
});
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(SecurityConstants.OrganizationAdminPolicy, policy =>
    {
        policy.AddAuthenticationSchemes(IdentityConstants.BearerScheme);
        policy.RequireClaim(SecurityConstants.OrganizationClaimType);
        policy.RequireRole(SecurityConstants.OrganizationAdminRole);
    })
    .AddPolicy(SecurityConstants.OrganizationOwnerPolicy, policy =>
    {
        policy.AddAuthenticationSchemes(IdentityConstants.BearerScheme);
        policy.RequireClaim(SecurityConstants.OrganizationClaimType);
        policy.RequireRole(SecurityConstants.OrganizationOwnerRole);
    })
    .AddPolicy(SecurityConstants.DeveloperPolicy, policy =>
    {
        policy.AddAuthenticationSchemes(IdentityConstants.BearerScheme);
        policy.RequireRole(SecurityConstants.DeveloperRole);
    });

#endregion

#region Database Configuration

SecretClient keyVaultClient =
    new SecretClient(new Uri(builder.Configuration["KeyVaultUri"]!), new DefaultAzureCredential());
var sqlConnectionString = keyVaultClient.GetSecret("starledger-sql-connectionstring").Value.Value;
builder.Services.AddDbContext<StarLedgerDbContext>(x =>
{
    x.UseSqlServer(sqlConnectionString);
    //x.AddSecretClient();
});
builder.Services.AddIdentityCore<User>(options =>
    {
        options.User.RequireUniqueEmail = true;
        //options.User.UserName = true;
    })
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<StarLedgerDbContext>()
    .AddApiEndpoints();

#endregion

#region Swagger Configuration

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition(IdentityConstants.BearerScheme,
        new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "Bearer",
            In = ParameterLocation.Header,
            Description = ".Net Bearer Authorization Header using the Bearer Scheme"
        });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "StarLedger.Api",
        Description = "This is the API for the Star Ledger website.",
        Contact = new OpenApiContact
        {
            Name = "Raphael Lutz",
            Email = "raphael.lutz.development@gmail.com",
            Url = new Uri("https://github.com/Detroyer2222"),
        }
    });
});

#endregion

#region Telemetry Configuration

//Prometheus Telemetry
builder.Services.AddOpenTelemetry()
    .WithMetrics(o =>
    {
        o.AddPrometheusExporter();
        o.AddMeter(
            "Microsoft.AspNetCore.Hosting",
            "Microsoft.AspNetCore.Server.Kestrel",
            "StarLedgerApi");
        o.AddView("request-duration",
            new ExplicitBucketHistogramConfiguration
            {
                Boundaries = new[] { 0, 0.005, 0.01, 0.025, 0.05, 0.075, 0.1, 0.25, 0.5, 0.75, 1, 2.5, 5, 7.5, 10 }
            });
    });

#endregion

#region Cors Setup

//Cors
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOriginsDev",
        policy =>
        {
            policy.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        });

    options.AddPolicy("StarLedgerWebsite",
        policy =>
        {
            policy.WithOrigins("https://star-ledger-website.vercel.app/")
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
});

#endregion

#region Other Configuration

builder.Services.AddProblemDetails();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.SerializerOptions.WriteIndented = true;
});

#endregion

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(o =>
{
    o.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler();
    //TODO: for production use the StarLedgerWebsite policy
    app.UseCors("AllowAllOriginsDev");
}
else
{
    app.UseDeveloperExceptionPage();
    app.UseCors("AllowAllOriginsDev");
}

app.UseHttpsRedirection();

#region Authentication and Authorization

SecurityConstants.ConfigureRoles(app.Services).Wait();

app.UseAuthentication();
app.UseAuthorization();

#endregion

#region Map Endpoints

//Map Metrics
app.MapPrometheusScrapingEndpoint();

//Map Identity Api Endpoints
app.RegisterIdentityEndpoints();
//Map User Api Endpoints
app.RegisterUserEndpoints();
//Map UserResource Api Endpoints
app.RegisterUserResourceEndpoints();
//Map Organization Api Endpoints
app.RegisterOrganizationEndpoints();
//Map Resource Api Endpoints
app.RegisterResourceEndpoints();

#endregion

app.Run();
