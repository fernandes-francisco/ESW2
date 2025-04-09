using System;
using System.Collections.Generic;

namespace ESW2.Entities;

public partial class banco
{
    public int id_banco { get; set; }

    public string nome_banco { get; set; } = null!;

    public virtual ICollection<deposito_prazo> deposito_prazos { get; set; } = new List<deposito_prazo>();
}
