using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Metrics;
using StarLedger.Api;
using StarLedger.Api.Extensions;
using StarLedger.Api.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddAuthentication()
    .AddPolicyScheme("MultiScheme", "Bearer or Cookie", options =>
    {
        options.ForwardDefaultSelector = context =>
        {
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if (authHeader?.StartsWith("Bearer ") == true)
            {
                return IdentityConstants.BearerScheme;
            }
            //
            return IdentityConstants.ApplicationScheme;
        };
    })
    .AddBearerToken(IdentityConstants.BearerScheme, options =>
    {

    })
    .AddCookie(IdentityConstants.ApplicationScheme, options =>
    {
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
    });
var authSchemes = new List<string>
{
    IdentityConstants.ApplicationScheme,
    IdentityConstants.BearerScheme
};
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(AuthorizationPolicyConstants.OrganizationAdminPolicy, policy =>
    {
        policy.AddAuthenticationSchemes(authSchemes.ToArray());
        policy.RequireClaim(AuthorizationPolicyConstants.OrganizationClaimType);
        policy.RequireClaim(AuthorizationPolicyConstants.OrganizationAdminClaimType, "true");
    })
    .AddPolicy(AuthorizationPolicyConstants.OrganizationOwnerPolicy, policy =>
    {
        policy.AddAuthenticationSchemes(authSchemes.ToArray());
        policy.RequireClaim(AuthorizationPolicyConstants.OrganizationClaimType);
        policy.RequireClaim(AuthorizationPolicyConstants.OrganizationAdminClaimType, "true");
    })
    .AddPolicy(AuthorizationPolicyConstants.DeveloperPolicy, policy =>
    {
        policy.AddAuthenticationSchemes(authSchemes.ToArray());
        policy.RequireClaim(AuthorizationPolicyConstants.DeveloperPolicy, "true");
    });

SecretClient keyVaultClient =
    new SecretClient(new Uri(builder.Configuration["KeyVaultUri"]!), new DefaultAzureCredential());
var sqlConnectionString = keyVaultClient.GetSecret("starledger-sql-connectionstring").Value.Value;
builder.Services.AddDbContext<StarLedgerDbContext>(x =>
{
    x.UseSqlServer(sqlConnectionString);
    //x.AddSecretClient();
});
builder.Services.AddIdentityCore<User>()
    .AddEntityFrameworkStores<StarLedgerDbContext>()
    .AddApiEndpoints();

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

app.UseAuthentication();
app.UseAuthorization();

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

app.Run();
