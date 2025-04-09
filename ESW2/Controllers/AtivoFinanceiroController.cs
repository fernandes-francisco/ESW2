using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ESW2.Context;
using ESW2.Entities;
using System.Linq;
using Microsoft.AspNetCore.Authorization; // Needed for [Authorize]
using System.Security.Claims; // Needed for ClaimTypes
using System.Threading.Tasks; // Needed for async Task
using System; // Needed for DateTime, etc.

namespace ESW2.Controllers
{
    [Authorize] // Ensure only logged-in users can access these actions
    public class AtivoFinanceiroController : Controller
    {
        private readonly MyDbContext _context;

        public AtivoFinanceiroController(MyDbContext context)
        {
            _context = context;
        }

        // Helper method to get the client ID for the current user
        private async Task<int?> GetCurrentClienteId()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return null;
            }

            var cliente = await _context.utilizador_clientes
                .AsNoTracking() // No tracking needed just to get the ID
                .Include(c => c.id_utilizadorNavigation)
                .FirstOrDefaultAsync(c => c.id_utilizadorNavigation.username == username);

            return cliente?.id_cliente;
        }

        // GET: AtivoFinanceiro
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var clienteId = await GetCurrentClienteId();
            if (clienteId == null)
            {
                // Should not happen if registration ensures client profile exists and user is authorized
                return Unauthorized("Perfil de cliente não encontrado ou utilizador não autenticado.");
            }

            var ativos = await _context.ativo_financeiros
                .Where(a => a.id_cliente == clienteId.Value)
                // Include details needed for the Index display (adjust as necessary)
                .Include(a => a.id_depositoNavigation).ThenInclude(d => d.id_bancoNavigation) // Example: Include bank name for deposits
                .Include(a => a.id_fundoNavigation)     // Example: Include fund name
                .Include(a => a.id_imovelNavigation)   // Example: Include property designation
                .OrderByDescending(a => a.data_inicio)
                .ToListAsync();

            return View(ativos);
        }

        // GET: AtivoFinanceiro/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var clienteId = await GetCurrentClienteId();
            if (clienteId == null)
            {
                return Unauthorized();
            }

            var ativo_financeiro = await _context.ativo_financeiros
                .Include(a => a.id_clienteNavigation) // Include client details if needed
                .ThenInclude(uc => uc.id_utilizadorNavigation) // And user details
                .Include(a => a.id_depositoNavigation).ThenInclude(d => d.id_bancoNavigation)
                .Include(a => a.id_fundoNavigation)
                .Include(a => a.id_imovelNavigation)
                .Include(a => a.id_adminNavigation) // Include admin details if assigned
                .FirstOrDefaultAsync(m => m.id_ativo == id);

            if (ativo_financeiro == null)
            {
                return NotFound();
            }

            // Ensure the current user owns this asset
            if (ativo_financeiro.id_cliente != clienteId.Value)
            {
                return Unauthorized("Não tem permissão para visualizar este ativo.");
            }

            return View(ativo_financeiro);
        }

        // GET: AtivoFinanceiro/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View(); // Return view Views/AtivoFinanceiro/Create.cshtml
        }

        // POST: AtivoFinanceiro/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("data_inicio,duracao_meses,percentual_imposto,id_fundo,id_imovel,id_deposito,estado")] ativo_financeiro ativo)

        {
            var clienteId = await GetCurrentClienteId();
            if (clienteId == null)
            {
                ModelState.AddModelError("", "Não foi possível identificar o cliente. Faça login novamente.");
                // Repopulate dropdowns if needed: ViewBag.Fundos = ...
                return View(ativo);
            }

            ativo.id_cliente = clienteId.Value;
            if (ativo.estado == default) 
            {
                 ativo.estado = estado_ativo.Ativo; 
            }
            ativo.id_admin = null; 

            int typeCount = (ativo.id_fundo.HasValue ? 1 : 0) +
                            (ativo.id_imovel.HasValue ? 1 : 0) +
                            (ativo.id_deposito.HasValue ? 1 : 0);

            if (typeCount == 0)
            {
                ModelState.AddModelError("", "Deve associar o ativo a um Fundo, Imóvel ou Depósito existente.");
            }
            else if (typeCount > 1)
            {
                ModelState.AddModelError("", "Um ativo financeiro só pode estar associado a um tipo de investimento (Fundo, Imóvel ou Depósito).");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(ativo);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Ativo financeiro criado com sucesso!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException /* ex */)
                {
                    ModelState.AddModelError("", "Não foi possível guardar o ativo. Verifique se os dados associados (Fundo/Imóvel/Depósito) existem e tente novamente.");
                }
            }
            return View(ativo);
        }

        // GET: AtivoFinanceiro/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var clienteId = await GetCurrentClienteId();
            if (clienteId == null)
            {
                return Unauthorized();
            }

            var ativo_financeiro = await _context.ativo_financeiros.FindAsync(id);
            if (ativo_financeiro == null)
            {
                return NotFound();
            }

            if (ativo_financeiro.id_cliente != clienteId.Value)
            {
                return Unauthorized("Não tem permissão para editar este ativo.");
            }

            return View(ativo_financeiro);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("id_ativo,id_cliente,data_inicio,duracao_meses,percentual_imposto,estado,id_fundo,id_imovel,id_deposito,id_admin")] ativo_financeiro ativo)

        {
            if (id != ativo.id_ativo)
            {
                return NotFound();
            }

            var clienteId = await GetCurrentClienteId();
            if (clienteId == null)
            {
                return Unauthorized(); 
            }

            var originalAsset = await _context.ativo_financeiros
                                        .AsNoTracking()
                                        .FirstOrDefaultAsync(a => a.id_ativo == id);

            if (originalAsset == null)
            {
                 return NotFound();
            }

             if (originalAsset.id_cliente != clienteId.Value)
            {
                 return Unauthorized("Não tem permissão para editar este ativo.");
            }

            ativo.id_cliente = clienteId.Value;
            ativo.id_admin = originalAsset.id_admin;
             int typeCount = (ativo.id_fundo.HasValue ? 1 : 0) +
                            (ativo.id_imovel.HasValue ? 1 : 0) +
                            (ativo.id_deposito.HasValue ? 1 : 0);

            if (typeCount == 0)
            {
                ModelState.AddModelError("", "Deve associar o ativo a um Fundo, Imóvel ou Depósito existente.");
            }
            else if (typeCount > 1)
            {
                 ModelState.AddModelError("", "Um ativo financeiro só pode estar associado a um tipo de investimento (Fundo, Imóvel ou Depósito).");
            }


            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(ativo);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Ativo financeiro atualizado com sucesso!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ativo_financeiroExists(ativo.id_ativo))
                    {
                        return NotFound();
                    }
                    else
                    {
                         ModelState.AddModelError("", "Este ativo foi modificado por outro utilizador. Recarregue a página e tente novamente.");

                    }
                }
                 catch (DbUpdateException /* ex */)
                {
                    ModelState.AddModelError("", "Não foi possível guardar as alterações. Verifique os dados e tente novamente.");
                }
                if (ModelState.ErrorCount == 0)
                {
                   return RedirectToAction(nameof(Index));
                }
            }
            return View(ativo);
        }

        // GET: AtivoFinanceiro/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var clienteId = await GetCurrentClienteId();
            if (clienteId == null)
            {
                return Unauthorized();
            }

            var ativo_financeiro = await _context.ativo_financeiros
                .Include(a => a.id_clienteNavigation).ThenInclude(uc => uc.id_utilizadorNavigation) // For display/confirmation
                .Include(a => a.id_depositoNavigation).ThenInclude(d => d.id_bancoNavigation)
                .Include(a => a.id_fundoNavigation)
                .Include(a => a.id_imovelNavigation)
                .FirstOrDefaultAsync(m => m.id_ativo == id);

            if (ativo_financeiro == null)
            {
                return NotFound();
            }
            if (ativo_financeiro.id_cliente != clienteId.Value)
            {
                return Unauthorized("Não tem permissão para eliminar este ativo.");
            }

            return View(ativo_financeiro); 
        }

        // POST: AtivoFinanceiro/Delete/5
        [HttpPost, ActionName("Delete")] 
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
             var clienteId = await GetCurrentClienteId();
            if (clienteId == null)
            {
                return Unauthorized();
            }

            var ativo_financeiro = await _context.ativo_financeiros.FindAsync(id);

            if (ativo_financeiro == null)
            {
                 TempData["InfoMessage"] = "Ativo financeiro não encontrado ou já eliminado.";
                 return RedirectToAction(nameof(Index));
            }
            if (ativo_financeiro.id_cliente != clienteId.Value)
            {
                TempData["ErrorMessage"] = "Não tem permissão para eliminar este ativo.";
                return RedirectToAction(nameof(Index)); 
            }

            try
            {
                _context.ativo_financeiros.Remove(ativo_financeiro);
                await _context.SaveChangesAsync();
                 TempData["SuccessMessage"] = "Ativo financeiro eliminado com sucesso!";
            }
             catch (DbUpdateException /* ex */)
            {
                 TempData["ErrorMessage"] = "Não foi possível eliminar o ativo. Pode estar associado a outros registos (ex: Pagamentos de Imposto).";
   
                 return RedirectToAction(nameof(Delete), new { id = id });
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ativo_financeiroExists(int id)
        {
            return _context.ativo_financeiros.Any(e => e.id_ativo == id);
        }
    }
}