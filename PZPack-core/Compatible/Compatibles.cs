using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PZPack.Core
{
    internal class Compatibles
    {
        public static bool IsCompatibleVersion(int version)
        {
            return version switch
            {
                1 => true,
                Common.Version => true,
                _ => false
            };
        }
    }
}
