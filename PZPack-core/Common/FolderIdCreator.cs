using System;
using System.Collections.Generic;
namespace PZPack.Core
{
    internal class FolderIdCreator
    {
        public const int RootId = 10000;
        private int Countor;
        public FolderIdCreator()
        {
            Countor = RootId + 1;
        }
        public int Next()
        {
            Countor++;
            return Countor;
        }
    }
}
