using System;
using System.Collections.Generic;

namespace ESW2.Entities;

public partial class utilizador
{
    public int id_utilizador { get; set; }

    public string username { get; set; } = null!;

    public string password { get; set; } = null!;

    public string? email { get; set; }

    public bool is_admin { get; set; }

    public virtual ICollection<utilizador_cliente> utilizador_clientes { get; set; } = new List<utilizador_cliente>();
}
