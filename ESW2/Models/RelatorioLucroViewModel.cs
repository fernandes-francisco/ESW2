using System;
using System.Collections.Generic;
using System.Linq;

namespace ESW2.Models
{
    public class RelatorioLucroViewModel
    {
        public DateTime? DataInicio { get; set; }
        public DateTime? DataFim { get; set; }
        public List<LinhaRelatorioLucro> Linhas { get; set; } = new();

        public decimal LucroTotalBruto => Linhas.Sum(l => l.LucroTotalBruto);
        public decimal LucroTotalLiquido => Linhas.Sum(l => l.LucroTotalLiquido);
        public decimal LucroMensalBruto => Linhas.Any() ? Linhas.Average(l => l.LucroMensalBruto) : 0;
        public decimal LucroMensalLiquido => Linhas.Any() ? Linhas.Average(l => l.LucroMensalLiquido) : 0;
    }

    public class LinhaRelatorioLucro
    {
        public string NomeAtivo { get; set; } = string.Empty;
        public decimal LucroTotalBruto { get; set; }
        public decimal LucroTotalLiquido { get; set; }
        public decimal LucroMensalBruto { get; set; }
        public decimal LucroMensalLiquido { get; set; }
    }
}
