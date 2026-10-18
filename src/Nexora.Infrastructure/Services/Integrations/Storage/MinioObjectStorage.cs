using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using Nexora.Application.Common.Abstractions.Documents;
using Nexora.Application.Common.Abstractions.Rag;

namespace Nexora.Infrastructure.Services.Integrations.Storage;

internal sealed class MinioObjectStorage(IMinioClient client) : IObjectStorage
{
    public async Task PutAsync(string bucket, string objectKey, Stream content, long sizeBytes, string contentType, CancellationToken cancellationToken)
    {
        var exists = await client.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucket), cancellationToken);
        if (!exists)
        {
            await client.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucket), cancellationToken);
        }

        var args = new PutObjectArgs().WithBucket(bucket).WithObject(objectKey).WithStreamData(content).WithObjectSize(sizeBytes).WithContentType(contentType);
        await client.PutObjectAsync(args, cancellationToken);
    }

    public async Task<Stream> GetAsync(string bucket, string objectKey, CancellationToken cancellationToken)
    {
        var output = new MemoryStream();
        try
        {
            var args = new GetObjectArgs().WithBucket(bucket).WithObject(objectKey).WithCallbackStream((stream, token) => stream.CopyToAsync(output, token));
            await client.GetObjectAsync(args, cancellationToken);
            output.Position = 0;
            return output;
        }
        catch
        {
            await output.DisposeAsync();
            throw;
        }
    }

    public async Task DeleteAsync(string bucket, string objectKey, CancellationToken cancellationToken)
    {
        try
        {
            await client.RemoveObjectAsync(new RemoveObjectArgs().WithBucket(bucket).WithObject(objectKey), cancellationToken);
        }
        catch (BucketNotFoundException)
        {
            // The object is already absent when its bucket no longer exists.
        }
    }
}
