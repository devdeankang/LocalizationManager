using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalizationManager
{
    public class ExportZipContent
    {
        public string entryName;
        public byte[] content;

        public ExportZipContent(string fileName, byte[] arr)
        {
            entryName = fileName;
            content = arr;
        }
    }
}
