using FileDriveServer.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using Server.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

//master encryption key
var masterEncryptionKeyBase64 = builder.Configuration["EncryptionSettings:MasterKey"];
if (string.IsNullOrEmpty(masterEncryptionKeyBase64))
{
    throw new InvalidOperationException("Master encryption key is not configured. Please set 'EncryptionSettings:MasterKey' in appsettings.json or environment variables.");
}

// MongoDB Configuration
const string mongoConnectionString = "mongodb://localhost:27017";
const string databaseName = "Drive";

// Register Services
builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoConnectionString));
builder.Services.AddSingleton(s => s.GetRequiredService<IMongoClient>().GetDatabase(databaseName));
builder.Services.AddSingleton<UserService>();
builder.Services.AddSingleton<FileService>();
builder.Services.AddSingleton<JwtService>();
builder.Services.AddSingleton(s => new FileEncryptionService(masterEncryptionKeyBase64));

builder.Services.AddControllers();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Add your custom ObjectIdConverter to the list of converters
        options.JsonSerializerOptions.Converters.Add(new ObjectIdConverter());

        // You might have other JSON options configured here as well
        // options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
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
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
            ValidAudience = builder.Configuration["JwtSettings:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:Secret"])
            )
        };
    });

builder.WebHost.UseUrls("http://0.0.0.0:123"); // Listen on all IPs
var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();


// Enable CORS
app.UseCors(policy => policy
    .AllowAnyOrigin()
    .AllowAnyHeader()
    .AllowAnyMethod()
);

app.UseHttpsRedirection();
app.MapControllers();
app.Run();
