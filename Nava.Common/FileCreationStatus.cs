using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nava.Common
{
    public class FileCreationResult
    {
        public FileCreationResult(FileCreationStatus fileCreationStatus, string fileName)
        {
            FileCreationStatus = fileCreationStatus;
            FileName = fileName;
        }
        public FileCreationStatus FileCreationStatus { get; set; }
        public string FileName { get; set; }
    }
    public enum FileCreationStatus
    {
        Success = 0,
        Failed = 1,
        NotDefined = 2
    }

    public enum FileDeletionStatus
    {
        Success = 0,
        Failed = 1
    }
}
