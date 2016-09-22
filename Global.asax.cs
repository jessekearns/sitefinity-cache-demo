using SitefinityWebApp.Mvc.Helpers;
using System;
using System.Web.Mvc;
using Telerik.Sitefinity.Abstractions;
using Telerik.Sitefinity.DynamicModules;
using Telerik.Sitefinity.Mvc;
using Telerik.Sitefinity.Utilities.TypeConverters;

namespace SitefinityWebApp
{
    public class Global : System.Web.HttpApplication
    {

        protected void Application_Start(object sender, EventArgs e)
        {
            Bootstrapper.Bootstrapped += Bootstrapper_Bootstrapped;
            Telerik.Sitefinity.Abstractions.Bootstrapper.Initialized += new EventHandler<Telerik.Sitefinity.Data.ExecutedEventArgs>(Bootstrapper_Initialized);
        }

        void Bootstrapper_Initialized(object sender, Telerik.Sitefinity.Data.ExecutedEventArgs e)
        {
            if (e.CommandName == "Bootstrapped")
            {
                DynamicModuleManager.Executing += new EventHandler<Telerik.Sitefinity.Data.ExecutingEventArgs>(OnPet_Updated);
            }
        }

        void Bootstrapper_Bootstrapped(object sender, EventArgs e)
        {
            // Bind interfaces.
            var factory = ObjectFactory.Resolve<ISitefinityControllerFactory>();
            ControllerBuilder.Current.SetControllerFactory(factory);
        }

        private void OnPet_Updated(object sender, Telerik.Sitefinity.Data.ExecutingEventArgs e)
        {
            if (e.CommandName == "CommitTransaction" || e.CommandName == "FlushTransaction")
            {
                var provider = sender as DynamicModuleDataProvider;
                var dirtyItems = provider.GetDirtyItems();

                if (dirtyItems.Count != 0)
                {
                    PetHelper petHelper = new PetHelper();
                    foreach (var item in dirtyItems)
                    {
                        if (item.GetType() == TypeResolutionService.ResolveType(petHelper.PetTypeString))
                        {
                            petHelper.FlushCache();
                        }
                    }
                }
            }
        }

        protected void Session_Start(object sender, EventArgs e)
        {

        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {

        }

        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {

        }

        protected void Application_Error(object sender, EventArgs e)
        {

        }

        protected void Session_End(object sender, EventArgs e)
        {

        }

        protected void Application_End(object sender, EventArgs e)
        {

        }
    }
}