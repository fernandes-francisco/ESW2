using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ESW2.Context;
using ESW2.Entities;
using Microsoft.EntityFrameworkCore;

namespace ESW2.Controllers
{
    public class AccountController : Controller
    {
        private readonly MyDbContext _context;

        public AccountController(MyDbContext context)
        {
            _context = context;
        }

        // GET: /Account/Register
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(utilizador user)
        {
            if (ModelState.IsValid)
            {
                // Adiciona o novo utilizador à base de dados
                _context.utilizadors.Add(user);
                await _context.SaveChangesAsync();
                // Redireciona para a página de login após o registo
                return RedirectToAction("Login");
            }
            return View(user);
        }

        // GET: /Account/Login
        public IActionResult Login()
        {
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password)
        {
            // Validação simples das credenciais (em produção, utilize um sistema mais robusto)
            var user = await _context.utilizadors
                .FirstOrDefaultAsync(u => u.username == username && u.password == password);
            if (user != null)
            {
                // Aqui pode ser implementada a lógica de autenticação (ex: criação de cookie ou sessão)
                return RedirectToAction("Index", "Home");
            }
            ModelState.AddModelError("", "Credenciais inválidas. Tente novamente.");
            return View();
        }
    }
}