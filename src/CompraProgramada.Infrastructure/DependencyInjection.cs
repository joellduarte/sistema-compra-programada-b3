using CompraProgramada.Application.Interfaces;
using CompraProgramada.Application.Services;
using CompraProgramada.Domain.Interfaces;
using CompraProgramada.Infrastructure.Data;
using CompraProgramada.Infrastructure.Data.Repositories;
using CompraProgramada.Infrastructure.Messaging;
using CompraProgramada.Infrastructure.Parsers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CompraProgramada.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<CompraProgramadaDbContext>(options =>
            options.UseMySql(
                connectionString,
                new MySqlServerVersion(new Version(8, 0, 0)),
                mysqlOptions =>
                {
                    mysqlOptions.MigrationsAssembly(
                        typeof(CompraProgramadaDbContext).Assembly.FullName);
                    mysqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorNumbersToAdd: null);
                }));

        // Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Repositories
        services.AddScoped<ICotacaoRepository, CotacaoRepository>();
        services.AddScoped<IClienteRepository, ClienteRepository>();
        services.AddScoped<IContaGraficaRepository, ContaGraficaRepository>();
        services.AddScoped<ICustodiaRepository, CustodiaRepository>();
        services.AddScoped<IHistoricoValorMensalRepository, HistoricoValorMensalRepository>();
        services.AddScoped<ICestaRecomendacaoRepository, CestaRecomendacaoRepository>();
        services.AddScoped<IOrdemCompraRepository, OrdemCompraRepository>();
        services.AddScoped<IDistribuicaoRepository, DistribuicaoRepository>();
        services.AddScoped<IEventoIRRepository, EventoIRRepository>();

        // Kafka
        services.AddSingleton<IKafkaProducer, KafkaProducer>();

        // Parsers
        services.AddSingleton<ICotahistParser, CotahistParser>();

        // Application Services
        services.AddScoped<ICotacaoService, CotacaoService>();
        services.AddScoped<IClienteService, ClienteService>();
        services.AddScoped<ICestaService, CestaService>();
        services.AddScoped<IMotorCompraService, MotorCompraService>();
        services.AddScoped<IEventoIRService, EventoIRService>();

        return services;
    }
}
