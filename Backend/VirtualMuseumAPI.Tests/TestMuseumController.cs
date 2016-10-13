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
    public class TestMuseumController
    {

        [TestMethod]
        public void GetMuseum_ShouldNotFindMuseum()
        {
            using (TransactionScope transaction = new TransactionScope())
            {
                //Test with an ID that doesn't exist
                var museumController = getMuseumController();
                var getMuseum = museumController.Get(0);
                Assert.IsInstanceOfType(getMuseum, typeof(NotFoundResult));
                // Not commiting transaction to leave DB in clean state
            }
        }


        [TestMethod]
        public void GetMuseumData_ShouldNotFindMuseum()
        {
            using (TransactionScope transaction = new TransactionScope())
            {
                //Test with an ID that doesn't exist
                var museumController = getMuseumController();
                var getMuseum = museumController.GetMuseumData(0);
                Assert.IsInstanceOfType(getMuseum, typeof(NotFoundResult));
                var testMuseums = GetTestMuseums();

                //No data assigned to the museum entry
                var postMuseum = museumController.Post(testMuseums[0]) as OkNegotiatedContentResult<MuseumModel>;
                getMuseum = museumController.GetMuseumData(postMuseum.Content.MuseumID);
                Assert.IsInstanceOfType(getMuseum, typeof(NotFoundResult));
                // Not commiting transaction to leave DB in clean state
            }
        }

        [TestMethod]
        public void PostMuseum_ShouldReturnCorrectMuseum()
        {    
            using (TransactionScope transaction = new TransactionScope())
            {
                var testMuseums = GetTestMuseums();
                var museumController = getMuseumController();
                var postMuseum = museumController.Post(testMuseums[0]) as OkNegotiatedContentResult<MuseumModel>;
                var getMuseum = museumController.Get(postMuseum.Content.MuseumID) as OkNegotiatedContentResult<MuseumModel>;
                Assert.IsNotNull(postMuseum);
                Assert.IsNotNull(getMuseum);
                Assert.AreEqual(getMuseum.Content.Name, postMuseum.Content.Name);
                Assert.AreEqual(getMuseum.Content.Description, postMuseum.Content.Description);
                Assert.AreEqual(getMuseum.Content.Privacy, postMuseum.Content.Privacy);

                // Not commiting transaction to leave DB in clean state
            }
        }

        [TestMethod]
        public void PostMuseum_ShouldFailWhenNotFilledInCorrectly()
        {
            using (TransactionScope transaction = new TransactionScope())
            {
                var museumcontroller = getMuseumController();
                museumcontroller.ModelState.AddModelError("Key", "ErrorMessage"); // Values of these two strings don't matter.  
                var museum = new MuseumModel() { };
                var postMuseum = museumcontroller.Post(museum);
                Assert.IsInstanceOfType(postMuseum, typeof(InvalidModelStateResult));

                // Not commiting transaction to leave DB in clean state
            }
        }

        [TestMethod]
        public void GetRandomMuseum_ShouldNotFindMuseum()
        {
            using (TransactionScope transaction = new TransactionScope())
            {
                var museumController = getMuseumController();
                //Initially, no museums in the database, so it should not find a random
                var testMuseums = GetTestMuseums();
                var getRandomMuseum = museumController.GetRandomMuseum();
                Assert.IsInstanceOfType(getRandomMuseum, typeof(NotFoundResult));

                //Post a private museum, after this, it should not find a random 
                museumController.Post(testMuseums[0]);
                getRandomMuseum = museumController.GetRandomMuseum();
                Assert.IsInstanceOfType(getRandomMuseum, typeof(NotFoundResult));

                //Post a public museum, but with no data. After this, it should not find a random 
                museumController.Post(testMuseums[1]);
                getRandomMuseum = museumController.GetRandomMuseum();
                Assert.IsInstanceOfType(getRandomMuseum, typeof(NotFoundResult));

                // Not commiting transaction to leave DB in clean state
            }
        }

        [TestMethod]
        public async Task GetRandomMuseum_ShouldFindMuseum()
        {
            using (TransactionScope transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var museumData = new Byte[100];
                var museumDataController = getMuseumController(museumData);
                var testMuseums = GetTestMuseums();
                //Create a new museum
                var postMuseum = museumDataController.Post(testMuseums[1]) as OkNegotiatedContentResult<MuseumModel>;
                //Assign museumData to it
                var postMuseumData = await museumDataController.PostAsync(postMuseum.Content.MuseumID);

                var getRandomMuseum = museumDataController.GetRandomMuseum() as OkNegotiatedContentResult<MuseumModel>;
                Assert.AreEqual(getRandomMuseum.Content.MuseumID, postMuseum.Content.MuseumID);

                // Not commiting transaction to leave DB in clean state
            }
        }

        [TestMethod]
        public async Task PostMuseumData_ShouldReturnCorrectMuseum()
        {
            using (TransactionScope transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                //Data to send
                var museumData = new Byte[100];
                //Get controller with MultipartFormDataContent
                var museumController = getMuseumController(museumData);
                //Get testmuseums array
                var testMuseums = GetTestMuseums();
                //Create a new museum
                var postMuseum = museumController.Post(testMuseums[0]) as OkNegotiatedContentResult<MuseumModel>;
                //Assign museumData to it
                var postMuseumData = await museumController.PostAsync(postMuseum.Content.MuseumID);
                //Get the new museum
                var getMuseum = museumController.GetMuseumData(postMuseum.Content.MuseumID) as VirtualMuseumDataResult;

                Assert.IsNotNull(postMuseumData);
                Assert.IsNotNull(getMuseum);
                //Make an byte array from the received stream to compare
                byte[] getMuseumByteArray;
                using (BinaryReader br = new BinaryReader(getMuseum.getStream()))
                {
                    getMuseumByteArray = br.ReadBytes(museumData.Length);
                }
                CollectionAssert.AreEqual(getMuseumByteArray, museumData);
                
            }
            
        }

        [TestMethod]
        public void PutMuseum_WorksCorrectly()
        {
            using (TransactionScope transaction = new TransactionScope())
            {
                var testMuseums = GetTestMuseums();
                var MuseumController = getMuseumController();
                var postMuseum = MuseumController.Post(testMuseums[0]) as OkNegotiatedContentResult<MuseumModel>;
                Assert.IsNotNull(postMuseum);

                var originalMuseum = MuseumController.Get(postMuseum.Content.MuseumID);
                var Museum = new MuseumModel() { MuseumID = postMuseum.Content.MuseumID, Name="Test", Description = "Leonardo Da Vinci's museum", Privacy= Privacy.Levels.PUBLIC };
                var putMuseum = MuseumController.Put(Museum.MuseumID, Museum) as OkNegotiatedContentResult<MuseumModel>;
                Assert.IsNotNull(putMuseum);
                Assert.AreNotEqual(putMuseum.Content.Name, postMuseum.Content.Name);
                Assert.AreNotEqual(putMuseum.Content.Description, postMuseum.Content.Description);
                Assert.AreEqual(putMuseum.Content.Privacy, Museum.Privacy);

                // Not commiting transaction to leave DB in clean state
            }
        }

        [TestMethod]
        public void PutMuseum_WorksInCorrectly()
        {
            using (TransactionScope transaction = new TransactionScope())
            {
                var testMuseums = GetTestMuseums();
                var MuseumController = getMuseumController();
                var postMuseum = MuseumController.Post(testMuseums[0]) as OkNegotiatedContentResult<MuseumModel>;
                Assert.IsNotNull(postMuseum);

                MuseumController = getMuseumController();
                var originalMuseum = MuseumController.Get(postMuseum.Content.MuseumID);
                var Museum = new MuseumModel() { MuseumID = postMuseum.Content.MuseumID, Name="Test",  Description = null };

                MuseumController = getMuseumController();
                MuseumController.ModelState.AddModelError("Key", "ErrorMessage"); // Values of these two strings don't matter.  
                var putMuseum = MuseumController.Put(Museum.MuseumID, Museum);
                Assert.IsInstanceOfType(putMuseum, typeof(InvalidModelStateResult));
                // Not commiting transaction to leave DB in clean state
            }
        }

        [TestMethod]
        public void PutMuseum_ShouldNotFindMuseum()
        {
            using (TransactionScope transaction = new TransactionScope())
            {
                //Test with an ID that doesn't exist
                var museumController = getMuseumController();
                var museum = new MuseumModel() { MuseumID = 0, Name="Test", Description = "Leonardo Da Vinci's museum", Privacy = Privacy.Levels.PUBLIC };
                var putMuseum = museumController.Put(0, museum);
                Assert.IsInstanceOfType(putMuseum, typeof(NotFoundResult));
                // Not commiting transaction to leave DB in clean state
            }
        }

        [TestMethod]
        public void ConnectedMuseum_ShouldNotReturnMuseums()
        {
            using (TransactionScope transaction = new TransactionScope())
            {
                //Test with an ID that doesn't exist
                var museumController = getMuseumController();
                var connectResult = museumController.GetConnectedMuseums();
                Assert.IsInstanceOfType(connectResult, typeof(NotFoundResult));
                // Not commiting transaction to leave DB in clean state
            }
        }

        [TestMethod]
        public void ConnectedMuseum_ShouldMuseums()
        {
            using (TransactionScope transaction = new TransactionScope())
            {
                //Test with an ID that doesn't exist
                var museumController = getMuseumController();
                var testMuseums = GetTestMuseums();
                museumController.Post(testMuseums[0]);
                museumController.Post(testMuseums[1]);

                var connectResult = museumController.GetConnectedMuseums() as OkNegotiatedContentResult<MuseumController.MuseumResults>;
                Assert.IsNotNull(connectResult);
                Assert.IsTrue(connectResult.Content.Museums.Any());
                Assert.IsTrue(connectResult.Content.Museums.Any(m => m.Description == testMuseums[0].Description));
                // Not commiting transaction to leave DB in clean state
            }
        }

        private List<MuseumModel> GetTestMuseums()
        {
            var testMuseums = new List<MuseumModel>();
            testMuseums.Add(new MuseumModel {Name="DemoMuseumPrivate", Description = "DemoMuseumPrivate", Privacy = Privacy.Levels.PRIVATE });
            testMuseums.Add(new MuseumModel {Name="DemoMuseumPublic", Description = "DemoMuseumPublic", Privacy = Privacy.Levels.PUBLIC });
            return testMuseums;
        }

        private MuseumController getMuseumController()
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
            fixture.Customize<MuseumController>(c => c
                .OmitAutoProperties()
                .With(x => x.User, Mock.Of<IPrincipal>(ip => ip.Identity == mockIdentity)));
            return fixture.CreateAnonymous<MuseumController>();
        }

        private MuseumController getMuseumController(byte[] buffer)
        {
            var museumController = getMuseumController();
            museumController.ControllerContext = FakeControllerContextWithMultiPartContentFactory.Create(buffer);
            var claim = new Claim("VirtualMuseum", "d32895c4-8d71-4340-884c-89293cc50784");
            var mockIdentity =
                Mock.Of<ClaimsIdentity>(ci => ci.FindFirst(It.IsAny<string>()) == claim);
            museumController.User = Mock.Of<IPrincipal>(ip => ip.Identity == mockIdentity);
            return museumController;
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
                    FileName = "Foo.txt"
                };
                content.Add(fileContent);
                request.Content = content;

                return new HttpControllerContext(new HttpConfiguration(), new HttpRouteData(new HttpRoute("")), request);
            }

        }
    }
}

