namespace NexusVerde.Api.Security;

public static class Policies
{
    public const string Cidadao = "Cidadao";
    public const string Gestor = "Gestor";   // Gestor ou Admin
    public const string Admin = "Admin";
}

public static class RateLimitPolicies
{
    public const string Auth = "auth";
    public const string Criacao = "criacao";
}

public static class CorsPolicies
{
    public const string App = "app";
}
