using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using BaseApi.Services;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using System.Linq;

namespace BaseApi.Storage
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddS3FileDataAccessLayer(this IServiceCollection services)
        {
            services.AddTransient<IFileDataLayer, S3FileDataLayer>();
            return services.AddTransient<FileDataAccessService>();
        }
    }

    public class S3FileDataLayer : IFileDataLayer
    {
        public readonly string BucketName;

        private readonly IAmazonS3 s3Client_;

        private readonly IConfiguration configuration_;

        private readonly int MaxDelete = 1000;

        public S3FileDataLayer(IConfiguration config, IAmazonS3 s3Client)
        {
            configuration_ = config;
            s3Client_ = s3Client;

            BucketName = configuration_.GetSection(Constants.Configuration.Sections.PathsKey)
                .GetValue<string>(Constants.Configuration.Sections.Paths.BucketKey);
        }

        public async Task CreateDirectory(string path, string bucket = null)
        {
            var putObjectRequest = new PutObjectRequest
            {
                BucketName = bucket ?? BucketName,
                Key = path.EndsWith("/") ? path : (path + "/")
            };

            await s3Client_.PutObjectAsync(putObjectRequest);
        }

        public async Task CreateFile(string path, Stream stream, string bucket = null)
        {
            var putObjectRequest = new PutObjectRequest
            {
                BucketName = bucket ?? BucketName,
                Key = path,
                InputStream = stream
            };

            await s3Client_.PutObjectAsync(putObjectRequest);
        }

        public async Task DeleteDirectory(string path, string bucket = null)
        {
            var files = await GetAllFiles(path);

            var total = 0;
            while (total < files.Count)
            {
                var count = 0;
                var deleteObjectsRequest = new DeleteObjectsRequest
                {
                    BucketName = bucket ?? BucketName
                };
                foreach (var file in files.Skip(total))
                {
                    deleteObjectsRequest.AddKey(file);
                    ++total;
                    if (++count == MaxDelete)
                    {
                        break;
                    }
                }
                await s3Client_.DeleteObjectsAsync(deleteObjectsRequest);
            }

            var deleteObjectRequest = new DeleteObjectRequest
            {
                BucketName = bucket ?? BucketName,
                Key = path.EndsWith("/") ? path : (path + "/")
            };

            await s3Client_.DeleteObjectAsync(deleteObjectRequest);
        }

        public async Task DeleteFile(string path, string bucket = null)
        {
            var deleteObjectRequest = new DeleteObjectRequest
            {
                BucketName = bucket ?? BucketName,
                Key = path
            };

            await s3Client_.DeleteObjectAsync(deleteObjectRequest);
        }

        public async Task<bool> DirectoryExists(string path, string bucket = null)
        {
            var request = new ListObjectsRequest
            {
                BucketName = bucket ?? BucketName,
                Prefix = path,
                MaxKeys = 1
            };

            var response = await s3Client_.ListObjectsAsync(request);

            return response.S3Objects.Any();
        }

        public async Task<bool> FileExists(string path, string bucket = null)
        {
            var request = new ListObjectsRequest
            {
                BucketName = bucket ?? BucketName,
                Prefix = path,
                MaxKeys = 1
            };

            var response = await s3Client_.ListObjectsAsync(request);

            return response.S3Objects.Any();
        }

        public async Task<ICollection<string>> GetAllFiles(string parentPath, string bucket = null)
        {
            var request = new ListObjectsRequest
            {
                BucketName = bucket ?? BucketName,
                Prefix = parentPath,
            };

            var response = await s3Client_.ListObjectsAsync(request);

            return response.S3Objects.Select(o => o.Key).ToArray();
        }

        public async Task<Stream> ReadFile(string path, string bucket = null)
        {
            var request = new GetObjectRequest()
            {
                BucketName = bucket ?? BucketName,
                Key = path,
            };

            var response = await s3Client_.GetObjectAsync(request);
            using (var fileStream = response.ResponseStream)
            {
                var outMemoryStream = new MemoryStream();
                await fileStream.CopyToAsync(outMemoryStream);
                outMemoryStream.Position = 0;
                return outMemoryStream;
            }
        }

        public async Task<string> StartMultipartUpload(string path, string bucket = null)
        {
            var multipartUploadStartRequest = new InitiateMultipartUploadRequest()
            {
                BucketName = bucket ?? BucketName,
                Key = path
            };

            var response = await s3Client_.InitiateMultipartUploadAsync(multipartUploadStartRequest);
            return response.UploadId;
        }

        public async Task<string> UploadPart(string path, string multipartUploadId, int part, Stream stream, bool last, string bucket = null)
        {
            using (var memStream = new MemoryStream())
            {
                await stream.CopyToAsync(memStream);
                memStream.Position = 0;

                var multipartUploadStartRequest = new UploadPartRequest()
                {
                    BucketName = bucket ?? BucketName,
                    Key = path,
                    UploadId = multipartUploadId,
                    InputStream = memStream,
                    PartNumber = part + 1,
                    IsLastPart = last
                };

                var response = await s3Client_.UploadPartAsync(multipartUploadStartRequest);
                return response.ETag;
            }
        }

        public async Task<string> StopMultipartUpload(string path, string multipartUploadId, ICollection<FilePart> parts, string bucket = null)
        {
            var multipartUploadStopRequest = new CompleteMultipartUploadRequest()
            {
                BucketName = bucket ?? BucketName,
                Key = path,
                UploadId = multipartUploadId,
                PartETags = parts.Select(p => new PartETag(p.Index + 1, p.ETag)).ToList()
            };

            await s3Client_.CompleteMultipartUploadAsync(multipartUploadStopRequest);
            return path;
        }

        public async Task UpdateFile(string path, Stream stream, bool append = false, string bucket = null)
        {
            if (!append)
            {
                await CreateFile(path, stream, bucket);
                return;
            }

            using (var writeStream = await ReadFile(path, bucket))
            {
                writeStream.Seek(0, SeekOrigin.End);
                await stream.CopyToAsync(writeStream);
                writeStream.Seek(0, SeekOrigin.Begin);
                await CreateFile(path, writeStream, bucket);
            }
        }

        public async Task CreatePathForFile(string filePath, string bucket = null)
        {
            //Not necessary for s3
            await Task.CompletedTask;
        }

        public async Task<string> ConvertToNativePath(string path, string bucket = null)
        {
            return await Task.FromResult(path.Replace('\\', '/'));
        }

        public string GetWorkingDirectory()
        {
            return "";
        }

        public async Task<bool> IsDirectory(string path, string bucket = null)
        {
            return await Task.FromResult(path.EndsWith("/"));
        }

        public async Task CopyFile(string sourcePath, string destinationPath, string sourceBucket = null, string destinationBucket = null)
        {
            using (var sourceStream = await ReadFile(sourcePath, sourceBucket))
            {
                await CreateFile(destinationPath, sourceStream, destinationBucket);
            }
        }

        public async Task CopyDirectory(string sourcePath, string destinationPath, string sourceBucket = null, string destinationBucket = null)
        {
            var sourceFiles = await GetAllFiles(sourcePath, sourceBucket);
            foreach (var sourceFile in sourceFiles)
            {
                var destinationFile = sourceFile.Remove(0, sourcePath.Length).Insert(0, destinationPath);
                if (await IsDirectory(sourceFile, sourceBucket))
                {
                    await CreateDirectory(destinationFile, destinationBucket);
                }
                else
                {
                    await CopyFile(sourceFile, destinationFile, sourceBucket, destinationBucket);
                }
            }
        }

        public string SanitizePath(string path)
        {
            return path.Replace("\\", "/");
        }
    }
}