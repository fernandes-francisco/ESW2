using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ESW2.Context;
using ESW2.Entities;
using ESW2.Models;

namespace ESW2.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly MyDbContext _context;

        public AdminController(MyDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Dashboard()
        {
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Banks()
        {
            var banks = _context.bancos.OrderBy(b => b.nome_banco).ToList();
            return View(banks);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddBank(string nome_banco)
        {
            if (string.IsNullOrWhiteSpace(nome_banco))
            {
                ModelState.AddModelError("nome_banco", "O nome do banco é obrigatório.");
                var banks = _context.bancos.OrderBy(b => b.nome_banco).ToList();
                return View("Banks", banks);
            }

            var banco = new banco { nome_banco = nome_banco.Trim() };
            _context.bancos.Add(banco);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Banks));
        }

        [HttpGet]
        public IActionResult EditBank(int id)
        {
            var banco = _context.bancos.Find(id);
            if (banco == null) return NotFound();
            return View(banco);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBank(int id, string nome_banco)
        {
            var banco = await _context.bancos.FindAsync(id);
            if (banco == null) return NotFound();

            if (string.IsNullOrWhiteSpace(nome_banco))
            {
                ModelState.AddModelError("nome_banco", "O nome do banco não pode ficar vazio.");
                return View(banco);
            }

            banco.nome_banco = nome_banco.Trim();
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Banks));
        }

        [HttpGet]
        public async Task<IActionResult> DeleteBank(int id)
        {
            var banco = await _context.bancos.FindAsync(id);
            if (banco != null)
            {
                _context.bancos.Remove(banco);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Banks));
        }

        [HttpGet]
        public IActionResult Settings()
        {
            var vm = new AdminSettingsViewModel
            {
                DefaultInterestRate = DefaultSettings.CurrentInterestRate,
                DefaultTaxRate = DefaultSettings.CurrentTaxRate
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateSettings(AdminSettingsViewModel vm)
        {
            if (ModelState.IsValid)
            {
                DefaultSettings.CurrentInterestRate = vm.DefaultInterestRate;
                DefaultSettings.CurrentTaxRate = vm.DefaultTaxRate;
                TempData["SuccessMessage"] = "Configurações atualizadas com sucesso!";
                return RedirectToAction(nameof(Settings));
            }
            return View("Settings", vm);
        }

        // Relatório de Depósitos por Banco
        [HttpGet]
        public IActionResult RelatorioBancos()
        {
            var resultado = _context.deposito_prazos
                .GroupBy(d => d.id_banco)
                .Select(g => new 
                {
                    Banco = _context.bancos.FirstOrDefault(b => b.id_banco == g.Key).nome_banco,
                    TotalDeposito = g.Sum(d => d.valor_deposito)
                })
                .ToList();

            return View("RelatorioBancos", resultado);
        }
    }

    public static class DefaultSettings
    {
        public static decimal CurrentInterestRate { get; set; } = 5.0M;
        public static decimal CurrentTaxRate { get; set; } = 15.0M;
    }
}
