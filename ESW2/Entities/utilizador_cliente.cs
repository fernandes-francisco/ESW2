using System;
using System.Collections.Generic;

namespace ESW2.Entities;

public partial class utilizador_cliente
{
    public int id_cliente { get; set; }

    public int id_utilizador { get; set; }

    public string? morada { get; set; }

    public string? nif { get; set; }

    public virtual ICollection<ativo_financeiro> ativo_financeiros { get; set; } = new List<ativo_financeiro>();

    public virtual utilizador id_utilizadorNavigation { get; set; } = null!;
}
