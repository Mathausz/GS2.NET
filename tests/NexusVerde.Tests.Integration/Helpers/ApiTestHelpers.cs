using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using NexusVerde.Tests.Integration.Fixtures;

namespace NexusVerde.Tests.Integration.Helpers;

/// <summary>Cenário pronto: região com ponto de água e orientação, criado pelo Admin.</summary>
public sealed record CenarioCrise(HttpClient Admin, string RegiaoId, string PontoId);

internal static class ApiTestHelpers
{
    private static int _contador;

    /// <summary>
    /// Coordenada exclusiva por cenário (clusters afastados ~100 km), para que o "ponto mais próximo" de um
    /// teste nunca seja confundido com o de outro, mesmo compartilhando o mesmo banco em memória.
    /// </summary>
    public static (double Lat, double Lng) NovaCoordenada()
    {
        var k = Interlocked.Increment(ref _contador);
        return (-10.0, -170.0 + k);
    }

    public static async Task<JsonNode> LerJsonAsync(this HttpResponseMessage response)
    {
        var texto = await response.Content.ReadAsStringAsync();
        return JsonNode.Parse(texto) ?? throw new InvalidOperationException($"Resposta sem JSON: {texto}");
    }

    /// <summary>Lista os "rel" do campo _links de um recurso ou de uma página.</summary>
    public static List<string> Rels(this JsonNode recurso) =>
        recurso["_links"]?.AsArray().Select(l => l!["rel"]!.GetValue<string>()).ToList() ?? new List<string>();

    public static async Task<string> LoginAsync(this HttpClient client, string email, string senha)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, senha });
        response.EnsureSuccessStatusCode();
        var json = await response.LerJsonAsync();
        return json["token"]!.GetValue<string>();
    }

    public static HttpClient ComToken(this NexusVerdeFactory factory, string? token)
    {
        var client = factory.CreateClient();
        if (token is not null)
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public static async Task<HttpClient> ComoAdminAsync(this NexusVerdeFactory factory)
    {
        var anonimo = factory.CreateClient();
        var token = await anonimo.LoginAsync(NexusVerdeFactory.AdminEmail, NexusVerdeFactory.AdminSenha);
        return factory.ComToken(token);
    }

    /// <summary>Registra um cidadão e devolve um cliente já autenticado.</summary>
    public static async Task<HttpClient> ComoCidadaoAsync(this NexusVerdeFactory factory, string? email = null)
    {
        email ??= $"cidadao-{Guid.NewGuid():N}@teste.com";
        var anonimo = factory.CreateClient();
        var registro = await anonimo.PostAsJsonAsync("/api/v1/auth/registro",
            new { nome = "Cidadão de Teste", email, senha = "Senha#12345" });
        registro.EnsureSuccessStatusCode();
        return factory.ComToken(await anonimo.LoginAsync(email, "Senha#12345"));
    }

    /// <summary>Admin cria um gestor do órgão informado e devolve um cliente autenticado como ele.</summary>
    public static async Task<HttpClient> ComoGestorAsync(this NexusVerdeFactory factory, HttpClient admin, string orgao)
    {
        var email = $"gestor-{Guid.NewGuid():N}@teste.com";
        var criado = await admin.PostAsJsonAsync("/api/v1/usuarios",
            new { nome = "Gestor de Teste", email, senha = "Gestor#12345", perfil = "Gestor", orgao });
        criado.EnsureSuccessStatusCode();
        return factory.ComToken(await factory.CreateClient().LoginAsync(email, "Gestor#12345"));
    }

    public static async Task<string> CriarRegiaoAsync(this HttpClient admin, string nome = "Sarandi", string estado = "RS",
        double lat = -30.0098, double lng = -51.1301)
    {
        var response = await admin.PostAsJsonAsync("/api/v1/regioes", new
        {
            nome, municipio = "Porto Alegre", estado, latitude = lat, longitude = lng, populacaoEstimada = 60000
        });
        response.EnsureSuccessStatusCode();
        return (await response.LerJsonAsync())["id"]!.GetValue<string>();
    }

    public static async Task<string> CriarPontoAsync(this HttpClient gestor, string regiaoId, string nome, double lat, double lng)
    {
        var response = await gestor.PostAsJsonAsync("/api/v1/pontos-agua", new
        {
            regiaoId, nome, endereco = "Rua Teste, 100", latitude = lat, longitude = lng, capacidadeLitrosDia = 10000
        });
        response.EnsureSuccessStatusCode();
        return (await response.LerJsonAsync())["id"]!.GetValue<string>();
    }

    /// <summary>Cria região + ponto de água (próximo ao centro) + orientação de água suspeita.</summary>
    public static async Task<CenarioCrise> PrepararCriseAsync(this NexusVerdeFactory factory)
    {
        var admin = await factory.ComoAdminAsync();
        var (lat, lng) = NovaCoordenada();
        var regiaoId = await admin.CriarRegiaoAsync($"Região {Guid.NewGuid():N}", "RS", lat, lng);
        var pontoId = await admin.CriarPontoAsync(regiaoId, "Ginásio Central", lat + 0.005, lng + 0.005);
        await admin.PostAsJsonAsync("/api/v1/orientacoes", new
        {
            tipo = "AguaSuspeita", titulo = "Não consuma a água", texto = "Ferva por 1 minuto antes de beber.", ordem = 1
        });
        return new CenarioCrise(admin, regiaoId, pontoId);
    }

    public static object PedidoDeAjuda(string regiaoId, string tipo = "SemAgua", int pessoas = 12) => new
    {
        regiaoId, tipo, descricao = "Bairro sem água desde ontem à noite", quantidadePessoas = pessoas, horasSemAgua = 20
    };

    public static Task<HttpResponseMessage> PostIdempotenteAsync(this HttpClient client, string url, object corpo, string? chave)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(corpo) };
        if (chave is not null) request.Headers.Add("Idempotency-Key", chave);
        return client.SendAsync(request);
    }

    /// <summary>Cidadão cria uma solicitação e devolve o id (falha o teste se não for 201).</summary>
    public static async Task<string> CriarSolicitacaoAsync(this HttpClient cidadao, string regiaoId, string tipo = "SemAgua")
    {
        var response = await cidadao.PostIdempotenteAsync("/api/v1/solicitacoes",
            PedidoDeAjuda(regiaoId, tipo), Guid.NewGuid().ToString());
        response.EnsureSuccessStatusCode();
        return (await response.LerJsonAsync())["solicitacao"]!["id"]!.GetValue<string>();
    }
}
