using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Windows.Data;

namespace LocalizationManager
{
    public enum TranslationStatus
    {
        STATUS_NONE,           //None
        STATUS_TBT,            //To-be-translated
        STATUS_PT,             //Partially Translated
        STATUS_TRANSLATED,     //Translated
        STATUS_PRE_TRANSLATED, //PreTranslated
    }

    public enum ExportFileType
    {
        NONE,
        REGULAR_FILE,           //KEY, Korean, Translation, Tag, Status, Desc
        EXTRA_FILE              //KEY, Korean, Translation, Tag, Status, Desc, Category, Partial
    }

    public enum ImportFileType
    {
        [Description("Only translations")]
        OnlyTargetLang = 0,
        [Description("All languages")]
        AllLangSyncTransInit
    }

    public enum ToolLineIndex
    {
        // key, status, sourceText, translationLang, Tag, Desc
        KEY = 0,
        STATUS,
        SOURCETEXT,
        TRANSLATION,
        TAG,
        DESC
    }


    class LocalizationData
    {
        public string projectName;
        public bool _isSynchronized = false;
        public bool _isPartiallyExport = false;

        //전체 로컬 데이터를 category, partial, language 순으로 저장
        public Dictionary<string, CategoryInfo> categoryInfos = new Dictionary<string, CategoryInfo>();

        //전체 중복키를 저장
        public Dictionary<string, List<TBTLine>> dupKeyDic = new Dictionary<string, List<TBTLine>>();

        public bool GetLocalDataModified()
        {
            foreach (CategoryInfo category in categoryInfos.Values)
            {
                if (category.isModified == true)
                {
                    return true;
                }
            }

            return false;
        }

        public void SetIsModified(bool isModified, string category, int partial, string language)
        {
            categoryInfos[category].partialInfos[partial].languageInfos[language].isModified = isModified;

            if (isModified == true)
            {
                categoryInfos[category].isModified = isModified;
                categoryInfos[category].partialInfos[partial].isModified = isModified;
            }
            else
            {
                //partial set
                bool isPartialModified = false;
                foreach (var languageInfo in categoryInfos[category].partialInfos[partial].languageInfos)
                {
                    if (languageInfo.Value.isModified == true)
                    {
                        isPartialModified = true;
                        break;
                    }
                }
                categoryInfos[category].partialInfos[partial].isModified = isPartialModified;

                //category set
                bool isCategoryModified = false;
                foreach (PartialInfo partialInfo in categoryInfos[category].partialInfos)
                {
                    if (partialInfo.isModified == true)
                    {
                        isCategoryModified = true;
                        break;
                    }
                }
                categoryInfos[category].isModified = isCategoryModified;
            }
        }

        public bool GetLocalDataPartiallyExport()
        {
            foreach (CategoryInfo category in categoryInfos.Values)
            {
                if (category.isExport == true)
                {
                    return true;
                }
            }

            return false;
        }

        public void ClearIsExportInfo()
        {
            foreach (var category in categoryInfos)
            {
                category.Value.isExport = false;

                foreach (PartialInfo partial in category.Value.partialInfos)
                {
                    partial.isExport = false;
                }
            }
        }

        public void SetIsExport(bool isExport, string category, int partial)
        {
            categoryInfos[category].partialInfos[partial].isExport = isExport;

            if (isExport == true)
            {
                categoryInfos[category].isExport = isExport;
            }
            else
            {
                //category set
                bool isCategoryExport = false;
                foreach (PartialInfo partialInfoInfo in categoryInfos[category].partialInfos)
                {
                    if (partialInfoInfo.isExport == true)
                    {
                        isCategoryExport = true;
                        break;
                    }
                }
                categoryInfos[category].isExport = isCategoryExport;
            }
        }

        public void InitLocalizationData(string projectName = null)
        {
            if (!string.IsNullOrEmpty(projectName))
                this.projectName = projectName;

            _isSynchronized = false;

            foreach (var categoryInfo in categoryInfos)
            {
                foreach (PartialInfo partialInfo in categoryInfo.Value.partialInfos)
                {
                    foreach (var languageInfo in partialInfo.languageInfos)
                    {
                        languageInfo.Value.fileInfo.Clear();
                        languageInfo.Value.tagKeysDic.Clear();
                        languageInfo.Value.translationStatusDic.Clear();
                    }
                    partialInfo.languageInfos.Clear();
                }
                categoryInfo.Value.partialInfos.Clear();
            }
            categoryInfos.Clear();
        }

