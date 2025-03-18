using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ESW2.Controllers
{
    [Authorize] // Apenas usuários logados podem acessar
    public class ClienteController : Controller
    {
        public IActionResult Perfil()
        {
            return View();
        }

        public IActionResult Config()
        {
            return View();
        }
    }
}