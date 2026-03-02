using CompraProgramada.Infrastructure;
using CompraProgramada.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
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
        Description = "Sistema de Compra Programada de Ações (Top Five) - Itaú Corretora. " +
            "Permite adesão de clientes, gestão de cestas recomendadas, execução de compras programadas " +
            "nos dias 5/15/25, rebalanceamento de carteiras e apuração fiscal (IR) via Kafka.",
        Contact = new OpenApiContact
        {
            Name = "Joel Lima"
        }
    });

    var xmlFilename = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});

var app = builder.Build();

// Aplicar migrations automaticamente no startup (facilita demo e testes)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CompraProgramadaDbContext>();
    db.Database.Migrate();
}

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
