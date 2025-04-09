using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // Required for [Table] and [ForeignKey]

namespace ESW2.Entities // Ensure this namespace is correct
{
    // Optional: Explicitly map to the table name if needed, though convention usually works
    // [Table("ativo_financeiro")]
    public partial class ativo_financeiro
    {
        [Key] // Explicitly mark the primary key
        [Column("id_ativo")] // Map to the exact column name
        public int id_ativo { get; set; }

        [Column("id_cliente")]
        // This FK itself is required by the database schema (INT NOT NULL)
        // EF Core often infers this, but [Required] can be added for model validation if desired
        // [Required]
        public int id_cliente { get; set; }

        [Column("id_admin")]
        public int? id_admin { get; set; } // Nullable FK

        [Column("data_inicio")]
        [Required] // Date should likely be required
        public DateOnly data_inicio { get; set; }

        [Column("duracao_meses")]
        [Required] // Duration should likely be required
        [Range(1, int.MaxValue, ErrorMessage = "Duração deve ser um número positivo.")] // Add validation
        public int duracao_meses { get; set; }

        [Column("percentual_imposto")]
        [Required] // Tax rate should likely be required
        [Range(0, 100, ErrorMessage = "Percentual de imposto deve estar entre 0 e 100.")] // Add validation
        public double percentual_imposto { get; set; }

        [Column("estado")]
        [Required] // Enum value is required (matches schema NOT NULL DEFAULT)
        public estado_ativo estado { get; set; } // Default is handled by DB or constructor if needed

        [Column("id_fundo")]
        public int? id_fundo { get; set; } // Nullable FK

        [Column("id_imovel")]
        public int? id_imovel { get; set; } // Nullable FK

        [Column("id_deposito")]
        public int? id_deposito { get; set; } // Nullable FK

        // --- Navigation Properties ---
        // Use nullable reference types (?) for navigation properties
        // that might not be loaded, especially during Create/Edit actions.
        // REMOVE [Required] attributes from all navigation properties.

        [ForeignKey("id_admin")] // Links this navigation prop to the id_admin FK prop
        public virtual administrador? id_adminNavigation { get; set; }

        [ForeignKey("id_cliente")] // Links to id_cliente FK prop
        public virtual utilizador_cliente? id_clienteNavigation { get; set; } // Changed to nullable

        [ForeignKey("id_deposito")] // Links to id_deposito FK prop
        public virtual deposito_prazo? id_depositoNavigation { get; set; }

        [ForeignKey("id_fundo")] // Links to id_fundo FK prop
        public virtual fundo_investimento? id_fundoNavigation { get; set; }

        [ForeignKey("id_imovel")] // Links to id_imovel FK prop
        public virtual imovel_arrendado? id_imovelNavigation { get; set; }

        // Collection Navigation Property (One-to-Many)
        // Initialize collections to prevent null reference exceptions.
        public virtual ICollection<pagamento_imposto> pagamento_impostos { get; set; } = new List<pagamento_imposto>();

        // Optional: Constructor to set default values if needed in C#
        // public ativo_financeiro()
        // {
        //     estado = estado_ativo.Ativo; // Default if not set by DB or binding
        //     pagamento_impostos = new List<pagamento_imposto>();
        // }
    }
}