using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoMoq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading;
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
    public class TestCreditController
    {
        [TestMethod]
        public void Post_EnterMuseum_ShouldAddCredits()
        {
            using (TransactionScope transaction = new TransactionScope())
            {
                var museumController = getMuseumController();
                var postMuseum = museumController.Post(new MuseumModel { Name = "DemoMuseumPrivate", Description = "DemoMuseumPrivate", Privacy = Privacy.Levels.PUBLIC }) as OkNegotiatedContentResult<MuseumModel>;
                var creditController = getCreditController(true);
                
                //Check the old credits
                var userInfoModel = creditController.Get() as OkNegotiatedContentResult<UserInfoViewModel>;
         
                //Enter a museum, should add credits
                var enteredUserInfoModel = creditController.Post(new CreditModel { Actions = CreditActionType.Actions.ENTERMUSEUM, ID = postMuseum.Content.MuseumID }) as OkNegotiatedContentResult<UserInfoViewModel>;
                Assert.IsTrue(enteredUserInfoModel.Content.Credits > userInfoModel.Content.Credits, "The new credits are not higher than the old");
                var newUserInfoModel = creditController.Get() as OkNegotiatedContentResult<UserInfoViewModel>;
                Assert.AreEqual(enteredUserInfoModel.Content.Credits, newUserInfoModel.Content.Credits);
                Assert.AreEqual(enteredUserInfoModel.Content.CreditsAdded, true);
                int newCredits = enteredUserInfoModel.Content.Credits;
                
                //Enter the same museum for the second time, should not add credits
                enteredUserInfoModel = creditController.Post(new CreditModel { Actions = CreditActionType.Actions.ENTERMUSEUM, ID = postMuseum.Content.MuseumID }) as OkNegotiatedContentResult<UserInfoViewModel>;
                Assert.AreEqual(newCredits, enteredUserInfoModel.Content.Credits);
            }
            
        }

        [TestMethod]
        public void Post_EnterMuseum_ShouldNotAddCredits()
        {
            using (TransactionScope transaction = new TransactionScope())
            {
                var museumController = getMuseumController();
                var postMuseum = museumController.Post(new MuseumModel { Name = "DemoMuseumPrivate", Description = "DemoMuseumPrivate", Privacy = Privacy.Levels.PUBLIC }) as OkNegotiatedContentResult<MuseumModel>;
                var creditController = getCreditController(false);

                //Check the old credits
                var userInfoModel = creditController.Get() as OkNegotiatedContentResult<UserInfoViewModel>;
                int oldCredits = userInfoModel.Content.Credits;
                //Enter a museum
                var enteredUserInfoModel = creditController.Post(new CreditModel { Actions = CreditActionType.Actions.ENTERMUSEUM, ID = postMuseum.Content.MuseumID }) as OkNegotiatedContentResult<UserInfoViewModel>;
                Assert.AreEqual(enteredUserInfoModel.Content.Credits , userInfoModel.Content.Credits);
                var newUserInfoModel = creditController.Get() as OkNegotiatedContentResult<UserInfoViewModel>;
                Assert.AreEqual(enteredUserInfoModel.Content.Credits, newUserInfoModel.Content.Credits);
                Assert.AreEqual(enteredUserInfoModel.Content.CreditsAdded, false);
            }

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
            var claim = new Claim("VirtualMuseum", "d32895c4-8d71-4340-884c-89293cc50784");
            var mockIdentity =
                Mock.Of<ClaimsIdentity>(ci => ci.FindFirst(It.IsAny<string>()) == claim);
            fixture.Customize<MuseumController>(c => c
                .OmitAutoProperties()
                .With(x => x.User, Mock.Of<IPrincipal>(ip => ip.Identity == mockIdentity)));
            return fixture.CreateAnonymous<MuseumController>();
        }

        private CreditController getCreditController(bool differentUser)
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            fixture.Customize<HttpRequestMessage>(c => c
            .Without(x => x.Content)
            .Do(x => x.Properties[HttpPropertyKeys.HttpConfigurationKey] =
                new HttpConfiguration()));
            fixture.Customize<CreditController>(c => c
                .OmitAutoProperties()
                .With(x => x.Request, fixture.Create<HttpRequestMessage>()));
            //Simulate this user to execute controller methods
            var claim = new Claim("VirtualMuseum2", "d32895c4-8d71-4340-884c-89293cc50789");
            if (!differentUser)
            {
                claim = new Claim("VirtualMuseum", "d32895c4-8d71-4340-884c-89293cc50784");

            }
            
            var mockIdentity =
                Mock.Of<ClaimsIdentity>(ci => ci.FindFirst(It.IsAny<string>()) == claim);
            fixture.Customize<CreditController>(c => c
                .OmitAutoProperties()
                .With(x => x.User, Mock.Of<IPrincipal>(ip => ip.Identity == mockIdentity)));
            return fixture.CreateAnonymous<CreditController>();
        }        
    }
}