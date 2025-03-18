﻿// <auto-generated />
using System;
using ESW2.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ESW2.Entities
{
    [DbContext(typeof(MyDbContext))]
    partial class MyDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.3")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.HasPostgresEnum(modelBuilder, "estado_ativo", new[] { "Ativo", "Encerrado", "Em_Periodo" });
            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("ESW2.Entities.administrador", b =>
                {
                    b.Property<int>("id_admin")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("id_admin"));

                    b.Property<string>("nome")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.HasKey("id_admin")
                        .HasName("administrador_pkey");

                    b.ToTable("administrador");
                });

            modelBuilder.Entity("ESW2.Entities.ativo_financeiro", b =>
                {
                    b.Property<int>("id_ativo")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("id_ativo"));

                    b.Property<DateOnly>("data_inicio")
                        .HasColumnType("date");

                    b.Property<int>("duracao_meses")
                        .HasColumnType("integer");

                    b.Property<int?>("id_admin")
                        .HasColumnType("integer");

                    b.Property<int>("id_cliente")
                        .HasColumnType("integer");

                    b.Property<double>("percentual_imposto")
                        .HasColumnType("double precision");

                    b.HasKey("id_ativo")
                        .HasName("ativo_financeiro_pkey");

                    b.HasIndex("id_admin");

                    b.HasIndex("id_cliente");

                    b.ToTable("ativo_financeiro");
                });

            modelBuilder.Entity("ESW2.Entities.banco", b =>
                {
                    b.Property<int>("id_banco")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("id_banco"));

                    b.Property<string>("nome_banco")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.HasKey("id_banco")
                        .HasName("banco_pkey");

                    b.ToTable("banco");
                });

            modelBuilder.Entity("ESW2.Entities.deposito_prazo", b =>
                {
                    b.Property<int>("id_deposito")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("id_deposito"));

                    b.Property<int>("id_ativo")
                        .HasColumnType("integer");

                    b.Property<int>("id_banco")
                        .HasColumnType("integer");

                    b.Property<string>("numero_conta_banco")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)");

                    b.Property<double>("taxa_juro_anual")
                        .HasColumnType("double precision");

                    b.Property<string>("titulares")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<double>("valor_deposito")
                        .HasColumnType("double precision");

                    b.HasKey("id_deposito")
                        .HasName("deposito_prazo_pkey");

                    b.HasIndex("id_ativo");

                    b.HasIndex("id_banco");

                    b.HasIndex(new[] { "numero_conta_banco" }, "deposito_prazo_numero_conta_banco_key")
                        .IsUnique();

                    b.ToTable("deposito_prazo");
                });

            modelBuilder.Entity("ESW2.Entities.fundo_investimento", b =>
                {
                    b.Property<int>("id_fundo")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("id_fundo"));

                    b.Property<int>("id_ativo")
                        .HasColumnType("integer");

                    b.Property<string>("nome")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<double>("taxa_juro_padrao")
                        .HasColumnType("double precision");

                    b.Property<double>("valor_investido")
                        .HasColumnType("double precision");

                    b.HasKey("id_fundo")
                        .HasName("fundo_investimento_pkey");

                    b.HasIndex("id_ativo");

                    b.ToTable("fundo_investimento");
                });

            modelBuilder.Entity("ESW2.Entities.imovel_arrendado", b =>
                {
                    b.Property<int>("id_imovel")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("id_imovel"));

                    b.Property<string>("designacao")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<int>("id_ativo")
                        .HasColumnType("integer");

                    b.Property<string>("localizacao")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<double>("valor_anual_despesas")
                        .HasColumnType("double precision");

                    b.Property<double>("valor_imovel")
                        .HasColumnType("double precision");

                    b.Property<double>("valor_mensal_cond")
                        .HasColumnType("double precision");

                    b.Property<double>("valor_renda")
                        .HasColumnType("double precision");

                    b.HasKey("id_imovel")
                        .HasName("imovel_arrendado_pkey");

                    b.HasIndex("id_ativo");

                    b.ToTable("imovel_arrendado");
                });

            modelBuilder.Entity("ESW2.Entities.pagamento_imposto", b =>
                {
                    b.Property<int>("id_imposto")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("id_imposto"));

                    b.Property<DateOnly>("data_pagamento")
                        .HasColumnType("date");

                    b.Property<int>("id_ativo")
                        .HasColumnType("integer");

                    b.Property<double>("valor_pago")
                        .HasColumnType("double precision");

                    b.HasKey("id_imposto")
                        .HasName("pagamento_imposto_pkey");

                    b.HasIndex("id_ativo");

                    b.ToTable("pagamento_imposto");
                });

            modelBuilder.Entity("ESW2.Entities.taxa_mensal", b =>
                {
                    b.Property<int>("id_taxa")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("id_taxa"));

                    b.Property<int>("id_fundo")
                        .HasColumnType("integer");

                    b.Property<DateOnly>("mes")
                        .HasColumnType("date");

                    b.Property<double>("taxa_juro")
                        .HasColumnType("double precision");

                    b.HasKey("id_taxa")
                        .HasName("taxa_mensal_pkey");

                    b.HasIndex("id_fundo");

                    b.ToTable("taxa_mensal");
                });

            modelBuilder.Entity("ESW2.Entities.utilizador", b =>
                {
                    b.Property<int>("id_utilizador")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("id_utilizador"));

                    b.Property<string>("password")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("character varying(255)");

                    b.Property<string>("username")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.HasKey("id_utilizador")
                        .HasName("utilizador_pkey");

                    b.HasIndex(new[] { "username" }, "utilizador_username_key")
                        .IsUnique();

                    b.ToTable("utilizador");
                });

            modelBuilder.Entity("ESW2.Entities.utilizador_cliente", b =>
                {
                    b.Property<int>("id_cliente")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("id_cliente"));

                    b.Property<int>("id_utilizador")
                        .HasColumnType("integer");

                    b.Property<string>("morada")
                        .HasColumnType("text");

                    b.Property<string>("nif")
                        .HasMaxLength(20)
                        .HasColumnType("character varying(20)");

                    b.HasKey("id_cliente")
                        .HasName("utilizador_cliente_pkey");

                    b.HasIndex("id_utilizador");

                    b.HasIndex(new[] { "nif" }, "utilizador_cliente_nif_key")
                        .IsUnique();

                    b.ToTable("utilizador_cliente");
                });

            modelBuilder.Entity("ESW2.Entities.ativo_financeiro", b =>
                {
                    b.HasOne("ESW2.Entities.administrador", "id_adminNavigation")
                        .WithMany("ativo_financeiros")
                        .HasForeignKey("id_admin")
                        .OnDelete(DeleteBehavior.SetNull)
                        .HasConstraintName("ativo_financeiro_id_admin_fkey");

                    b.HasOne("ESW2.Entities.utilizador_cliente", "id_clienteNavigation")
                        .WithMany("ativo_financeiros")
                        .HasForeignKey("id_cliente")
                        .IsRequired()
                        .HasConstraintName("ativo_financeiro_id_cliente_fkey");

                    b.Navigation("id_adminNavigation");

                    b.Navigation("id_clienteNavigation");
                });

            modelBuilder.Entity("ESW2.Entities.deposito_prazo", b =>
                {
                    b.HasOne("ESW2.Entities.ativo_financeiro", "id_ativoNavigation")
                        .WithMany("deposito_prazos")
                        .HasForeignKey("id_ativo")
                        .IsRequired()
                        .HasConstraintName("deposito_prazo_id_ativo_fkey");

                    b.HasOne("ESW2.Entities.banco", "id_bancoNavigation")
                        .WithMany("deposito_prazos")
                        .HasForeignKey("id_banco")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("deposito_prazo_id_banco_fkey");

                    b.Navigation("id_ativoNavigation");

                    b.Navigation("id_bancoNavigation");
                });

            modelBuilder.Entity("ESW2.Entities.fundo_investimento", b =>
                {
                    b.HasOne("ESW2.Entities.ativo_financeiro", "id_ativoNavigation")
                        .WithMany("fundo_investimentos")
                        .HasForeignKey("id_ativo")
                        .IsRequired()
                        .HasConstraintName("fundo_investimento_id_ativo_fkey");

                    b.Navigation("id_ativoNavigation");
                });

            modelBuilder.Entity("ESW2.Entities.imovel_arrendado", b =>
                {
                    b.HasOne("ESW2.Entities.ativo_financeiro", "id_ativoNavigation")
                        .WithMany("imovel_arrendados")
                        .HasForeignKey("id_ativo")
                        .IsRequired()
                        .HasConstraintName("imovel_arrendado_id_ativo_fkey");

                    b.Navigation("id_ativoNavigation");
                });

            modelBuilder.Entity("ESW2.Entities.pagamento_imposto", b =>
                {
                    b.HasOne("ESW2.Entities.ativo_financeiro", "id_ativoNavigation")
                        .WithMany("pagamento_impostos")
                        .HasForeignKey("id_ativo")
                        .IsRequired()
                        .HasConstraintName("pagamento_imposto_id_ativo_fkey");

                    b.Navigation("id_ativoNavigation");
                });

            modelBuilder.Entity("ESW2.Entities.taxa_mensal", b =>
                {
                    b.HasOne("ESW2.Entities.fundo_investimento", "id_fundoNavigation")
                        .WithMany("taxa_mensals")
                        .HasForeignKey("id_fundo")
                        .IsRequired()
                        .HasConstraintName("taxa_mensal_id_fundo_fkey");

                    b.Navigation("id_fundoNavigation");
                });

            modelBuilder.Entity("ESW2.Entities.utilizador_cliente", b =>
                {
                    b.HasOne("ESW2.Entities.utilizador", "id_utilizadorNavigation")
                        .WithMany("utilizador_clientes")
                        .HasForeignKey("id_utilizador")
                        .IsRequired()
                        .HasConstraintName("utilizador_cliente_id_utilizador_fkey");

                    b.Navigation("id_utilizadorNavigation");
                });

            modelBuilder.Entity("ESW2.Entities.administrador", b =>
                {
                    b.Navigation("ativo_financeiros");
                });

            modelBuilder.Entity("ESW2.Entities.ativo_financeiro", b =>
                {
                    b.Navigation("deposito_prazos");

                    b.Navigation("fundo_investimentos");

                    b.Navigation("imovel_arrendados");

                    b.Navigation("pagamento_impostos");
                });

            modelBuilder.Entity("ESW2.Entities.banco", b =>
                {
                    b.Navigation("deposito_prazos");
                });

            modelBuilder.Entity("ESW2.Entities.fundo_investimento", b =>
                {
                    b.Navigation("taxa_mensals");
                });

            modelBuilder.Entity("ESW2.Entities.utilizador", b =>
                {
                    b.Navigation("utilizador_clientes");
                });

            modelBuilder.Entity("ESW2.Entities.utilizador_cliente", b =>
                {
                    b.Navigation("ativo_financeiros");
                });
#pragma warning restore 612, 618
        }
    }
}
