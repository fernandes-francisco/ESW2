using System;
using System.Collections.Generic;

namespace ESW2.Entities;

public partial class fundo_investimento
{
    public int id_fundo { get; set; }

    public string nome { get; set; } = null!;

    public double valor_investido { get; set; }

    public double taxa_juro_padrao { get; set; }

    public virtual ICollection<ativo_financeiro> ativo_financeiros { get; set; } = new List<ativo_financeiro>();

    public virtual ICollection<taxa_mensal> taxa_mensals { get; set; } = new List<taxa_mensal>();
}
