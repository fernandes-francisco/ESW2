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

        // Alias Dashboard for login redirect
        [HttpGet]
        public IActionResult Dashboard()
        {
            return RedirectToAction(nameof(Index));
        }

        // Main Dashboard
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        // List and Add Banks
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

        // Settings GET
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

        // Settings POST
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
    }

    // Simple in-memory settings store
    public static class DefaultSettings
    {
        public static decimal CurrentInterestRate { get; set; } = 5.0M;
        public static decimal CurrentTaxRate { get; set; } = 15.0M; // percentage
    }
}
