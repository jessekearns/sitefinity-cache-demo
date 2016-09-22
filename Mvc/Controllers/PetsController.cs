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
    [ControllerToolboxItem(Name = "Pets", Title = "Pets Listing", SectionName = "Cache Demo Widgets")]
    public class PetsController : Controller
    {
        public bool UseCache { get; set; }

        public ActionResult Index(string tagName = "")
        {
            PetHelper helper = new PetHelper();
            PetCollectionViewModel model = new PetCollectionViewModel();
            model.TagName = tagName;

            // Time model data retrieval
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            model.Pets = helper.GetPets(UseCache, model.TagName);
            stopwatch.Stop();
            model.TimeInfo = FormatTimeElapsed(stopwatch.Elapsed, UseCache);
            return View("default", model);
        }

        private string FormatTimeElapsed(TimeSpan elapsed, bool useCache)
        {
            string cacheInfoString = useCache ? "Using cache," : "Without using cache,";
            return String.Format("{0} retrieving pets took {1}", cacheInfoString, elapsed);
        }

        protected override void HandleUnknownAction(string actionName)
        {
            this.Index().ExecuteResult(this.ControllerContext);
        }
    }
}