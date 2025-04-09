using System;
using System.Collections.Generic;

namespace ESW2.Entities;

public partial class imovel_arrendado
{
    public int id_imovel { get; set; }

    public string designacao { get; set; } = null!;

    public string localizacao { get; set; } = null!;

    public double valor_imovel { get; set; }

    public double valor_renda { get; set; }

    public double valor_mensal_cond { get; set; }

    public double valor_anual_despesas { get; set; }

    public virtual ICollection<ativo_financeiro> ativo_financeiros { get; set; } = new List<ativo_financeiro>();
}
