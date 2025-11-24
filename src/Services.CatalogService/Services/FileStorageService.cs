using Microsoft.Extensions.Options;
using Minio.DataModel.Args;
using Minio;
using Services.CatalogService.Config;

namespace Services.CatalogService.Services
{
    public interface IFileStorageService
    {
        Task<string> UploadAsync(IFormFile file, string bucketName, string? folder = null);
    }

    public class MinIOService : IFileStorageService
    {
        private readonly IMinioClient _minio;
        private readonly string _baseUrl;
        private readonly ILogger<MinIOService> _logger;

        public MinIOService(IMinioClient minio, IOptions<MinIOOptions> options, ILogger<MinIOService> logger)
        {
            _minio = minio;
            _baseUrl = options.Value.BaseUrl.TrimEnd('/');
            _logger = logger;
        }

        public async Task<string> UploadAsync(IFormFile file, string bucketName, string? folder = null)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty");

            var extension = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid()}{extension}";
            var objectName = string.IsNullOrEmpty(folder)
                ? fileName
                : $"{folder.TrimEnd('/')}/{fileName}";

            var fullUrl = $"{_baseUrl}/{bucketName}/{objectName}";

            try
            {
                _logger.LogInformation("Starting upload | Bucket: {Bucket} | Object: {ObjectName} | Size: {Size} bytes | Original: {OriginalName}",
                    bucketName, objectName, file.Length, file.FileName);

                // Đảm bảo bucket tồn tại
                var beArgs = new BucketExistsArgs().WithBucket(bucketName);
                bool found = await _minio.BucketExistsAsync(beArgs);
                if (!found)
                {
                    _logger.LogInformation("Bucket {Bucket} not found → creating...", bucketName);
                    var mbArgs = new MakeBucketArgs().WithBucket(bucketName);
                    await _minio.MakeBucketAsync(mbArgs);
                }

                // Upload
                var putArgs = new PutObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
                    .WithStreamData(file.OpenReadStream())
                    .WithObjectSize(file.Length)
                    .WithContentType(file.ContentType);

                var response = await _minio.PutObjectAsync(putArgs);

                _logger.LogInformation("Upload SUCCESS | URL: {Url} ", fullUrl);

                return fullUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Upload FAILED | Bucket: {Bucket} | Object: {ObjectName} | File: {FileName}",
                    bucketName, objectName, file.FileName);
                throw;
            }
        }
    }
}
