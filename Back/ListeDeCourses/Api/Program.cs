using ListeDeCourses.Api.Repositories;
using ListeDeCourses.Api.Services;
using ListeDeCourses.Api.Extensions;
using FluentValidation;
using FluentValidation.AspNetCore;
using MongoDB.Driver;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using ListeDeCourses.Api.Settings;
using Microsoft.OpenApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Text;


var builder = WebApplication.CreateBuilder(args);

JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

builder.Services.AddSingleton<IMongoClient>(_ =>
    new MongoClient(builder.Configuration.GetConnectionString("MongoDb") ?? "mongodb://localhost:27017"));
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IMongoClient>().GetDatabase(
        builder.Configuration.GetValue<string>("MongoDbDatabase") ?? "liste-de-courses"));

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
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key));

const string FrontCors = "FrontCors";

var allowed = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontCors, policy =>
    {
        if (allowed.Length > 0) policy.WithOrigins(allowed).AllowAnyHeader().AllowAnyMethod();
        else policy.AllowAnyHeader().AllowAnyMethod(); // fallback dev
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontCors, policy =>
    {
        policy
            .WithOrigins("http://localhost:5173", "http://192.168.1.12:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
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

builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());

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


var app = builder.Build();

app.UseGlobalErrorHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(FrontCors);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
