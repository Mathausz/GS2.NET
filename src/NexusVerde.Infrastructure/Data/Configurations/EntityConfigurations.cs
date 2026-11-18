using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexusVerde.Domain.Entities;

namespace NexusVerde.Infrastructure.Data.Configurations;

public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> b)
    {
        b.ToTable("NV_USUARIOS");
        b.HasKey(x => x.Id);
        b.Property(x => x.Nome).HasMaxLength(120).IsRequired();
        b.Property(x => x.Email).HasMaxLength(160).IsRequired();
        b.Property(x => x.SenhaHash).HasMaxLength(500).IsRequired();
        b.Property(x => x.Perfil).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Orgao).HasConversion<string>().HasMaxLength(20);
        b.HasIndex(x => x.Email).IsUnique();
    }
}

public class RegiaoConfiguration : IEntityTypeConfiguration<Regiao>
{
    public void Configure(EntityTypeBuilder<Regiao> b)
    {
        b.ToTable("NV_REGIOES");
        b.HasKey(x => x.Id);
        b.Property(x => x.Nome).HasMaxLength(120).IsRequired();
        b.Property(x => x.Municipio).HasMaxLength(120).IsRequired();
        b.Property(x => x.Estado).HasMaxLength(2).IsRequired();
        b.Property(x => x.NivelRisco).HasConversion<string>().HasMaxLength(20);
        b.HasIndex(x => new { x.Estado, x.Municipio });
    }
}

public class DesastreConfiguration : IEntityTypeConfiguration<Desastre>
{
    public void Configure(EntityTypeBuilder<Desastre> b)
    {
        b.ToTable("NV_DESASTRES");
        b.HasKey(x => x.Id);
        b.Property(x => x.Tipo).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Descricao).HasMaxLength(1000).IsRequired();
        b.Ignore(x => x.EstaAtivo);
        b.HasOne<Regiao>().WithMany().HasForeignKey(x => x.RegiaoId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.RegiaoId, x.Status });
    }
}

public class PontoAguaConfiguration : IEntityTypeConfiguration<PontoAgua>
{
    public void Configure(EntityTypeBuilder<PontoAgua> b)
    {
        b.ToTable("NV_PONTOS_AGUA");
        b.HasKey(x => x.Id);
        b.Property(x => x.Nome).HasMaxLength(120).IsRequired();
        b.Property(x => x.Endereco).HasMaxLength(200).IsRequired();
        b.Property(x => x.Descricao).HasMaxLength(500);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.Ignore(x => x.PodeAtender);
        b.HasOne<Regiao>().WithMany().HasForeignKey(x => x.RegiaoId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.Status);
    }
}

public class SolicitacaoAjudaConfiguration : IEntityTypeConfiguration<SolicitacaoAjuda>
{
    public void Configure(EntityTypeBuilder<SolicitacaoAjuda> b)
    {
        b.ToTable("NV_SOLICITACOES");
        b.HasKey(x => x.Id);
        b.Property(x => x.Descricao).HasMaxLength(1000).IsRequired();
        b.Property(x => x.Tipo).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.NivelPrioridade).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.OrgaoDestino).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.HasOne<Usuario>().WithMany().HasForeignKey(x => x.CidadaoId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Regiao>().WithMany().HasForeignKey(x => x.RegiaoId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Desastre>().WithMany().HasForeignKey(x => x.DesastreId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<PontoAgua>().WithMany().HasForeignKey(x => x.PontoSugeridoId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.Status, x.OrgaoDestino, x.ScorePrioridade });
        b.HasIndex(x => x.CidadaoId);
    }
}

public class AtendimentoConfiguration : IEntityTypeConfiguration<Atendimento>
{
    public void Configure(EntityTypeBuilder<Atendimento> b)
    {
        b.ToTable("NV_ATENDIMENTOS");
        b.HasKey(x => x.Id);
        b.Property(x => x.Observacao).HasMaxLength(1000);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.HasOne<SolicitacaoAjuda>().WithMany().HasForeignKey(x => x.SolicitacaoId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Usuario>().WithMany().HasForeignKey(x => x.GestorId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<PontoAgua>().WithMany().HasForeignKey(x => x.PontoAguaId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.SolicitacaoId);
    }
}

public class OrientacaoConfiguration : IEntityTypeConfiguration<Orientacao>
{
    public void Configure(EntityTypeBuilder<Orientacao> b)
    {
        b.ToTable("NV_ORIENTACOES");
        b.HasKey(x => x.Id);
        b.Property(x => x.Titulo).HasMaxLength(150).IsRequired();
        b.Property(x => x.Texto).HasMaxLength(2000).IsRequired();
        b.Property(x => x.Tipo).HasConversion<string>().HasMaxLength(20);
        b.HasIndex(x => x.Tipo);
    }
}

public class RegistroIdempotenciaConfiguration : IEntityTypeConfiguration<RegistroIdempotencia>
{
    public void Configure(EntityTypeBuilder<RegistroIdempotencia> b)
    {
        b.ToTable("NV_IDEMPOTENCIA");
        b.HasKey(x => x.Id);
        b.Property(x => x.Chave).HasMaxLength(100).IsRequired();
        b.Property(x => x.Rota).HasMaxLength(200).IsRequired();
        b.Property(x => x.HashCorpo).HasMaxLength(64).IsRequired();
        b.Property(x => x.ContentType).HasMaxLength(100);
        b.Property(x => x.CorpoResposta).HasColumnType("CLOB");
        b.HasIndex(x => new { x.UsuarioId, x.Chave, x.Rota }).IsUnique();
    }
}

public class EventoTimelineConfiguration : IEntityTypeConfiguration<EventoTimeline>
{
    public void Configure(EntityTypeBuilder<EventoTimeline> b)
    {
        b.ToTable("NV_TIMELINE");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasMaxLength(36);
        b.Property(x => x.Tipo).HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.Descricao).HasMaxLength(1000).IsRequired();
        b.Property(x => x.AutorPerfil).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Orgao).HasConversion<string>().HasMaxLength(20);

        // Campo livre (score, nível, ponto...) gravado como JSON em uma coluna de texto.
        b.Property(x => x.Dados)
            .HasConversion(
                v => DadosJson.Serializar(v),
                s => DadosJson.Desserializar(s),
                new ValueComparer<IReadOnlyDictionary<string, string>>(
                    (a, c) => DadosJson.Serializar(a) == DadosJson.Serializar(c),
                    v => DadosJson.Serializar(v).GetHashCode(),
                    v => DadosJson.Desserializar(DadosJson.Serializar(v))))
            .HasMaxLength(2000)
            .IsRequired();

        b.HasOne<SolicitacaoAjuda>().WithMany().HasForeignKey(x => x.SolicitacaoId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.SolicitacaoId, x.OcorridoEm });
    }
}

/// <summary>Conversão do campo <see cref="EventoTimeline.Dados"/> para JSON (usada pelo EF Core).</summary>
internal static class DadosJson
{
    public static string Serializar(IReadOnlyDictionary<string, string>? dados) =>
        JsonSerializer.Serialize(dados ?? new Dictionary<string, string>());

    public static IReadOnlyDictionary<string, string> Desserializar(string? json) =>
        string.IsNullOrWhiteSpace(json)
            ? new Dictionary<string, string>()
            : JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
}
