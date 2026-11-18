using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace NexusVerde.Infrastructure.Data;

/// <summary>Garante que datas lidas do banco sejam tratadas como UTC (a API sempre grava UtcNow).</summary>
public sealed class UtcDateTimeConverter()
    : ValueConverter<DateTime, DateTime>(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

public sealed class NullableUtcDateTimeConverter()
    : ValueConverter<DateTime?, DateTime?>(
        v => v,
        v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);
