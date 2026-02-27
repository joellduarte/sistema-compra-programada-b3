using CompraProgramada.Infrastructure;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Infrastructure (EF Core + MySQL)
builder.Services.AddInfrastructure(builder.Configuration);

// Controllers
builder.Services.AddControllers();

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Compra Programada API",
        Version = "v1",
        Description = "Sistema de Compra Programada de Ações - Itaú Corretora"
    });
});

var app = builder.Build();

// Swagger sempre habilitado (facilita demonstração)
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Compra Programada API v1");
    options.RoutePrefix = string.Empty;
});

app.UseHttpsRedirection();
app.MapControllers();

app.Run();

// Necessário para testes de integração com WebApplicationFactory
public partial class Program { }
