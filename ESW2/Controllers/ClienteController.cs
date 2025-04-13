using ESW2.Context;
using ESW2.Utilities; 
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
            return View();
        }

        public IActionResult Config()
        {
            _logger.Log("Acedendo às configurações do cliente...");
            return View();
        }

        [HttpGet]
        public IActionResult Index()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                _logger.Log("Erro: Username não encontrado para acesso ao Index.");
                return Unauthorized("Username not found.");
            }

            var cliente = _context.utilizador_clientes
                .Include(c => c.id_utilizadorNavigation)
                .FirstOrDefault(c => c.id_utilizadorNavigation.username == username);

            if (cliente == null)
            {
                _logger.Log($"Erro: Perfil do cliente não encontrado para o username {username}.");
                return Unauthorized("Client profile not found.");
            }

            var previewAtivos = _context.ativo_financeiros
                .Where(a => a.id_cliente == cliente.id_cliente)
                .OrderByDescending(a => a.data_inicio)
                .Take(2)
                .ToList();

            ViewBag.PreviewAtivos = previewAtivos;

            _logger.Log($"Index acedido com sucesso para o cliente {username}. Ativos encontrados: {previewAtivos.Count}.");
            return View();
        }

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