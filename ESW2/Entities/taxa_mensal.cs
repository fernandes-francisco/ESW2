using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ESW2.Entities;

[Table("taxa_mensal")]
public partial class taxa_mensal
{
    [Key]
    public int id_taxa { get; set; }

    public int id_fundo { get; set; }

    public DateOnly mes { get; set; }

    public double taxa_juro { get; set; }

    [ForeignKey("id_fundo")]
    [InverseProperty("taxa_mensals")]
    public virtual fundo_investimento id_fundoNavigation { get; set; } = null!;
}
