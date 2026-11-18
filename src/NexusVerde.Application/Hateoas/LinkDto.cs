namespace NexusVerde.Application.Hateoas;

/// <summary>Link HATEOAS: relação (rel), destino (href) e verbo HTTP (method).</summary>
public sealed record LinkDto(string Rel, string Href, string Method = "GET");
