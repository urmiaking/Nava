using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;

using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nava.Common;

namespace Nava.Data.Contracts
{
    public interface IFileRepository
    {
        Task<FileCreationResult> SaveFileAsync(IFormFile file, string path);
        FileDeletionStatus DeleteFile(string path);

        string GetFilePath(string folderName, string fileName);

        string GetFileContentType(string fileName);

        string GetFileExtension(string fileName);
    }
}
