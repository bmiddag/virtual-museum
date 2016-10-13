using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using VirtualMuseumAPI.Models;

namespace VirtualMuseumAPI.Controllers
{
    public class LinkController : Controller
    {
        // GET: Museum
        public ActionResult Museum(int id)
        {
            var dc = new VirtualMuseumDataContext();
            if (dc.Museums.Any(m => m.ID == id))
            {
                var museum = dc.Museums.First(m => m.ID == id);

                return View(museum);
            }
            return HttpNotFound();
        }

        // GET: Filter
        public ActionResult Filter()
        {
            return View();
        }

        // GET: Art
        public ActionResult Art()
        {
            return View();
        }
    }
}
