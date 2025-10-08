using ListeDeCourses.Api.Repositories;
using ListeDeCourses.Api.Services;
using ListeDeCourses.Api.Extensions;
using FluentValidation;
using FluentValidation.AspNetCore;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ListeDeCourses.Api.Settings;
using Microsoft.OpenApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.DataProtection;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

var mongoUri = Environment.GetEnvironmentVariable("MONGODB_URI")
               ?? builder.Configuration.GetConnectionString("MongoDb")
               ?? "mongodb://localhost:27017";

var mongoUrl = new MongoUrl(mongoUri);

var configuredDbName = builder.Configuration.GetValue<string>("MongoDbDatabase");
var mongoDbName = !string.IsNullOrWhiteSpace(mongoUrl.DatabaseName)
    ? mongoUrl.DatabaseName
    : (string.IsNullOrWhiteSpace(configuredDbName) ? "ShopList" : configuredDbName);

var mongoSettings = MongoClientSettings.FromUrl(mongoUrl);
mongoSettings.ServerSelectionTimeout = TimeSpan.FromSeconds(5);

builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoSettings));
builder.Services.AddSingleton(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(mongoDbName));

builder.Services
    .AddScoped<UtilisateurRepository>()
    .AddScoped<IngredientRepository>()
    .AddScoped<PlatRepository>()
    .AddScoped<ListeRepository>();

builder.Services
    .AddScoped<UtilisateurService>()
    .AddScoped<IngredientService>()
    .AddScoped<PlatService>()
    .AddScoped<ListeService>();

builder.Services.AddHttpContextAccessor();

builder.Services.AddOptions<JwtSettings>()
    .Bind(builder.Configuration.GetSection("Jwt"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>() ?? new JwtSettings();
var jwtKey = Environment.GetEnvironmentVariable("JWT__KEY") ?? jwtSettings.Key ?? "dev-only-key-change-me";
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

const string FrontCors = "FrontCors";
var allowed = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontCors, policy =>
    {
        if (allowed.Length > 0)
            policy.WithOrigins(allowed).AllowAnyHeader().AllowAnyMethod();
        else
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.SaveToken = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            NameClaimType = "sub",
            RoleClaimType = "role"
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        o.JsonSerializerOptions.WriteIndented = false;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ListeDeCourses.Api", Version = "v1" });
    var scheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    };
    c.AddSecurityDefinition("Bearer", scheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement { { scheme, Array.Empty<string>() } });
});

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<ListeDeCourses.Api.Validators.UtilisateurCreateDtoValidator>();

builder.Services
    .AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/data/keys"));

var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseGlobalErrorHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

app.UseCors(FrontCors);
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/", () => Results.Ok(new { ok = true })).AllowAnonymous();

app.MapGet("/healthz", () => Results.Ok(new { ok = true })).AllowAnonymous();

app.MapGet("/healthz/db", async (IMongoDatabase db, ILoggerFactory lf, CancellationToken ct) =>
{
    var logger = lf.CreateLogger("HealthzDb");
    try
    {
        var result = await db.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: ct);
        return Results.Ok(new { ok = true, database = db.DatabaseNamespace.DatabaseName, ping = result.ToString() });
    }
    catch (MongoAuthenticationException mae)
    {
        logger.LogError(mae, "Mongo authentication failed");
        return Results.Problem(title: "Mongo authentication failed", statusCode: 500);
    }
    catch (TimeoutException te)
    {
        logger.LogError(te, "Mongo timeout (Network Access / IP allowlist ?)");
        return Results.Problem(title: "Mongo timeout (Network Access / IP allowlist ?)", statusCode: 504);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Mongo unexpected error");
        return Results.Problem(title: "Mongo unexpected error", statusCode: 500);
    }
}).AllowAnonymous();

app.Run();
