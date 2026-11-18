using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusVerde.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Linha do tempo das solicitações passa a ser gravada no Oracle (tabela NV_TIMELINE),
    /// substituindo a coleção do MongoDB.
    /// </summary>
    public partial class AddTimeline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NV_TIMELINE",
                columns: table => new
                {
                    Id = table.Column<string>(type: "NVARCHAR2(36)", maxLength: 36, nullable: false),
                    SolicitacaoId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    Tipo = table.Column<string>(type: "NVARCHAR2(30)", maxLength: 30, nullable: false),
                    Descricao = table.Column<string>(type: "NVARCHAR2(1000)", maxLength: 1000, nullable: false),
                    AutorId = table.Column<Guid>(type: "RAW(16)", nullable: true),
                    AutorPerfil = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: true),
                    Orgao = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: true),
                    Dados = table.Column<string>(type: "NVARCHAR2(2000)", maxLength: 2000, nullable: false),
                    OcorridoEm = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NV_TIMELINE", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NV_TIMELINE_NV_SOLICITACOES_SolicitacaoId",
                        column: x => x.SolicitacaoId,
                        principalTable: "NV_SOLICITACOES",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NV_TIMELINE_SolicitacaoId_OcorridoEm",
                table: "NV_TIMELINE",
                columns: new[] { "SolicitacaoId", "OcorridoEm" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NV_TIMELINE");
        }
    }
}
