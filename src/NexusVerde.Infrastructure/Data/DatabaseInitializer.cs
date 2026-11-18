using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NexusVerde.Application.Interfaces;
using NexusVerde.Domain.Entities;
using NexusVerde.Domain.Enums;

namespace NexusVerde.Infrastructure.Data;

/// <summary>
/// Prepara o banco na subida da aplicação: aplica migrations (Oracle) ou cria o esquema (InMemory)
/// e, se habilitado, carrega dados de demonstração. O admin só é criado se as credenciais vierem
/// de configuração/variáveis de ambiente (Seed__AdminEmail e Seed__AdminPassword).
/// </summary>
public static class DatabaseInitializer
{
    public static async Task InicializarAsync(IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var config = sp.GetRequiredService<IConfiguration>();
        var environment = sp.GetRequiredService<IHostEnvironment>();
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseInitializer");
        var context = sp.GetRequiredService<NexusVerdeDbContext>();
        var emMemoria = ModoPersistencia.UsarEmMemoria(config, environment);

        if (emMemoria)
        {
            await context.Database.EnsureCreatedAsync(ct);
            logger.LogWarning("Banco EM MEMÓRIA (modo demonstração): os dados são perdidos ao reiniciar. " +
                              "Defina ConnectionStrings__Oracle para usar o Oracle.");
        }
        else if (config.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
        {
            await context.Database.MigrateAsync(ct);
        }

        if (config.GetValue<bool>("Database:RunSeedOnStartup"))
        {
            await PopularDadosDemoAsync(context, ct);
            logger.LogInformation("Dados de demonstração verificados/carregados.");
        }

        await CriarAdminInicialAsync(sp, config, context, logger, ct);

        // Usuários de demonstração (appsettings.Development.json → Demo:Usuarios) existem só no banco em memória:
        // nunca são criados no Oracle, então nenhuma senha do repositório vale em um banco real.
        if (emMemoria && environment.IsDevelopment())
            await CriarUsuariosDemoAsync(sp, config, context, logger, ct);
    }

    private static async Task CriarUsuariosDemoAsync(IServiceProvider sp, IConfiguration config,
        NexusVerdeDbContext context, ILogger logger, CancellationToken ct)
    {
        var hasher = sp.GetRequiredService<IPasswordHasher>();

        foreach (var item in config.GetSection("Demo:Usuarios").GetChildren())
        {
            var email = item["Email"]?.Trim().ToLowerInvariant();
            var senha = item["Senha"];
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha)) continue;
            if (await context.Usuarios.AnyAsync(u => u.Email == email, ct)) continue;

            var perfil = Enum.TryParse<PerfilUsuario>(item["Perfil"], true, out var p) ? p : PerfilUsuario.Cidadao;
            var orgao = Enum.TryParse<OrgaoResponsavel>(item["Orgao"], true, out var o) ? o : OrgaoResponsavel.Nenhum;

            context.Usuarios.Add(new Usuario(item["Nome"] ?? email, email, hasher.Hash(senha), perfil, orgao));
            logger.LogInformation("Usuário de demonstração disponível: {Email} ({Perfil})", email, perfil);
        }

