using System;
using System.Collections.Generic;

namespace ESW2.Entities;

public partial class taxa_mensal
{
    public int id_taxa { get; set; }

    public int id_fundo { get; set; }

    public DateOnly mes { get; set; }

    public double taxa_juro { get; set; }

    public virtual fundo_investimento id_fundoNavigation { get; set; } = null!;
}
