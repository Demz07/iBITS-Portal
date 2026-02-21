using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace iBITS_Portal.Controllers
{
    public class ErrorController : Controller
    {
        private readonly ILogger<ErrorController> _logger;

        public ErrorController(ILogger<ErrorController> logger)
        {
            _logger = logger;
        }

        [Route("Error/{statusCode}")]
        public IActionResult HttpStatusCodeHandler(int statusCode)
        {
            _logger.LogWarning($"HTTP Error {statusCode} occurred");

            switch (statusCode)
            {
                case 404:
                    ViewBag.ErrorMessage = "Page Not Found";
                    ViewBag.ErrorDescription = "The page you are looking for doesn't exist.";
                    break;
                case 500:
                    ViewBag.ErrorMessage = "Internal Server Error";
                    ViewBag.ErrorDescription = "We're experiencing technical difficulties. Please try again later.";
                    break;
                default:
                    ViewBag.ErrorMessage = "Error";
                    ViewBag.ErrorDescription = "An unexpected error occurred.";
                    break;
            }

            ViewBag.StatusCode = statusCode;
            return View("Error");
        }

        [Route("Error")]
        public IActionResult Error()
        {
            var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            _logger.LogError($"Unhandled exception occurred. Request ID: {requestId}");

            ViewBag.ErrorMessage = "Something went wrong";
            ViewBag.ErrorDescription = "We're working to fix the issue. Please try again later.";
            ViewBag.RequestId = requestId;
            
            return View();
        }
    }
}
