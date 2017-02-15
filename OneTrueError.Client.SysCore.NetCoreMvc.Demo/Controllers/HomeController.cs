using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace OneTrueError.Client.SysCore.NetCoreMvc.Demo.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public ActionResult SimulatedFailure()
        {
            ViewBag.Title = "Hello";
            ViewBag.Model = new
            {
                state = "Running",
                Collected = true
            };
            throw new InvalidOperationException("SimulatedFailure NetCoreMvc V1");
            return View();
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
