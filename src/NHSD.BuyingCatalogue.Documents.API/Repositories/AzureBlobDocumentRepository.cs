﻿using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Flurl;

namespace NHSD.BuyingCatalogue.Documents.API.Repositories
{
    internal class AzureBlobDocumentRepository : IDocumentRepository
    {
        private readonly BlobContainerClient client;

        public AzureBlobDocumentRepository(BlobContainerClient client)
        {
            this.client = client;
        }

        public async Task<IDocument> DownloadAsync(string solutionId, string documentName)
        {
            try
            {
                return new AzureBlobDocument(
                    await client.GetBlobClient(Url.Combine(solutionId, documentName)).DownloadAsync());
            }
            catch (RequestFailedException e)
            {
                throw new DocumentRepositoryException(e, e.Status);
            }
        }

        public async IAsyncEnumerable<string> GetFileNamesAsync(string directory)
        {
            var all = client.GetBlobsAsync(BlobTraits.All, BlobStates.None, $"{directory}/");

            await foreach (var blob in all)
            {
                yield return Path.GetFileName(blob.Name);
            }
        }
    }
}
