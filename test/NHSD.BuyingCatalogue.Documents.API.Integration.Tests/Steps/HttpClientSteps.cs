using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using NHSD.BuyingCatalogue.Documents.API.IntegrationTests.Support;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Assist;

namespace NHSD.BuyingCatalogue.Documents.API.IntegrationTests.Steps
{
    [Binding]
    internal class HttpClientSteps
    {
        private readonly AzureBlobStorageScenarioContext azureBlobStorageScenarioContext;
        private readonly ScenarioContext context;

        public HttpClientSteps(ScenarioContext context, AzureBlobStorageScenarioContext azureBlobStorageScenarioContext)
        {
            this.context = context;
            this.context["RootUrl"] = ServiceUrl.Working;
            this.azureBlobStorageScenarioContext = azureBlobStorageScenarioContext;
        }

        [Then(@"a response with status code ([\d]+) is returned")]
        public void AResponseIsReturned(int code)
        {
            var response = context["Response"] as HttpResponseMessage;
            response.Should().NotBeNull();
            response.StatusCode.Should().Be(code);
        }

        [Then(@"the content of the response is equal to (.*) belonging to (.*)")]
        public async Task ContentOfTheResponseIsEqualTo(string fileName, string solutionId)
        {
            const string sampleDataPath = "SampleData";

            var response = context["Response"] as HttpResponseMessage;
            response.Should().NotBeNull();

            using var responseStream = await response.Content.ReadAsStreamAsync();
            var responseBytes = await GetBytesFromStream(responseStream);

            using var ourFileStream = File.OpenRead(Path.Combine(sampleDataPath, solutionId, fileName));
            var ourFileBytes = await GetBytesFromStream(ourFileStream);

            responseBytes.Should().BeEquivalentTo(ourFileBytes);
        }

        [When("a GET (.*) document request is made for solution (.*)")]
        public async Task GetDocumentAsStreamForSolution(string fileName, string solutionId)
        {
            await GetResponseFromEndpoint(solutionId, fileName);
        }

        [When("a GET documents request is made for solution (.*)")]
        public async Task GetDocumentsForSolution(string solutionId)
        {
            await GetResponseFromEndpoint(solutionId);
        }

        [Given(@"the blob storage service is down")]
        public void GivenTheBlobStorageServiceIsDown()
        {
            context["RootUrl"] = ServiceUrl.Broken;
        }

        [Then(@"the returned response contains the following file names")]
        public async Task ResponseContainsFiles(Table table)
        {
            var elements = table.CreateInstance<FileTable>();

            var response = context["Response"] as HttpResponseMessage;
            response.Should().NotBeNull();

            var content = JToken.Parse(await response.Content.ReadAsStringAsync());
            content.Select(t => t.Value<string>()).Should().BeEquivalentTo(elements.FileNames);
        }

        private static async Task<byte[]> GetBytesFromStream(Stream stream)
        {
            var resultBytes = new byte[stream.Length];
            await stream.ReadAsync(resultBytes, 0, (int)stream.Length);
            return resultBytes;
        }

        private async Task GetResponseFromEndpoint(string solutionId, string fileName = null)
        {
            using var client = new HttpClient();

            var slnId = azureBlobStorageScenarioContext.TryToGetGuidFromSolutionId(solutionId);
            var response = await client.GetAsync(new Uri($"{context["RootUrl"]}/{slnId}/documents/{fileName}"))
                .ConfigureAwait(false);
            context["Response"] = response;
        }

        private static class ServiceUrl
        {
            internal const string Broken = "http://localhost:8091/api/v1/Solutions";
            internal const string Working = "http://localhost:8090/api/v1/Solutions";
        }

        private class FileTable
        {
            public IEnumerable<string> FileNames { get; set; }
        }
    }
}
