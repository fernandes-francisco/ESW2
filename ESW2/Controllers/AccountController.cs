using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using ESW2.Context;
using ESW2.Entities;
using Microsoft.EntityFrameworkCore;

namespace ESW2.Controllers
{
    public class AccountController : Controller
    {
        private readonly MyDbContext _context;
        private const string AdminPassword = "es2"; // Senha fixa para tornar-se admin

        public AccountController(MyDbContext context)
        {
            _context = context;
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
            var user = await _context.utilizadors
                .FirstOrDefaultAsync(u => u.username == username && u.password == password);

            if (user != null)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.username),
                    new Claim(ClaimTypes.Role, user.isAdmin ? "Admin" : "Cliente")
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                if (user.isAdmin)
                {
                    ViewBag.SuccessMessage = "Login de administrador realizado com sucesso!";
                    return RedirectToAction("Dashboard", "Admin");
                }
                else
                {
                    ViewBag.SuccessMessage = "Login de cliente realizado com sucesso!";
                    return RedirectToAction("Index", "Home");
                }
            }

            ViewBag.ErrorMessage = "Credenciais inválidas!";
            return View();
        }

        // GET: /Account/Register
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string username, string password)
        {
            if (ModelState.IsValid)
            {
                // Verifica se o username já existe
                var existingUser = await _context.utilizadors
                    .FirstOrDefaultAsync(u => u.username == username);

                if (existingUser != null)
                {
                    ViewBag.ErrorMessage = "Este nome de usuário já está em uso. Escolha outro.";
                    return View();
                }

                // Criar utilizador (sempre como cliente no início)
                var newUser = new utilizador { username = username, password = password, isAdmin = false };
                _context.utilizadors.Add(newUser);
                await _context.SaveChangesAsync();

                ViewBag.SuccessMessage = "Registo realizado com sucesso! Faça login para continuar.";
                return RedirectToAction("Login");
            }

            ViewBag.ErrorMessage = "Erro ao tentar registrar. Verifique os dados e tente novamente.";
            return View();
        }

        // GET: /Account/UpgradeToAdmin
        public IActionResult UpgradeToAdmin()
        {
            return View();
        }

        // POST: /Account/UpgradeToAdmin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpgradeToAdmin(string username, string adminPassword)
        {
            var user = await _context.utilizadors
                .FirstOrDefaultAsync(u => u.username == username);

            if (user == null)
            {
                ViewBag.ErrorMessage = "Usuário não encontrado!";
                return View();
            }

            if (adminPassword == AdminPassword)
            {
                user.isAdmin = true;
                await _context.SaveChangesAsync();
                ViewBag.SuccessMessage = "Conta atualizada para Administrador com sucesso!";
            }
            else
            {
                ViewBag.ErrorMessage = "Senha de admin incorreta!";
            }

            return View();
        }

        // GET: /Account/Logout
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}
