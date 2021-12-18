namespace PZPack.Core
{
    public interface IFolderNode
    {
        string Name { get; }
        string FullName { get; }
        int Id { get; }
        int ParentId { get; }
        IEnumerable<IFolderNode> GetChildren();
    }
}
