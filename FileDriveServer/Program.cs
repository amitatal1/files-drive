using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Server.Services;

var builder = WebApplication.CreateBuilder(args);

// MongoDB Configuration
const string mongoConnectionString = "mongodb://localhost:27017";
const string databaseName = "Drive";

// Register Services
builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoConnectionString));
builder.Services.AddSingleton(s => s.GetRequiredService<IMongoClient>().GetDatabase(databaseName));
builder.Services.AddSingleton<UserService>();
builder.Services.AddSingleton<FileService>();

builder.Services.AddControllers();

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


// Enable CORS
app.UseCors(builder => builder
    .AllowAnyOrigin()
    .AllowAnyHeader()
    .AllowAnyMethod()
);

app.UseHttpsRedirection();
app.MapControllers();
app.Run();
