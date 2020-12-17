using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using BaseApi.Services;

namespace BaseApi.Storage
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddWindowsFileDataAccessLayer(this IServiceCollection services)
        {
            services.AddTransient<FileDataLayerInterface, WindowsFileDataLayerInterface>();
            return services.AddTransient<FileDataAccessService>();
        }
    }
    public class WindowsFileDataLayerInterface : FileDataLayerInterface
    {
        public static readonly string WorkingDirectory = Path.GetTempPath();

        public readonly string PartsSubDirectory = "parts";

        public static string RootPath(string path)
        {
            return Path.IsPathRooted(path) ? path : Path.Combine(WorkingDirectory, path);
        }

        public string GetWorkingDirectory()
        {
            return SanitizePath(WorkingDirectory);
        }

        public async Task CreateDirectory(string path, string bucket = null)
        {
            path = RootPath(path);
            Directory.CreateDirectory(path);
            await Task.CompletedTask;
        }

        public async Task CreateFile(string path, Stream stream, string bucket = null)
        {
            path = RootPath(path);

            using (var writeStream = File.Create(path))
            {
                await stream.CopyToAsync(writeStream);
            }
        }

        public async Task DeleteDirectory(string path, string bucket = null)
        {
            path = RootPath(path);
            Directory.Delete(path, true);
            await Task.CompletedTask;
        }

        public async Task DeleteFile(string path, string bucket = null)
        {
            path = RootPath(path);
            File.Delete(path);
            await Task.CompletedTask;
        }

        public async Task<bool> DirectoryExists(string path, string bucket = null)
        {
            path = RootPath(path);
            return await Task.FromResult(Directory.Exists(path));
        }

        public async Task<bool> FileExists(string path, string bucket = null)
        {
            path = RootPath(path);
            return await Task.FromResult(File.Exists(path));
        }

        public async Task<ICollection<string>> GetAllFiles(string parentPath, string bucket = null)
        {
            parentPath = RootPath(parentPath);
            return await Task.FromResult(Directory.GetFileSystemEntries(parentPath, "*", SearchOption.AllDirectories));
        }

        public async Task<Stream> ReadFile(string path, string bucket = null)
        {
            path = RootPath(path);
            using (var fileStream = File.OpenRead(path))
            {
                var outMemoryStream = new MemoryStream();
                await fileStream.CopyToAsync(outMemoryStream);
                outMemoryStream.Position = 0;
                return outMemoryStream;
            }
        }

        public async Task<string> StartMultipartUpload(string path, string bucket = null)
        {
            var uploadId = Guid.NewGuid().ToString();
            path = $"{RootPath(path)}_{uploadId}_{PartsSubDirectory}";

            var uploadFolderExists = await DirectoryExists(path);
            if (uploadFolderExists)
            {
                throw new Exception($"multipart upload directory already exists {path}");
            }

            await CreateDirectory(path);

            return uploadId;
        }

        public async Task<string> UploadPart(string path, string multipartUploadId, int part, Stream stream, bool last, string bucket = null)
        {
            path = $"{RootPath(path)}_{multipartUploadId}_{PartsSubDirectory}";

            var uploadFolderExists = await DirectoryExists(path);
            if (!uploadFolderExists)
            {
                throw new Exception($"multipart upload directory doesnt exist {path}");
            }

            var partPath = $"{path}/{part.ToString()}";
            await CreateFile(partPath, stream);
            return "NA";
        }

        public async Task<string> StopMultipartUpload(string path, string multipartUploadId, ICollection<FilePart> parts, string bucket = null)
        {
            var partsPath = $"{RootPath(path)}_{multipartUploadId}_{PartsSubDirectory}";
            path = RootPath(path);

            var uploadFolderExists = await DirectoryExists(partsPath);
            if (!uploadFolderExists)
            {
                throw new Exception($"multipart upload directory doesnt exist {partsPath}");
            }

            var allParts = await GetAllFiles(partsPath);
            foreach (var part in allParts)
            {
                await UpdateFile(path, await ReadFile(part), true);
            }

            await DeleteDirectory(partsPath);

            return path;
        }

        public async Task UpdateFile(string path, Stream stream, bool append = false, string bucket = null)
        {
            path = RootPath(path);
            if (!append)
            {
                await CreateFile(path, stream);
                return;
            }

            using (var writeStream = File.OpenWrite(path))
            {
                writeStream.Seek(0, SeekOrigin.End);
                await stream.CopyToAsync(writeStream);
            }
        }

        public async Task CreatePathForFile(string filePath, string bucket = null)
        {
            filePath = RootPath(filePath);
            var parentPath = Directory.GetParent(filePath).FullName;
            if ((await DirectoryExists(parentPath)))
            {
                return;
            }
            await CreateDirectory(parentPath);
        }

        public async Task<string> ConvertToNativePath(string path, string bucket = null)
        {
            return await Task.FromResult(RootPath(path.Replace('/', '\\')));
        }

        public async Task<bool> IsDirectory(string path, string bucket = null)
        {
            return await DirectoryExists(path);
        }

        public async Task CopyFile(string sourcePath, string destinationPath, string sourceBucket = null, string destinationBucket = null)
        {
            File.Copy(sourcePath, destinationPath);
            await Task.CompletedTask;
        }

        public async Task CopyDirectory(string sourcePath, string destinationPath, string sourceBucket = null, string destinationBucket = null)
        {
            // destinationPath = SanitizePath(Path.Combine(GetWorkingDirectory(), destinationPath));
            //Now Create all of the directories
            foreach (string dirPath in Directory.GetDirectories(sourcePath, "*",
                SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Remove(0, sourcePath.Length).Insert(0, destinationPath));
            }

            //Copy all the files & Replaces any files with the same name
            foreach (string sourceFile in Directory.GetFiles(sourcePath, "*",
                SearchOption.AllDirectories))
            {
                var destinationFile = SanitizePath(sourceFile.Remove(0, sourcePath.Length).Insert(0, destinationPath));
                var cleanedSourceFile = SanitizePath(sourceFile);
                File.Copy(cleanedSourceFile, destinationFile, true);
            }

            await Task.CompletedTask;
        }

        public string SanitizePath(string path)
        {
            return path.Replace("\\", "/");
        }
    }
}
