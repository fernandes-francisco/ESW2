using ESW2.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ESW2.Controllers
{
    [Authorize] 
    public class ClienteController : Controller
    {
        private readonly MyDbContext _context;

        public ClienteController(MyDbContext context)
        {
            _context = context;
        }
        
        
        public IActionResult Perfil()
        {
            return View();
        }

        public IActionResult Config()
        {
            return View();
        }
        
        [HttpGet]
        public IActionResult Index()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized("Username not found."); 
            }

            var cliente = _context.utilizador_clientes
                .Include(c => c.id_utilizadorNavigation) 
                .FirstOrDefault(c => c.id_utilizadorNavigation.username == username);

            if (cliente == null)
            {
                 return Unauthorized("Client profile not found.");
            }
            var previewAtivos = _context.ativo_financeiros
                                        .Where(a => a.id_cliente == cliente.id_cliente)
                                        .OrderByDescending(a => a.data_inicio) 
                                        .Take(2) 
                                        .ToList();

            ViewBag.PreviewAtivos = previewAtivos;


            return View(); 
        }

        
    }
}