        await context.SaveChangesAsync(ct);
    }

    private static async Task CriarAdminInicialAsync(IServiceProvider sp, IConfiguration config,
        NexusVerdeDbContext context, ILogger logger, CancellationToken ct)
    {
        var email = config["Seed:AdminEmail"];
        var senha = config["Seed:AdminPassword"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha)) return;

        email = email.Trim().ToLowerInvariant();
        if (await context.Usuarios.AnyAsync(u => u.Email == email, ct)) return;

        var hasher = sp.GetRequiredService<IPasswordHasher>();
        var nome = config["Seed:AdminNome"] ?? "Administrador Nexus Verde";
        context.Usuarios.Add(new Usuario(nome, email, hasher.Hash(senha), PerfilUsuario.Admin));
        await context.SaveChangesAsync(ct);
        logger.LogInformation("Usuário administrador inicial criado: {Email}", email);
    }

    private static async Task PopularDadosDemoAsync(NexusVerdeDbContext context, CancellationToken ct)
    {
        if (!await context.Regioes.AnyAsync(ct))
        {
            var poa = new Regiao("Zona Norte - Sarandi", "Porto Alegre", "RS", -30.0098, -51.1301, 60000);
            var canoas = new Regiao("Mathias Velho", "Canoas", "RS", -29.9285, -51.1850, 45000);
            var sapucaia = new Regiao("Centro", "São Leopoldo", "RS", -29.7600, -51.1470, 38000);
            context.Regioes.AddRange(poa, canoas, sapucaia);

            var enchente = new Desastre(poa.Id, TipoDesastre.Enchente, 4,
                "Alagamento de bairros com interrupção do abastecimento e risco de contaminação da rede.");
            context.Desastres.Add(enchente);
            poa.DefinirRisco(NivelRisco.Alto);

            context.PontosAgua.AddRange(
                new PontoAgua(poa.Id, "Ginásio Tesourinha", "Av. Padre Cacique, 891 - Porto Alegre", -30.0560, -51.2290, 40000,
                    "Distribuição de água potável e galões. Funciona 24h."),
                new PontoAgua(canoas.Id, "Escola Municipal Rio Branco", "Rua Rio Branco, 450 - Canoas", -29.9170, -51.1830, 25000,
                    "Caminhão-pipa com abastecimento a cada 6 horas."),
                new PontoAgua(sapucaia.Id, "Praça do Imigrante", "Praça do Imigrante, s/n - São Leopoldo", -29.7590, -51.1490, 18000,
                    "Poço tratado e monitorado pela prefeitura."),
                new PontoAgua(poa.Id, "Unidade Básica Sarandi", "Rua Sarandi, 120 - Porto Alegre", -30.0150, -51.1350, 12000,
                    "Água potável para consumo humano e orientações de saúde."));
        }

        if (!await context.Orientacoes.AnyAsync(ct))
        {
            context.Orientacoes.AddRange(
                new Orientacao(TipoSolicitacao.AguaSuspeita, "Não consuma nem use para higiene",
                    "Se a água estiver turva, com cheiro ou cor diferente, ou tiver sido atingida por enchente, não beba, não cozinhe e não escove os dentes com ela.", 1),
                new Orientacao(TipoSolicitacao.AguaSuspeita, "Como tratar a água em casa",
                    "Use apenas água de ponto seguro. Se não houver, ferva por 1 minuto ou adicione 2 gotas de hipoclorito de sódio 2,5% (sem perfume) por litro, aguardando 30 minutos antes de beber.", 2),
                new Orientacao(TipoSolicitacao.AguaSuspeita, "Afaste pessoas e animais da fonte",
                    "Sinalize o local e evite o contato da água suspeita com crianças, idosos e animais até a equipe de resposta avaliar.", 3),
                new Orientacao(TipoSolicitacao.SemAgua, "Vá ao ponto de água mais próximo",
                    "Use o ponto indicado pelo aplicativo, leve recipientes limpos com tampa e priorize crianças, idosos e pessoas doentes.", 1),
                new Orientacao(TipoSolicitacao.SemAgua, "Economize a água disponível",
                    "Reserve a água potável para beber e cozinhar. Evite lavar áreas externas e reutilize a água de usos leves para a descarga.", 2),
                new Orientacao(TipoSolicitacao.AcessoNegado, "Mantenha a segurança e registre o ocorrido",
                    "Se alguém impede seu acesso à água, não se exponha a risco. Anote local, horário e quem impediu. A solicitação foi encaminhada à Polícia.", 1),
                new Orientacao(TipoSolicitacao.AcessoNegado, "Em emergência, ligue 190",
                    "Se houver ameaça ou violência, ligue para a Polícia Militar (190). Para riscos de saúde, ligue para o SAMU (192).", 2));
        }

        await context.SaveChangesAsync(ct);
    }
}
