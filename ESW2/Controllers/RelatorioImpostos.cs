using ESW2.Context;
using ESW2.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Collections.Generic;

namespace ESW2.Controllers
{
    public class RelatorioImpostosController : Controller
    {
        private readonly MyDbContext _context;

        public RelatorioImpostosController(MyDbContext context)
        {
            _context = context;
        }

        public IActionResult RelatorioImpostos(DateTime? dataInicio, DateTime? dataFim)
        {
            if (dataInicio == null || dataFim == null)
            {
                return View(new List<RelatorioImpostoViewModel>());
            }

            var resultado = _context.pagamento_impostos
                .Where(p =>
                    p.data_pagamento >= DateTime.SpecifyKind(dataInicio.Value, DateTimeKind.Utc) &&
                    p.data_pagamento <= DateTime.SpecifyKind(dataFim.Value, DateTimeKind.Utc)
                )
                .GroupBy(p => new
                {
                    AtivoId = p.id_ativo,
                    Mes = p.data_pagamento.Month,
                    Ano = p.data_pagamento.Year
                })
                .AsEnumerable() // <<--- Força execução da query no lado do cliente
                .Select(g => new RelatorioImpostoViewModel
                {
                    NomeAtivo = $"Ativo #{g.Key.AtivoId}", // Agora pode usar string interpolation
                    Mes = g.Key.Mes,
                    Ano = g.Key.Ano,
                    TotalImposto = g.Sum(x => x.valor_pago)
                })
                .OrderBy(r => r.NomeAtivo)
                .ThenBy(r => r.Ano)
                .ThenBy(r => r.Mes)
                .ToList();



            return View(resultado);
        }

    }
}