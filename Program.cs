using Microsoft.Azure.Cosmos;
using Azure.Storage.Blobs;
using Microsoft.OpenApi.Models;
using System.Reflection;
using SocialMediaApi.Helpers;


// Initialisation de l'application
var builder = WebApplication.CreateBuilder(args);

// Configuration des services
builder.Services.AddControllers();
builder.Services.AddSwaggerGen();
builder.Services.AddEndpointsApiExplorer();

// Configuration de Cosmos DB
builder.Services.AddSingleton(s =>
{
    var configuration = builder.Configuration.GetSection("CosmosDb");
    var cosmosClient = new CosmosClient(configuration["Account"], configuration["Key"]);
    return cosmosClient;
});

builder.Services.AddSingleton(x =>
{
    // Récupération de la chaîne de connexion BlobStorage depuis la configuration
    var blobStorageConnectionString = builder.Configuration.GetSection("BlobStorage")["ConnectionString"];
    return new BlobServiceClient(blobStorageConnectionString);
});

// Configuration de Blob Storage
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Ajouter le filtre pour les fichiers
    options.OperationFilter<FileUploadOperationFilter>();
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SocialMediaApi",
        Version = "v1",
        Description = "API pour gérer les médias et les publications."
    });
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description =
            "Enter 'Bearer' [space] and then your valid token in the text input below.\r\n\r\nExample: \"Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9\"",
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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
            new string[] { }
        }
    });
});

// Construction de l'application
var app = builder.Build();

// Configuration du pipeline HTTP
// if (app.Environment.IsDevelopment())
// {
//     app.UseSwagger();
//     app.UseSwaggerUI();
// }

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.MapControllers();

// Exemple d'endpoint WeatherForecast
var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

// Définition de WeatherForecast
record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
