using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ESW2.Context;
using ESW2.Entities;
using ESW2.Models; // Assuming ResetPasswordViewModel is here
using Microsoft.Extensions.Logging; // Added for logging
using System; // Added for Exception

namespace ESW2.Controllers
{
    public class AccountController : Controller
    {
        private readonly MyDbContext _context;
        private readonly ILogger<AccountController> _logger; // Added logger
        private const string AdminPassword = "es2"; // Fixed password for becoming admin

        // Inject DbContext and Logger
        public AccountController(MyDbContext context, ILogger<AccountController> logger)
        {
            _context = context;
            _logger = logger; // Assign logger
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
                     // Check for returnUrl first if implementing redirection after login
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
            ModelState.AddModelError(string.Empty, "Credenciais inválidas!"); // Use ModelState for error messages
            return View(); // Return the view with the error message
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            // If using a ViewModel for registration, pass it here:
            // return View(new RegisterViewModel());
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        // Consider using a ViewModel like RegisterViewModel for better validation and structure
        public async Task<IActionResult> Register(string username, string password /*, string email, string confirmPassword etc. if using ViewModel */)
        {
            // Basic validation (add more if needed, ideally with a ViewModel)
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                 ModelState.AddModelError("", "Nome de utilizador e senha são obrigatórios.");
                 return View();
            }

            // Check if username already exists
            var existingUser = await _context.utilizadors
                .FirstOrDefaultAsync(u => u.username == username);

            if (existingUser != null)
            {
                ModelState.AddModelError("username", "Este nome de utilizador já está em uso. Escolha outro.");
                return View(); // Return view with model state error
            }

            // --- Transaction to ensure both user and client profile are created ---
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // 1. Create the main user record
                    var newUser = new utilizador
                    {
                        username = username,
                        // --- Security Warning: Plain Text Password ---
                        password = password, // Storing plain text as requested, VERY INSECURE
                        // --- End Security Warning ---
                        is_admin = false
                        // email = model.Email // Add email if collected
                    };
                    _context.utilizadors.Add(newUser);

                    // Save the user to generate the id_utilizador (auto-incremented by DB)
                    await _context.SaveChangesAsync();

                    // --- FIX: Create the associated client profile ---
                    // The newUser.id_utilizador now has the value generated by the database
                    var newClientProfile = new utilizador_cliente
                    {
                        id_utilizador = newUser.id_utilizador, // Link to the user created above
                        morada = null, // Set to null or default initially
                        nif = null     // Set to null or default initially
                        // You could add fields to the registration form for these later
                    };
                    _context.utilizador_clientes.Add(newClientProfile);

                    // Save the client profile
                    await _context.SaveChangesAsync();
                    // --- End Fix ---

                    // If both saves succeeded, commit the transaction
                    await transaction.CommitAsync();
                    _logger.LogInformation("User {Username} registered successfully with client profile.", username);

                    // Use TempData for success message on redirect
                    TempData["SuccessMessage"] = "Registo realizado com sucesso! Faça login para continuar.";
                    return RedirectToAction("Login");
                }
                catch (Exception ex)
                {
                    // If any error occurred, roll back the transaction
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error during registration for user {Username}", username);
                    ModelState.AddModelError("", "Ocorreu um erro inesperado durante o registo. Tente novamente.");
                    return View(); // Return view with error
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

            if (adminPassword == AdminPassword) // Simple check against fixed password
            {
                if (user.is_admin)
                {
                    ViewBag.InfoMessage = "Este utilizador já é um Administrador."; 
                }
                else
                {
                     user.is_admin = true;
                     await _context.SaveChangesAsync();
                     _logger.LogInformation("User {Username} upgraded to Admin.", username);
                     // Use TempData for message on success potentially followed by redirect
                     TempData["SuccessMessage"] = $"Conta '{username}' atualizada para Administrador com sucesso!";
                     // Consider redirecting after success, e.g., back to an admin management page or home
                     // return RedirectToAction("SomeAdminAction", "Admin");
                }
            }
            else
            {
                ModelState.AddModelError("", "Senha de admin incorreta!");
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