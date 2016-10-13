using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using System.Web.Http;
using System.Web.Http.Hosting;
using System.Web.Http.Results;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoMoq;
using VirtualMuseumAPI.Controllers;
using VirtualMuseumAPI.Models;

namespace VirtualMuseumAPI.Tests
{
    [TestClass]
    public class TestArtistController
    {
        [TestMethod]
        public void PostArtist_ShouldReturnCorrectArtist()
        {
            using (TransactionScope transaction = new TransactionScope())
            {
                var testArtists = GetTestArtists();
                var artistController = getArtistController();
                var postArtist = artistController.Post(testArtists[0]) as OkNegotiatedContentResult<ArtistModel>;
                Assert.IsNotNull(postArtist);
                var getArtist = artistController.Get(postArtist.Content.ArtistID) as OkNegotiatedContentResult<ArtistModel>;
                Assert.IsNotNull(getArtist);
                Assert.AreEqual(getArtist.Content.Name, postArtist.Content.Name);
                Assert.AreEqual(getArtist.Content.ArtistID, postArtist.Content.ArtistID);

                // Not commiting transaction to leave DB in clean state
            }
        }

        [TestMethod]
        public void PostArtist_ShouldFailWhenNotFilledInCorrectly()
        {
            using (TransactionScope transaction = new TransactionScope())
            {
                var artistController = getArtistController();
                artistController.ModelState.AddModelError("Key", "ErrorMessage"); // Values of these two strings don't matter.  
                var artist = new ArtistModel() {};
                var postArtist = artistController.Post(artist);
                Assert.IsInstanceOfType(postArtist, typeof(InvalidModelStateResult));
  
                // Not commiting transaction to leave DB in clean state
            }
        }

        [TestMethod]
        public void PutArtist_WorksCorrectly()
        {
            using (TransactionScope transaction = new TransactionScope())
            {
                var testArtists = GetTestArtists();
                var artistController = getArtistController();
                var postArtist = artistController.Post(testArtists[0]) as OkNegotiatedContentResult<ArtistModel>;
                Assert.IsNotNull(postArtist);

                var originalArtist = artistController.Get(postArtist.Content.ArtistID);
                var artist = new ArtistModel() { ArtistID = postArtist.Content.ArtistID, Name = "Leonardo Da Vinci"};
                var putArtist = artistController.Put(artist.ArtistID, artist) as OkNegotiatedContentResult<ArtistModel>;
                Assert.IsNotNull(putArtist);
                Assert.AreNotEqual(putArtist.Content.Name, postArtist.Content.Name);
                Assert.AreEqual(putArtist.Content.Name, artist.Name);

                // Not commiting transaction to leave DB in clean state
            }
        }

        [TestMethod]
        public void PutArtist_WorksInCorrectly()
        {
            using (TransactionScope transaction = new TransactionScope())
            {
                var testArtists = GetTestArtists();
                var artistController = getArtistController();
                var postArtist = artistController.Post(testArtists[0]) as OkNegotiatedContentResult<ArtistModel>;
                Assert.IsNotNull(postArtist);

                artistController = getArtistController();
                var originalArtist = artistController.Get(postArtist.Content.ArtistID);
                var artist = new ArtistModel() { ArtistID = postArtist.Content.ArtistID, Name = null };
                
                artistController = getArtistController();
                artistController.ModelState.AddModelError("Key", "ErrorMessage"); // Values of these two strings don't matter.  
                var putArtist = artistController.Put(artist.ArtistID, artist);
                Assert.IsInstanceOfType(putArtist, typeof(InvalidModelStateResult));
                // Not commiting transaction to leave DB in clean state
            }
        }

