using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ESW2.Context;
using ESW2.Entities;
using ESW2.Models;

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
        [HttpGet]
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
        [HttpGet]
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
                var existingUser = await _context.utilizadors
                    .FirstOrDefaultAsync(u => u.username == username);

                if (existingUser != null)
                {
                    ViewBag.ErrorMessage = "Este nome de utilizador já está em uso. Escolha outro.";
                    return View();
                }

                var newUser = new utilizador { username = username, password = password, isAdmin = false };
                _context.utilizadors.Add(newUser);
                await _context.SaveChangesAsync();

                ViewBag.SuccessMessage = "Registo realizado com sucesso! Faça login para continuar.";
                return RedirectToAction("Login");
            }

            ViewBag.ErrorMessage = "Erro ao tentar registar. Verifique os dados e tente novamente.";
            return View();
        }

        // GET: /Account/UpgradeToAdmin
        [HttpGet]
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
                ViewBag.ErrorMessage = "Utilizador não encontrado!";
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

        // GET: /Account/ForgotPassword
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: /Account/ForgotPassword
        [HttpPost]
        public IActionResult ForgotPassword(string email)
        {
            // Simulação de envio de email
            TempData["Message"] = "Se o email existir, será enviado um link de redefinição.";
            return RedirectToAction("Login");
        }

        // GET: /Account/ResetPassword
        [HttpGet]
        public IActionResult ResetPassword(string email, string token)
        {
            var model = new ResetPasswordViewModel
            {
                Email = email,
                Token = token
            };

            return View(model);
        }

        // POST: /Account/ResetPassword
        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Procurar utilizador pelo email
            var user = await _context.utilizadors
                .FirstOrDefaultAsync(u => u.username == model.Email); // Se usares outro campo tipo "email", troca aqui

            if (user == null)
            {
                ModelState.AddModelError("", "Utilizador não encontrado.");
                return View(model);
            }

            // Atualizar a senha (⚠️ aqui não há hashing!)
            user.password = model.NovaSenha;
            await _context.SaveChangesAsync();

            TempData["Message"] = "Senha redefinida com sucesso!";
            return RedirectToAction("Login");
        }

        // GET: /Account/Logout
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}
