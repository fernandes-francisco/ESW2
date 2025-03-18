using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ESW2.Entities;

[Table("utilizador")]
[Index("username", Name = "utilizador_username_key", IsUnique = true)]
public partial class utilizador
{
    [Key]
    public int id_utilizador { get; set; }

    [StringLength(100)]
    public string username { get; set; } = null!;

    [StringLength(255)]
    public string password { get; set; } = null!;

    [InverseProperty("id_utilizadorNavigation")]
    public virtual ICollection<utilizador_cliente> utilizador_clientes { get; set; } = new List<utilizador_cliente>();
}
