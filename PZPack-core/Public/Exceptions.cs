using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PZPack.Core.Exceptions
{
    public class SourceDirectoryIsEmptyException : Exception {
        public string DirectoryPath { get; init; }
        public SourceDirectoryIsEmptyException(string dirPath, string? message = default) : base(message) {
            DirectoryPath = dirPath;
        }
    }
    public class OutputDirectoryIsNotEmptyException : Exception
    {
        public string DirectoryPath { get; init; }
        public OutputDirectoryIsNotEmptyException(string dirPath, string? message = default) : base(message)
        {
            DirectoryPath = dirPath;
        }
    }
    public class OutputFileAlreadyExistsException : Exception
    {
        public string FileName { get; init; }
        public OutputFileAlreadyExistsException(string fileName, string? message = default) : base(message) {
            FileName = fileName;
        }
    }
    public class FileVersionNotCompatiblityException : Exception
    {
        public int Version { get; init; }
        public FileVersionNotCompatiblityException(int version, string? message = default) : base(message)
        {
            Version = version;
        }
    }
    public class PZSignCheckedException: Exception
    {
        public PZSignCheckedException(string? message = default) : base(message) { }
    }
    public class PZPasswordIncorrectException : Exception
    {
        public PZPasswordIncorrectException(string? message = default) : base(message) { }
    }
    public class PathIsNotDirectoryException : Exception
    {
        public string DirectoryPath { get; init; }
        public PathIsNotDirectoryException(string dirPath, string? message = default) : base(message)
        {
            DirectoryPath = dirPath;
        }
    }

}
