using System.Collections.ObjectModel;

namespace PZPack.View.Utils
{
    class BlockSizes : ObservableCollection<int>
    {
        public BlockSizes()
        {
            Add(64 * 1024);
            Add(256 * 1024);
            Add(1024 * 1024);
            Add(2 * 1024 * 1024);
            Add(4 * 1024 * 1024);
            Add(8 * 1024 * 1024);
            Add(16 * 1024 * 1024);
            Add(32 * 1024 * 1024);
            Add(64 * 1024 * 1024);
        }
    }
}
