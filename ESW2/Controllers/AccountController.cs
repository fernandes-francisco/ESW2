using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ESW2.Context;
using ESW2.Entities;
using ESW2.Models;
using Microsoft.Extensions.Logging;
using System;

namespace ESW2.Controllers
{
    public class AccountController : Controller
    {
        private readonly MyDbContext _context;
        private readonly ILogger<AccountController> _logger;
        private const string AdminPassword = "es2"; // Fixed password for becoming admin

        // Inject DbContext and Logger
        public AccountController(MyDbContext context, ILogger<AccountController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl; // Store return URL if provided
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            // Validate input
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ViewData["ErrorMessage"] = "Por favor, preencha todos os campos.";
                return View();
            }

            // --- Security Warning: Plain Text Password ---
            // In a real application, NEVER compare plain text passwords.
            // Always hash passwords on registration and compare hashes on login.
            var user = await _context.utilizadors
                .FirstOrDefaultAsync(u => u.username == username && u.password == password);
            // --- End Security Warning ---

            if (user != null)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.username), // Stores username
                    new Claim(ClaimTypes.NameIdentifier, user.id_utilizador.ToString()), // Stores user ID
                    new Claim(ClaimTypes.Role, user.is_admin ? "Admin" : "Cliente") // Stores Role
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                // Sign the user in
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal,
                    new AuthenticationProperties { IsPersistent = false }); // IsPersistent=false means session cookie

                _logger.LogInformation("User {Username} logged in successfully.", username);

                // Redirect based on role
                if (user.is_admin)
                {
                    // Redirect Admin to their dashboard (ensure AdminController/Dashboard exists)
                    return RedirectToAction("Dashboard", "Admin");
                }
                else
                {
                    // Redirect non-admin (Cliente) to the Cliente area (ensure ClienteController/Index exists)
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }
                    else
                    {
                        return RedirectToAction("Index", "Cliente");
                    }
                }
            }

            // If login fails
            _logger.LogWarning("Failed login attempt for username: {Username}", username);
            ViewData["ErrorMessage"] = "Nome de utilizador ou palavra-passe incorretos.";
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
        public async Task<IActionResult> Register(string username, string email, string password, string nif, string morada)
        {
            // 1. Basic validation
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ViewData["ErrorMessage"] = "Nome de utilizador, email e senha são obrigatórios.";
                return View();
            }

            if (string.IsNullOrWhiteSpace(nif) || !System.Text.RegularExpressions.Regex.IsMatch(nif, @"^\d{9}$"))
            {
                ViewData["ErrorMessage"] = "O NIF é obrigatório e deve ter exatamente 9 dígitos numéricos.";
                return View();
            }

            if (string.IsNullOrWhiteSpace(morada) || morada.Length > 200)
            {
                ViewData["ErrorMessage"] = "A morada é obrigatória e não pode ter mais de 200 caracteres.";
                return View();
            }

            // 2. Check for existing user
            var existingUser = await _context.utilizadors
                .FirstOrDefaultAsync(u => u.username == username);
            if (existingUser != null)
            {
                ViewData["ErrorMessage"] = "Este nome de utilizador já está em uso.";
                return View();
            }

            // 3. Wrap in a transaction to insert both tables
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // Create the user
                    var newUser = new utilizador
                    {
                        username = username,
                        email = email,
                        password = password, // WARNING: plaintext!
                        is_admin = false
                    };
                    _context.utilizadors.Add(newUser);
                    await _context.SaveChangesAsync();

                    // Create the linked client profile
                    var newClientProfile = new utilizador_cliente
                    {
                        id_utilizador = newUser.id_utilizador,
                        morada = morada,
                        nif = nif
                    };
                    _context.utilizador_clientes.Add(newClientProfile);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();
                    TempData["SuccessMessage"] = "Registo realizado com sucesso! Faça login para continuar.";
                    return RedirectToAction("Login");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Erro no registo para {Username}", username);
                    ViewData["ErrorMessage"] = "Ocorreu um erro inesperado. Tente novamente.";
                    return View();
                }
            }
        }

        // GET: /Account/Logout
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            _logger.LogInformation("User logged out.");
            return RedirectToAction("Login"); // Redirect to Login page after logout
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
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(adminPassword))
            {
                ModelState.AddModelError("", "Nome de utilizador e senha de admin são obrigatórios.");
                return View();
            }

            var user = await _context.utilizadors
                .FirstOrDefaultAsync(u => u.username == username);

            if (user == null)
            {
                ModelState.AddModelError("", "Utilizador não encontrado!");
                return View();
            }

            if (adminPassword != AdminPassword)
            {
                ModelState.AddModelError("", "Senha de admin incorreta!");
                return View();
            }

            if (user.is_admin)
            {
                ViewBag.InfoMessage = "Este utilizador já é um Administrador.";
                return View();
            }

            // Promote to admin
            user.is_admin = true;

            // Create a corresponding administrador record
            var novoAdmin = new administrador
            {
                nome = user.username
                // You could set other fields here if needed
            };
            _context.administradors.Add(novoAdmin);

            await _context.SaveChangesAsync();

            _logger.LogInformation("User {Username} upgraded to Admin and administrador record created.", username);

            TempData["SuccessMessage"] = $"Conta '{username}' atualizada para Administrador com sucesso!";
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
        [ValidateAntiForgeryToken]
        public IActionResult ForgotPassword(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError("email", "O endereço de email é obrigatório.");
                return View();
            }
            return RedirectToAction("Login");
        }

        // GET: /Account/ResetPassword
        [HttpGet]
        public IActionResult ResetPassword(string email, string token) // Token is simulated here
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
            {
                // In a real app, validate the token properly
                TempData["ErrorMessage"] = "Link de redefinição inválido ou expirado.";
                return RedirectToAction("Login");
            }
            var model = new ResetPasswordViewModel { Email = email, Token = token };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _context.utilizadors.FirstOrDefaultAsync(u => u.email == model.Email);

            if (user == null)
            {
                return RedirectToAction("Login");
            }

            user.password = model.NovaSenha;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Password reset successfully for user associated with email: {Email}", model.Email);

            TempData["SuccessMessage"] = "Senha redefinida com sucesso! Pode agora fazer login com a nova senha.";
            return RedirectToAction("Login");
        }
    }
}