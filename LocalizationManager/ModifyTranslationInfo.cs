using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalizationManager
{
    public class ModifyTranslationInfo
    {
        public string Category { get; private set; }
        public int Partial { get; private set; }
        public string Language { get; private set; }
        public string Key { get; private set; } // 중복되는 번역 위함
        public string Translation { get; private set; }

        public ModifyTranslationInfo(string category, int partial, string language, string key, string translation)
        {
            Category = category;
            Partial = partial;
            Language = language;
            Key = key;
            Translation = translation;
        }
    }
}
