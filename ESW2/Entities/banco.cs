using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ESW2.Entities;

[Table("banco")]
public partial class banco
{
    [Key]
    public int id_banco { get; set; }

    [StringLength(100)]
    public string nome_banco { get; set; } = null!;

    [InverseProperty("id_bancoNavigation")]
    public virtual ICollection<deposito_prazo> deposito_prazos { get; set; } = new List<deposito_prazo>();
}
