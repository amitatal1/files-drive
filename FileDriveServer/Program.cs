using FileDriveServer.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using Server.Services;
using System.Text;
using System.IO;
using System;
using System.Text.Json.Serialization;

// Helper to retrieve secrets from Docker secrets or environment variables
string GetSecret(string secretName, string fallbackEnvVarName = null)
{
    var secretPath = Path.Combine("/run/secrets", secretName);
    if (File.Exists(secretPath))
    {
        return File.ReadAllText(secretPath).Trim();
    }

    if (fallbackEnvVarName != null)
    {
        var envValue = Environment.GetEnvironmentVariable(fallbackEnvVarName);
        if (!string.IsNullOrEmpty(envValue))
        {
            return envValue;
        }
    }
    throw new InvalidOperationException($"Secret '{secretName}' not found. Ensure it's mounted via Docker secrets or provided as environment variable '{fallbackEnvVarName}'.");
}


var builder = WebApplication.CreateBuilder(args);

// Retrieve sensitive settings using the GetSecret helper
var jwtSecret = GetSecret("jwt_secret.txt", "JWT_SECRET");
var jwtIssuer = GetSecret("jwt_issuer.txt", "JWT_ISSUER");
var jwtAudience = GetSecret("jwt_audience.txt", "JWT_AUDIENCE");
var masterEncryptionKeyBase64 = GetSecret("encryption_master_key.txt", "MASTER_ENCRYPTION_KEY"); 
int jwtExpiryMinutes = 60;


// MongoDB Configuration
const string mongoConnectionString = "mongodb://admin:password@mongodb:27017/?authSource=admin"; // Use 'mongodb' as hostname for Docker Compose
const string databaseName = "Drive";

// Register services
builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoConnectionString));
builder.Services.AddSingleton(s => s.GetRequiredService<IMongoClient>().GetDatabase(databaseName));
builder.Services.AddSingleton<UserService>();
// Register JwtService with the retrieved secret values
builder.Services.AddSingleton(s => new JwtService(jwtSecret, jwtIssuer, jwtAudience, jwtExpiryMinutes));
builder.Services.AddSingleton(s => new FileEncryptionService(masterEncryptionKeyBase64));
builder.Services.AddSingleton<FileService>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new ObjectIdConverter());
    });

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer, 
            ValidAudience = jwtAudience, 
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSecret) 
            )
        };
    });

builder.WebHost.UseUrls("http://0.0.0.0:123");
var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// Enable CORS
app.UseCors(policy => policy
    .AllowAnyOrigin()
    .AllowAnyHeader()
    .AllowAnyMethod()
);

app.Run();
app.MapControllers();
