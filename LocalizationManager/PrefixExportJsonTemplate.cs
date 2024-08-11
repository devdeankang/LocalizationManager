using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalizationManager
{
    class PrefixExportJsonTemplate
    {
        public string prefixName { get; private set; }
        public SortedDictionary<string, string> exportPrefixDic;

        public PrefixExportJsonTemplate(string prefixName)
        {
            this.prefixName = prefixName;
            exportPrefixDic = new SortedDictionary<string, string>(new CustomComparer());
        }
    }
}
