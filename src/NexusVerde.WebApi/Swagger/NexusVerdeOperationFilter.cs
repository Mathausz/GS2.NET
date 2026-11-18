using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using NexusVerde.Api.Middleware;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace NexusVerde.Api.Swagger;

/// <summary>
/// Documenta no Swagger: parâmetros de query em camelCase, cadeado (Bearer) apenas nos endpoints protegidos,
/// respostas 401/403 e o header obrigatório Idempotency-Key nas operações idempotentes.
/// </summary>
public sealed class NexusVerdeOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var metadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;

        // Query string em camelCase (page, pageSize, sortBy, desc...), como o JSON da API. O binding
        // do ASP.NET Core não diferencia maiúsculas, então o nome documentado funciona igual.
        if (operation.Parameters is not null)
        {
            foreach (var parametro in operation.Parameters.Where(p => p.In == ParameterLocation.Query && p.Name.Length > 0))
                parametro.Name = char.ToLowerInvariant(parametro.Name[0]) + parametro.Name[1..];
        }

        var protegido = metadata.OfType<IAuthorizeData>().Any() && !metadata.OfType<IAllowAnonymous>().Any();
        if (protegido)
        {
            operation.Security ??= new List<OpenApiSecurityRequirement>();
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                }] = Array.Empty<string>()
            });
            operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Token ausente, inválido ou expirado." });
            operation.Responses.TryAdd("403", new OpenApiResponse { Description = "Perfil sem permissão para esta operação." });
        }

        if (metadata.OfType<IdempotentMetadata>().Any())
        {
            operation.Parameters ??= new List<OpenApiParameter>();
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = IdempotencyMiddleware.HeaderName,
                In = ParameterLocation.Header,
                Required = true,
                Description = "Chave única da operação (ex.: um GUID). Repetir a chave com o mesmo corpo devolve a resposta original sem duplicar o efeito.",
                Schema = new OpenApiSchema { Type = "string", MaxLength = 100 }
            });
            operation.Responses.TryAdd("422", new OpenApiResponse { Description = "Idempotency-Key reutilizada com corpo diferente." });
        }
    }
}
