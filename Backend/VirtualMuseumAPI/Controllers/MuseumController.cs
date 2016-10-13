using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using VirtualMuseumAPI.Helpers;
using VirtualMuseumAPI.Models;
using Microsoft.AspNet.Identity;
using System.Data.Linq;
using System.IO;
using System.Net.Http.Headers;
using Ploeh.AutoFixture;
using System.Web.Http.Hosting;
using System.Web.WebPages;
using LinqKit;
using Microsoft.Ajax.Utilities;

namespace VirtualMuseumAPI.Controllers
{
    [Authorize]
    public class MuseumController : ApiController
    {
        public class MuseumResults
        {
            public IEnumerable<MuseumModel> Museums { get; set; }
        }

        VirtualMuseumDataContext dc;

        public MuseumController()
        {
            dc = new VirtualMuseumDataContext();
        }
        /// <summary>
        /// Get the serialized binary file that is assigned to the museum with the specified id
        /// </summary>
        /// <param name="id">The Museum's unique ID</param>
        /// <returns></returns>
        [AllowAnonymous]
        [Route("api/Museum/{id}/data")]
        [HttpGet]
        public IHttpActionResult GetMuseumData(int id)
        {

            if (!dc.Museums.Any(a => a.ID == id  && a.Data != null))
            {
                return NotFound();
            }
            else
            {
                Binary bin = dc.Museums.First(p => p.ID == id).Data;
                MemoryStream stream = new MemoryStream(bin.ToArray());
                return new VirtualMuseumDataResult(stream, "application/octet-stream");
            }
        }

        /// <summary>
        /// Get the properties of a random museum. You have to get the binary data via the /data.
        /// </summary>
        /// <param name="id">The Museum's unique ID</param>
        /// <returns></returns>
        [AllowAnonymous]
        [Route("api/Museum/random")]
        [HttpGet]
        public IHttpActionResult GetRandomMuseum()
        {

            if (dc.Museums.Any(a=> a.PrivacyLevel == dc.PrivacyLevels.Where(b => b.Name == "PUBLIC").First() && a.Data != null) )
            {
                Random rand = new Random();
                return Get(
                    VirtualMuseumUtils.RandomIEnumerableElement(
                    dc.Museums.Where(a => a.PrivacyLevel == dc.PrivacyLevels.Where(b => b.Name == "PUBLIC").First()), rand).ID
                    );
            }
            else
            {
                return NotFound();
            }

                       
        }

        // GET api/Museums/connected
        /// <summary>
        /// Get the museums connected to the user.
        /// </summary>
        /// <returns>A list of museums which belong to user</returns>
        [Route("api/Museum/connected")]
        [HttpGet]
        public IHttpActionResult GetConnectedMuseums()
        {
            string userid = User.Identity.GetUserId();

            if (!dc.Museums.Any(m => m.OwnerID == userid))
            {
                return NotFound();
            }
            var museums = dc.Museums.Where(m => m.OwnerID == userid).
                Select(m => new MuseumModel
                {
                    OwnerName = dc.AspNetUsers.First(a => a.Id == m.OwnerID).UserName,
                    Description = m.Description,
                    Name = m.Name,
                    LastModified = m.ModiDate,
                    MuseumID = m.ID,
                    Privacy = (Privacy.Levels)Enum.Parse(typeof(Privacy.Levels), dc.PrivacyLevels.First(a => a.ID == m.PrivacyLevelID).Name),
                    Visited = m.Visited
                }).ToList();

            return Ok(new MuseumResults() { Museums = museums });
        }