        void AddNewInfo(string category, int partial, string language)
        {
            lock (categoryInfos)
            {
                AddCategoryInfo(category);
                AddPartialInfo(category, partial);
                AddLanguageInfo(category, partial, language);
            }
        }

        void AddCategoryInfo(string category)
        {
            if (!categoryInfos.ContainsKey(category))
            {
                categoryInfos.Add(category, new CategoryInfo(category));
            }
        }

        void AddPartialInfo(string category, int partial)
        {
            List<PartialInfo> partialInfos = categoryInfos[category].partialInfos;
            while (partialInfos.Count <= partial)
            {
                partialInfos.Add(new PartialInfo(category, partialInfos.Count));
            }
        }

        void AddLanguageInfo(string category, int partial, string language)
        {
            if (!categoryInfos[category].partialInfos[partial].languageInfos.ContainsKey(language))
            {
                categoryInfos[category].partialInfos[partial].languageInfos.Add(language, new LangFile(category, partial, language));
            }
        }

        public void AddFileInfo(string category, int partial, string language, Dictionary<string, FileLine> fileInfo)
        {
            AddNewInfo(category, partial, language);

            LangFile languageInfo = categoryInfos[category].partialInfos[partial].languageInfos[language];

            languageInfo.fileName = GetFileName(category, partial, language);
            languageInfo.fileInfo = fileInfo;

            foreach(KeyValuePair<string, FileLine> fileLinePair in fileInfo)
            {
                fileLinePair.Value.file = languageInfo;
            }
        }

        public static string GetFileName(string category, int partial, string language, LocalizationFileType fileType = LocalizationFileType.NONE)
        {
            string name;

            if (partial == 0)
            {
                name = string.Format("{0}{1}", category, language);
            }
            else
            {
                name = string.Format("{0}{1}{2}", category, partial, language);
            }

            switch (fileType)
            {
                case LocalizationFileType.NONE:
                    return name;
                case LocalizationFileType.CSV:
                    return string.Format("{0}{1}.csv", LocalizationDataManager.LOCALIZATION_FILENAME_PREFIX, name);
                case LocalizationFileType.XLSX:
                    return string.Format("{0}{1}.xlsx", LocalizationDataManager.LOCALIZATION_FILENAME_PREFIX, name);
                default:
                    return name;
            }
        }

        //TranslationStatus Set
        public void SetTranslationStatus(string category, int partial, string key, string translationStatus)
        {
            categoryInfos[category].partialInfos[partial].languageInfos["Korean"].fileInfo[key].translationStatus = translationStatus;
        }

