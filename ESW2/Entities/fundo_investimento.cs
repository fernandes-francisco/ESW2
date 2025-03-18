using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ESW2.Entities;

[Table("fundo_investimento")]
public partial class fundo_investimento
{
    [Key]
    public int id_fundo { get; set; }

    public int id_ativo { get; set; }

    [StringLength(100)]
    public string nome { get; set; } = null!;

    public double valor_investido { get; set; }

    public double taxa_juro_padrao { get; set; }

    [ForeignKey("id_ativo")]
    [InverseProperty("fundo_investimentos")]
    public virtual ativo_financeiro id_ativoNavigation { get; set; } = null!;

    [InverseProperty("id_fundoNavigation")]
    public virtual ICollection<taxa_mensal> taxa_mensals { get; set; } = new List<taxa_mensal>();
}
