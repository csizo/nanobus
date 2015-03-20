using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IO;

namespace System.System.IO
{
    public static class MemoryStreamPool
    {
        private static readonly RecyclableMemoryStreamManager MemoryStreamManager = new RecyclableMemoryStreamManager();

        public static MemoryStream GetStream(string tag)
        {
            return MemoryStreamManager.GetStream(tag);
        }
    }
}
