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
            ViewBag.DataInicio = dataInicio?.ToString("yyyy-MM-dd");
            ViewBag.DataFim = dataFim?.ToString("yyyy-MM-dd");

            if (dataInicio == null || dataFim == null)
            {
                return View(new List<RelatorioImpostoViewModel>());
            }

            // Converta para UTC para garantir compatibilidade com o tipo do banco
            var inicio = DateTime.SpecifyKind(dataInicio.Value.Date, DateTimeKind.Local).ToUniversalTime();
            var fim = DateTime.SpecifyKind(dataFim.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Local).ToUniversalTime();

            var resultadoBruto = _context.pagamento_impostos
                .Where(p => p.data_pagamento >= inicio && p.data_pagamento <= fim)
                .GroupBy(p => new
                {
                    AtivoId = p.id_ativo,
                    Mes = p.data_pagamento.Month,
                    Ano = p.data_pagamento.Year
                })
                .Select(g => new
                {
                    AtivoId = g.Key.AtivoId,
                    Mes = g.Key.Mes,
                    Ano = g.Key.Ano,
                    TotalImposto = g.Sum(x => x.valor_pago)
                })
                .OrderBy(r => r.AtivoId)
                .ThenBy(r => r.Ano)
                .ThenBy(r => r.Mes)
                .ToList();

            var resultado = resultadoBruto
                .Select(r => new RelatorioImpostoViewModel
                {
                    NomeAtivo = $"Ativo #{r.AtivoId}",
                    Mes = r.Mes,
                    Ano = r.Ano,
                    TotalImposto = r.TotalImposto
                }).ToList();

            return View(resultado);
        }
    }
}