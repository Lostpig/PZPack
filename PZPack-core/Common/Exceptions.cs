namespace PZPack.Core.Exceptions
{
    public class OldVersionEncryptException : Exception
    {
        public OldVersionEncryptException(string? message = default) : base(message) { }
    }
    public class SourceDirectoryIsEmptyException : Exception
    {
        public string DirectoryPath { get; init; }
        public SourceDirectoryIsEmptyException(string dirPath, string? message = default) : base(message)
        {
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
        public OutputFileAlreadyExistsException(string fileName, string? message = default) : base(message)
        {
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
    public class PZSignCheckedException : Exception
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

    public class PZFolderNotFoundException : Exception
    {
        public int Id { get; init; }
        public string Name { get; init; }
        public PZFolderNotFoundException(string name, int id, string? message = default) : base(message)
        {
            Name = name;
            Id = id;
        }
    }
    public class PZFileNotFoundException : Exception
    {
        public int Id { get; init; }
        public string Name { get; init; }
        public PZFileNotFoundException(string name, int id, string? message = default) : base(message)
        {
            Name = name;
            Id = id;
        }
    }

    public class FileInIndexNotEncodeException : Exception
    {
        public int Id { get; init; }
        public string Name { get; init; }
        public FileInIndexNotEncodeException(string name, int id, string? message = default) : base(message)
        {
            Name = name;
            Id = id;
        }
    }
    public class DuplicateNameException : Exception
    {
        public string Name { get; init; }
        public DuplicateNameException(string name, string? message = default) : base(message)
        {
            Name = name;
        }
    }
}
