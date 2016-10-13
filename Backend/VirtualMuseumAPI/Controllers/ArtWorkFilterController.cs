using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using VirtualMuseumAPI.Models;
using Microsoft.AspNet.Identity;
using VirtualMuseumAPI.Helpers;

namespace VirtualMuseumAPI.Controllers
{
    [Authorize]
    public class ArtWorkFilterController : ApiController
    {
        VirtualMuseumDataContext dc;

        public ArtWorkFilterController()
        {
            dc = new VirtualMuseumDataContext();
        }
        public class ArtWorkFilterResults
        {
            public IEnumerable<ArtWorkFilterModel> ArtWorkFilters { get; set; }
        }

        /// <summary>
        /// Get all the filters that are assigned to a specific user
        /// </summary>
        /// <returns></returns>
        [Route("api/ArtWorkFilter/connected")]
        [HttpGet]
        public ArtWorkFilterResults Get()
        {
            string userID = User.Identity.GetUserId();
            List<ArtWorkFilterModel> filters = new List<ArtWorkFilterModel>();
            foreach (ArtworkFilter filter in dc.ArtworkFilters.Where(a => a.ArtworkFiltersXUsers.Any(b => b.UID == userID)))
            {
                ArtWorkFilterModel artworkFilterModel = new ArtWorkFilterModel();
                artworkFilterModel.ArtistID = filter.ArtistID;
                artworkFilterModel.ArtWorkID = filter.ArtworkID ?? -1;
                artworkFilterModel.Pairs = new List<KeyValuePair>();
                foreach (ArtworkFilterValue filtervalue in dc.ArtworkFilterValues.Where(f => f.ArtworkFilterID == filter.ID))
                {
                    artworkFilterModel.Pairs.Add(new KeyValuePair() { Name = filtervalue.ArtworkKey.name, Value = filtervalue.Value });
                }
                filters.Add(artworkFilterModel);
            }
            ArtWorkFilterResults result = new ArtWorkFilterResults()
            {
                ArtWorkFilters = filters
            };
            return result;
        }

        [Route("api/ArtWorkFilter/{id}")]
        [HttpGet]
        public IHttpActionResult Get(int id)
        {
            if (!dc.ArtworkFilters.Any(a => a.ID == id))
            {
                return NotFound();
            }
            ArtworkFilter filter = dc.ArtworkFilters.First(a => a.ID == id);
            ArtWorkFilterModel artworkFilterModel = new ArtWorkFilterModel();
            artworkFilterModel.ArtistID = filter.ArtistID;
            artworkFilterModel.ArtWorkID = filter.ArtworkID ?? -1;
            artworkFilterModel.Pairs = new List<KeyValuePair>();

            foreach (ArtworkFilterValue filtervalue in dc.ArtworkFilterValues.Where(f => f.ArtworkFilterID == filter.ID))
            {
                artworkFilterModel.Pairs.Add(new KeyValuePair() { Name = filtervalue.ArtworkKey.name, Value = filtervalue.Value });
            }
            return Ok(artworkFilterModel);
        }

        /// <summary>
        /// Search for a specific filter
        /// </summary>
        /// <param name="pair">Name or Value</param>
        /// <returns>Array of found filters</returns>
        public ArtWorkFilterResults Get([FromUri] KeyValuePair pair)
        {
            List<ArtworkFilter> filters = new List<ArtworkFilter>();
            if ((!String.IsNullOrEmpty(pair.Name)) && String.IsNullOrEmpty(pair.Value))
            {
                foreach (ArtworkFilter filter in dc.ArtworkFilters.Where(a => a.ArtworkFilterValues.Any(b => b.ArtworkKey.name.ToLower() == pair.Name.Trim().ToLower())))
                {
                    filters.Add(filter);
                }

            }
            else if ((!String.IsNullOrEmpty(pair.Value)) && String.IsNullOrEmpty(pair.Name))
            {
                foreach (ArtworkFilter filter in dc.ArtworkFilters.Where(a => a.ArtworkFilterValues.Any(b => b.Value.ToLower() == pair.Value.Trim().ToLower())))
                {
                    filters.Add(filter);
                }
            }
            else if ((!String.IsNullOrEmpty(pair.Value)) && (!String.IsNullOrEmpty(pair.Name)))
            {
                foreach (ArtworkFilter filter in dc.ArtworkFilters.Where(a => a.ArtworkFilterValues.Any(b => b.Value.ToLower() == pair.Value.Trim().ToLower() && b.ArtworkKey.name.ToLower() == pair.Name.Trim().ToLower())))
                {
                    filters.Add(filter);
                }
            }
            List<ArtWorkFilterModel> results = new List<ArtWorkFilterModel>();
            foreach (ArtworkFilter filter in filters)
            {
                ArtWorkFilterModel artWorkFilterModel = new ArtWorkFilterModel();
                artWorkFilterModel.ArtistID = filter.ArtistID;
                foreach (ArtworkFilterValue filtervalue in dc.ArtworkFilterValues.Where(f => f.ArtworkFilterID == filter.ID))
                {
                    artWorkFilterModel.Pairs.Add(new KeyValuePair() { Name = filtervalue.ArtworkKey.name, Value = filtervalue.Value });
                }
                results.Add(artWorkFilterModel);

            }

            return new ArtWorkFilterResults() { ArtWorkFilters = results };
        }

