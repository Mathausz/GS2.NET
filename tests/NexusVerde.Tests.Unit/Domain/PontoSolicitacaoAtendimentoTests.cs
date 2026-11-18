using NexusVerde.Domain.Entities;
using NexusVerde.Domain.Enums;
using NexusVerde.Domain.Exceptions;

namespace NexusVerde.Tests.Unit.Domain;

public class PontoAguaTests
{
    [Theory]
    [InlineData(StatusPontoAgua.Ativo, true)]
    [InlineData(StatusPontoAgua.Lotado, false)]
    [InlineData(StatusPontoAgua.Contaminado, false)]
    [InlineData(StatusPontoAgua.Inativo, false)]
    public void PodeAtender_ConformeStatus_SomenteAtivoAtende(StatusPontoAgua status, bool esperado)
    {
        // Arrange
        var ponto = new PontoAgua(Guid.NewGuid(), "Ginásio", "Rua A, 1", -30, -51, 1000);

        // Act
        ponto.AlterarStatus(status);

        // Assert
        Assert.Equal(esperado, ponto.PodeAtender);
    }

    [Fact]
    public void Construtor_CapacidadeNegativa_LancaDomainException()
    {
        // Arrange & Act
        Action act = () => new PontoAgua(Guid.NewGuid(), "Ginásio", "Rua A, 1", -30, -51, -5);

        // Assert
        Assert.Throws<DomainException>(act);
    }

    [Fact]
    public void Atualizar_DescricaoEmBranco_ArmazenaNulo()
    {
        // Arrange
        var ponto = new PontoAgua(Guid.NewGuid(), "Ginásio", "Rua A, 1", -30, -51, 1000, "Água 24h");

        // Act
        ponto.Atualizar("Ginásio", "Rua A, 1", -30, -51, 1000, "   ");

        // Assert
        Assert.Null(ponto.Descricao);
    }
}

public class SolicitacaoAjudaTests
{
    private static SolicitacaoAjuda Nova() => TestData.Solicitacao(Guid.NewGuid(), Guid.NewGuid());

    [Fact]
    public void IniciarAtendimento_SolicitacaoAberta_MudaParaEmAtendimento()
    {
        // Arrange
        var solicitacao = Nova();

        // Act
        solicitacao.IniciarAtendimento();

        // Assert
        Assert.Equal(StatusSolicitacao.EmAtendimento, solicitacao.Status);
    }

    [Fact]
    public void Cancelar_SolicitacaoEmAtendimento_LancaDomainException()
    {
        // Arrange
        var solicitacao = Nova();
        solicitacao.IniciarAtendimento();

        // Act
        Action act = () => solicitacao.Cancelar();

        // Assert
        Assert.Throws<DomainException>(act);
    }

    [Fact]
    public void IniciarAtendimento_SolicitacaoCancelada_LancaDomainException()
    {
        // Arrange
        var solicitacao = Nova();
        solicitacao.Cancelar();

        // Act
        Action act = () => solicitacao.IniciarAtendimento();

        // Assert
        Assert.Throws<DomainException>(act);
    }

    [Fact]
    public void RetornarParaAberta_SolicitacaoAberta_LancaDomainException()
    {
        // Arrange
        var solicitacao = Nova();

        // Act
        Action act = () => solicitacao.RetornarParaAberta();

        // Assert
        Assert.Throws<DomainException>(act);
    }

    [Fact]
    public void MarcarAtendida_SolicitacaoCancelada_LancaDomainException()
    {
        // Arrange
        var solicitacao = Nova();
        solicitacao.Cancelar();

        // Act
        Action act = () => solicitacao.MarcarAtendida();

        // Assert
        Assert.Throws<DomainException>(act);
    }

    [Fact]
    public void MarcarAtendida_SolicitacaoEmAtendimento_MudaParaAtendida()
    {
        // Arrange
        var solicitacao = Nova();
        solicitacao.IniciarAtendimento();

        // Act
        solicitacao.MarcarAtendida();

        // Assert
        Assert.Equal(StatusSolicitacao.Atendida, solicitacao.Status);
    }

    [Fact]
    public void Construtor_QuantidadeDePessoasZero_LancaDomainException()
    {
        // Arrange & Act
        Action act = () => new SolicitacaoAjuda(
            Guid.NewGuid(), Guid.NewGuid(), TipoSolicitacao.SemAgua, "Sem água", 0, 1, -30, -51, 10,
            NivelPrioridade.Baixa, OrgaoResponsavel.Prefeitura, null, null);

        // Assert
        Assert.Throws<DomainException>(act);
    }
}

