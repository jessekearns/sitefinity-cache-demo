using SitefinityWebApp.Mvc.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Telerik.Microsoft.Practices.EnterpriseLibrary.Caching;
using Telerik.Microsoft.Practices.EnterpriseLibrary.Caching.Expirations;
using Telerik.Microsoft.Practices.EnterpriseLibrary.Logging;
using Telerik.OpenAccess;
using Telerik.Sitefinity.Data;
using Telerik.Sitefinity.DynamicModules;
using Telerik.Sitefinity.DynamicModules.Model;
using Telerik.Sitefinity.GenericContent.Model;
using Telerik.Sitefinity.Libraries.Model;
using Telerik.Sitefinity.Lifecycle;
using Telerik.Sitefinity.Model;
using Telerik.Sitefinity.Modules.Libraries;
using Telerik.Sitefinity.RelatedData;
using Telerik.Sitefinity.Services;
using Telerik.Sitefinity.Taxonomies;
using Telerik.Sitefinity.Taxonomies.Model;
using Telerik.Sitefinity.Utilities.TypeConverters;

namespace SitefinityWebApp.Mvc.Helpers
{
    public class PetHelper
    {
        #region Constants
        // Cache length 24 hours
        double CacheMinutes = 1440;
        public string PetTypeString = "Telerik.Sitefinity.DynamicTypes.Model.Pets.Pet";
        string TaxonomyName = "Tags";
        static readonly object _lockObject = new object();

        private TaxonomyManager _taxonomyManager;
        private LibrariesManager _librariesManager;
        private DynamicModuleManager _dynamicModuleManager;
        private ICacheManager _cacheManager;

        TaxonomyManager taxonomyManager
        {
            get
            {
                if (_taxonomyManager == null)
                {
                    _taxonomyManager = TaxonomyManager.GetManager();
                }
                return _taxonomyManager;
            }
        }
        LibrariesManager librariesManager
        {
            get
            {
                if (_librariesManager == null)
                {
                    _librariesManager = LibrariesManager.GetManager();
                }
                return _librariesManager;
            }
        }
        DynamicModuleManager dynamicModuleManager
        {
            get
            {
                if (_dynamicModuleManager == null)
                {
                    _dynamicModuleManager = DynamicModuleManager.GetManager();
                }
                return _dynamicModuleManager;
            }
        }
        ICacheManager cacheManager
        {
            get
            {
                if (_cacheManager == null)
                {
                    _cacheManager = SystemManager.GetCacheManager(CacheManagerInstance.Global);
                }
                return _cacheManager;
            }
        }
        #endregion

        #region Public Helper Methods
        public List<PetModel> GetPets(bool useCache = false, string tag = "")
        {
            List<PetModel> list;
            if (useCache)
            {
                list = RetrieveCachedPets(tag);
            }
            else
            {
                list = QueryPets(tag);
            }
            return list;
        }

        public void FlushCache()
        {
            IList<Taxon> tags = GetTags();
            foreach (Taxon tag in tags)
            {
                cacheManager.Remove(GetPetKey(tag.UrlName));
            }
            // Handle the default case
            cacheManager.Remove(GetPetKey(String.Empty));
        }

        public void ClearAllPets()
        {
            dynamicModuleManager.Provider.SuppressSecurityChecks = true;
            Type petType = TypeResolutionService.ResolveType(PetTypeString);
            var masterPets = dynamicModuleManager.GetDataItems(petType).Where(p => p.Status == ContentLifecycleStatus.Master);
            foreach (DynamicContent pet in masterPets)
            {
                dynamicModuleManager.DeleteDataItem(pet);
            }
            dynamicModuleManager.SaveChanges();
        }

        public void GeneratePets(int amount)
        {
            Type petType = TypeResolutionService.ResolveType(PetTypeString);
            IList<Taxon> tags = GetTags();
            List<Guid> tagIDs = new List<Guid>();
            foreach (Taxon tag in tags)
            {
                tagIDs.Add(tag.Id);
            }
            List<Image> images = GetImages();
            Random rnd = new Random();
            for (int i = 0; i < amount; i++)
            {
                // Generate name (pet#), select a random tag and a random image, and generate the item
                string name = "pet" + (i + 1);
                int taxonIndex = rnd.Next(tagIDs.Count);
                Guid tagID = tagIDs[taxonIndex];
                int imageIndex = rnd.Next(images.Count);
                Image image = images[imageIndex];
                try
                {
                    GeneratePet(petType, name, tagID, image);
                }
                catch (Exception ex)
                {
                    Logger.Writer.Write(String.Format("Pet with name {0} could not be added: {1}", name, ex.Message));
                }
            }
        }
        #endregion

