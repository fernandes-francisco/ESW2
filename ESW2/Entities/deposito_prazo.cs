using System;
using System.Collections.Generic;

namespace ESW2.Entities;

public partial class deposito_prazo
{
    public int id_deposito { get; set; }

    public int id_banco { get; set; }

    public double valor_deposito { get; set; }

    public string numero_conta_banco { get; set; } = null!;

    public string titulares { get; set; } = null!;

    public double taxa_juro_anual { get; set; }

    public virtual ICollection<ativo_financeiro> ativo_financeiros { get; set; } = new List<ativo_financeiro>();

    public virtual banco id_bancoNavigation { get; set; } = null!;
}
