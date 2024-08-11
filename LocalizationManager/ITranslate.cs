using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalizationManager
{
    public interface ITranslate
    {
        string Translate(string sourceLang, string targetLang, string sourceText);
    }
}
