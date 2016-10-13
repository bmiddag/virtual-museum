using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoMoq;
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
using VirtualMuseumAPI.Controllers;
using VirtualMuseumAPI.Models;

namespace VirtualMuseumAPI.Tests
{
    [TestClass]
    public class TestArtWorkFilterController
    {
        [TestMethod]
        public void PostArtWorkFilter_ShouldReturnCorrectFilter()
        {
            using (TransactionScope transaction = new TransactionScope())
            {
                var testFilters = GetTestFilters();
                var filterController = getArtWorkFilterController();
                filterController.Post(testFilters[0]);
                //Should now be connected to this user
                var connectedFilters = filterController.Get();
                bool isAssigned = false;
                foreach (ArtWorkFilterModel model in connectedFilters.ArtWorkFilters)
                {
                    if (model.Pairs.Any(a => a.Name == testFilters[0].Pairs.ElementAt(0).Name && a.Value == testFilters[0].Pairs.ElementAt(0).Value))
                    {
                        isAssigned = true;
                    }
                }
                Assert.IsTrue(isAssigned);

                //Make a new where the key does already exist
                filterController.Post(testFilters[1]);
                //Should now be connected to this user
                connectedFilters = filterController.Get();
                isAssigned = false;
                foreach (ArtWorkFilterModel model in connectedFilters.ArtWorkFilters)
                {
                    if (model.Pairs.Any(a => a.Name == testFilters[1].Pairs.ElementAt(0).Name && a.Value == testFilters[1].Pairs.ElementAt(0).Value))
                    {
                        isAssigned = true;
                    }
                }
                Assert.IsTrue(isAssigned);

            }
        }

        [TestMethod]
        public void SearchArtWorkFilter_ShouldReturnCorrectFilter()
        {
            using (TransactionScope transaction = new TransactionScope())
            {
               
            }
        }

        [TestMethod]
        public void DeleteArtWorkFilter_ShouldNotContainFilter()
        {
            using (TransactionScope transaction = new TransactionScope())
            {
                
            }
        }

        private List<ArtWorkFilterModel> GetTestFilters()
        {
            var testfilters = new List<ArtWorkFilterModel>();
            var pairs = new List<KeyValuePair>();
            pairs.Add((new KeyValuePair() { Name = "testkey1", Value = "testvalue1" }));
            testfilters.Add(new ArtWorkFilterModel() { ArtWorkID=-1, ArtistID = 1, Pairs = pairs }); 
            var pairs2 = new List<KeyValuePair>();
            pairs2.Add((new KeyValuePair() { Name = "testkey1", Value = "testvalue2" }));
            testfilters.Add(new ArtWorkFilterModel() { ArtWorkID=-1, ArtistID = 1, Pairs = pairs2 }); 
            return testfilters;
        }

        private ArtWorkFilterController getArtWorkFilterController()
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            fixture.Customize<HttpRequestMessage>(c => c
            .Without(x => x.Content)
            .Do(x => x.Properties[HttpPropertyKeys.HttpConfigurationKey] =
                new HttpConfiguration()));
            fixture.Customize<ArtWorkFilterController>(c => c
                .OmitAutoProperties()
                .With(x => x.Request, fixture.Create<HttpRequestMessage>()));
            //Simulate this user to execute controller methods
            var claim = new Claim("VirtualMuseum", "d32895c4-8d71-4340-884c-89293cc50784");
            var mockIdentity =
                Mock.Of<ClaimsIdentity>(ci => ci.FindFirst(It.IsAny<string>()) == claim);
            fixture.Customize<ArtWorkFilterController>(c => c
                .OmitAutoProperties()
                .With(x => x.User, Mock.Of<IPrincipal>(ip => ip.Identity == mockIdentity)));
            return fixture.CreateAnonymous<ArtWorkFilterController>();
        }
    }
}