        /// <summary>
        /// Assign a filter to the user, makes a new one if the filter doesn't already exist
        /// </summary>
        /// <param name="pair">The name and the value of the filter, e.g. Genre, nsfw</param>
        /// <returns>The filter that is assigned</returns>
        public IHttpActionResult Post(ArtWorkFilterModel filterModel)
        {
            VirtualMuseumFactory factory = new VirtualMuseumFactory();

            bool exists = false;
            int foundFilterID = -1;
            if (filterModel.ArtWorkID == -1 || filterModel.ArtWorkID == null)
            {
                if(filterModel.Pairs == null){
                    return BadRequest();
                }
                foreach (ArtworkFilter filter in dc.ArtworkFilters.Where(a => a.ArtistID == filterModel.ArtistID))
                {
                    bool suitable = true;
                    int suitablefilter = filter.ID;
                    if (filterModel.Pairs.Count == filter.ArtworkFilterValues.Count)
                    {
                        foreach (ArtworkFilterValue value in filter.ArtworkFilterValues)
                        {
                            if (!filterModel.Pairs.Any(a => a.Name == value.ArtworkKey.name && a.Value == value.Value))
                            {
                                suitable = false;
                            }
                        }
                    }
                    else
                    {
                        suitable = false;
                    }

                    if (suitable)
                    {
                        exists = true;
                        foundFilterID = suitablefilter;
                    }
                }
            }
            else
            {
                if (dc.ArtworkFilters.Any(f => f.ArtworkID == filterModel.ArtWorkID))
                {
                    exists = true;
                    foundFilterID = dc.ArtworkFilters.First(f => f.ArtworkID == filterModel.ArtWorkID).ID;
                }
            }


            if (exists)
            {
                factory.AssignArtWorkFilterToUser(foundFilterID, User.Identity);
                return Get(foundFilterID);
            }
            else
            {

                ArtworkFilter newFilter = factory.CreateArtWorkFilter(filterModel.ArtistID, User.Identity, filterModel.ArtWorkID);
                if (filterModel.Pairs != null)
                {
                    foreach (KeyValuePair keyPair in filterModel.Pairs)
                    {
                        //If key does not exist, make a new
                        ArtworkKey key = null;
                        if (!dc.ArtworkKeys.Any(a => a.name.ToLower() == keyPair.Name.Trim().ToLower()))
                        {
                            key = factory.CreateArtworkKey(keyPair.Name.ToLower().Trim());
                        }
                        else
                        {
                            key = dc.ArtworkKeys.First(a => a.name.ToLower() == keyPair.Name.Trim().ToLower());
                        }
                        factory.CreateArtWorkFilterValue(newFilter.ID, key.ID, keyPair.Value);
                    }
                }
                
                factory.AssignArtWorkFilterToUser(newFilter.ID, User.Identity);
                return Get(newFilter.ID);
            }
        }

        /// <summary>
        /// Deletes an existing filter from the current user. If the filter is not assigned to other user, it will be deleted completely
        /// </summary>
        /// <param name="id">The ArtworkFilter's unique ID</param>
        /// <returns></returns>
        public IHttpActionResult Delete(int id)
        {
            String userId = User.Identity.GetUserId();
            if (!dc.ArtworkFilters.Any(a => a.ID == id))
            {
                return NotFound();
            }
            if (dc.ArtworkFiltersXUsers.Any(a => a.UID == userId && a.ArtworkFilterID == id))
            {
                dc.ArtworkFiltersXUsers.DeleteOnSubmit(dc.ArtworkFiltersXUsers.First(a => a.UID == userId && a.ArtworkFilterID == id));
                dc.SubmitChanges();
            }
            else
            {
                return NotFound();
            }

            //If not assigned to other user, delete filter
            if (dc.ArtworkFiltersXUsers.Any(a => a.ArtworkFilterID == id))
            {
                dc.ArtworkFilters.DeleteOnSubmit(dc.ArtworkFilters.First());
                dc.SubmitChanges();
            }
            return Ok();
        }

    }
}
