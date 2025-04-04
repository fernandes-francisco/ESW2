using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ESW2.Context;
using ESW2.Entities;
using System.Linq;

namespace ESW2.Controllers
{
    public class AtivoFinanceiroController : Controller
    {
        private readonly MyDbContext _context;

        public AtivoFinanceiroController(MyDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var ativos = _context.ativo_financeiros
                .Include(a => a.id_clienteNavigation)
                .ToList();

            return View(ativos);
        }

        [HttpGet]
        public IActionResult Manage()
        {
            var ativos = _context.ativo_financeiros.ToList();
            return View("Manage", ativos);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(ativo_financeiro ativo)
        {
            if (ModelState.IsValid)
            {
                _context.ativo_financeiros.Add(ativo);
                _context.SaveChanges();
                return RedirectToAction("Manage");
            }

            return View(ativo);
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var ativo = _context.ativo_financeiros.Find(id);
            if (ativo == null)
                return NotFound();

            return View(ativo);
        }

        [HttpPost]
        public IActionResult Edit(int id, ativo_financeiro updatedAtivo)
        {
            if (ModelState.IsValid)
            {
                var ativo = _context.ativo_financeiros.Find(id);
                if (ativo == null)
                    return NotFound();

                ativo.data_inicio = updatedAtivo.data_inicio;
                ativo.duracao_meses = updatedAtivo.duracao_meses;
                ativo.percentual_imposto = updatedAtivo.percentual_imposto;

                _context.ativo_financeiros.Update(ativo);
                _context.SaveChanges();
                return RedirectToAction("Manage");
            }

            return View(updatedAtivo);
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            var ativo = _context.ativo_financeiros.Find(id);
            if (ativo != null)
            {
                _context.ativo_financeiros.Remove(ativo);
                _context.SaveChanges();
            }

            return RedirectToAction("Manage");
        }
    }
}
