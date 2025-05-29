namespace ESW2.Models;
public class RelatorioImpostosViewModel
{
    public DateTime DataInicio { get; set; }
    public DateTime DataFim { get; set; }
    public List<ImpostoMensalAtivo> Itens { get; set; } = new();
}

public class ImpostoMensalAtivo
{
    public string NomeAtivo { get; set; }
    public int Ano { get; set; }
    public int Mes { get; set; }
    public decimal ValorTotalImposto { get; set; }
}