        #region Model Retrieval
        private List<PetModel> RetrieveCachedPets(string tag)
        {
            // Attempt to retrieve the collection of cached items with this tag
            string cacheKey = GetPetKey(tag);
            List<PetModel> list = (List<PetModel>)cacheManager[cacheKey];

            // If the cache for this key is populated, return - if null, query as normal for these items and save them to the cache
            if (list != null)
            {
                return list;
            }
            // Prevent multiple users from entering this region - the second user will be blocked until the first has finished populating the cache
            lock (_lockObject)
            {
                // Redundant call here to avoid unnecessary execution
                // Ex: if User 1 enters the locked region and User 2 is blocked after finding the cache empty,
                // User 1 will cause the cache to be populated before User 2 enters the locked region.
                // This redundant check prevents User 2 from unneccessarily populating the cache a second time.
                list = (List<PetModel>)cacheManager[cacheKey];
                if (list != null)
                {
                    return list;
                }
                // Query for pet items as normal
                list = QueryPets(tag);
                // Cache the newly queried items
                CachePets(cacheKey, list);
            }
            return list;
        }

        private List<PetModel> QueryPets(string tag)
        {
            List<DynamicContent> dynItems = QueryPetContent(tag);
            List<PetModel> models = new List<PetModel>();
            foreach (DynamicContent dynItem in dynItems)
            {
                PetModel model = CreateModel(dynItem);
                models.Add(model);
            }
            return models;
        }
        #endregion

        #region Data Access
        private Taxon GetTag(string tagName)
        {
            FlatTaxonomy category = taxonomyManager.GetTaxonomies<FlatTaxonomy>().Where(c => c.Name == TaxonomyName).FirstOrDefault();
            Taxon tag = category.Taxa.Where(t => t.UrlName == tagName).FirstOrDefault();
            return tag;
        }

        private IList<Taxon> GetTags()
        {
            FlatTaxonomy category = taxonomyManager.GetTaxonomies<FlatTaxonomy>().Where(c => c.Name == TaxonomyName).FirstOrDefault();
            IList<Taxon> tags = category.Taxa;
            return tags;
        }

        private List<Image> GetImages()
        {
            List<Image> images = librariesManager.GetImages().Where(i => i.Status == Telerik.Sitefinity.GenericContent.Model.ContentLifecycleStatus.Live).ToList();
            return images;
        }

        private List<DynamicContent> QueryPetContent(string tag)
        {
            // Resolve tag to an item - if we can't and it's not the tag-less case, return an empty list
            Taxon TagItem = null;
            if (!String.IsNullOrEmpty(tag))
            {
                TagItem = GetTag(tag);
                if (TagItem == null)
                {
                    return new List<DynamicContent>();
                }
            }
            
            Type petType = TypeResolutionService.ResolveType(PetTypeString);

            // Get all live pet items
            IQueryable<DynamicContent> results = dynamicModuleManager.GetDataItems(petType).Where(p => p.Status == ContentLifecycleStatus.Live);
            // If we have a valid tag, filter by that tag
            if (TagItem != null)
            {
                results = results.Where(p => p.GetValue<TrackedList<Guid>>("Tags").Contains(TagItem.Id));
            }
            return results.ToList();
        }

        private void GeneratePet(Type petType, string name, Guid tagID, Image image)
        {
            dynamicModuleManager.Provider.SuppressSecurityChecks = true;
 	        DynamicContent petItem = dynamicModuleManager.CreateDataItem(petType);
            petItem.SetValue("Name", name);
            petItem.UrlName = name;
            petItem.Organizer.AddTaxa("Tags", tagID);
            petItem.CreateRelation(image, "Image");
            dynamicModuleManager.Lifecycle.Publish(petItem);
            dynamicModuleManager.SaveChanges();
        }

        private PetModel CreateModel(DynamicContent dynItem)
        {
            PetModel model = new PetModel();
            model.Name = dynItem.GetValue("Name").ToString();

            string imgUrl = "";
            Image imageField = dynItem.GetRelatedItems<Image>("Image").FirstOrDefault();
            if (imageField != null)
            {
                imgUrl = imageField.Url;
            }
            model.ImageUrl = imgUrl;
            model.Id = dynItem.Id;
            return model;
        }
        #endregion

        #region Caching Logic
        private void CachePets(string cacheKey, List<PetModel> list)
        {
            List<ICacheItemExpiration> cacheParams = new List<ICacheItemExpiration>();
            List<DataItemCacheDependency> dependencies = new List<DataItemCacheDependency>();

            // Add dependency on each pet item
            foreach (PetModel pet in list)
            {
                if (pet != null && pet.Id != Guid.Empty)
                {
                    DataItemCacheDependency dependency
                        = new DataItemCacheDependency(typeof(DynamicContent), pet.Id);
                    dependencies.Add(dependency);
                }
            }

            if (dependencies.Count > 0)
                cacheParams.AddRange(dependencies);

            cacheParams.Add(new SlidingTime(TimeSpan.FromMinutes(CacheMinutes)));

            cacheManager.Add(
                cacheKey,
                list,
                CacheItemPriority.Normal,
                null,
                cacheParams.ToArray<ICacheItemExpiration>());
        }

        private string GetPetKey(string tag)
        {
            return "pets_" + tag;
        }
        #endregion
    }
}
