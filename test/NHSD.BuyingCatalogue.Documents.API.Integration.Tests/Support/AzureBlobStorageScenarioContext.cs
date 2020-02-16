using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using FluentAssertions;

namespace NHSD.BuyingCatalogue.Documents.API.IntegrationTests.Support
{
    internal class AzureBlobStorageScenarioContext
    {
        private const string ConnectionString = "UseDevelopmentStorage=true";
        private const string ContainerName = "container-1";
        private const string SampleDataPath = "SampleData";

        private readonly BlobContainerClient blobContainer;
        private readonly Dictionary<string, string> solutionIdsToGuids = new Dictionary<string, string>();

        public AzureBlobStorageScenarioContext()
        {
            var blobServiceClient = new BlobServiceClient(ConnectionString);
            blobContainer = blobServiceClient.GetBlobContainerClient(ContainerName);
        }

        public async Task ClearStorage()
        {
            foreach (var blob in solutionIdsToGuids.Values.SelectMany(directory => blobContainer.GetBlobs(prefix: directory)))
            {
                await blobContainer.DeleteBlobAsync(blob.Name);
            }
        }

        public async Task InsertFileToStorage(string solutionId, string fileName)
        {
            InsertIntoMapping(solutionId);
            var blobClient = blobContainer.GetBlobClient(Path.Combine(solutionIdsToGuids[solutionId], fileName));
            using var uploadFileStream = File.OpenRead(Path.Combine(SampleDataPath, solutionId, fileName));
            var response = await blobClient
                .UploadAsync(uploadFileStream, new BlobHttpHeaders())
                .ConfigureAwait(false);

            response.GetRawResponse().Status.Should().Be(201);
        }

        public string TryToGetGuidFromSolutionId(string solutionId)
        {
            return solutionIdsToGuids.TryGetValue(solutionId, out string solutionIdAsGuid) ? solutionIdAsGuid : Guid.Empty.ToString();
        }

        private void InsertIntoMapping(string solutionId)
        {
            if (!solutionIdsToGuids.ContainsKey(solutionId))
            {
                solutionIdsToGuids[solutionId] = Guid.NewGuid().ToString();
            }
        }
    }
}
