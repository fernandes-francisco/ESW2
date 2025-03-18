using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ESW2.Entities;

[Table("utilizador_cliente")]
[Index("nif", Name = "utilizador_cliente_nif_key", IsUnique = true)]
public partial class utilizador_cliente
{
    [Key]
    public int id_cliente { get; set; }

    public int id_utilizador { get; set; }

    public string? morada { get; set; }

    [StringLength(20)]
    public string? nif { get; set; }

    [InverseProperty("id_clienteNavigation")]
    public virtual ICollection<ativo_financeiro> ativo_financeiros { get; set; } = new List<ativo_financeiro>();

    [ForeignKey("id_utilizador")]
    [InverseProperty("utilizador_clientes")]
    public virtual utilizador id_utilizadorNavigation { get; set; } = null!;
}
