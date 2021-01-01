using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BaseApi.Services
{
    public interface IFileDataLayer
    {
        string GetWorkingDirectory();

        Task CreateFile(string path,
                        Stream stream,
                        string bucket = null);

        Task<Stream> ReadFile(string path,
                              string bucket = null);

        Task UpdateFile(string path,
                        Stream stream,
                        bool append = false,
                        string bucket = null);

        Task DeleteFile(string path,
                        string bucket = null);

        Task<bool> FileExists(string path,
                              string bucket = null);

        Task<ICollection<string>> GetAllFiles(string parentPath,
                                              string bucket = null);

        Task CreateDirectory(string path,
                             string bucket = null);

        Task DeleteDirectory(string path,
                             string bucket = null);

        Task<bool> DirectoryExists(string path,
                                   string bucket = null);

        Task<string> StartMultipartUpload(string path,
                                          string bucket = null);

        Task<string> UploadPart(string path,
                                string multipartUploadId,
                                int part,
                                Stream stream,
                                bool last,
                                string bucket = null);

        Task<string> StopMultipartUpload(string path,
                                         string multipartUploadId,
                                         ICollection<FilePart> parts,
                                         string bucket = null);

        Task CreatePathForFile(string filePath,
                               string bucket = null);

        Task<string> ConvertToNativePath(string path,
                                         string bucket = null);

        Task<bool> IsDirectory(string path,
                               string bucket = null);

        Task CopyFile(string sourcePath,
                      string destinationPath,
                      string sourceBucket = null,
                      string destinationBucket = null);

        Task CopyDirectory(string sourcePath,
                           string destinationPath,
                           string sourceBucket = null,
                           string destinationBucket = null);

        string SanitizePath(string path);
    }
}
