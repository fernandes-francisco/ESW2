using System;
using System.Collections.Generic;

namespace ESW2.Entities;

public partial class pagamento_imposto
{
    public int id_imposto { get; set; }

    public int id_ativo { get; set; }

    public DateTime data_pagamento { get; set; }

    public double valor_pago { get; set; }

    public virtual ativo_financeiro id_ativoNavigation { get; set; } = null!;
}
