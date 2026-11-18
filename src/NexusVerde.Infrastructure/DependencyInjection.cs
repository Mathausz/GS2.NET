using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NexusVerde.Application.Interfaces;
using NexusVerde.Application.Interfaces.Repositories;
using NexusVerde.Infrastructure.Data;
using NexusVerde.Infrastructure.Repositories;
using NexusVerde.Infrastructure.Security;
using NexusVerde.Infrastructure.Services;

namespace NexusVerde.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registra DbContext (Oracle ou memória), repositórios, hasher de senha, cliente de clima e store de idempotência.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // A configuração é lida de forma "preguiçosa" (no momento da criação do contexto),
        // o que permite sobrescrevê-la em testes e por variáveis de ambiente.
        services.AddDbContext<NexusVerdeDbContext>((sp, options) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var environment = sp.GetRequiredService<IHostEnvironment>();

            if (ModoPersistencia.UsarEmMemoria(config, environment))
            {
                options.UseInMemoryDatabase(ModoPersistencia.NomeBancoEmMemoria(config));
                return;
            }

            var connectionString = config.GetConnectionString("Oracle");
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException(
                    "Connection string 'Oracle' não configurada. Defina a variável de ambiente " +
                    "ConnectionStrings__Oracle (veja .env.example) ou rode em Development para usar o banco em memória.");

            options.UseOracle(connectionString, OracleCompatibilidade.Configurar);
        });

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IRegiaoRepository, RegiaoRepository>();
        services.AddScoped<IDesastreRepository, DesastreRepository>();
        services.AddScoped<IPontoAguaRepository, PontoAguaRepository>();
        services.AddScoped<ISolicitacaoRepository, SolicitacaoRepository>();
        services.AddScoped<IAtendimentoRepository, AtendimentoRepository>();
        services.AddScoped<IOrientacaoRepository, OrientacaoRepository>();
        services.AddScoped<ITimelineRepository, TimelineRepository>();

        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddScoped<IIdempotencyStore, IdempotencyStore>();

        services.AddHttpClient<IWeatherClient, OpenMeteoWeatherClient>((sp, client) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var baseUrl = config["Weather:BaseUrl"];
            client.BaseAddress = new Uri(string.IsNullOrWhiteSpace(baseUrl) ? "https://api.open-meteo.com/" : baseUrl);
            client.Timeout = TimeSpan.FromSeconds(config.GetValue("Weather:TimeoutSeconds", 5));
        });

        return services;
    }
}
