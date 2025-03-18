using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ESW2.Entities;

[Table("ativo_financeiro")]
public partial class ativo_financeiro
{
    [Key]
    public int id_ativo { get; set; }

    public int id_cliente { get; set; }

    public int? id_admin { get; set; }

    public DateOnly data_inicio { get; set; }

    public int duracao_meses { get; set; }

    public double percentual_imposto { get; set; }

    [InverseProperty("id_ativoNavigation")]
    public virtual ICollection<deposito_prazo> deposito_prazos { get; set; } = new List<deposito_prazo>();

    [InverseProperty("id_ativoNavigation")]
    public virtual ICollection<fundo_investimento> fundo_investimentos { get; set; } = new List<fundo_investimento>();

    [ForeignKey("id_admin")]
    [InverseProperty("ativo_financeiros")]
    public virtual administrador? id_adminNavigation { get; set; }

    [ForeignKey("id_cliente")]
    [InverseProperty("ativo_financeiros")]
    public virtual utilizador_cliente id_clienteNavigation { get; set; } = null!;

    [InverseProperty("id_ativoNavigation")]
    public virtual ICollection<imovel_arrendado> imovel_arrendados { get; set; } = new List<imovel_arrendado>();

    [InverseProperty("id_ativoNavigation")]
    public virtual ICollection<pagamento_imposto> pagamento_impostos { get; set; } = new List<pagamento_imposto>();
}