        public bool isExistKey(string key, ref string category, ref int partial)
        {
            foreach (KeyValuePair<string, CategoryInfo> categoryPair in categoryInfos)
            {
                CategoryInfo categoryInfo = categoryPair.Value;
                foreach (PartialInfo partialInfo in categoryInfo.partialInfos)
                {
                    //Korean 파일을 읽음
                    Dictionary<string, FileLine> koreanInfo = partialInfo.languageInfos["Korean"].fileInfo;

                    foreach (KeyValuePair<string, FileLine> krLine in koreanInfo)
                    {
                        if (krLine.Value.key.Equals(key))
                        {
                            category = categoryInfo._category;
                            partial = partialInfo._partial;

                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public string GetImportTypeDesc(ImportFileType fileType)
        {
            return fileType.GetType().GetMember(fileType.ToString())[0].GetCustomAttribute<DescriptionAttribute>().Description;
        }

        public void SwapKey(string originCategory, int originPartial, string renameCategory, int renamePartial, string originalKey, string newKey)
        {
            foreach (string language in LocalizationDataManager.Instance.configData.Languages)
            {
                if (categoryInfos[originCategory].partialInfos[originPartial].languageInfos[language].fileInfo.ContainsKey(originalKey) == false) continue;
                if (categoryInfos[renameCategory].partialInfos[renamePartial].languageInfos[language].fileInfo.ContainsKey(newKey) == false) continue;

                var originValue = categoryInfos[originCategory].partialInfos[originPartial].languageInfos[language].fileInfo[originalKey];
                originValue.key = newKey;
                var renameValue = categoryInfos[renameCategory].partialInfos[renamePartial].languageInfos[language].fileInfo[newKey];
                renameValue.key = originalKey;

                categoryInfos[originCategory].partialInfos[originPartial].languageInfos[language].fileInfo.Remove(originalKey);
                categoryInfos[renameCategory].partialInfos[renamePartial].languageInfos[language].fileInfo.Remove(newKey);

                categoryInfos[renameCategory].partialInfos[renamePartial].languageInfos[language].fileInfo[originalKey] = renameValue;
                categoryInfos[originCategory].partialInfos[originPartial].languageInfos[language].fileInfo[newKey] = originValue;

                SetIsModified(true, originCategory, originPartial, language);
                SetIsModified(true, renameCategory, renamePartial, language);
            }

            return;
        }

        public void RenameKey(string category, int partial, string originalKey, string newKey)
        {
            foreach (string language in LocalizationDataManager.Instance.configData.Languages)
            {
                if (categoryInfos[category].partialInfos[partial].languageInfos[language].fileInfo.ContainsKey(originalKey) == false) continue;

                var value = categoryInfos[category].partialInfos[partial].languageInfos[language].fileInfo[originalKey];
                value.key = newKey;
                //value.isModified = true;
                categoryInfos[category].partialInfos[partial].languageInfos[language].fileInfo.Remove(originalKey);
                categoryInfos[category].partialInfos[partial].languageInfos[language].fileInfo[newKey] = value;

                SetIsModified(true, category, partial, language);
            }

            ////TBT Dic 변경
            //var originalTBTKey = GetTBTKey(category, partial, originalKey);
            //var newTBTKey = GetTBTKey(category, partial, newKey);

            //if (TBTDic.ContainsKey(originalTBTKey))
            //{
            //    var tbtValue = TBTDic[originalTBTKey];
            //    tbtValue.key = newKey;
            //    TBTDic.Remove(originalTBTKey);
            //    TBTDic[newTBTKey] = tbtValue;
            //}

            return;
        }

        public void FixKorean(string category, int partial, string key, string korean)
        {
            foreach (string language in LocalizationDataManager.Instance.configData.Languages)
            {
                var value = categoryInfos[category].partialInfos[partial].languageInfos[language].fileInfo[key];
                value.sourceText = korean;
                //value.isModified = true;

                SetIsModified(true, category, partial, language);
            }

            ////TBT Dic 변경
            //var originalTBTKey = GetTBTKey(category, partial, key);

            //if (TBTDic.ContainsKey(originalTBTKey))
            //{
            //    var tbtValue = TBTDic[originalTBTKey];
            //    tbtValue.korean = korean;
            //}

            return;
        }

        public void ModifyTranslation(string category, int partial, string language, string key, string modifyTranslation)
        {
            var fileLine = categoryInfos[category].partialInfos[partial].languageInfos[language].fileInfo[key];
            fileLine.translation = modifyTranslation;

            SetIsModified(true, category, partial, language);

            return;
        }

        public void RemoveKey(string category, int partial, string originalKey)
        {
            foreach (string language in LocalizationDataManager.Instance.configData.Languages)
            {
                categoryInfos[category].partialInfos[partial].languageInfos[language].fileInfo.Remove(originalKey);

                SetIsModified(true, category, partial, language);
            }

            ////TBT Dic 변경
            //TBTDic.Remove(GetTBTKey(category, partial, originalKey));

            return;
        }

        public bool RemoveTag(string tag)
        {
            bool isRemoveTag = false;
            foreach (KeyValuePair<string, CategoryInfo> categoryPair in categoryInfos)
            {
                foreach (PartialInfo partialInfo in categoryPair.Value.partialInfos)
                {
                    foreach (KeyValuePair<string, LangFile> languagePair in partialInfo.languageInfos)
                    {
                        bool isFileModified = false;
                        foreach (KeyValuePair<string, FileLine> line in languagePair.Value.fileInfo)
                        {
                            if (line.Value.tag.Equals(tag))
                            {
                                line.Value.tag = string.Empty;
                                //line.Value.isModified = true;
                                isFileModified = true;

                                isRemoveTag = true;
                            }
                        }

                        if (isFileModified)
                        {
                            SetIsModified(true, categoryPair.Key, partialInfo._partial, languagePair.Key);
                        }
                    }
                }
            }

            return isRemoveTag;
        }

        public bool RemoveTagByKey(string key)
        {
            bool isRemoveTag = false;
            foreach (KeyValuePair<string, CategoryInfo> categoryPair in categoryInfos)
            {
                foreach (PartialInfo partialInfo in categoryPair.Value.partialInfos)
                {
                    foreach (KeyValuePair<string, LangFile> languagePair in partialInfo.languageInfos)
                    {
                        bool isFileModified = false;
                        foreach (KeyValuePair<string, FileLine> line in languagePair.Value.fileInfo)
                        {
                            if (String.CompareOrdinal(line.Key, key) == 0)
                            {
                                line.Value.tag = string.Empty;
                                //line.Value.isModified = true;
                                isFileModified = true;
                                isRemoveTag = true;
                            }
                        }

                        if (isFileModified)
                        {
                            SetIsModified(true, categoryPair.Key, partialInfo._partial, languagePair.Key);
                        }
                    }
                }
            }

            return isRemoveTag;
        }

        public int MoveKey(string key, string newCategory, int newPartial)
        {
            string originCategory = string.Empty;
            int originPartial = -1;

            //key가 존재하지 않으면 return
            if (!isExistKey(key, ref originCategory, ref originPartial)) return 1;

            //같은 category, partial로 옮길 수 없음
            if (originCategory.Equals(newCategory) && originPartial == newPartial) return 2;

            foreach (string language in LocalizationDataManager.Instance.configData.Languages)
            {
                if (categoryInfos[originCategory].partialInfos[originPartial].languageInfos[language].fileInfo.ContainsKey(key))
                {
                    var value = categoryInfos[originCategory].partialInfos[originPartial].languageInfos[language].fileInfo[key];
                    categoryInfos[originCategory].partialInfos[originPartial].languageInfos[language].fileInfo.Remove(key);
                    categoryInfos[newCategory].partialInfos[newPartial].languageInfos[language].fileInfo[key] = value;

                    SetIsModified(true, originCategory, originPartial, language);
                    SetIsModified(true, newCategory, newPartial, language);
                }
            }

            ////TBT Dic 변경
            //var tbtKey = GetTBTKey(originCategory, originPartial, key);
            //if (TBTDic.ContainsKey(tbtKey))
            //{
            //    var tbtValue = TBTDic[tbtKey];
            //    tbtValue.category = newCategory;
            //    tbtValue.partial = newPartial;
            //}

            return 0;
        }

        public static string GetTBTKey(string category, int partial, string key)
        {
            return string.Format("{0}_{1}{2}", key, category, partial);
        }
    }

    public class DataInfo
    {
        public string _category = string.Empty;
        public int _partial = -1;
        public string _language = string.Empty;

        public int totalKeys;
        public int dupKeys;
        public Dictionary<string, Dictionary<string, FileLine>> tagKeysDic = new Dictionary<string, Dictionary<string, FileLine>>();
        public Dictionary<string, Dictionary<string, FileLine>> translationStatusDic = new Dictionary<string, Dictionary<string, FileLine>>();

        public void InitSummaryDic()
        {
            tagKeysDic.Clear();
            translationStatusDic.Clear();

            totalKeys = 0;
            dupKeys = 0;
            translationStatusDic["(None)"] = new Dictionary<string, FileLine>();
            translationStatusDic[LocalizationDataManager.ATS_ADD] = new Dictionary<string, FileLine>();
            translationStatusDic[LocalizationDataManager.ATS_UPDATE] = new Dictionary<string, FileLine>();
            translationStatusDic[LocalizationDataManager.ATS_PT] = new Dictionary<string, FileLine>();
            translationStatusDic[LocalizationDataManager.ATS_TRANSLATED] = new Dictionary<string, FileLine>();
            //translationStatusDic[LocalizationDataManager.ATS_PRE_TRANS] = new Dictionary<string, FileLine>();
        }
    }

    public class CategoryInfo : DataInfo
    {
        public bool isModified = false;
        public bool isExport = false;
        public List<PartialInfo> partialInfos = new List<PartialInfo>();

        public CategoryInfo(string category)
        {
            _category = category;
        }
    }

    public class PartialInfo : DataInfo
    {
        public bool isModified = false;
        public bool isExport = false;
        public Dictionary<string, LangFile> languageInfos = new Dictionary<string, LangFile>();

        public PartialInfo(string category, int partial)
        {
            _category = category;
            _partial = partial;
        }
    }

    public class LangFile : DataInfo
    {
        public string fileName;
        public bool isModified = false;
        public Dictionary<string, FileLine> fileInfo;

        public LangFile(string category, int partial, string language)
        {
            _category = category;
            _partial = partial;
            _language = language;
        }

        public bool IsSourceLanguage()
        {
            return (string.CompareOrdinal(_language, ConfigData.SourceLanguage) == 0);
        }
    }

    public class LineInfo
    {
        public static string[] CPLLine = { "Key", "Status", "Korean", "Translation", "Tag", "Desc" };
        public static string[] CPLine = { "Key", "Translation Status", "Korean", "Translation", "Tag", "Desc" };
        public static string[] saveRegularFileLine = { "KEY", "Korean", "Translation", "Tag", "Status", "Description" };
        public static string[] saveExtraFileLine = { "KEY", "Korean", "Translation", "Tag", "Status", "Description", "Category", "Partial" };
    }

    public class FileLine
    {
        public bool isModified = false;
        public bool isModifiedNotExport = false;       //수정되었지만 Export 대상은 아닐 때

        public string key { get; set; }
        public string status { get; set; }
        public string sourceText { get; set; }
        public string translation { get; set; }
        public string tag { get; set; }
        public string desc { get; set; }

        public LangFile file;
        public string translationStatus { get; set; }

        public FileLine()
        {
        }



        public FileLine(string key, string korean, string tag, string status, string desc, string translation = null)
        {
            this.key = key;
            this.sourceText = korean;
            if (this.translation != null)
                this.translation = translation;
            this.tag = tag;
            this.status = status;
            this.desc = desc;
        }

        public FileLine ToNewFileLine()
        {
            FileLine line = new FileLine();
            line.key = key;
            line.status = status;
            line.sourceText = sourceText;
            line.translation = translation;
            line.tag = tag;
            line.desc = desc;
            return line;
        }

        public TBTLine ToTBTLine(string category, int partial)
        {
            TBTLine line = new TBTLine();
            line.key = key;
            line.status = status;
            line.sourceText = sourceText;
            line.translation = translation;
            line.tag = tag;
            line.desc = desc;
            line.category = category;
            line.partial = partial;
            return line;
        }

        public bool IsStatusUpdate()
        {
            if (string.IsNullOrEmpty(status))
                return false;
            if (string.CompareOrdinal(status, LocalizationDataManager.STATUS_UPDATE) == 0 || string.CompareOrdinal(status, LocalizationDataManager.STATUS_UPDATE_ALT) == 0)
                return true;
            return false;
        }
    }

    public class TBTLine : FileLine
    {
        public string category { get; set; }
        public int partial { get; set; }

        public TBTLine()
        {
        }

        public TBTLine(string category, int partial, string key, string korean, string translationStatus, string status)
        {
            this.category = category;
            this.partial = partial;
            this.key = key;
            this.sourceText = korean;
            this.translationStatus = translationStatus;
            this.status = status;
        }

        public TBTLine(string key, string korean, string tag, string status, string desc, string category, int partial, string translation = null)
        {
            this.key = key;
            this.sourceText = korean;
            if (this.translation != null)
                this.translation = translation;
            this.tag = tag;
            this.status = status;
            this.desc = desc;
            this.category = category;
            this.partial = partial;
        }
    }

    public class TBTMap : ClassMap<TBTLine>
    {
        public TBTMap(string language = null)
        {
            if (language.Equals("Korean"))
            {
                Map(m => m.key).Index(0).Name("KEY");
                Map(m => m.sourceText).Index(1).Name("Korean");
                Map(m => m.tag).Index(2).Name("Tag");
                Map(m => m.status).Index(3).Name("Status");
                Map(m => m.desc).Index(4).Name("Desc");
                Map(m => m.category).Index(5).Name("Category");
                Map(m => m.partial).Index(6).Name("Partial");
            }
            else
            {
                Map(m => m.key).Index(0).Name("KEY");
                Map(m => m.sourceText).Index(1).Name("Korean");
                Map(m => m.translation).Index(2).Name(language);
                Map(m => m.tag).Index(3).Name("Tag");
                Map(m => m.status).Index(4).Name("Status");
                Map(m => m.desc).Index(5).Name("Desc");
                Map(m => m.category).Index(6).Name("Category");
                Map(m => m.partial).Index(7).Name("Partial");
            }
        }
    }

    public class FileLineMap : ClassMap<FileLine>
    {
        public FileLineMap(string language)
        {
            if (language.Equals("Korean"))
            {
                Map(m => m.key).Index(0).Name("KEY");
                Map(m => m.sourceText).Index(1).Name("Korean");
                Map(m => m.tag).Index(2).Name("Tag");
                Map(m => m.status).Index(3).Name("Status");
                Map(m => m.desc).Index(4).Name("Description");
            }
            else
            {
                Map(m => m.key).Index(0).Name("KEY");
                Map(m => m.sourceText).Index(1).Name("Korean");
                Map(m => m.translation).Index(2).Name(language);
                Map(m => m.tag).Index(3).Name("Tag");
                Map(m => m.status).Index(4).Name("Status");
                Map(m => m.desc).Index(5).Name("Description");
            }
        }
    }

    //Add, Update 개수
    public class TBTNum
    {
        public int addNum;
        public int updateNum;

        public TBTNum()
        {
            addNum = 0;
            updateNum = 0;
        }

        public string ToTBTNumStr()
        {
            return string.Format("Add : {0}, Fix : {1}", addNum, updateNum);
        }
    }

}

    