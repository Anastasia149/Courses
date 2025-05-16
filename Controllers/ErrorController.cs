using Microsoft.AspNetCore.Mvc;

namespace Courses.Controllers
{
    public class ErrorController : Controller
    {
        [Route("Error/404")]
        public IActionResult NotFound404()
        {
            // Можно передать свою модель в представление
            return View("NotFound"); // Ищет Views/Error/NotFound.cshtml
        }

        [Route("Error/{statusCode:int}")]
        public IActionResult Error(int statusCode)
        {
            if (statusCode == 404)
            {
                return NotFound404();
            }

            // Обработка других ошибок (500, 403 и т.д.)
            return View("Error"); // Views/Error/Error.cshtml
        }
    }
}