        /// <summary>
        /// Get a list of museums, or filter on a list of museums
        /// </summary>
        /// <param name="Museum search">museums fields</param>
        /// <returns></returns>
        public IHttpActionResult Get([FromUri] MuseumSearchModel msm)
        {
            string userID = User.Identity.GetUserId();
            var predicate = PredicateBuilder.True<Museum>();
            
            // Only show public museums, or show the users museums
            predicate.And(m => (m.PrivacyLevelID == 1 || m.OwnerID == userID));

            if (!msm.Description.IsNullOrWhiteSpace())
            {
                predicate = predicate.And(m => m.Description.ToLower().Contains(msm.Description.ToLower()));
            }

            if (!msm.OwnerName.IsNullOrWhiteSpace())
            {
                //TODO: fix case insensitivity
                var users = from user in dc.AspNetUsers where user.UserName.Contains(msm.OwnerName) select user.Id;
                predicate = predicate.And(m => users.Contains(m.OwnerID));
            }

            if (!msm.Name.IsNullOrWhiteSpace())
            {
                predicate = predicate.And(m => m.Name.ToLower().Contains(msm.Name.ToLower()));
            }

            //TODO: rating

            // Execute all substrings
            var museums = dc.Museums.Where(predicate).Select(m => new MuseumModel
                {
                    OwnerName = dc.AspNetUsers.First(a => a.Id == m.OwnerID).UserName,
                    Description = m.Description,
                    Name = m.Name,
                    LastModified = m.ModiDate,
                    MuseumID = m.ID,
                    Privacy = (Privacy.Levels)Enum.Parse(typeof(Privacy.Levels), dc.PrivacyLevels.First(a => a.ID == m.PrivacyLevelID).Name),
                    Visited = m.Visited
                }).ToList();

            return Ok(new MuseumResults() { Museums = museums });
        }

        // GET api/museum/id
        /// <summary>
        /// Get the properties of the museum with the specified id
        /// </summary>
        /// <param name="id">The Museum's unique ID</param>
        /// <returns>The museum object with the specified id</returns>
        [AllowAnonymous]
        public IHttpActionResult Get(int id)
        {

            if (!dc.Museums.Any(a => a.ID == id))
            {
                return NotFound();
            }
            else
            {
                Museum museum = dc.Museums.First(p => p.ID == id);
                MuseumModel model = new MuseumModel();
                model.OwnerName = dc.AspNetUsers.First(a => a.Id == museum.OwnerID).UserName;
                model.Description = museum.Description;
                model.Name = museum.Name;
                model.LastModified = museum.ModiDate;
                model.MuseumID = museum.ID;
                model.Privacy = (Privacy.Levels)Enum.Parse(typeof(Privacy.Levels), dc.PrivacyLevels.First(a => a.ID == museum.PrivacyLevelID).Name);
                model.Visited = museum.Visited;
                return Ok(model);
            }
        }


        // POST api/Museum
        /// <summary>
        /// Creates a new Virtual Museum
        /// </summary>
        /// <param name="model"></param>
        /// <returns>The newly created museum object</returns>
        public IHttpActionResult Post(MuseumModel model)
        {
            if (ModelState.IsValid)
            {
    
                VirtualMuseumFactory factory = new VirtualMuseumFactory(dc);
                Museum museum = factory.CreateMuseum(model.Name, model.Description, model.Privacy, User.Identity, User.Identity);
                model.OwnerName = User.Identity.GetUserName();
                model.MuseumID = museum.ID;
                model.LastModified = museum.ModiDate;
                model.Privacy = (Privacy.Levels)Enum.Parse(typeof(Privacy.Levels), dc.PrivacyLevels.Where(a => a.ID == museum.PrivacyLevelID).First().Name);
                return Ok(model);
            }
            else
            {
                return BadRequest(ModelState);
            }
        }

