using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NamePipe.Helper
{
    public sealed class Info
    {
        public const int BufferSize = 2048;
        public readonly byte[] Buffer;
        public readonly StringBuilder StringBuilder;

        public Info()
        {
            Buffer = new byte[BufferSize];
            StringBuilder = new StringBuilder();
        }
    }
}
