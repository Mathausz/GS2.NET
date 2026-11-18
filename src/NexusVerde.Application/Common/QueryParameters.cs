using System.ComponentModel.DataAnnotations;

namespace NexusVerde.Application.Common;

/// <summary>
/// Parâmetros comuns de paginação e ordenação das listagens. Todos são opcionais na query string:
/// sem eles, a API devolve a primeira página com <see cref="PageSizePadrao"/> itens na ordenação padrão.
/// </summary>
public class QueryParameters
{
    public const int PageSizePadrao = 10;
    public const int PageSizeMaximo = 50;

    /// <summary>Página desejada (começa em 1). Padrão: 1.</summary>
    [Range(1, int.MaxValue, ErrorMessage = "page deve ser maior ou igual a 1.")]
    public int? Page { get; set; }

    /// <summary>Itens por página (1 a 50). Padrão: 10.</summary>
    [Range(1, PageSizeMaximo, ErrorMessage = "pageSize deve estar entre 1 e 50.")]
    public int? PageSize { get; set; }

    /// <summary>Campo de ordenação (varia por recurso; inválido é ignorado).</summary>
    public string? SortBy { get; set; }

    /// <summary>Ordem decrescente. Padrão: false.</summary>
    public bool? Desc { get; set; }

    // Métodos (e não propriedades) para não serem tratados como parâmetros de query pelo [AsParameters].

    /// <summary>Página efetiva, aplicando o padrão.</summary>
    public int PaginaEfetiva() => Math.Max(Page ?? 1, 1);

    /// <summary>Tamanho de página efetivo, aplicando o padrão e o limite.</summary>
    public int TamanhoEfetivo() => Math.Clamp(PageSize ?? PageSizePadrao, 1, PageSizeMaximo);

    /// <summary>Indica se a ordenação deve ser decrescente.</summary>
    public bool Decrescente() => Desc == true;
}
