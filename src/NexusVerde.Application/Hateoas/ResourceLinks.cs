using NexusVerde.Application.Common;
using NexusVerde.Application.DTOs;
using NexusVerde.Domain.Enums;

namespace NexusVerde.Application.Hateoas;

/// <summary>
/// HATEOAS: cada recurso devolvido pelas consultas leva os links das ações possíveis para QUEM está
/// consultando e no ESTADO atual do recurso (ex.: só o autor vê "cancelar" e só enquanto está Aberta).
/// Lógica pura, sem HTTP, para ser testável em isolamento.
/// </summary>
public static class ResourceLinks
{
    public const string Base = "/api/v1";

    private static LinkDto L(string rel, string href, string method = "GET") => new(rel, href, method);

    private static bool Gestor(UsuarioContexto? u) => u is { Perfil: PerfilUsuario.Gestor or PerfilUsuario.Admin };
    private static bool Admin(UsuarioContexto? u) => u is { Perfil: PerfilUsuario.Admin };

    public static RegiaoResponse Com(RegiaoResponse r, UsuarioContexto? usuario)
    {
        var self = $"{Base}/regioes/{r.Id}";
        var links = new List<LinkDto>
        {
            L("self", self),
            L("colecao", $"{Base}/regioes"),
            L("desastres", $"{Base}/desastres?regiaoId={r.Id}"),
            L("pontos-agua", $"{Base}/pontos-agua?regiaoId={r.Id}")
        };
        if (Admin(usuario))
        {
            links.Add(L("atualizar", self, "PUT"));
            links.Add(L("remover", self, "DELETE"));
        }
        return r with { Links = links };
    }

    public static DesastreResponse Com(DesastreResponse d, UsuarioContexto? usuario)
    {
        var self = $"{Base}/desastres/{d.Id}";
        var links = new List<LinkDto>
        {
            L("self", self),
            L("colecao", $"{Base}/desastres"),
            L("regiao", $"{Base}/regioes/{d.RegiaoId}")
        };
        if (Gestor(usuario)) links.Add(L("atualizar", self, "PUT"));
        if (Admin(usuario)) links.Add(L("remover", self, "DELETE"));
        return d with { Links = links };
    }

    public static PontoAguaResponse Com(PontoAguaResponse p, UsuarioContexto? usuario)
    {
        var self = $"{Base}/pontos-agua/{p.Id}";
        var links = new List<LinkDto>
        {
            L("self", self),
            L("colecao", $"{Base}/pontos-agua"),
            L("regiao", $"{Base}/regioes/{p.RegiaoId}")
        };
        if (Gestor(usuario))
        {
            links.Add(L("atualizar", self, "PUT"));
            links.Add(L("alterar-status", $"{self}/status", "PATCH"));
        }
        if (Admin(usuario)) links.Add(L("remover", self, "DELETE"));
        return p with { Links = links };
    }

    public static PontoAguaProximoResponse Com(PontoAguaProximoResponse p, UsuarioContexto? usuario) =>
        p with { Ponto = Com(p.Ponto, usuario) };

    public static SolicitacaoResponse Com(SolicitacaoResponse s, UsuarioContexto? usuario)
    {
        var self = $"{Base}/solicitacoes/{s.Id}";
        var links = new List<LinkDto>
        {
            L("self", self),
            L("timeline", $"{self}/timeline"),
            L("regiao", $"{Base}/regioes/{s.RegiaoId}")
        };
        if (s.PontoSugeridoId.HasValue)
            links.Add(L("ponto-sugerido", $"{Base}/pontos-agua/{s.PontoSugeridoId.Value}"));

        if (usuario is { Perfil: PerfilUsuario.Cidadao } && usuario.Id == s.CidadaoId)
        {
            links.Add(L("minhas", $"{Base}/solicitacoes/minhas"));
            if (s.Status == StatusSolicitacao.Aberta)
                links.Add(L("cancelar", $"{self}/cancelar", "PATCH"));
        }
        else if (Gestor(usuario) && usuario!.PodeVerOrgao(s.OrgaoDestino))
        {
            links.Add(L("fila", $"{Base}/solicitacoes/fila"));
            if (s.Status == StatusSolicitacao.Aberta)
                links.Add(L("assumir-atendimento", $"{Base}/atendimentos", "POST"));
            if (s.Status is StatusSolicitacao.Aberta or StatusSolicitacao.EmAtendimento)
                links.Add(L("adicionar-nota", $"{self}/notas", "POST"));
        }
        return s with { Links = links };
    }

    public static AtendimentoResponse Com(AtendimentoResponse a, UsuarioContexto? usuario)
    {
        var self = $"{Base}/atendimentos/{a.Id}";
        var links = new List<LinkDto>
        {
            L("self", self),
            L("colecao", $"{Base}/atendimentos"),
            L("solicitacao", $"{Base}/solicitacoes/{a.SolicitacaoId}")
        };
        if (a.PontoAguaId.HasValue)
            links.Add(L("ponto-agua", $"{Base}/pontos-agua/{a.PontoAguaId.Value}"));

        var responsavel = usuario is not null && (Admin(usuario) || usuario.Id == a.GestorId);
        if (responsavel && a.Status == StatusAtendimento.EmAndamento)
        {
            links.Add(L("concluir", $"{self}/status", "PATCH"));
            links.Add(L("cancelar", $"{self}/status", "PATCH"));
        }
        return a with { Links = links };
    }

    public static OrientacaoResponse Com(OrientacaoResponse o, UsuarioContexto? usuario)
    {
        var self = $"{Base}/orientacoes/{o.Id}";
        var links = new List<LinkDto> { L("self", self), L("colecao", $"{Base}/orientacoes") };
        if (Admin(usuario))
        {
            links.Add(L("atualizar", self, "PUT"));
            links.Add(L("remover", self, "DELETE"));
        }
        return o with { Links = links };
    }

    public static UsuarioResponse Com(UsuarioResponse u, UsuarioContexto? usuario)
    {
        var self = $"{Base}/usuarios/{u.Id}";
        var links = new List<LinkDto> { L("self", self), L("colecao", $"{Base}/usuarios") };
        if (Admin(usuario) && u.Ativo) links.Add(L("desativar", self, "DELETE"));
        return u with { Links = links };
    }

    public static TimelineEventoResponse Com(TimelineEventoResponse e) =>
        e with { Links = new List<LinkDto> { L("solicitacao", $"{Base}/solicitacoes/{e.SolicitacaoId}") } };

    /// <summary>Links de navegação de uma listagem paginada, preservando os demais filtros da consulta.</summary>
    public static IReadOnlyList<LinkDto> Pagina(string rota, IReadOnlyDictionary<string, string> consulta,
        int pagina, int tamanhoPagina, int totalPaginas)
    {
        var ultima = Math.Max(totalPaginas, 1);

        string Href(int p)
        {
            var partes = consulta
                .Where(kv => !string.Equals(kv.Key, "page", StringComparison.OrdinalIgnoreCase)
                             && !string.Equals(kv.Key, "pageSize", StringComparison.OrdinalIgnoreCase))
                .Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}")
                .ToList();
            partes.Add($"page={p}");
            partes.Add($"pageSize={tamanhoPagina}");
            return $"{rota}?{string.Join("&", partes)}";
        }

        var links = new List<LinkDto> { L("self", Href(pagina)), L("first", Href(1)), L("last", Href(ultima)) };
        if (pagina > 1) links.Add(L("prev", Href(pagina - 1)));
        if (pagina < totalPaginas) links.Add(L("next", Href(pagina + 1)));
        return links;
    }
}
