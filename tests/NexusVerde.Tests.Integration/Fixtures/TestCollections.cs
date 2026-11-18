// As coleções sobem várias instâncias da API (factories diferentes). Rodá-las em sequência evita disputa
// na inicialização do logger estático do Serilog (bootstrap logger) e deixa os testes de rate limit estáveis.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace NexusVerde.Tests.Integration.Fixtures;

/// <summary>
/// Collection Fixture: uma única instância da API (e do banco em memória) é compartilhada por todos os
/// testes de integração, em vez de subir uma aplicação por classe. Cada teste cria seus próprios dados
/// (regiões em coordenadas únicas, e-mails únicos), então não há interferência entre eles.
/// </summary>
[CollectionDefinition(Name)]
public class IntegrationTestCollection : ICollectionFixture<NexusVerdeFactory>
{
    public const string Name = "Integration Tests";
}

/// <summary>Coleção própria, com limites de requisição baixos, para o teste de rate limiting (429).</summary>
[CollectionDefinition(Name)]
public class RateLimitTestCollection : ICollectionFixture<RateLimitedFactory>
{
    public const string Name = "RateLimit Tests";
}

/// <summary>Coleção com limite baixo de criação, para o 429 da política "criacao".</summary>
[CollectionDefinition(Name)]
public class CriacaoRateLimitTestCollection : ICollectionFixture<CriacaoLimitadaFactory>
{
    public const string Name = "Criacao RateLimit Tests";
}

/// <summary>Coleção com o serviço externo de clima indisponível (health check Degraded).</summary>
[CollectionDefinition(Name)]
public class ClimaIndisponivelTestCollection : ICollectionFixture<ClimaIndisponivelFactory>
{
    public const string Name = "Clima Indisponivel Tests";
}

/// <summary>Coleção que sobe a API sem nenhuma configuração (modo dotnet run em Development).</summary>
[CollectionDefinition(Name)]
public class ZeroConfigTestCollection : ICollectionFixture<ZeroConfigFactory>
{
    public const string Name = "Zero Config Tests";
}
