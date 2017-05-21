using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Wodsoft.Document.Hosting.Controllers
{
    public class DocController : Controller
    {
        public async Task<IActionResult> Index(string lang, string path)
        {
            var docProvider = HttpContext.RequestServices.GetRequiredService<IDocumentProvider>();
            await docProvider.LoadAsync();
            var language = docProvider.Languages.SingleOrDefault(t => t.Value == lang);
            if (language == null)
                return NotFound();
            DocumentManager manager = new DocumentManager(docProvider);
            await manager.InitializeAsync();
            var content = manager.GetContent(path, language);
            ViewBag.Pages = docProvider.Pages;
            return View(content);
        }
    }
}
