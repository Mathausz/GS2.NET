using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusVerde.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NV_IDEMPOTENCIA",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    Chave = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false),
                    UsuarioId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    Rota = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    HashCorpo = table.Column<string>(type: "NVARCHAR2(64)", maxLength: 64, nullable: false),
                    StatusCode = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    ContentType = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: true),
                    CorpoResposta = table.Column<string>(type: "CLOB", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NV_IDEMPOTENCIA", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NV_ORIENTACOES",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    Tipo = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: false),
                    Titulo = table.Column<string>(type: "NVARCHAR2(150)", maxLength: 150, nullable: false),
                    Texto = table.Column<string>(type: "NVARCHAR2(2000)", maxLength: 2000, nullable: false),
                    Ordem = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NV_ORIENTACOES", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NV_REGIOES",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    Nome = table.Column<string>(type: "NVARCHAR2(120)", maxLength: 120, nullable: false),
                    Municipio = table.Column<string>(type: "NVARCHAR2(120)", maxLength: 120, nullable: false),
                    Estado = table.Column<string>(type: "NVARCHAR2(2)", maxLength: 2, nullable: false),
                    Latitude = table.Column<double>(type: "BINARY_DOUBLE", nullable: false),
                    Longitude = table.Column<double>(type: "BINARY_DOUBLE", nullable: false),
                    PopulacaoEstimada = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    NivelRisco = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NV_REGIOES", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NV_USUARIOS",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    Nome = table.Column<string>(type: "NVARCHAR2(120)", maxLength: 120, nullable: false),
                    Email = table.Column<string>(type: "NVARCHAR2(160)", maxLength: 160, nullable: false),
                    SenhaHash = table.Column<string>(type: "NVARCHAR2(500)", maxLength: 500, nullable: false),
                    Perfil = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: false),
                    Orgao = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: false),
                    Ativo = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NV_USUARIOS", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NV_DESASTRES",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    RegiaoId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    Tipo = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: false),
                    Gravidade = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    Descricao = table.Column<string>(type: "NVARCHAR2(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: false),
                    IniciadoEm = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    EncerradoEm = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NV_DESASTRES", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NV_DESASTRES_NV_REGIOES_RegiaoId",
                        column: x => x.RegiaoId,
                        principalTable: "NV_REGIOES",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NV_PONTOS_AGUA",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    RegiaoId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    Nome = table.Column<string>(type: "NVARCHAR2(120)", maxLength: 120, nullable: false),
                    Endereco = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: false),
                    Latitude = table.Column<double>(type: "BINARY_DOUBLE", nullable: false),
                    Longitude = table.Column<double>(type: "BINARY_DOUBLE", nullable: false),
                    CapacidadeLitrosDia = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    Status = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: false),
                    Descricao = table.Column<string>(type: "NVARCHAR2(500)", maxLength: 500, nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NV_PONTOS_AGUA", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NV_PONTOS_AGUA_NV_REGIOES_RegiaoId",
                        column: x => x.RegiaoId,
                        principalTable: "NV_REGIOES",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NV_SOLICITACOES",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    CidadaoId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    RegiaoId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    DesastreId = table.Column<Guid>(type: "RAW(16)", nullable: true),
                    PontoSugeridoId = table.Column<Guid>(type: "RAW(16)", nullable: true),
                    Tipo = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: false),
                    Descricao = table.Column<string>(type: "NVARCHAR2(1000)", maxLength: 1000, nullable: false),
                    QuantidadePessoas = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    HorasSemAgua = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    Latitude = table.Column<double>(type: "BINARY_DOUBLE", nullable: false),
                    Longitude = table.Column<double>(type: "BINARY_DOUBLE", nullable: false),
                    ScorePrioridade = table.Column<double>(type: "BINARY_DOUBLE", nullable: false),
                    NivelPrioridade = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: false),
                    OrgaoDestino = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NV_SOLICITACOES", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NV_SOLICITACOES_NV_DESASTRES_DesastreId",
                        column: x => x.DesastreId,
                        principalTable: "NV_DESASTRES",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NV_SOLICITACOES_NV_PONTOS_AGUA_PontoSugeridoId",
                        column: x => x.PontoSugeridoId,
                        principalTable: "NV_PONTOS_AGUA",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NV_SOLICITACOES_NV_REGIOES_RegiaoId",
                        column: x => x.RegiaoId,
                        principalTable: "NV_REGIOES",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NV_SOLICITACOES_NV_USUARIOS_CidadaoId",
                        column: x => x.CidadaoId,
                        principalTable: "NV_USUARIOS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NV_ATENDIMENTOS",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    SolicitacaoId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    GestorId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    PontoAguaId = table.Column<Guid>(type: "RAW(16)", nullable: true),
                    Status = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: false),
                    Observacao = table.Column<string>(type: "NVARCHAR2(1000)", maxLength: 1000, nullable: true),
                    IniciadoEm = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    FinalizadoEm = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NV_ATENDIMENTOS", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NV_ATENDIMENTOS_NV_PONTOS_AGUA_PontoAguaId",
                        column: x => x.PontoAguaId,
                        principalTable: "NV_PONTOS_AGUA",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NV_ATENDIMENTOS_NV_SOLICITACOES_SolicitacaoId",
                        column: x => x.SolicitacaoId,
                        principalTable: "NV_SOLICITACOES",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NV_ATENDIMENTOS_NV_USUARIOS_GestorId",
                        column: x => x.GestorId,
                        principalTable: "NV_USUARIOS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NV_ATENDIMENTOS_GestorId",
                table: "NV_ATENDIMENTOS",
                column: "GestorId");

            migrationBuilder.CreateIndex(
                name: "IX_NV_ATENDIMENTOS_PontoAguaId",
                table: "NV_ATENDIMENTOS",
                column: "PontoAguaId");

            migrationBuilder.CreateIndex(
                name: "IX_NV_ATENDIMENTOS_SolicitacaoId",
                table: "NV_ATENDIMENTOS",
                column: "SolicitacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_NV_DESASTRES_RegiaoId_Status",
                table: "NV_DESASTRES",
                columns: new[] { "RegiaoId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_NV_IDEMPOTENCIA_UsuarioId_Chave_Rota",
                table: "NV_IDEMPOTENCIA",
                columns: new[] { "UsuarioId", "Chave", "Rota" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NV_ORIENTACOES_Tipo",
                table: "NV_ORIENTACOES",
                column: "Tipo");

            migrationBuilder.CreateIndex(
                name: "IX_NV_PONTOS_AGUA_RegiaoId",
                table: "NV_PONTOS_AGUA",
                column: "RegiaoId");

            migrationBuilder.CreateIndex(
                name: "IX_NV_PONTOS_AGUA_Status",
                table: "NV_PONTOS_AGUA",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_NV_REGIOES_Estado_Municipio",
                table: "NV_REGIOES",
                columns: new[] { "Estado", "Municipio" });

            migrationBuilder.CreateIndex(
                name: "IX_NV_SOLICITACOES_CidadaoId",
                table: "NV_SOLICITACOES",
                column: "CidadaoId");

            migrationBuilder.CreateIndex(
                name: "IX_NV_SOLICITACOES_DesastreId",
                table: "NV_SOLICITACOES",
                column: "DesastreId");

            migrationBuilder.CreateIndex(
                name: "IX_NV_SOLICITACOES_PontoSugeridoId",
                table: "NV_SOLICITACOES",
                column: "PontoSugeridoId");

            migrationBuilder.CreateIndex(
                name: "IX_NV_SOLICITACOES_RegiaoId",
                table: "NV_SOLICITACOES",
                column: "RegiaoId");

            migrationBuilder.CreateIndex(
                name: "IX_NV_SOLICITACOES_Status_OrgaoDestino_ScorePrioridade",
                table: "NV_SOLICITACOES",
                columns: new[] { "Status", "OrgaoDestino", "ScorePrioridade" });

            migrationBuilder.CreateIndex(
                name: "IX_NV_USUARIOS_Email",
                table: "NV_USUARIOS",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NV_ATENDIMENTOS");

            migrationBuilder.DropTable(
                name: "NV_IDEMPOTENCIA");

            migrationBuilder.DropTable(
                name: "NV_ORIENTACOES");

            migrationBuilder.DropTable(
                name: "NV_SOLICITACOES");

            migrationBuilder.DropTable(
                name: "NV_DESASTRES");

            migrationBuilder.DropTable(
                name: "NV_PONTOS_AGUA");

            migrationBuilder.DropTable(
                name: "NV_USUARIOS");

            migrationBuilder.DropTable(
                name: "NV_REGIOES");
        }
    }
}
