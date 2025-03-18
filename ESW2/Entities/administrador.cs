using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ESW2.Entities;

[Table("administrador")]
public partial class administrador
{
    [Key]
    public int id_admin { get; set; }

    [StringLength(100)]
    public string nome { get; set; } = null!;

    [InverseProperty("id_adminNavigation")]
    public virtual ICollection<ativo_financeiro> ativo_financeiros { get; set; } = new List<ativo_financeiro>();
}
