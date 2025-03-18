using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ESW2.Entities;

[Table("imovel_arrendado")]
public partial class imovel_arrendado
{
    [Key]
    public int id_imovel { get; set; }

    public int id_ativo { get; set; }

    [StringLength(100)]
    public string designacao { get; set; } = null!;

    public string localizacao { get; set; } = null!;

    public double valor_imovel { get; set; }

    public double valor_renda { get; set; }

    public double valor_mensal_cond { get; set; }

    public double valor_anual_despesas { get; set; }

    [ForeignKey("id_ativo")]
    [InverseProperty("imovel_arrendados")]
    public virtual ativo_financeiro id_ativoNavigation { get; set; } = null!;
}