        // POST api/Museum/id
        /// <summary>
        /// Assigning a serialized binary file to the Museum with the specified id
        /// </summary>
        /// <param name="id">The Museum's unique ID</param>
        /// <returns>The museum object with the specified id, the file has been assigned.</returns>
        public async Task<IHttpActionResult> PostAsync(int id)
        {

            if (!dc.Museums.Any(a => a.ID == id))
            {
                return NotFound();
            }
            if (ModelState.IsValid)
            {
                if (Request.Content.IsMimeMultipartContent())
                {
                    var provider = new MultipartMemoryStreamProvider();
                    await Request.Content.ReadAsMultipartAsync(provider);
                    var file = provider.Contents.First();
                    var buffer = await file.ReadAsByteArrayAsync();
                    Museum museum = dc.Museums.Where(a=> a.ID == id).First();
                    museum.Data = buffer;
                    museum.ModiBy = User.Identity.GetUserId();
                    dc.Museums.Where(a=> a.ID == id).First().Data = buffer;
                    dc.SubmitChanges();
                    MuseumModel MuseumModel = new MuseumModel(){
                        Name = museum.Name,
                        OwnerName = dc.AspNetUsers.First(a => a.Id == museum.OwnerID).UserName,
                        Description = museum.Description,
                        LastModified = museum.ModiDate,
                        MuseumID = museum.ID
                    };

                    return Ok(MuseumModel);
                }
                else
                {
                    return BadRequest(ModelState);
                }
            }
            else
            {
                return BadRequest(ModelState);
            }
        }

        // PUT api/Museum/id
        /// <summary>
        /// Edit the Museum's properties
        /// </summary>
        /// <param name="id">The Museum's unique ID</param>
        /// <param name="model"></param>
        /// <returns></returns>
        public IHttpActionResult Put(int id, MuseumModel model)
        {
            if (ModelState.IsValid)
            {
    
                if (!dc.Museums.Any(a => a.ID == id))
                {
                    return NotFound();
                }
                else
                {
                    PrivacyLevel privacyLevel = dc.PrivacyLevels.Where(a => a.Name == "PRIVATE").First();
                    if (dc.PrivacyLevels.Any(a => a.Name == Enum.GetName(typeof(Privacy.Levels), (int) model.Privacy)))
                    {
                        privacyLevel = dc.PrivacyLevels.Where(a => a.Name == Enum.GetName(typeof(Privacy.Levels), (int)model.Privacy)).First();
                    }
                   
                    Museum museum = dc.Museums.First(a => a.ID == id);
                    museum.PrivacyLevelID = privacyLevel.ID;
                    museum.Description = model.Description;
                    museum.Name = model.Name;
                    museum.ModiBy = User.Identity.GetUserId();
                    museum.ModiDate =  DateTime.Now;
                    dc.SubmitChanges();
                    model.OwnerName = User.Identity.GetUserName();
                    model.LastModified = museum.ModiDate;
                    model.MuseumID = museum.ID;
                    return Ok(model);
                }
            }
            else
            {
                return BadRequest(ModelState);
            }
        }

        //DELETE api/Museum/5
        public IHttpActionResult Delete(int id)
        {
            if (!dc.Museums.Any(m => m.ID == id))
            {
                return NotFound();
            }
            else
            {
                var artWorksInMuseum = dc.MuseumsXArtworks.Where(m => m.MuseumID == id);
                foreach (var artwork in artWorksInMuseum)
                {
                    dc.MuseumsXArtworks.DeleteOnSubmit(artwork);
                }

                var visits = dc.MuseumUserVisits.Where(m => m.MuseumID == id);
                foreach (var visit in visits)
                {
                    dc.MuseumUserVisits.DeleteOnSubmit(visit);
                }

                var ratings = dc.MuseumRatings.Where(m => m.MuseumID == id);
                foreach (var rating in ratings)
                {
                    dc.MuseumRatings.DeleteOnSubmit(rating);
                }

                var metadatas = dc.MuseumMetadatas.Where(m => m.MuseumID == id);
                foreach (var metadata in metadatas)
                {
                    dc.MuseumMetadatas.DeleteOnSubmit(metadata);
                }

                Museum museum = dc.Museums.First(m => m.ID == id);
                dc.Museums.DeleteOnSubmit(museum);
                dc.SubmitChanges();
                return Ok();
            }
        }
    }   
}
