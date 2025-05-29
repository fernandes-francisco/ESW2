using ESW2.Context;
using ESW2.Utilities; 
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Threading.Tasks;
using System.Linq;

namespace ESW2.Controllers
{
    [Authorize]
    public class ClienteController : Controller
    {
        private readonly MyDbContext _context;
        private readonly Logger _logger = Logger.GetInstance();

        public ClienteController(MyDbContext context)
        {
            _context = context;
        }

        public IActionResult Perfil()
        {
            _logger.Log("Acedendo ao perfil do cliente...");

            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                _logger.Log("Erro: Username não encontrado para acesso ao Perfil.");
                return Unauthorized("Username not found.");
            }

            var cliente = _context.utilizador_clientes
                .Include(c => c.id_utilizadorNavigation)
                .FirstOrDefault(c => c.id_utilizadorNavigation.username == username);

            int totalAtivos = 0;
            if (cliente != null)
                totalAtivos = _context.ativo_financeiros.Count(a => a.id_cliente == cliente.id_cliente);

            ViewBag.Cliente = cliente;
            ViewBag.TotalAtivos = totalAtivos;

            return View();
        }

        [HttpPost]
        public IActionResult EditarPerfil(string Nif, string Morada, string Email)
        {
            var username = User.Identity?.Name;
            var cliente = _context.utilizador_clientes
                .Include(c => c.id_utilizadorNavigation)
                .FirstOrDefault(c => c.id_utilizadorNavigation.username == username);

            if (cliente != null)
            {
                cliente.nif = Nif;
                cliente.morada = Morada;
                if (!string.IsNullOrWhiteSpace(Email))
                {
                    cliente.id_utilizadorNavigation.email = Email;
                }
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Perfil atualizado com sucesso!";
            }
            else
            {
                TempData["ErrorMessage"] = "Erro ao atualizar perfil.";
            }
            return RedirectToAction("Perfil");
        }

        [HttpPost]
        public IActionResult AlterarSenha(string SenhaAtual, string NovaSenha, string ConfirmarNovaSenha)
        {
            var username = User.Identity?.Name;
            var utilizador = _context.utilizadors.FirstOrDefault(u => u.username == username);

            if (NovaSenha != ConfirmarNovaSenha)
            {
                TempData["ErrorMessage"] = "As palavras-passe não coincidem.";
                return RedirectToAction("Perfil");
            }

            if (utilizador == null)
            {
                TempData["ErrorMessage"] = "Utilizador não encontrado.";
                return RedirectToAction("Perfil");
            }

            // Exemplo simples, ajuste conforme sua autenticação real!
            if (utilizador.password != SenhaAtual)
            {
                TempData["ErrorMessage"] = "Palavra-passe atual incorreta.";
                return RedirectToAction("Perfil");
            }

            utilizador.password = NovaSenha;
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Palavra-passe alterada com sucesso!";
            return RedirectToAction("Perfil");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarPerfil()
        {
            var username = User.Identity?.Name;
            var cliente = await _context.utilizador_clientes
                .Include(c => c.id_utilizadorNavigation)
                .FirstOrDefaultAsync(c => c.id_utilizadorNavigation.username == username);

            if (cliente != null)
            {
                // Apaga todos os ativos financeiros do cliente
                var ativos = _context.ativo_financeiros.Where(a => a.id_cliente == cliente.id_cliente);
                _context.ativo_financeiros.RemoveRange(ativos);

                // Apaga o perfil de cliente
                _context.utilizador_clientes.Remove(cliente);

                // Apaga o utilizador
                var utilizador = cliente.id_utilizadorNavigation;
                _context.utilizadors.Remove(utilizador);

                await _context.SaveChangesAsync();

                // Termina a sessão do utilizador
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                TempData["SuccessMessage"] = "Perfil eliminado com sucesso!";
                return RedirectToAction("Login", "Account");
            }

            TempData["ErrorMessage"] = "Ocorreu um erro ao eliminar o perfil.";
            return RedirectToAction("Perfil");
        }

        public IActionResult Config()
        {
            _logger.Log("Acedendo às configurações do cliente...");
            return View();
        }

        // Index REMOVIDO! (Painel do Cliente não existe mais)

        // Método para testar o Singleton
        public IActionResult TestLogger()
        {
            Logger logger1 = Logger.GetInstance();
            Logger logger2 = Logger.GetInstance();
            var isSameInstance = logger1 == logger2;
            _logger.Log($"Teste Singleton: logger1 == logger2? {isSameInstance}");
            return Content($"Logger instances are the same: {isSameInstance}");
        }
    }
}
