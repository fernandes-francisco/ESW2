using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ESW2.Entities;

[Table("pagamento_imposto")]
public partial class pagamento_imposto
{
    [Key]
    public int id_imposto { get; set; }

    public int id_ativo { get; set; }

    public DateOnly data_pagamento { get; set; }

    public double valor_pago { get; set; }

    [ForeignKey("id_ativo")]
    [InverseProperty("pagamento_impostos")]
    public virtual ativo_financeiro id_ativoNavigation { get; set; } = null!;
}