        [TestMethod]
        public void GetConnectedArtists_ShouldBeAListWithArtists()
        {
            using (TransactionScope transaction = new TransactionScope())
            {
                var artistController = getArtistController();
                var connectedArtistsResponse =
                    artistController.GetConnectedArtists() as OkNegotiatedContentResult<ArtistController.ArtistResults>;
                var connectedArtists = connectedArtistsResponse.Content.Artists;
                
                Assert.IsNotNull(connectedArtists);
                Assert.IsTrue(connectedArtists.Any());
                Assert.IsNotNull(connectedArtists.ElementAt(0).Name);
                Assert.IsNotNull(connectedArtists.ElementAt(0).ArtistID);
            }
        }

        [TestMethod]
        public void GetArtists_ReturnsList()
        {
            using (TransactionScope transaction = new TransactionScope())
            {
                var artistController = getArtistController();
                var artistsResponse =
                    artistController.Get() as OkNegotiatedContentResult<ArtistController.ArtistResults>;
                var artists = artistsResponse.Content.Artists;

                Assert.IsNotNull(artists);
                Assert.IsTrue(artists.Any());
                Assert.IsNotNull(artists.ElementAt(0).Name);
                Assert.IsNotNull(artists.ElementAt(0).ArtistID);
            }
        }

        [TestMethod]
        public void GetArtist_ReturnsArtist()
        {
            using (TransactionScope transaction = new TransactionScope())
            {
                var artistController = getArtistController();
                var artistsResponse =
                    artistController.Get(1) as OkNegotiatedContentResult<ArtistModel>;
                var artist = artistsResponse.Content;

                Assert.IsNotNull(artist);
                Assert.IsNotNull(artist.Name);
                Assert.IsNotNull(artist.ArtistID);
            }
        }

        [TestMethod]
        public void GetArtist_NotFound()
        {
            using (TransactionScope transaction = new TransactionScope())
            {
                var artistController = getArtistController();
                var artistsResponse =
                    artistController.Get(0) as NotFoundResult;

                Assert.IsNotNull(artistsResponse);
            }
        }

        [TestMethod]
        public void DeleteArtist()
        {
            using (TransactionScope transaction = new TransactionScope())
            {
                var testArtists = GetTestArtists();
                var artistController = getArtistController();
                var postArtist = artistController.Post(testArtists[0]) as OkNegotiatedContentResult<ArtistModel>;
                Assert.IsNotNull(postArtist);
                var deletedArtistResult = artistController.Delete(postArtist.Content.ArtistID) as OkResult;
                Assert.IsNotNull(deletedArtistResult);
                var notFoundResult = artistController.Delete(postArtist.Content.ArtistID) as NotFoundResult;
                Assert.IsNotNull(notFoundResult);

                // Not commiting transaction to leave DB in clean state
            }
        }

        private List<ArtistModel> GetTestArtists()
        {
            var testArtists = new List<ArtistModel>();
            testArtists.Add(new ArtistModel() { Name = "Virtual Museum" });
            testArtists.Add(new ArtistModel(){Name = "Leonardo DaVince"});
            return testArtists;
        }

        private ArtistController getArtistController()
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            fixture.Customize<HttpRequestMessage>(c => c
            .Without(x => x.Content)
            .Do(x => x.Properties[HttpPropertyKeys.HttpConfigurationKey] =
                new HttpConfiguration()));
            fixture.Customize<MuseumController>(c => c
                .OmitAutoProperties()
                .With(x => x.Request, fixture.Create<HttpRequestMessage>()));
            //Simulate this user to execute controller methods
            var claim = new Claim("VirtualMuseum", "d32895c4-8d71-4340-884c-89293cc50784");
            var mockIdentity =
                Mock.Of<ClaimsIdentity>(ci => ci.FindFirst(It.IsAny<string>()) == claim);
            fixture.Customize<ArtistController>(c => c
                .OmitAutoProperties()
                .With(x => x.User, Mock.Of<IPrincipal>(ip => ip.Identity == mockIdentity)));
            return fixture.CreateAnonymous<ArtistController>();
        }
    }
}
