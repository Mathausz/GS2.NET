using System.ComponentModel.DataAnnotations;

namespace NexusVerde.Api.Filters;

/// <summary>Valida DataAnnotations do argumento T e responde 400 (ValidationProblem) quando inválido.</summary>
public sealed class ValidationFilter<T> : IEndpointFilter where T : class
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var argumento = context.Arguments.OfType<T>().FirstOrDefault();
        if (argumento is null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["body"] = new[] { "Corpo da requisição ausente ou inválido." }
            });
        }

        var resultados = new List<ValidationResult>();
        if (!Validator.TryValidateObject(argumento, new ValidationContext(argumento), resultados, validateAllProperties: true))
        {
            var erros = resultados
                .SelectMany(r => (r.MemberNames.Any() ? r.MemberNames : new[] { string.Empty })
                    .Select(m => (Campo: m, Mensagem: r.ErrorMessage ?? "Valor inválido.")))
                .GroupBy(x => x.Campo)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Mensagem).ToArray());

            return Results.ValidationProblem(erros);
        }

        return await next(context);
    }
}

public static class ValidationFilterExtensions
{
    public static RouteHandlerBuilder WithValidation<T>(this RouteHandlerBuilder builder) where T : class =>
        builder.AddEndpointFilter<ValidationFilter<T>>();
}
