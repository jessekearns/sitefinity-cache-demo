using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SitefinityWebApp.Mvc.Models
{
    public class PetCollectionViewModel
    {
        public string TagName;
        public string TimeInfo;
        public List<PetModel> Pets;

        public PetCollectionViewModel()
        {
            Pets = new List<PetModel>();
        }
    }
}