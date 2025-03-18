using System;
using System.Collections.Generic;
using ESW2.Entities;
using Microsoft.EntityFrameworkCore;

namespace ESW2.Context;

public partial class MyDbContext : DbContext
{
    public MyDbContext()
    {
    }

    public MyDbContext(DbContextOptions<MyDbContext> options)
        : base(options)
    {
    }

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

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=Eng_Soft;Username=postgres;Password=postgres");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresEnum("estado_ativo", new[] { "Ativo", "Encerrado", "Em_Periodo" });

        modelBuilder.Entity<administrador>(entity =>
        {
            entity.HasKey(e => e.id_admin).HasName("administrador_pkey");
        });

        modelBuilder.Entity<ativo_financeiro>(entity =>
        {
            entity.HasKey(e => e.id_ativo).HasName("ativo_financeiro_pkey");

            entity.HasOne(d => d.id_adminNavigation).WithMany(p => p.ativo_financeiros)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("ativo_financeiro_id_admin_fkey");

            entity.HasOne(d => d.id_clienteNavigation).WithMany(p => p.ativo_financeiros)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("ativo_financeiro_id_cliente_fkey");
        });

        modelBuilder.Entity<banco>(entity =>
        {
            entity.HasKey(e => e.id_banco).HasName("banco_pkey");
        });

        modelBuilder.Entity<deposito_prazo>(entity =>
        {
            entity.HasKey(e => e.id_deposito).HasName("deposito_prazo_pkey");

            entity.HasOne(d => d.id_ativoNavigation).WithMany(p => p.deposito_prazos)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("deposito_prazo_id_ativo_fkey");

            entity.HasOne(d => d.id_bancoNavigation).WithMany(p => p.deposito_prazos).HasConstraintName("deposito_prazo_id_banco_fkey");
        });

        modelBuilder.Entity<fundo_investimento>(entity =>
        {
            entity.HasKey(e => e.id_fundo).HasName("fundo_investimento_pkey");

            entity.HasOne(d => d.id_ativoNavigation).WithMany(p => p.fundo_investimentos)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fundo_investimento_id_ativo_fkey");
        });

        modelBuilder.Entity<imovel_arrendado>(entity =>
        {
            entity.HasKey(e => e.id_imovel).HasName("imovel_arrendado_pkey");

            entity.HasOne(d => d.id_ativoNavigation).WithMany(p => p.imovel_arrendados)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("imovel_arrendado_id_ativo_fkey");
        });

        modelBuilder.Entity<pagamento_imposto>(entity =>
        {
            entity.HasKey(e => e.id_imposto).HasName("pagamento_imposto_pkey");

            entity.HasOne(d => d.id_ativoNavigation).WithMany(p => p.pagamento_impostos)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("pagamento_imposto_id_ativo_fkey");
        });

        modelBuilder.Entity<taxa_mensal>(entity =>
        {
            entity.HasKey(e => e.id_taxa).HasName("taxa_mensal_pkey");

            entity.HasOne(d => d.id_fundoNavigation).WithMany(p => p.taxa_mensals)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("taxa_mensal_id_fundo_fkey");
        });

        modelBuilder.Entity<utilizador>(entity =>
        {
            entity.HasKey(e => e.id_utilizador).HasName("utilizador_pkey");
        });

        modelBuilder.Entity<utilizador_cliente>(entity =>
        {
            entity.HasKey(e => e.id_cliente).HasName("utilizador_cliente_pkey");

            entity.HasOne(d => d.id_utilizadorNavigation).WithMany(p => p.utilizador_clientes)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("utilizador_cliente_id_utilizador_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
