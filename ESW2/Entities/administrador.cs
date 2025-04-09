using System;
using System.Collections.Generic;

namespace ESW2.Entities;

public partial class administrador
{
    public int id_admin { get; set; }

    public string nome { get; set; } = null!;

    public virtual ICollection<ativo_financeiro> ativo_financeiros { get; set; } = new List<ativo_financeiro>();
}
