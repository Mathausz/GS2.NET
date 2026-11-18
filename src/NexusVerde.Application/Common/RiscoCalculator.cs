using NexusVerde.Domain.Enums;

namespace NexusVerde.Application.Common;

public static class RiscoCalculator
{
    /// <summary>Nível de risco da região a partir da maior gravidade entre seus desastres ativos (0 = nenhum).</summary>
    public static NivelRisco Classificar(int maiorGravidadeAtiva) => maiorGravidadeAtiva switch
    {
        >= 5 => NivelRisco.Critico,
        4 => NivelRisco.Alto,
        3 => NivelRisco.Medio,
        _ => NivelRisco.Baixo
    };
}