public class AtendimentoTests
{
    [Fact]
    public void Concluir_AtendimentoEmAndamento_RegistraFimStatusEObservacao()
    {
        // Arrange
        var atendimento = new Atendimento(Guid.NewGuid(), Guid.NewGuid(), null, "Iniciado");

        // Act
        atendimento.Concluir("Água entregue");

        // Assert
        Assert.Equal(StatusAtendimento.Concluido, atendimento.Status);
        Assert.NotNull(atendimento.FinalizadoEm);
        Assert.Equal("Água entregue", atendimento.Observacao);
    }

    [Fact]
    public void Concluir_AtendimentoJaCancelado_LancaDomainException()
    {
        // Arrange
        var atendimento = new Atendimento(Guid.NewGuid(), Guid.NewGuid(), null, null);
        atendimento.Cancelar(null);

        // Act
        Action act = () => atendimento.Concluir(null);

        // Assert
        Assert.Throws<DomainException>(act);
    }

    [Fact]
    public void Cancelar_SemNovaObservacao_PreservaObservacaoAnterior()
    {
        // Arrange
        var atendimento = new Atendimento(Guid.NewGuid(), Guid.NewGuid(), null, "Equipe a caminho");

        // Act
        atendimento.Cancelar(null);

        // Assert
        Assert.Equal("Equipe a caminho", atendimento.Observacao);
    }
}

public class OrientacaoTests
{
    [Fact]
    public void Construtor_DadosValidos_PreencheCampos()
    {
        // Arrange & Act
        var orientacao = new Orientacao(TipoSolicitacao.AguaSuspeita, " Não beba ", "Ferva por 1 minuto.", 2);

        // Assert
        Assert.Equal("Não beba", orientacao.Titulo);
        Assert.Equal(TipoSolicitacao.AguaSuspeita, orientacao.Tipo);
        Assert.Equal(2, orientacao.Ordem);
    }

    [Fact]
    public void Construtor_TextoVazio_LancaDomainException()
    {
        // Arrange & Act
        Action act = () => new Orientacao(TipoSolicitacao.SemAgua, "Título", " ", 1);

        // Assert
        Assert.Throws<DomainException>(act);
    }

    [Fact]
    public void Atualizar_OrdemForaDaFaixa_LancaDomainException()
    {
        // Arrange
        var orientacao = new Orientacao(TipoSolicitacao.SemAgua, "Título", "Texto válido", 1);

        // Act
        Action act = () => orientacao.Atualizar(TipoSolicitacao.SemAgua, "Título", "Texto válido", 5000);

        // Assert
        Assert.Throws<DomainException>(act);
    }
}

public partial class EventoTimelineTests
{
    [Fact]
    public void Criar_DadosValidos_PreencheEventoComDataUtc()
    {
        // Arrange
        var solicitacaoId = Guid.NewGuid();
        var dados = new Dictionary<string, string> { ["score"] = "41.7" };

        // Act
        var evento = EventoTimeline.Criar(solicitacaoId, TipoEventoTimeline.Criada, "Solicitação criada.",
            Guid.NewGuid(), PerfilUsuario.Cidadao, OrgaoResponsavel.Nenhum, dados);

        // Assert
        Assert.Equal(solicitacaoId, evento.SolicitacaoId);
        Assert.Equal(TipoEventoTimeline.Criada, evento.Tipo);
        Assert.Equal("41.7", evento.Dados["score"]);
        Assert.Equal(DateTimeKind.Utc, evento.OcorridoEm.Kind);
    }

    [Fact]
    public void Criar_SolicitacaoVazia_LancaDomainException()
    {
        // Arrange & Act
        Action act = () => EventoTimeline.Criar(Guid.Empty, TipoEventoTimeline.Criada, "Descrição");

        // Assert
        Assert.Throws<DomainException>(act);
    }

    [Fact]
    public void Criar_DescricaoVazia_LancaDomainException()
    {
        // Arrange & Act
        Action act = () => EventoTimeline.Criar(Guid.NewGuid(), TipoEventoTimeline.NotaAdicionada, "   ");

        // Assert
        Assert.Throws<DomainException>(act);
    }

    [Fact]
    public void Criar_SemDados_UsaDicionarioVazio()
    {
        // Arrange & Act
        var evento = EventoTimeline.Criar(Guid.NewGuid(), TipoEventoTimeline.Cancelada, "Cancelada.");

        // Assert
        Assert.Empty(evento.Dados);
    }
}

public class GuardTests
{
    [Fact]
    public void NotEmpty_TextoAcimaDoLimite_LancaDomainException()
    {
        // Arrange
        var texto = new string('x', 11);

        // Act
        Action act = () => NexusVerde.Domain.Commons.Guard.NotEmpty(texto, "Campo", 10);

        // Assert
        Assert.Throws<DomainException>(act);
    }

    [Fact]
    public void NotEmpty_TextoComEspacos_RetornaTextoAparado()
    {
        // Arrange & Act
        var resultado = NexusVerde.Domain.Commons.Guard.NotEmpty("  oi  ", "Campo", 10);

        // Assert
        Assert.Equal("oi", resultado);
    }
}
