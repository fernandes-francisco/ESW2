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

                // Define a mensagem de sucesso para exibir na view
                TempData["SuccessMessage"] = "Banco excluído com sucesso!";
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

        [HttpGet]
        public IActionResult RelatorioBancos(DateTime? dataInicio, DateTime? dataFim)
        {
            // Definir intervalo padrão de 30 dias caso as datas não sejam fornecidas
            if (!dataInicio.HasValue) dataInicio = DateTime.Today.AddDays(-30);
            if (!dataFim.HasValue) dataFim = DateTime.Today;

            // Converter datas para DateOnly para fazer a comparação
            var dataInicioOnly = DateOnly.FromDateTime(dataInicio.Value);
            var dataFimOnly = DateOnly.FromDateTime(dataFim.Value);

            // Consulta considerando a data de início do ativo financeiro associado ao depósito
            var resultado = (from af in _context.ativo_financeiros
                join dp in _context.deposito_prazos on af.id_deposito equals dp.id_deposito
                join b in _context.bancos on dp.id_banco equals b.id_banco
                where af.data_inicio >= dataInicioOnly && af.data_inicio <= dataFimOnly
                group new { dp, b } by new { dp.id_banco, b.nome_banco } into g
                select new
                {
                    Banco = g.Key.nome_banco,
                    TotalDeposito = g.Sum(x => x.dp.valor_deposito),
                    CustoTotalJuros = g.Sum(x => x.dp.valor_deposito * (x.dp.taxa_juro_anual / 100))
                }).ToList();

            // Passar as datas para a View para reutilização
            ViewBag.DataInicio = dataInicio.Value.ToString("yyyy-MM-dd");
            ViewBag.DataFim = dataFim.Value.ToString("yyyy-MM-dd");

            return View("RelatorioBancos", resultado);
        }




    }


    public static class DefaultSettings
    {
        public static decimal CurrentInterestRate { get; set; } = 5.0M;
        public static decimal CurrentTaxRate { get; set; } = 15.0M;
    }
}
