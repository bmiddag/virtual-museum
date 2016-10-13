using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoMoq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Hosting;
using System.Web.Http.Results;
using System.Web.Http.Routing;
using VirtualMuseumAPI.Controllers;
using VirtualMuseumAPI.Helpers;
using VirtualMuseumAPI.Models;

namespace VirtualMuseumAPI.Tests
{
    [TestClass]
    public class TestArtWorkController
    {
        [TestMethod]
        public void GetArtWorkData_ShouldNotFindArtWork()
        {
            using (TransactionScope transaction = new TransactionScope())
            {
                //Test with an ID that doesn't exist
                var ArtWorkController = getArtWorkController();
                var getArtWork = ArtWorkController.GetArtworkData(0);
                Assert.IsInstanceOfType(getArtWork, typeof(NotFoundResult));
                var testArtWorks = GetTestArtWorks();

                // Not commiting transaction to leave DB in clean state
            }
        }

        [TestMethod]
        public async Task PostArtWorkData_ShouldReturnCorrectArtWork()
        {
            using (TransactionScope transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                //Data to send
                var ArtWorkData = Convert.FromBase64String(VirtualMuseumTestUtils.img);
                //Get controller with MultipartFormDataContent
                var ArtWorkController = getArtWorkController(ArtWorkData);
                var postArtWorkData = await ArtWorkController.PostAsync() as OkNegotiatedContentResult<VirtualMuseumAPI.Controllers.ArtWorkController.ArtworkResults>;
                //Get the new ArtWork
                var getArtWork = ArtWorkController.GetArtworkData(postArtWorkData.Content.ArtWorks.First().ArtWorkID, 5) as VirtualMuseumDataResult;

                Assert.IsNotNull(postArtWorkData);
                Assert.IsNotNull(getArtWork);
                //Make an byte array from the received stream to compare
                byte[] getArtWorkByteArray;
                using (BinaryReader br = new BinaryReader(getArtWork.getStream()))
                {
                    getArtWorkByteArray = br.ReadBytes(ArtWorkData.Length);
                }
                CollectionAssert.AreEqual(getArtWorkByteArray, ArtWorkData);
            }
        }

        [TestMethod]
        public async Task PutArtWork_WorksCorrectly()
        {
            using (TransactionScope transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var testArtWorks = GetTestArtWorks();
                //Data to send
                var ArtWorkData = Convert.FromBase64String(VirtualMuseumTestUtils.img);
                //Get controller with MultipartFormDataContent
                var ArtWorkController = getArtWorkController(ArtWorkData);
                var postArtWorkData = await ArtWorkController.PostAsync() as OkNegotiatedContentResult<VirtualMuseumAPI.Controllers.ArtWorkController.ArtworkResults>;
                Assert.IsNotNull(postArtWorkData);
                ArtWorkController = getArtWorkController();
                var ArtWork = new ArtWorkModel() { ArtWorkID = postArtWorkData.Content.ArtWorks.First().ArtWorkID, ArtistID = 1, Name = "Mercator", Metadata = new List<KeyValuePair>() { new KeyValuePair() { Name = "Genre", Value = "nsfw" } } };
                var putArtWork = ArtWorkController.Put(ArtWork.ArtWorkID, ArtWork) as OkNegotiatedContentResult<ArtWorkModel>;
                Assert.IsNotNull(putArtWork);
                Assert.AreEqual(putArtWork.Content.Name, "Mercator");
                Assert.AreEqual(putArtWork.Content.Metadata.First().Name, "Genre");
                Assert.AreEqual(putArtWork.Content.Metadata.First().Value, "nsfw");
                // Not commiting transaction to leave DB in clean state
            }
        }

