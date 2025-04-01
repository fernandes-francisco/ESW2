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
            var username = User.Identity?.Name;

            var cliente = _context.utilizador_clientes
                .Include(c => c.id_utilizadorNavigation)
                .FirstOrDefault(c => c.id_utilizadorNavigation.username == username);

            if (cliente == null)
                return Unauthorized();

            var ativos = _context.ativo_financeiros
                .Where(a => a.id_cliente == cliente.id_cliente)
                .ToList();

            return View(ativos);
        }

    }
}