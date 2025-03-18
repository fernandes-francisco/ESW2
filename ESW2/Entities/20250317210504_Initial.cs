using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ESW2.Entities
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:estado_ativo", "Ativo,Encerrado,Em_Periodo");

            migrationBuilder.CreateTable(
                name: "administrador",
                columns: table => new
                {
                    id_admin = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("administrador_pkey", x => x.id_admin);
                });

            migrationBuilder.CreateTable(
                name: "banco",
                columns: table => new
                {
                    id_banco = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nome_banco = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("banco_pkey", x => x.id_banco);
                });

            migrationBuilder.CreateTable(
                name: "utilizador",
                columns: table => new
                {
                    id_utilizador = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    password = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("utilizador_pkey", x => x.id_utilizador);
                });

            migrationBuilder.CreateTable(
                name: "utilizador_cliente",
                columns: table => new
                {
                    id_cliente = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_utilizador = table.Column<int>(type: "integer", nullable: false),
                    morada = table.Column<string>(type: "text", nullable: true),
                    nif = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("utilizador_cliente_pkey", x => x.id_cliente);
                    table.ForeignKey(
                        name: "utilizador_cliente_id_utilizador_fkey",
                        column: x => x.id_utilizador,
                        principalTable: "utilizador",
                        principalColumn: "id_utilizador");
                });

            migrationBuilder.CreateTable(
                name: "ativo_financeiro",
                columns: table => new
                {
                    id_ativo = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_cliente = table.Column<int>(type: "integer", nullable: false),
                    id_admin = table.Column<int>(type: "integer", nullable: true),
                    data_inicio = table.Column<DateOnly>(type: "date", nullable: false),
                    duracao_meses = table.Column<int>(type: "integer", nullable: false),
                    percentual_imposto = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ativo_financeiro_pkey", x => x.id_ativo);
                    table.ForeignKey(
                        name: "ativo_financeiro_id_admin_fkey",
                        column: x => x.id_admin,
                        principalTable: "administrador",
                        principalColumn: "id_admin",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "ativo_financeiro_id_cliente_fkey",
                        column: x => x.id_cliente,
                        principalTable: "utilizador_cliente",
                        principalColumn: "id_cliente");
                });

            migrationBuilder.CreateTable(
                name: "deposito_prazo",
                columns: table => new
                {
                    id_deposito = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_ativo = table.Column<int>(type: "integer", nullable: false),
                    id_banco = table.Column<int>(type: "integer", nullable: false),
                    valor_deposito = table.Column<double>(type: "double precision", nullable: false),
                    numero_conta_banco = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    titulares = table.Column<string>(type: "text", nullable: false),
                    taxa_juro_anual = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("deposito_prazo_pkey", x => x.id_deposito);
                    table.ForeignKey(
                        name: "deposito_prazo_id_ativo_fkey",
                        column: x => x.id_ativo,
                        principalTable: "ativo_financeiro",
                        principalColumn: "id_ativo");
                    table.ForeignKey(
                        name: "deposito_prazo_id_banco_fkey",
                        column: x => x.id_banco,
                        principalTable: "banco",
                        principalColumn: "id_banco",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "fundo_investimento",
                columns: table => new
                {
                    id_fundo = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_ativo = table.Column<int>(type: "integer", nullable: false),
                    nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    valor_investido = table.Column<double>(type: "double precision", nullable: false),
                    taxa_juro_padrao = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("fundo_investimento_pkey", x => x.id_fundo);
                    table.ForeignKey(
                        name: "fundo_investimento_id_ativo_fkey",
                        column: x => x.id_ativo,
                        principalTable: "ativo_financeiro",
                        principalColumn: "id_ativo");
                });

            migrationBuilder.CreateTable(
                name: "imovel_arrendado",
                columns: table => new
                {
                    id_imovel = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_ativo = table.Column<int>(type: "integer", nullable: false),
                    designacao = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    localizacao = table.Column<string>(type: "text", nullable: false),
                    valor_imovel = table.Column<double>(type: "double precision", nullable: false),
                    valor_renda = table.Column<double>(type: "double precision", nullable: false),
                    valor_mensal_cond = table.Column<double>(type: "double precision", nullable: false),
                    valor_anual_despesas = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("imovel_arrendado_pkey", x => x.id_imovel);
                    table.ForeignKey(
                        name: "imovel_arrendado_id_ativo_fkey",
                        column: x => x.id_ativo,
                        principalTable: "ativo_financeiro",
                        principalColumn: "id_ativo");
                });

            migrationBuilder.CreateTable(
                name: "pagamento_imposto",
                columns: table => new
                {
                    id_imposto = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_ativo = table.Column<int>(type: "integer", nullable: false),
                    data_pagamento = table.Column<DateOnly>(type: "date", nullable: false),
                    valor_pago = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pagamento_imposto_pkey", x => x.id_imposto);
                    table.ForeignKey(
                        name: "pagamento_imposto_id_ativo_fkey",
                        column: x => x.id_ativo,
                        principalTable: "ativo_financeiro",
                        principalColumn: "id_ativo");
                });

            migrationBuilder.CreateTable(
                name: "taxa_mensal",
                columns: table => new
                {
                    id_taxa = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_fundo = table.Column<int>(type: "integer", nullable: false),
                    mes = table.Column<DateOnly>(type: "date", nullable: false),
                    taxa_juro = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("taxa_mensal_pkey", x => x.id_taxa);
                    table.ForeignKey(
                        name: "taxa_mensal_id_fundo_fkey",
                        column: x => x.id_fundo,
                        principalTable: "fundo_investimento",
                        principalColumn: "id_fundo");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ativo_financeiro_id_admin",
                table: "ativo_financeiro",
                column: "id_admin");

            migrationBuilder.CreateIndex(
                name: "IX_ativo_financeiro_id_cliente",
                table: "ativo_financeiro",
                column: "id_cliente");

            migrationBuilder.CreateIndex(
                name: "deposito_prazo_numero_conta_banco_key",
                table: "deposito_prazo",
                column: "numero_conta_banco",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_deposito_prazo_id_ativo",
                table: "deposito_prazo",
                column: "id_ativo");

            migrationBuilder.CreateIndex(
                name: "IX_deposito_prazo_id_banco",
                table: "deposito_prazo",
                column: "id_banco");

            migrationBuilder.CreateIndex(
                name: "IX_fundo_investimento_id_ativo",
                table: "fundo_investimento",
                column: "id_ativo");

            migrationBuilder.CreateIndex(
                name: "IX_imovel_arrendado_id_ativo",
                table: "imovel_arrendado",
                column: "id_ativo");

            migrationBuilder.CreateIndex(
                name: "IX_pagamento_imposto_id_ativo",
                table: "pagamento_imposto",
                column: "id_ativo");

            migrationBuilder.CreateIndex(
                name: "IX_taxa_mensal_id_fundo",
                table: "taxa_mensal",
                column: "id_fundo");

            migrationBuilder.CreateIndex(
                name: "utilizador_username_key",
                table: "utilizador",
                column: "username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_utilizador_cliente_id_utilizador",
                table: "utilizador_cliente",
                column: "id_utilizador");

            migrationBuilder.CreateIndex(
                name: "utilizador_cliente_nif_key",
                table: "utilizador_cliente",
                column: "nif",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "deposito_prazo");

            migrationBuilder.DropTable(
                name: "imovel_arrendado");

            migrationBuilder.DropTable(
                name: "pagamento_imposto");

            migrationBuilder.DropTable(
                name: "taxa_mensal");

            migrationBuilder.DropTable(
                name: "banco");

            migrationBuilder.DropTable(
                name: "fundo_investimento");

            migrationBuilder.DropTable(
                name: "ativo_financeiro");

            migrationBuilder.DropTable(
                name: "administrador");

            migrationBuilder.DropTable(
                name: "utilizador_cliente");

            migrationBuilder.DropTable(
                name: "utilizador");
        }
    }
}
