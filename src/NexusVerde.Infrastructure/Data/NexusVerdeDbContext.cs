using Microsoft.EntityFrameworkCore;
using NexusVerde.Domain.Entities;

namespace NexusVerde.Infrastructure.Data;

public class NexusVerdeDbContext(DbContextOptions<NexusVerdeDbContext> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Regiao> Regioes => Set<Regiao>();
    public DbSet<Desastre> Desastres => Set<Desastre>();
    public DbSet<PontoAgua> PontosAgua => Set<PontoAgua>();
    public DbSet<SolicitacaoAjuda> Solicitacoes => Set<SolicitacaoAjuda>();
    public DbSet<Atendimento> Atendimentos => Set<Atendimento>();
    public DbSet<Orientacao> Orientacoes => Set<Orientacao>();
    public DbSet<RegistroIdempotencia> RegistrosIdempotencia => Set<RegistroIdempotencia>();
    public DbSet<EventoTimeline> Timeline => Set<EventoTimeline>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<NullableUtcDateTimeConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NexusVerdeDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
