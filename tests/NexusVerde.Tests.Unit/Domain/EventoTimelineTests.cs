using NexusVerde.Domain.Entities;
using NexusVerde.Domain.Enums;

namespace NexusVerde.Tests.Unit.Domain;

/// <summary>Regras do evento da linha do tempo gravado no Oracle (NV_TIMELINE).</summary>
public partial class EventoTimelineTests
{
    [Fact]
    public void Criar_VariosEventosEmSequencia_TemHorariosEstritamenteCrescentes()
    {
        // Arrange
        var solicitacaoId = Guid.NewGuid();

        // Act
        var eventos = Enumerable.Range(0, 1000)
            .Select(_ => EventoTimeline.Criar(solicitacaoId, TipoEventoTimeline.NotaAdicionada, "Nota."))
            .ToList();

        // Assert: a ordem cronológica da linha do tempo nunca empata, mesmo no mesmo instante.
        for (var i = 1; i < eventos.Count; i++)
            Assert.True(eventos[i].OcorridoEm > eventos[i - 1].OcorridoEm);
    }

    [Fact]
    public void Criar_EventosDistintos_RecebemIdsUnicosEmFormatoGuid()
    {
        // Act
        var a = EventoTimeline.Criar(Guid.NewGuid(), TipoEventoTimeline.Criada, "Criada.");
        var b = EventoTimeline.Criar(Guid.NewGuid(), TipoEventoTimeline.Criada, "Criada.");

        // Assert
        Assert.NotEqual(a.Id, b.Id);
        Assert.True(Guid.TryParse(a.Id, out _));
        Assert.Equal(DateTimeKind.Utc, a.OcorridoEm.Kind);
    }
}
