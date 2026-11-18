using System.ComponentModel.DataAnnotations;

namespace NexusVerde.Application.Validation;

/// <summary>Garante que um Guid foi informado (diferente de Guid.Empty).</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class NotEmptyGuidAttribute : ValidationAttribute
{
    public NotEmptyGuidAttribute() : base("O identificador informado é inválido.") { }

    public override bool IsValid(object? value) => value is Guid g && g != Guid.Empty;
}
