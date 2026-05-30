using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Bebrakumpis.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Bebrakumpis.Infrastructure.Services;

public class AzureBlobStorageService(IConfiguration configuration) : IBlobStorageService
{
    private readonly BlobContainerClient _containerClient = new(
        configuration["AzureBlobStorage:ConnectionString"]!,
        configuration["AzureBlobStorage:ContainerName"]!);

    public async Task<string> UploadAsync(Stream content, string contentType, string blobName, CancellationToken cancellationToken = default)
    {
        var blobClient = _containerClient.GetBlobClient(blobName);
        await blobClient.UploadAsync(content, new BlobHttpHeaders { ContentType = contentType }, cancellationToken: cancellationToken);
        return blobClient.Uri.ToString();
    }

    public async Task DeleteByUrlAsync(string blobUrl, CancellationToken cancellationToken = default)
    {
        var blobName = new BlobUriBuilder(new Uri(blobUrl)).BlobName;
        var blobClient = _containerClient.GetBlobClient(blobName);
        await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }
}
