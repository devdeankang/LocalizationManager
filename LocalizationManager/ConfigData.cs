using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using log4net;

namespace LocalizationManager
{
    public enum LocalizationFileType
    {
        NONE,
        CSV,
        XLSX,
    }

    class DefaultConfig
    {
        public readonly string[] categories = 
        { 
            "Basic",
            "Skill",
            "Quest",
            "Tutorial",
            "Item"
        };
        public readonly string[] languages = 
        { 
            "Korean",
            "English",
            "Japanese",
            "ChineseTraditional",
            "ChineseSimplified" 
        };
    }

    public class ExportTemplate
    {
        public string name;

        public List<string> languageList;
    }

    class ConfigData
    {
        public const int LATEST_PROJECT_VERSION = 1;

        public const string SourceLanguage = "Korean";
        private static readonly ILog log = LogManager.GetLogger(typeof(ConfigData));

        public string ProjectName;

        public Dictionary<string, string> ProjectDirs;

        public List<string> Categories;
        public List<string> Languages;

        public List<ExportTemplate> ExportTemplateList;

        public LocalizationFileType LoadFileExtensionType;
        public LocalizationFileType SaveFileExtensionType;

        public int ProjectVersion;

        static Dictionary<string, string> LanguageCodes = new Dictionary<string, string>();

        [JsonIgnore]
        public string Directory;

        public ConfigData(string projectName, string directory)
        {
            ProjectVersion = LATEST_PROJECT_VERSION;
            ProjectName = projectName;
            Directory = directory;

            ProjectDirs = new Dictionary<string, string>
            {
                { "Basic", "Basic" },
                { "All", "TableData" }
            };

            DefaultConfig defaultConfig = new DefaultConfig();
            SetDefaultCategory(defaultConfig.categories);
            SetDefaultLanguage(defaultConfig.languages);
            
            LoadFileExtensionType = LocalizationFileType.CSV;
            SaveFileExtensionType = LocalizationFileType.CSV;

            LanguageCode();
        }

        public void SetDefaultCategory(string[] categories)
        {
            Categories = new List<string>();
            foreach (string c in categories)
            {
                if (c.Equals("Basic")) continue;

                Categories.Add(c);
            }

            Categories.Sort();
            Categories.Insert(0, "Basic");
        }

        public void SetDefaultLanguage(string[] languages)
        {
            Languages = new List<string>();
            foreach (string l in languages)
            {
                if (l.Equals("Korean")) continue;

                Languages.Add(l);
            }

            Languages.Sort();
            Languages.Insert(0, "Korean");
        }

        public void SortCategory()
        {
            Categories.Sort();
            Categories.Remove("Basic");
            Categories.Insert(0, "Basic");
        }

        public void SortLanguages()
        {
            Languages.Sort();
            Languages.Remove("Korean");
            Languages.Insert(0, "Korean");
        }

        public ExportTemplate GetExportTemplate(string templateNmae)
        {
            foreach (ExportTemplate template in ExportTemplateList)
            {
                if (template.name.Equals(templateNmae))
                {                    
                    return template;
                }
            }

            return null;
        }

        public void SaveConfigData()
        {
            string configPath = Path.Combine(Directory, string.Format("{0}.lmp", ProjectName));

            string contents = SerializeConfigData();

            try
            { 
                File.WriteAllText(configPath, contents, Encoding.UTF8);
            }
            catch (Exception e)
            {
                log.Error(e.ToString());               
                
                //저장 실패 시?

            }
        }

        static public ConfigData LoadConfigData(string projectDir, string projectName)
        {
            string projectFile = string.Format("{0}.lmp", projectName);
            string filePath = Path.Combine(projectDir, projectFile);
            if (!File.Exists(filePath))
            {
                return null;
            }

            var configText = File.ReadAllText(filePath);
            ConfigData configData = JsonConvert.DeserializeObject<ConfigData>(configText);
            configData.Directory = projectDir;

            //category, languages 순서 정렬
            configData.SortCategory();
            configData.SortLanguages();

            return configData;
        }

        public bool isExistCategoryName(string category)
        {
            foreach (var c in Categories)
            {
                if (c.Equals(category))
                {
                    return true;
                }
            }

            return false;
        }

        public bool isExistLanguageName(string language)
        {
            foreach (var l in Languages)
            {
                if (l.Equals(language))
                {
                    return true;
                }
            }

            return false;
        }

        private string SerializeConfigData()
        {
            return JsonConvert.SerializeObject(this);
        }

        public static void LanguageCode()
        {
            LanguageCodes.Clear();
            LanguageCodes.Add("korean", "ko");
            LanguageCodes.Add("japanese", "ja");
            LanguageCodes.Add("english", "en");
            LanguageCodes.Add("french", "fr");
            LanguageCodes.Add("thai", "th");
            LanguageCodes.Add("vietnamese", "vi");
            LanguageCodes.Add("portuguese", "pt-PT");
            LanguageCodes.Add("german", "de");
            LanguageCodes.Add("russian", "ru");
            LanguageCodes.Add("spanish", "es");
            LanguageCodes.Add("chinesetraditional", "zh-TW");
            LanguageCodes.Add("chinesesimplified", "zh-CN");
        }

        public static string GetLanguageCode(string Lang)
        {
            if (LanguageCodes.ContainsKey(Lang.ToLower()))
                return LanguageCodes[Lang.ToLower()];
            else
                return null;
        }
    }
}