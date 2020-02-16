using System.Threading.Tasks;
using TechTalk.SpecFlow;

namespace NHSD.BuyingCatalogue.Documents.API.IntegrationTests.Support
{
    internal class IntegrationSetUpFixture
    {
        [Binding]
        public class IntegrationSetupFixture
        {
            private readonly AzureBlobStorageScenarioContext scenarioContext;

            public IntegrationSetupFixture(AzureBlobStorageScenarioContext scenarioContext)
            {
                this.scenarioContext = scenarioContext;
            }

            [AfterScenario]
            public async Task ClearBlobContainer()
            {
                await scenarioContext.ClearStorage();
            }
        }
    }
}