        [TestMethod]
        public async Task PutArtWork_WorksInCorrectly()
        {
            using (TransactionScope transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var testArtWorks = GetTestArtWorks();
                //Data to send
                var ArtWorkData = Convert.FromBase64String(VirtualMuseumTestUtils.img);
                //Get controller with MultipartFormDataContent
                var ArtWorkController = getArtWorkController(ArtWorkData);
                var postArtWorkData = await ArtWorkController.PostAsync() as OkNegotiatedContentResult<VirtualMuseumAPI.Controllers.ArtWorkController.ArtworkResults>;
                Assert.IsNotNull(postArtWorkData);

                ArtWorkController = getArtWorkController();
                var ArtWork = new ArtWorkModel() { ArtWorkID = postArtWorkData.Content.ArtWorks.First().ArtWorkID, Name = null, Metadata = null };

                ArtWorkController = getArtWorkController();
                ArtWorkController.ModelState.AddModelError("Key", "ErrorMessage"); // Values of these two strings don't matter.  
                var putArtWork = ArtWorkController.Put(ArtWork.ArtWorkID, ArtWork);
                Assert.IsInstanceOfType(putArtWork, typeof(InvalidModelStateResult));
                // Not commiting transaction to leave DB in clean state
            }
        }

        [TestMethod]
        public void PutArtWork_ShouldNotFindArtWork()
        {
            using (TransactionScope transaction = new TransactionScope())
            {
                //Test with an ID that doesn't exist
                var ArtWorkController = getArtWorkController();
                var ArtWork = new ArtWorkModel() { ArtWorkID = 0, Name = "Artwork0", Metadata = new List<KeyValuePair>() { new KeyValuePair() { Name = "Genre", Value = "nsfw" } } };
                var putArtWork = ArtWorkController.Put(0, ArtWork);
                Assert.IsInstanceOfType(putArtWork, typeof(NotFoundResult));
                // Not commiting transaction to leave DB in clean state
            }
        }





        private List<ArtWorkModel> GetTestArtWorks()
        {
            var testArtWorks = new List<ArtWorkModel>();
            testArtWorks.Add(new ArtWorkModel { ArtistID=1, Name="Artwork1",  Metadata = new List<KeyValuePair>(){ new KeyValuePair(){Name = "Genre", Value = "nsfw"}}});
            testArtWorks.Add(new ArtWorkModel { ArtistID = 1, Name = "Artwork2", Metadata = new List<KeyValuePair>() { new KeyValuePair() { Name = "Genre", Value = "nsfw" } } });
            return testArtWorks;
        }

        private ArtWorkController getArtWorkController()
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            fixture.Customize<HttpRequestMessage>(c => c
            .Without(x => x.Content)
            .Do(x => x.Properties[HttpPropertyKeys.HttpConfigurationKey] =
                new HttpConfiguration()));
            fixture.Customize<ArtWorkController>(c => c
                .OmitAutoProperties()
                .With(x => x.Request, fixture.Create<HttpRequestMessage>()));
            //Simulate this user to execute controller methods
            var claim = new Claim("VirtualArtWork", "d32895c4-8d71-4340-884c-89293cc50784");
            var mockIdentity =
                Mock.Of<ClaimsIdentity>(ci => ci.FindFirst(It.IsAny<string>()) == claim);
            fixture.Customize<ArtWorkController>(c => c
                .OmitAutoProperties()
                .With(x => x.User, Mock.Of<IPrincipal>(ip => ip.Identity == mockIdentity)));
            return fixture.CreateAnonymous<ArtWorkController>();
        }

        private ArtWorkController getArtWorkController(byte[] buffer)
        {
            var ArtWorkController = getArtWorkController();
            ArtWorkController.ControllerContext = FakeControllerContextWithMultiPartContentFactory.Create(buffer);
            var claim = new Claim("VirtualArtWork", "d32895c4-8d71-4340-884c-89293cc50784");
            var mockIdentity =
                Mock.Of<ClaimsIdentity>(ci => ci.FindFirst(It.IsAny<string>()) == claim);
            ArtWorkController.User = Mock.Of<IPrincipal>(ip => ip.Identity == mockIdentity);
            return ArtWorkController;
        }

        public class FakeControllerContextWithMultiPartContentFactory
        {
            public static HttpControllerContext Create(byte[] buffer)
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "");
                var content = new MultipartFormDataContent();

                var fileContent = new ByteArrayContent(buffer);
                fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                {
                    FileName = "Foo.jpg"
                };
                content.Add(fileContent);
                request.Content = content;

                return new HttpControllerContext(new HttpConfiguration(), new HttpRouteData(new HttpRoute("")), request);
            }

        }
    }
}
