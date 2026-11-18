using Microsoft.EntityFrameworkCore;
using Oracle.EntityFrameworkCore.Infrastructure;

namespace NexusVerde.Infrastructure.Data;

/// <summary>
/// O Oracle da FIAP é 19c. Sem esta configuração o provider gera SQL do Oracle 23 (ex.: coluna <c>BOOLEAN</c>),
/// que o 19c rejeita (ORA-00902). Com a compatibilidade 19, <c>bool</c> vira <c>NUMBER(1)</c>.
/// </summary>
public static class OracleCompatibilidade
{
    public static void Configurar(OracleDbContextOptionsBuilder oracle) =>
        oracle.UseOracleSQLCompatibility(OracleSQLCompatibility.DatabaseVersion19);
}
