using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ESW2.Entities;

[Table("deposito_prazo")]
[Index("numero_conta_banco", Name = "deposito_prazo_numero_conta_banco_key", IsUnique = true)]
public partial class deposito_prazo
{
    [Key]
    public int id_deposito { get; set; }

    public int id_ativo { get; set; }

    public int id_banco { get; set; }

    public double valor_deposito { get; set; }

    [StringLength(50)]
    public string numero_conta_banco { get; set; } = null!;

    public string titulares { get; set; } = null!;

    public double taxa_juro_anual { get; set; }

    [ForeignKey("id_ativo")]
    [InverseProperty("deposito_prazos")]
    public virtual ativo_financeiro id_ativoNavigation { get; set; } = null!;

    [ForeignKey("id_banco")]
    [InverseProperty("deposito_prazos")]
    public virtual banco id_bancoNavigation { get; set; } = null!;
}
