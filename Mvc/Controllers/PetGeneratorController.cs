using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Telerik.Sitefinity.Mvc;
using SitefinityWebApp.Mvc.Models;
using SitefinityWebApp.Mvc.Helpers;
using System.Diagnostics;

namespace SitefinityWebApp.Mvc.Controllers
{
    [ControllerToolboxItem(Name = "PetGenerator", Title = "Pet Generator", SectionName = "Cache Demo Widgets")]
    public class PetGeneratorController : Controller
    {
        public ActionResult Index()
        {
            return View("default");
        }

        [HttpPost]
        public ActionResult Generate(int RequestedPets)
        {
            PetHelper helper = new PetHelper();
            helper.ClearAllPets();
            helper.GeneratePets(RequestedPets);
            return View("success");
        }

        protected override void HandleUnknownAction(string actionName)
        {
            this.Index().ExecuteResult(this.ControllerContext);
        }
    }
}