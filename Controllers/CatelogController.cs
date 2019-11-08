using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace CatalogAPI.Controllers
{
    public class CatelogController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}