using NexusVerde.Domain.Enums;

namespace NexusVerde.Application.Common;

public readonly record struct ResultadoPrioridade(double Score, NivelPrioridade Nivel);

/// <summary>
/// Priorização das solicitações por regras (0 a 100).
/// Soma: gravidade do desastre (até 50) + pessoas afetadas (até 20) + horas sem água (até 20)
/// + chuva prevista (até 10) + peso do tipo da solicitação (até 15), limitado a 100.
/// </summary>
public static class PrioridadeCalculator
{
    public static ResultadoPrioridade Calcular(TipoSolicitacao tipo, int gravidadeDesastre,
        int quantidadePessoas, int horasSemAgua, double? chuvaPrevistaMm)
    {
        var gravidade = Math.Clamp(gravidadeDesastre, 0, 5) * 10.0;
        var pessoas = Math.Clamp(quantidadePessoas, 0, 100) * 0.2;
        var tempo = Math.Clamp(horasSemAgua, 0, 72) / 72.0 * 20.0;
        var chuva = chuvaPrevistaMm.HasValue ? Math.Clamp(chuvaPrevistaMm.Value, 0, 50) / 50.0 * 10.0 : 0.0;
        var pesoTipo = tipo switch
        {
            TipoSolicitacao.AcessoNegado => 15.0,
            TipoSolicitacao.AguaSuspeita => 10.0,
            _ => 0.0
        };

        var score = Math.Round(Math.Min(100.0, gravidade + pessoas + tempo + chuva + pesoTipo), 1);
        return new ResultadoPrioridade(score, Classificar(score));
    }

    public static NivelPrioridade Classificar(double score) => score switch
    {
        >= 75 => NivelPrioridade.Critica,
        >= 55 => NivelPrioridade.Alta,
        >= 30 => NivelPrioridade.Media,
        _ => NivelPrioridade.Baixa
    };

    /// <summary>Órgão que recebe a solicitação: acesso negado vai para a Polícia.</summary>
    public static OrgaoResponsavel OrgaoDestinoPorTipo(TipoSolicitacao tipo) => tipo switch
    {
        TipoSolicitacao.AcessoNegado => OrgaoResponsavel.Policia,
        TipoSolicitacao.AguaSuspeita => OrgaoResponsavel.DefesaCivil,
        _ => OrgaoResponsavel.Prefeitura
    };
}
