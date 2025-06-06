﻿using System;
using System.Collections.Generic;
using ESW2.Entities; // Ensure this points to your entities namespace
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion; // <<-- Add this for EnumToStringConverter

namespace ESW2.Context // Ensure this namespace is correct
{
    public partial class MyDbContext : DbContext
    {
        // Constructor for Dependency Injection
        public MyDbContext(DbContextOptions<MyDbContext> options)
            : base(options)
        {
        }

        // --- DbSets ---
        public virtual DbSet<administrador> administradors { get; set; }
        public virtual DbSet<ativo_financeiro> ativo_financeiros { get; set; }
        public virtual DbSet<banco> bancos { get; set; }
        public virtual DbSet<deposito_prazo> deposito_prazos { get; set; }
        public virtual DbSet<fundo_investimento> fundo_investimentos { get; set; }
        public virtual DbSet<imovel_arrendado> imovel_arrendados { get; set; }
        public virtual DbSet<pagamento_imposto> pagamento_impostos { get; set; }
        public virtual DbSet<taxa_mensal> taxa_mensals { get; set; }
        public virtual DbSet<utilizador> utilizadors { get; set; }
        public virtual DbSet<utilizador_cliente> utilizador_clientes { get; set; }


        // REMOVE or comment out OnConfiguring if using Dependency Injection.
        
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=Eng_Soft;Username=postgres;Password=2167");
            }
        }
        

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // --- Enum Mapping ---
            // Keep the global mapping in Program.cs using NpgsqlDataSourceBuilder.MapEnum<T>
            // Remove any HasPostgresEnum<T> calls from here.
           // --- Entity Configurations ---

            modelBuilder.Entity<administrador>(entity =>
            {
                entity.HasKey(e => e.id_admin).HasName("administrador_pkey");
                entity.ToTable("administrador");
                entity.Property(e => e.nome).HasMaxLength(100);
            });

            modelBuilder.Entity<ativo_financeiro>(entity =>
            {
                entity.HasKey(e => e.id_ativo).HasName("ativo_financeiro_pkey");
                entity.ToTable("ativo_financeiro");
                
                entity.Property(e => e.estado)
                    .HasColumnName("estado")
                    .HasColumnType("estado_ativo");
                // --- Relationships (Keep as scaffolded/previously corrected) ---
                entity.HasOne(d => d.id_adminNavigation).WithMany(p => p.ativo_financeiros)
                    .HasForeignKey(d => d.id_admin)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("ativo_financeiro_id_admin_fkey");

                entity.HasOne(d => d.id_clienteNavigation).WithMany(p => p.ativo_financeiros)
                    .HasForeignKey(d => d.id_cliente)
                    .OnDelete(DeleteBehavior.ClientSetNull) // Or Restrict/NoAction if preferred
                    .IsRequired()
                    .HasConstraintName("ativo_financeiro_id_cliente_fkey");

                entity.HasOne(d => d.id_depositoNavigation).WithMany(p => p.ativo_financeiros)
                    .HasForeignKey(d => d.id_deposito)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("ativo_financeiro_id_deposito_fkey");

                entity.HasOne(d => d.id_fundoNavigation).WithMany(p => p.ativo_financeiros)
                    .HasForeignKey(d => d.id_fundo)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("ativo_financeiro_id_fundo_fkey");

                entity.HasOne(d => d.id_imovelNavigation).WithMany(p => p.ativo_financeiros)
                    .HasForeignKey(d => d.id_imovel)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("ativo_financeiro_id_imovel_fkey");
            });

            // --- Configurations for other entities (Keep as before) ---
            modelBuilder.Entity<banco>(entity =>
            {
                entity.HasKey(e => e.id_banco).HasName("banco_pkey");
                entity.ToTable("banco");
                entity.Property(e => e.nome_banco).HasMaxLength(100);
            });

            modelBuilder.Entity<deposito_prazo>(entity =>
            {
                entity.HasKey(e => e.id_deposito).HasName("deposito_prazo_pkey");
                entity.ToTable("deposito_prazo");
                entity.HasIndex(e => e.numero_conta_banco, "deposito_prazo_numero_conta_banco_key").IsUnique();
                entity.Property(e => e.numero_conta_banco).HasMaxLength(50);
                entity.HasOne(d => d.id_bancoNavigation).WithMany(p => p.deposito_prazos)
                    .HasForeignKey(d => d.id_banco)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("deposito_prazo_id_banco_fkey");
            });

            modelBuilder.Entity<fundo_investimento>(entity =>
            {
                entity.HasKey(e => e.id_fundo).HasName("fundo_investimento_pkey");
                entity.ToTable("fundo_investimento");
                entity.Property(e => e.nome).HasMaxLength(100);
            });

            modelBuilder.Entity<imovel_arrendado>(entity =>
            {
                entity.HasKey(e => e.id_imovel).HasName("imovel_arrendado_pkey");
                entity.ToTable("imovel_arrendado");
                entity.Property(e => e.designacao).HasMaxLength(100);
            });

            modelBuilder.Entity<pagamento_imposto>(entity =>
            {
                entity.HasKey(e => e.id_imposto).HasName("pagamento_imposto_pkey");
                entity.ToTable("pagamento_imposto");
                entity.HasOne(d => d.id_ativoNavigation).WithMany(p => p.pagamento_impostos)
                    .HasForeignKey(d => d.id_ativo)
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired()
                    .HasConstraintName("pagamento_imposto_id_ativo_fkey");
            });

            modelBuilder.Entity<taxa_mensal>(entity =>
            {
                entity.HasKey(e => e.id_taxa).HasName("taxa_mensal_pkey");
                entity.ToTable("taxa_mensal");
                entity.HasOne(d => d.id_fundoNavigation).WithMany(p => p.taxa_mensals)
                    .HasForeignKey(d => d.id_fundo)
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired()
                    .HasConstraintName("taxa_mensal_id_fundo_fkey");
            });

            modelBuilder.Entity<utilizador>(entity =>
            {
                entity.HasKey(e => e.id_utilizador).HasName("utilizador_pkey");
                entity.ToTable("utilizador");
                entity.HasIndex(e => e.username, "utilizador_username_key").IsUnique();
                entity.Property(e => e.email).HasMaxLength(255);
                entity.Property(e => e.password).HasMaxLength(255);
                entity.Property(e => e.username).HasMaxLength(100);
            });

            modelBuilder.Entity<utilizador_cliente>(entity =>
            {
                entity.HasKey(e => e.id_cliente).HasName("utilizador_cliente_pkey");
                entity.ToTable("utilizador_cliente");
                entity.HasIndex(e => e.nif, "utilizador_cliente_nif_key").IsUnique();
                entity.Property(e => e.nif).HasMaxLength(20);
                entity.HasOne(d => d.id_utilizadorNavigation).WithMany(p => p.utilizador_clientes)
                    .HasForeignKey(d => d.id_utilizador)
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired()
                    .HasConstraintName("utilizador_cliente_id_utilizador_fkey");
            });

            // Call the partial method if it exists
            OnModelCreatingPartial(modelBuilder);
        }

        // Keep this if you use partial classes for further configuration
        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}