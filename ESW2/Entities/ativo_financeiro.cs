using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ESW2.Entities;

public partial class ativo_financeiro
{
    public int id_ativo { get; set; }

    public int id_cliente { get; set; }

    public int? id_admin { get; set; }

    public DateOnly data_inicio { get; set; }

    public int duracao_meses { get; set; }

    public double percentual_imposto { get; set; }

    public int? id_fundo { get; set; }

    public int? id_imovel { get; set; }

    public int? id_deposito { get; set; }
    [Required]
    // Ensure this uses the C# enum you just created
    public estado_ativo estado { get; set; } = estado_ativo.Ativo; 

    public virtual administrador? id_adminNavigation { get; set; }

    public virtual utilizador_cliente id_clienteNavigation { get; set; } = null!;

    public virtual deposito_prazo? id_depositoNavigation { get; set; }

    public virtual fundo_investimento? id_fundoNavigation { get; set; }

    public virtual imovel_arrendado? id_imovelNavigation { get; set; }

    public virtual ICollection<pagamento_imposto> pagamento_impostos { get; set; } = new List<pagamento_imposto>();
}
