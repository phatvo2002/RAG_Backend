using Minio;
using Minio.DataModel.Args;
using Nexora.Application.Common.Abstractions.Rag;

namespace Nexora.Infrastructure.Storage;

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
}
