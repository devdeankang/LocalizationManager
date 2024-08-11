using CsvHelper;
using CsvHelper.Configuration;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using log4net;
using Newtonsoft.Json.Linq;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LocalizationManager
{
    class LocalizationDataManager
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(LocalizationDataManager));

        public const string LOCALIZATION_FILENAME_PREFIX = "Localization_";

        public const string EXPORT_ALL = "Export_All";
        public const string EXPORT_TBT = "Export_TBT";
        public const string EXPORT_TAG = "Export_Tag";

        public const string ATS_ADD = "New Keys (TBT)";
        public const string ATS_UPDATE = "Update (TBT)";
        public const string ATS_PT = "Partially Translated";
        //public const string ATS_PRE_TRANS = "Pre-Translated";
        public const string ATS_TRANSLATED = "Translated All";

        public const string STATUS_EMPTY = "Empty";
        public const string STATUS_NEW = "New";
        public const string STATUS_NEW_ALT = "Add";
        public const string STATUS_UPDATE = "Update";
        public const string STATUS_UPDATE_ALT = "Fix";
        public const string STATUS_KOREAN = "Korean";
        public const string STATUS_TRANSLATED = "Translated";
        public const string STATUS_PRE_TRANSLATED = "PreTranslated";

        public static string[] STATUS_ARR =
        {
            "",
            STATUS_NEW,
            STATUS_UPDATE,
            STATUS_TRANSLATED,
            STATUS_PRE_TRANSLATED
        };


        private static LocalizationDataManager instance = null;

        private LocalizationDataManager()
        {
            localData = new LocalizationData();
        }

        public static LocalizationDataManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new LocalizationDataManager();
                }
                return instance;
            }
        }

        public LocalizationData localData;
        public ConfigData configData;

        public List<PartialInfo> GetAllPartialInfos()
        {
            List<PartialInfo> ret = new List<PartialInfo>();

            foreach (string category in configData.Categories)
            {
                CategoryInfo categoryInfo = localData.categoryInfos[category];
                foreach (PartialInfo partialInfo in categoryInfo.partialInfos)
                    ret.Add(partialInfo);
            }

            return ret;
        }

        public static List<PartialInfo> GetPartialInfos(string category)
        {
            if (Instance.localData.categoryInfos.TryGetValue(category, out CategoryInfo categoryInfo) == false)
                return null;
            return categoryInfo.partialInfos;
        }

        public static Dictionary<string, LangFile> GetLangFiles(string category, int partial)
        {
            if (Instance.localData.categoryInfos.TryGetValue(category, out CategoryInfo categoryInfo) == false)
                return null;
            if (categoryInfo.partialInfos.Count <= partial)
                return null;
            return categoryInfo.partialInfos[partial].languageInfos;
        }

        public static Dictionary<string, FileLine> GetFileInfo(string category, int partial, string language)
        {
            if (Instance.localData.categoryInfos.TryGetValue(category, out CategoryInfo categoryInfo) == false)
                return null;
            if (categoryInfo.partialInfos.Count <= partial)
                return null;
            if (categoryInfo.partialInfos[partial].languageInfos.TryGetValue(language, out LangFile languageInfo) == false)
                return null;
            return languageInfo.fileInfo;
        }

        public static FileLine GetFileLine(string category, int partial, string language, string key)
        {
            if (Instance.localData.categoryInfos.TryGetValue(category, out CategoryInfo categoryInfo) == false)
                return null;
            if (categoryInfo.partialInfos.Count <= partial)
                return null;
            if (categoryInfo.partialInfos[partial].languageInfos.TryGetValue(language, out LangFile languageInfo) == false)
                return null;
            if (languageInfo.fileInfo.TryGetValue(key, out FileLine fileLine) == false)
                return null;
            return fileLine;
        }

        public static FileLine FindFileLine(string key, string category = null, int partial = -1, string language = null)
        {
            foreach (KeyValuePair<string, CategoryInfo> categoryPair in Instance.localData.categoryInfos)
            {
                if ((category != null) && string.CompareOrdinal(categoryPair.Key, category) != 0)
                    continue;
                foreach (PartialInfo partialInfo in categoryPair.Value.partialInfos)
                {
                    if ((partial != -1) && (partial != partialInfo._partial))
                        continue;
                    foreach (KeyValuePair<string, LangFile> langFilePair in partialInfo.languageInfos)
                    {
                        if ((language != null) && string.CompareOrdinal(langFilePair.Key, language) != 0)
                            continue;
                        if ((language == null) && string.CompareOrdinal(langFilePair.Key, ConfigData.SourceLanguage) != 0)
                            continue;
                        if (langFilePair.Value.fileInfo.TryGetValue(key, out FileLine ret))
                            return ret;
                    }
                }
            }
            return null;
        }

        // Key에 대한 모든 FileLIne
        public static List<FileLine> FindFileLines(string key, string category = null, int partial = -1, string language = null)
        {
            FileLine fileLine = null;
            List<FileLine> ret = new List<FileLine>();
            foreach (KeyValuePair<string, CategoryInfo> categoryPair in Instance.localData.categoryInfos)
            {
                if ((category != null) && string.CompareOrdinal(categoryPair.Key, category) != 0)
                    continue;
                foreach (PartialInfo partialInfo in categoryPair.Value.partialInfos)
                {
                    if ((partial != -1) && (partial != partialInfo._partial))
                        continue;
                    foreach (KeyValuePair<string, LangFile> langFilePair in partialInfo.languageInfos)
                    {
                        if ((language != null) && string.CompareOrdinal(langFilePair.Key, language) != 0)
                            continue;
                        if (langFilePair.Value.fileInfo.TryGetValue(key, out fileLine))
                            ret.Add(fileLine);
                    }
                }
            }
            return ret;
        }


        #region Load Project Config and Localization Data
        public bool LoadConfigData(ProjectInfo info)
        {
            configData = ConfigData.LoadConfigData(info.Directory, info.Name);
            if (configData == null)
                return false;
            return true;
        }

        public bool LoadLocalizationDataCSV_Category(string category, Action<string> SetMessage)
        {
            var directory = configData.ProjectDirs.ContainsKey(category) ?
                    configData.ProjectDirs[category] : configData.ProjectDirs["All"];
            directory = Path.Combine(configData.Directory, directory);

            foreach (string language in configData.Languages)
            {
                int partial = 0;
                int partialCount = 0;

                while (true)
                {
                    string fileName = LocalizationData.GetFileName(category, partial, language, LocalizationFileType.CSV);
                    var filePath = Path.Combine(directory, fileName);

                    //
                    if (localData.categoryInfos.ContainsKey(category))
                        partialCount = localData.categoryInfos[category].partialInfos.Count;

                    if (!File.Exists(filePath))
                    {
                        if (partial == 0 || partial < partialCount)
                        {
                            Dictionary<string, FileLine> fileLineDic = new Dictionary<string, FileLine>();
                            localData.AddFileInfo(category, partial, language, fileLineDic);
                            partial++;
                            continue;
                        }
                        else
                        {
                            break;
                        }
                    }

                    var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture);
                    csvConfig.BadDataFound = null;

                    //category partial langugae
                    using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var reader = new StreamReader(fs))
                    using (var csv = new CsvParser(reader, csvConfig))
                    {
                        Dictionary<string, FileLine> fileLineDic = new Dictionary<string, FileLine>();
                        bool isLangKorean = language.Equals(ConfigData.SourceLanguage, StringComparison.Ordinal);
                        string[] header = csv.Read();

                        int keyIndex = -1;
                        int sourceTextIndex = -1;
                        int translationIndex = -1;
                        int tagIndex = -1;
                        int statusIndex = -1;
                        int descIndex = -1;
                        int translationStatusIndex = -1;

                        //Header 분석
                        for (int i = 0; i < header.Length; i++)
                        {
                            switch (header[i].ToLower())
                            {
                                case "key":
                                    keyIndex = i;
                                    break;
                                case "korean":
                                    sourceTextIndex = i;
                                    break;
                                case "tag":
                                    tagIndex = i;
                                    break;
                                case "status":
                                    statusIndex = i;
                                    break;
                                case "description":
                                case "desc":
                                    descIndex = i;
                                    break;
                                case "translationstatus":
                                    translationStatusIndex = i;
                                    break;
                            }
                                                        
                            foreach (string configLanguage in configData.Languages)
                            {
                                if (header[i].ToLower() == configLanguage.ToLower())
                                {
                                    translationIndex = i;
                                    break;
                                }
                            }
                        }

                        //header 매칭 실패
                        try
                        {
                            if (keyIndex != -1 || sourceTextIndex != -1 || tagIndex != -1 || statusIndex != -1 || descIndex != -1) { }
                            if (isLangKorean != false && translationIndex != -1) { }
                        }
                        catch (Exception e)
                        {
                            log.Error(e.Message);
                            return false;
                        }


                        while (true)
                        {
                            string[] record = csv.Read();
                            if (record == null)
                                break;

                            //빈 key가 입력되는 것 방지
                            if (string.IsNullOrEmpty(record[keyIndex]) == true)
                                continue;

                            FileLine line = new FileLine();

                            line.key = record[keyIndex];
                            line.sourceText = ((sourceTextIndex != -1) && sourceTextIndex < record.Length) ? record[sourceTextIndex] : null;
                            line.tag = ((tagIndex != -1) && tagIndex < record.Length) ? record[tagIndex] : null;
                            line.status = ((statusIndex != -1) && statusIndex < record.Length) ? record[statusIndex] : null;
                            line.desc = ((descIndex != -1) && descIndex < record.Length) ? record[descIndex] : null;
                            if (isLangKorean == false)
                            {
                                line.translation = record[translationIndex];
                            }
                            else //Korean
                            {
                                if (translationStatusIndex != -1)
                                {
                                    line.translationStatus = record[translationStatusIndex];
                                }
                            }

                            var addKey = line.key;
                            while (fileLineDic.ContainsKey(addKey))
                            {
                                addKey = string.Format("{0} (1)", addKey);
                            }

                            fileLineDic.Add(addKey, line);
                        }
                        localData.AddFileInfo(category, partial, language, fileLineDic);
                    }

                    SetMessage(string.Format("Complete Loading {0}", fileName));

                    partial++;
                }

            }

            return true;
        }

        public bool LoadLocalizationDataXLSX_Category(string category, Action<string> SetMessage)
        {
            var dicrectory = configData.ProjectDirs.ContainsKey(category) ?
                    configData.ProjectDirs[category] : configData.ProjectDirs["All"];
            dicrectory = Path.Combine(configData.Directory, dicrectory);

            foreach (string language in configData.Languages)
            {
                int partial = 0;

                while (true)
                {
                    string fileName = LocalizationData.GetFileName(category, partial, language, LocalizationFileType.XLSX);
                    var filePath = Path.Combine(dicrectory, fileName);

                    if (!File.Exists(filePath))
                    {
                        if (partial <= 0)
                        {
                            Dictionary<string, FileLine> emptyFileLineDic = new Dictionary<string, FileLine>();
                            localData.AddFileInfo(category, partial, language, emptyFileLineDic);
                            partial++;
                            continue;
                        }
                        else
                        {
                            break;
                        }

                    }

                    XSSFWorkbook workBook;
                    using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        workBook = new XSSFWorkbook(stream);
                        stream.Close();
                    }

                    Dictionary<string, FileLine> fileLineDic = new Dictionary<string, FileLine>();
                    bool isLangKorean = language.Equals("Korean");
                    for (int sheetIndex = 0; sheetIndex < workBook.NumberOfSheets; sheetIndex++)
                    {
                        ISheet sheet = workBook.GetSheetAt(sheetIndex);
                        string sheetName = sheet.SheetName;
                        if (sheet == null) continue;

                        //시트가 숨겨져 있으면 추출하지 않는다.
                        if (workBook.IsSheetHidden(sheetIndex) == true)
                        {
                            continue;
                        }

                        int keyIndex = -1;
                        int sourceTextIndex = -1;
                        int translationIndex = -1;
                        int tagIndex = -1;
                        int statusIndex = -1;
                        int descIndex = -1;
                        int translationStatusIndex = -1;

                        //0번째 줄이 field 가정
                        IRow fieldRow = sheet.GetRow(0);
                        if (fieldRow == null)
                        {
                            //필드가 없다
                            continue;
                        }

                        int fieldNum = fieldRow.Cells.Count;
                        for (int colIndex = 0; colIndex < fieldRow.Cells.Count; colIndex++)
                        {
                            ICell cell = fieldRow.GetCell(colIndex);
                            if (cell == null)
                            {
                                //필드가 없다
                                continue;
                            }
                            cell.SetCellType(CellType.String);
                            string fieldName = cell.RichStringCellValue.ToString().ToLower();

                            switch (fieldName)
                            {
                                case "key":
                                    keyIndex = colIndex;
                                    break;
                                case "korean":
                                    sourceTextIndex = colIndex;
                                    break;
                                case "tag":
                                    tagIndex = colIndex;
                                    break;
                                case "status":
                                    statusIndex = colIndex;
                                    break;
                                case "description":
                                case "desc":
                                    descIndex = colIndex;
                                    break;
                                case "translationstatus":
                                    translationStatusIndex = colIndex;
                                    break;
                            }

                            //Language ==> english 이렇게 바꿔야함
                            foreach (string configLanguage in configData.Languages)
                            {
                                if (fieldName == configLanguage.ToLower())
                                {
                                    translationIndex = colIndex;
                                    break;
                                }
                            }
                        }

                        //header 매칭 실패
                        try
                        {
                            if (keyIndex != -1 || sourceTextIndex != -1 || tagIndex != -1 || statusIndex != -1 || descIndex != -1) { }
                            if (isLangKorean != false && translationIndex != -1) { }
                        }
                        catch (Exception e)
                        {
                            log.Error(e.Message);
                            return false;
                        }

                        for (int rowIdx = 1; rowIdx < sheet.LastRowNum + 1; rowIdx++)
                        {
                            IRow row = sheet.GetRow(rowIdx);
                            if (row == null)
                            {
                                continue;
                            }

                            FileLine line = new FileLine();

                            for (int colIndex = 0; colIndex < fieldNum; colIndex++)
                            {
                                ICell cell = row.GetCell(colIndex);
                                string cellStr = string.Empty;
                                if (cell != null)
                                {
                                    switch (cell.CellType)
                                    {
                                        case CellType.Numeric:
                                            cellStr = cell.NumericCellValue.ToString();
                                            break;
                                        case CellType.String:
                                            cellStr = cell.StringCellValue;
                                            break;
                                        default:
                                            break;
                                    }
                                }

                                if (colIndex == keyIndex)
                                    line.key = cellStr;
                                else if (colIndex == sourceTextIndex)
                                    line.sourceText = cellStr;
                                else if (colIndex == translationIndex)
                                    line.translation = cellStr;
                                else if (colIndex == tagIndex)
                                    line.tag = cellStr;
                                else if (colIndex == statusIndex)
                                    line.status = cellStr;
                                else if (colIndex == descIndex)
                                    line.desc = cellStr;
                            }

                            //빈 key가 입력되는 것 방지
                            if (string.IsNullOrEmpty(line.key) == true)
                                continue;

                            string addKey = line.key;
                            while (fileLineDic.ContainsKey(addKey))
                            {
                                addKey = string.Format("{0} (1)", addKey);
                            }

                            fileLineDic.Add(addKey, line);
                        }
                    }

                    localData.AddFileInfo(category, partial, language, fileLineDic);

                    SetMessage(string.Format("Complete Loading {0}", fileName));

                    partial++;
                }
            }

            return true;
        }

        #endregion

        public void SetSummaryInfo()
        {
            foreach (string category in configData.Categories)
            {
                CategoryInfo categoryInfo = localData.categoryInfos[category];
                categoryInfo.InitSummaryDic();

                foreach (PartialInfo partialInfo in categoryInfo.partialInfos)
                {
                    LangFile koreanInfo = partialInfo.languageInfos["Korean"];

                    partialInfo.InitSummaryDic();
                    partialInfo.totalKeys = koreanInfo.fileInfo.Count;

                    foreach (KeyValuePair<string, FileLine> line in koreanInfo.fileInfo)
                    {
                        //Set TagDic
                        if (!string.IsNullOrEmpty(line.Value.tag))
                        {
                            if (!categoryInfo.tagKeysDic.ContainsKey(line.Value.tag))
                            {
                                categoryInfo.tagKeysDic[line.Value.tag] = new Dictionary<string, FileLine>();
                            }
                            if (!partialInfo.tagKeysDic.ContainsKey(line.Value.tag))
                            {
                                partialInfo.tagKeysDic[line.Value.tag] = new Dictionary<string, FileLine>();
                            }

                            categoryInfo.tagKeysDic[line.Value.tag][line.Key] = line.Value;
                            partialInfo.tagKeysDic[line.Value.tag][line.Key] = line.Value;
                        }

                        //Set TranslationStatusDic
                        if (!string.IsNullOrEmpty(line.Value.translationStatus))
                        {
                            categoryInfo.translationStatusDic[line.Value.translationStatus][line.Key] = line.Value;
                            partialInfo.translationStatusDic[line.Value.translationStatus][line.Key] = line.Value;
                        }
                        else
                        {
                            categoryInfo.translationStatusDic["(None)"][line.Key] = line.Value;
                            partialInfo.translationStatusDic["(None)"][line.Key] = line.Value;
                        }
                    }

                    categoryInfo.totalKeys += partialInfo.totalKeys;
                }
            }
        }

        public void SaveLocalizationData()
        {
            bool isPartiallySave = localData._isPartiallyExport;

            //전체 파일 저장
            foreach (KeyValuePair<string, CategoryInfo> category in localData.categoryInfos)
            {
                //특정 category_partial만 저장할 때
                if (isPartiallySave == true && category.Value.isExport == false) continue;

                var directory = configData.ProjectDirs.ContainsKey(category.Key) ?
                    configData.ProjectDirs[category.Key] : configData.ProjectDirs["All"];
                directory = Path.Combine(configData.Directory, directory);

                //폴더 없으면 만든다.
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                foreach (PartialInfo partialInfo in category.Value.partialInfos)
                {
                    //특정 category_partial만 저장할 때
                    if (isPartiallySave == true && partialInfo.isExport == false) continue;

                    foreach (KeyValuePair<string, LangFile> language in partialInfo.languageInfos)
                    {
                        //수정되지 않은 파일이면 continue;
                        if (language.Value.isModified == false) continue;

                        try
                        {
                            LocalizationFileType saveFileType = configData.SaveFileExtensionType;

                            var filepath = Path.Combine(directory, LocalizationData.GetFileName(category.Key, partialInfo._partial, language.Key, saveFileType));

                            switch (saveFileType)
                            {
                                case LocalizationFileType.CSV:
                                    SaveData.SaveRegularCSV(filepath, language.Key, language.Value.fileInfo.Values.ToList());
                                    break;
                                case LocalizationFileType.XLSX:
                                    string sheetName = LocalizationData.GetFileName(category.Key, partialInfo._partial, language.Key, LocalizationFileType.NONE);
                                    SaveData.SaveRegularXLSX(filepath, sheetName, language.Key, language.Value.fileInfo.Values.ToList());
                                    break;
                                default:
                                    break;
                            }

                            //해당 cateogry, partial, language 의 모든 modified false로 수정
                            //fileline은 수정하지 않는다 fileline의 modified는 sync key 여부이기 때문에
                            //종료하지 않고 저장하고 난 후에 export 해도 똑같이 export 되어야 한다.
                            localData.SetIsModified(false, category.Key, partialInfo._partial, language.Key);
                        }
                        catch (Exception e)
                        {
                            log.Error(e.ToString());
                        }
                    }
                }
            }

        }

        public void SyncKeys()
        {
            foreach (string category in configData.Categories)
            {
                CategoryInfo categoryInfo = localData.categoryInfos[category];
                foreach (PartialInfo partialInfo in categoryInfo.partialInfos)
                {
                    //Korean 파일을 읽어서 다른 Language 와 비교한 뒤 Key Sync 
                    Dictionary<string, FileLine> koreanInfo = partialInfo.languageInfos[ConfigData.SourceLanguage].fileInfo;

                    foreach (KeyValuePair<string, LangFile> language in partialInfo.languageInfos)
                    {
                        if (language.Key == ConfigData.SourceLanguage)
                            continue;

                        Dictionary<string, FileLine> languageInfo = language.Value.fileInfo;
                        foreach (KeyValuePair<string, FileLine> krLine in koreanInfo)
                        {
                            //Key 존재 유무 판별
                            string krKey = krLine.Key;
                            FileLine fileLine = krLine.Value;

                            if (!languageInfo.ContainsKey(krKey))
                            {
                                //Korean이 붙어있는 경우
                                //Human error (Add 되어야 하는데 Korean이 붙어있으므로 지워준다)
                                if (fileLine.status.Equals(ConfigData.SourceLanguage, StringComparison.OrdinalIgnoreCase))
                                {
                                    fileLine.status = string.Empty;
                                    localData.SetIsModified(true, category, partialInfo._partial, ConfigData.SourceLanguage);
                                }

                                //AddKey
                                SyncKeyAddFileLine(category, partialInfo._partial, krKey, fileLine, language.Key);
                            }
                            else
                            {
                                //Korean(한글), desc, tag 중 하나라도 다르면 sync Keys 해야함
                                if (fileLine.sourceText == languageInfo[krKey].sourceText &&
                                        fileLine.desc == languageInfo[krKey].desc &&
                                        fileLine.tag == languageInfo[krKey].tag)
                                {
                                    continue;
                                }

                                bool isModifiedNotExport = false;

                                //korean은 같은데 desc, tag중 하나라도 다른 경우
                                if (fileLine.sourceText == languageInfo[krKey].sourceText &&
                                        (fileLine.desc != languageInfo[krKey].desc || fileLine.tag != languageInfo[krKey].tag))
                                {
                                    isModifiedNotExport = true;
                                }

                                //Korean만 변경하는 경우(Status가 Korean일 때)
                                //Update 에서 제외 (오타수정 or 띄어쓰기 수정)
                                if (fileLine.status.Equals("Korean", StringComparison.OrdinalIgnoreCase))
                                {
                                    fileLine.status = string.Empty;
                                    fileLine.isModifiedNotExport = true;
                                    isModifiedNotExport = true;
                                    localData.SetIsModified(true, category, partialInfo._partial, "Korean");
                                }

                                //Korean만 변경하는 경우 Update에서 제외
                                if (fileLine.isModifiedNotExport == true)
                                {
                                    isModifiedNotExport = true;
                                }

                                //UpdateKey
                                SyncKeyUpdateFileLine(category, partialInfo._partial, krKey, fileLine, isModifiedNotExport, language.Key);
                            }
                        }
                    }



                }
            }

            SetTranslationStatus();

            SetSummaryInfo();

            localData._isSynchronized = true;
        }

        void SyncKeyAddFileLine(string category, int partial, string key, FileLine line, string language)
        {
            Dictionary<string, LangFile> languageInfos = localData.categoryInfos[category].partialInfos[partial].languageInfos;
            var fileInfo = languageInfos[language].fileInfo;

            FileLine newLine = new FileLine();
            newLine.key = line.key;
            newLine.sourceText = line.sourceText;
            newLine.translation = string.Empty;
            newLine.tag = line.tag;
            newLine.desc = line.desc;
            newLine.status = STATUS_NEW;
            newLine.isModified = true;

            fileInfo.Add(key, newLine);

            //수정 설정
            localData.SetIsModified(true, category, partial, language);
        }

        void SyncKeyUpdateFileLine(string category, int partial, string key, FileLine line, bool isModifiedNotExport, string language)
        {
            Dictionary<string, LangFile> languageInfos = localData.categoryInfos[category].partialInfos[partial].languageInfos;
            var fileInfo = languageInfos[language].fileInfo;

            fileInfo[key].sourceText = line.sourceText;
            fileInfo[key].tag = line.tag;
            fileInfo[key].desc = line.desc;
            fileInfo[key].isModified = true;

            //수정 설정
            localData.SetIsModified(true, category, partial, language);

            if (isModifiedNotExport)
            {
                fileInfo[key].isModifiedNotExport = true;
            }
            else
            {
                fileInfo[key].status = STATUS_UPDATE;
            }
        }

        //이것도 category로 해서 스레드
        public void SyncKeyOrders()
        {
            foreach (string category in configData.Categories)
            {
                CategoryInfo categoryInfo = localData.categoryInfos[category];
                foreach (PartialInfo partialInfo in categoryInfo.partialInfos)
                {
                    //Korean 파일을 읽어서 다른 Language 파일 순서로 Synchronize
                    Dictionary<string, FileLine> koreanInfo = partialInfo.languageInfos["Korean"].fileInfo;

                    bool needSyncKeyOrders = false;
                    foreach (KeyValuePair<string, LangFile> languageInfo in partialInfo.languageInfos)
                    {
                        if (needSyncKeyOrders == false && NeedSyncKeyOrders(category, partialInfo._partial, languageInfo.Key) == false)
                            continue;

                        needSyncKeyOrders = true;

                        Dictionary<string, FileLine> originLanguageDic = languageInfo.Value.fileInfo;
                        Dictionary<string, FileLine> newLanguageDic = new Dictionary<string, FileLine>();

                        foreach (KeyValuePair<string, FileLine> krLine in koreanInfo)
                        {
                            if (originLanguageDic.ContainsKey(krLine.Key) == false) continue;

                            newLanguageDic.Add(krLine.Key, originLanguageDic[krLine.Key]);
                        }

                        languageInfo.Value.fileInfo.Clear();
                        languageInfo.Value.fileInfo = newLanguageDic;
                        localData.SetIsModified(true, category, partialInfo._partial, languageInfo.Key);
                    }
                }
            }

            //SummaryInfo
            SetSummaryInfo();
        }

        public bool NeedSyncKeyOrders(string category, int partial, string language)
        {
            if (language == "Korean")
                return false;

            LangFile koreanInfo = localData.categoryInfos[category].partialInfos[partial].languageInfos["Korean"];
            LangFile languageInfo = localData.categoryInfos[category].partialInfos[partial].languageInfos[language];

            //if (koreanInfo.fileInfo.Count != languageInfo.fileInfo.Count)
            //    return true;

            int koreanIndex = 0;
            int languageIndex = 0;
            while (true)
            {
                if (koreanIndex >= koreanInfo.fileInfo.Count || languageIndex >= languageInfo.fileInfo.Count)
                    break;

                string koreanKey = koreanInfo.fileInfo.Keys.ElementAt(koreanIndex);
                string languageKey = languageInfo.fileInfo.Keys.ElementAt(languageIndex);

                if (koreanKey != languageKey)
                {
                    if (languageInfo.fileInfo.ContainsKey(koreanKey) == true)
                    {
                        return true;
                    }

                    koreanIndex++;
                    continue;
                }

                koreanIndex++;
                languageIndex++;
            }

            return false;
        }

        //이것도 category로 해서 스레드
        public void SyncKeyOrders_Category(string category)
        {
            CategoryInfo categoryInfo = localData.categoryInfos[category];
            foreach (PartialInfo partialInfo in categoryInfo.partialInfos)
            {
                //Korean 파일을 읽어서 다른 Language 파일 순서로 Synchronize
                Dictionary<string, FileLine> koreanInfo = partialInfo.languageInfos["Korean"].fileInfo;

                bool needSyncKeyOrders = false;
                foreach (KeyValuePair<string, LangFile> languageInfo in partialInfo.languageInfos)
                {
                    if (needSyncKeyOrders == false && NeedSyncKeyOrders(category, partialInfo._partial, languageInfo.Key) == false)
                        continue;

                    needSyncKeyOrders = true;
                    Dictionary<string, FileLine> originLanguageDic = languageInfo.Value.fileInfo;
                    Dictionary<string, FileLine> newLanguageDic = new Dictionary<string, FileLine>();

                    foreach (KeyValuePair<string, FileLine> krLine in koreanInfo)
                    {
                        if (originLanguageDic.ContainsKey(krLine.Key) == false) continue;

                        newLanguageDic.Add(krLine.Key, originLanguageDic[krLine.Key]);
                    }

                    languageInfo.Value.fileInfo.Clear();
                    languageInfo.Value.fileInfo = newLanguageDic;
                    localData.SetIsModified(true, category, partialInfo._partial, languageInfo.Key);
                }
            }
        }


        #region Import

        public bool ImportFiles(string[] filePaths, int importType, Action<string> SetMessage)
        {
            //생성된 시간대로 정렬
            //보류
            //filePaths = filePaths.OrderByDescending(s => File.GetCreationTime(s)).ToArray();

            foreach (string path in filePaths)
            {
                switch (Path.GetExtension(path))
                {
                    case ".csv":
                        //SetMessage(string.Format("Import {0}", Path.GetFileName(path)));
                        ImportCSVFile(path, importType);
                        break;
                    case ".xlsx":
                        //SetMessage(string.Format("Import {0}", Path.GetFileName(path)));
                        ImportXLSXFile(path, importType);
                        break;
                    case ".zip":
                        ImportZipFile(path, SetMessage, importType);
                        break;
                    default:
                        break;
                }

            }

            //SetMessage("Set Translation Status");
            SetTranslationStatus();
            //SetMessage("Set Summary Information");
            SetSummaryInfo();

            return true;
        }

        public bool ImportCSVFile(string filePath, int importType)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            string category = string.Empty;
            int partial = 0;
            string language = string.Empty;

            if (!fileName.StartsWith(LOCALIZATION_FILENAME_PREFIX)) return false;

            ExportFileType fileNameStatus = GetFileType(fileName, ref category, ref partial, ref language, importType);

            switch (fileNameStatus)
            {
                case ExportFileType.NONE:
                    return false;
                case ExportFileType.REGULAR_FILE:
                    return ImportRegularCSV(filePath, category, partial, language, importType);
                case ExportFileType.EXTRA_FILE:
                    return ImportExtraCSV(filePath, importType);
            }

            return true;
        }

        //실제로 번역 비교하여 localization data에 적용
        void ImportLocalizationData(string category, int partial, string language, Dictionary<string, FileLine> importingFileLineDic, int importType)
        {
            string key = string.Empty;
            string importingTranslation = string.Empty;
            string importingSourceText = string.Empty;

            bool IsImportingSourceLangFile = string.CompareOrdinal(language, ConfigData.SourceLanguage) == 0;

            Dictionary<string, FileLine> fileInfo = localData.categoryInfos[category].partialInfos[partial].languageInfos[language].fileInfo;
            //importingFileLineDic = localData.categoryInfos[category].partialInfos[partial].languageInfos[language].fileInfo;
            foreach (FileLine importingFileLine in importingFileLineDic.Values)
            {
                key = importingFileLine.key;
                importingTranslation = importingFileLine.translation;
                importingSourceText = importingFileLine.sourceText;

                //if (!fileInfo.ContainsKey(key))
                //{
                //    //Korean에 해당 Key가 없다
                //    return;
                //    //일단은 continue 한다.
                //    //나중에 false return해서 오류 dialog 출력하게 만들 것
                //    //ERROR: [category][partial]Korean에 Key [key]가 존재하지 않습니다.
                //}

                FileLine line = fileInfo[key];

                if (line.translation != importingTranslation)
                {
                    line.translation = importingTranslation;
                }

                if (!IsImportingSourceLangFile)
                {
                    if (!string.IsNullOrEmpty(importingTranslation.Trim()))
                    {
                        PartialInfo partialInfo = localData.categoryInfos[category].partialInfos[partial];
                        if (string.CompareOrdinal(partialInfo._language, ConfigData.SourceLanguage) != 0)
                            line.status = STATUS_TRANSLATED;
                    }
                    localData.SetIsModified(true, category, partial, language);
                }

                // ImportFileType
                if (IsImportingSourceLangFile && importType != (int)ImportFileType.OnlyTargetLang)
                {
                    if (line.sourceText != importingSourceText)
                    {
                        line.sourceText = importingSourceText;
                        line.isModified = true;

                        //line.sourceText로 모든 언어의 sourceText로 변경
                        PartialInfo partialInfo = localData.categoryInfos[category].partialInfos[partial];
                        Dictionary<string, LangFile> langInfos = partialInfo.languageInfos;
                        foreach (LangFile langInfo in langInfos.Values)
                        {
                            if (string.CompareOrdinal(langInfo._language, ConfigData.SourceLanguage) == 0)
                                continue;

                            if (string.IsNullOrEmpty(langInfo.fileInfo[key].translation))
                                langInfo.fileInfo[key].status = STATUS_NEW;
                            else
                                langInfo.fileInfo[key].status = STATUS_UPDATE;
                            langInfo.isModified = true;
                        }
                    }
                }
            }
        }


        bool ImportRegularCSV(string filePath, string category, int partial, string language, int importType)
        {
            //정규번역파일 Import
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(fs))
            using (var csv = new CsvParser(reader, CultureInfo.InvariantCulture))
            {
                Dictionary<string, FileLine> fileLineDic = new Dictionary<string, FileLine>();
                bool isLangKorean = language.Equals(ConfigData.SourceLanguage);
                string[] header = csv.Read();

                int keyIndex = -1;
                int sourceTextIndex = -1;
                int translationIndex = -1;
                int tagIndex = -1;
                int statusIndex = -1;
                int descIndex = -1;
                int translationStatusIndex = -1;

                //Header 분석
                for (int i = 0; i < header.Length; i++)
                {
                    switch (header[i].ToLower())
                    {
                        case "key":
                            keyIndex = i;
                            break;
                        case "korean":
                            sourceTextIndex = i;
                            break;
                        case "tag":
                            tagIndex = i;
                            break;
                        case "status":
                            statusIndex = i;
                            break;
                        case "description":
                        case "desc":
                            descIndex = i;
                            break;
                        case "translationstatus":
                            translationStatusIndex = i;
                            break;
                    }

                    //Language ==> english 이렇게 바꿔야함
                    foreach (string configLanguage in configData.Languages)
                    {
                        if (header[i].ToLower() == configLanguage.ToLower())
                        {
                            translationIndex = i;
                            break;
                        }
                    }
                }

                //header 매칭 실패시
                try
                {
                    if (keyIndex != -1 || sourceTextIndex != -1 || tagIndex != -1 || statusIndex != -1 || descIndex != -1) { }
                    if (isLangKorean != false && translationIndex != -1) { }
                }
                catch (Exception e)
                {
                    log.Error(e.Message);
                    return false;
                }

                while (true)
                {
                    string[] record = csv.Read();
                    if (record == null)
                        break;

                    //빈 key가 입력되는 것 방지
                    if (string.IsNullOrEmpty(record[keyIndex]) == true)
                        continue;

                    FileLine line = new FileLine();

                    line.key = record[keyIndex];
                    line.sourceText = record[sourceTextIndex];
                    line.tag = record[tagIndex];
                    line.status = record[statusIndex];
                    line.desc = record[descIndex];
                    if (isLangKorean == false)
                    {
                        line.translation = record[translationIndex];
                    }
                    else //Korean
                    {
                        if (translationStatusIndex != -1)
                        {
                            line.translationStatus = record[translationStatusIndex];
                        }
                    }

                    var addKey = line.key;
                    while (fileLineDic.ContainsKey(addKey))
                    {
                        addKey = string.Format("{0} (1)", addKey);
                    }

                    fileLineDic.Add(addKey, line);
                }
                ImportLocalizationData(category, partial, language, fileLineDic, importType);
            }
            return true;
        }


        bool ImportExtraCSV(string filePath, int importType)
        {
            //추가번역파일 Import
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(fs))
            using (var csv = new CsvParser(reader, CultureInfo.InvariantCulture))
            {
                //Dictionary<string, FileLine> fileLineDic = new Dictionary<string, FileLine>();
                Dictionary<string, FileLine> fileInfo = new Dictionary<string, FileLine>();

                string[] header = csv.Read();

                string category = string.Empty;
                int partial = 0;
                string language = string.Empty;

                int keyIndex = -1;
                int sourceTextIndex = -1;
                int translationIndex = -1;
                int tagIndex = -1;
                int statusIndex = -1;
                int descIndex = -1;
                int translationStatusIndex = -1;
                int categoryIndex = -1;
                int partialIndex = -1;

                //Header 분석
                for (int i = 0; i < header.Length; i++)
                {
                    switch (header[i].ToLower())
                    {
                        case "key":
                            keyIndex = i;
                            break;
                        case "korean":
                            sourceTextIndex = i;
                            break;
                        case "tag":
                            tagIndex = i;
                            break;
                        case "status":
                            statusIndex = i;
                            break;
                        case "description":
                        case "desc":
                            descIndex = i;
                            break;
                        case "translationstatus":
                            translationStatusIndex = i;
                            break;
                        case "category":
                            categoryIndex = i;
                            break;
                        case "partial":
                            partialIndex = i;
                            break;
                    }

                    //Language ==> english 이렇게 바꿔야함
                    foreach (string configLanguage in configData.Languages)
                    {
                        if (header[i].ToLower() == configLanguage.ToLower())
                        {
                            translationIndex = i;
                            break;
                        }
                    }
                }

                //header 매칭 실패시
                try
                {
                    if (keyIndex != -1 || sourceTextIndex != -1 || tagIndex != -1 || statusIndex != -1 || descIndex != -1) { }
                }
                catch (Exception e)
                {
                    log.Error(e.Message);
                    return false;
                }

                while (true)
                {
                    string[] record = csv.Read();
                    if (record == null)
                        break;

                    //빈 key가 입력되는 것 방지
                    if (string.IsNullOrEmpty(record[keyIndex]) == true)
                        continue;

                    if (translationIndex != -1)
                    {
                        fileInfo = localData.categoryInfos[record[categoryIndex]]
                            .partialInfos[Convert.ToInt32(record[partialIndex])].languageInfos[header[translationIndex]].fileInfo;

                        fileInfo[record[keyIndex]].translation = record[translationIndex];
                        language = header[translationIndex];
                    }
                    else
                    {
                        // SourceLanguage
                        fileInfo = localData.categoryInfos[record[categoryIndex]]
                            .partialInfos[Convert.ToInt32(record[partialIndex])].languageInfos[ConfigData.SourceLanguage].fileInfo;
                        language = ConfigData.SourceLanguage;
                    }
                    category = record[categoryIndex];
                    partial = Convert.ToInt32(record[partialIndex]);
                }
                ImportLocalizationData(category, partial, language, fileInfo, importType);
            }

            return true;
        }

        public bool ImportXLSXFile(string filePath, int importType)
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            string category = string.Empty;
            int partial = 0;
            string language = string.Empty;

            if (!fileName.StartsWith(LOCALIZATION_FILENAME_PREFIX)) return false;

            ExportFileType fileNameStatus = GetFileType(fileName, ref category, ref partial, ref language, importType);

            switch (fileNameStatus)
            {
                case ExportFileType.NONE:
                    return false;
                case ExportFileType.REGULAR_FILE:
                    return ImportRegularXLSXFile(filePath, category, partial, language, importType);
                case ExportFileType.EXTRA_FILE:
                    return ImportExtraXLSXFile(filePath, importType);
            }

            return true;
        }

        bool ImportRegularXLSXFile(string filePath, string category, int partial, string language, int importType)
        {
            XSSFWorkbook workBook;
            using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                workBook = new XSSFWorkbook(stream);
                stream.Close();
            }

            return ImportRegularXLSX(workBook, category, partial, language, importType);
        }

        bool ImportRegularXLSX(XSSFWorkbook workBook, string category, int partial, string language, int importType)
        {
            for (int sheetIndex = 0; sheetIndex < workBook.NumberOfSheets; sheetIndex++)
            {
                ISheet sheet = workBook.GetSheetAt(sheetIndex);
                string sheetName = sheet.SheetName;
                if (sheet == null) continue;

                //시트가 숨겨져 있으면 추출하지 않는다.
                if (workBook.IsSheetHidden(sheetIndex) == true)
                {
                    continue;
                }

                int keyIndex = -1;
                int sourceTextIndex = -1;
                int translationIndex = -1;
                int tagIndex = -1;
                int statusIndex = -1;
                int descIndex = -1;

                //0번째 줄이 field 가정
                IRow fieldRow = sheet.GetRow(0);
                if (fieldRow == null)
                {
                    //필드가 없다
                    continue;
                }

                int fieldNum = fieldRow.Cells.Count;
                for (int colIndex = 0; colIndex < fieldRow.Cells.Count; colIndex++)
                {
                    ICell cell = fieldRow.GetCell(colIndex);
                    if (cell == null)
                    {
                        //필드가 없다
                        continue;
                    }
                    cell.SetCellType(CellType.String);
                    string fieldName = cell.RichStringCellValue.ToString().ToLower();

                    switch (fieldName)
                    {
                        case "key":
                            keyIndex = colIndex;
                            break;
                        case "korean":
                            sourceTextIndex = colIndex;
                            break;
                        case "tag":
                            tagIndex = colIndex;
                            break;
                        case "status":
                            statusIndex = colIndex;
                            break;
                        case "description":
                        case "desc":
                            descIndex = colIndex;
                            break;
                    }

                    //Language ==> english 이렇게 바꿔야함
                    foreach (string configLanguage in configData.Languages)
                    {
                        if (fieldName == configLanguage.ToLower())
                        {
                            translationIndex = colIndex;
                            break;
                        }
                    }
                }

                //header 매칭 실패
                if (keyIndex == -1 || sourceTextIndex == -1 || tagIndex == -1 ||
                    statusIndex == -1 || descIndex == -1 || translationIndex == -1)
                {
                    continue;
                }

                for (int rowIdx = 1; rowIdx < sheet.LastRowNum + 1; rowIdx++)
                {
                    IRow row = sheet.GetRow(rowIdx);
                    if (row == null)
                    {
                        continue;
                    }

                    ICell statusCell = row.GetCell(statusIndex);
                    if (statusCell == null) continue;
                    string status = statusCell.StringCellValue;

                    ICell keyCell = row.GetCell(keyIndex);
                    if (keyCell == null) continue;
                    string key = keyCell.StringCellValue;

                    ICell translationCell = row.GetCell(translationIndex);
                    if (translationCell == null) continue;
                    string translation = translationCell.StringCellValue;

                    ICell originTextCell = row.GetCell(sourceTextIndex);
                    if (originTextCell == null) continue;
                    string originText = originTextCell.StringCellValue;

                    Dictionary<string, FileLine> fileInfo = localData.categoryInfos[category].partialInfos[partial].languageInfos[language].fileInfo;
                    ImportLocalizationData(category, partial, language, fileInfo, importType);
                }
            }

            return true;
        }

        bool ImportExtraXLSXFile(string filePath, int importType)
        {
            XSSFWorkbook workBook;
            using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                workBook = new XSSFWorkbook(stream);
                stream.Close();
            }

            return ImportExtraXLSX(workBook, importType);
        }

        bool ImportExtraXLSX(XSSFWorkbook workBook, int importType)
        {
            for (int sheetIndex = 0; sheetIndex < workBook.NumberOfSheets; sheetIndex++)
            {
                ISheet sheet = workBook.GetSheetAt(sheetIndex);
                string sheetName = sheet.SheetName;
                if (sheet == null) continue;

                //시트가 숨겨져 있으면 추출하지 않는다.
                if (workBook.IsSheetHidden(sheetIndex) == true)
                {
                    continue;
                }

                int keyIndex = -1;
                int sourceTextIndex = -1;
                int translationIndex = -1;
                int tagIndex = -1;
                int statusIndex = -1;
                int descIndex = -1;
                int categoryIndex = -1;
                int partialIndex = -1;

                //0번째 줄이 field 가정
                IRow fieldRow = sheet.GetRow(0);
                if (fieldRow == null)
                {
                    //필드가 없다
                    continue;
                }

                int fieldNum = fieldRow.Cells.Count;
                string language = string.Empty;
                for (int colIndex = 0; colIndex < fieldRow.Cells.Count; colIndex++)
                {
                    ICell cell = fieldRow.GetCell(colIndex);
                    if (cell == null)
                    {
                        //필드가 없다
                        continue;
                    }
                    cell.SetCellType(CellType.String);
                    string fieldName = cell.RichStringCellValue.ToString().ToLower();

                    switch (fieldName)
                    {
                        case "key":
                            keyIndex = colIndex;
                            break;
                        case "korean":
                            sourceTextIndex = colIndex;
                            break;
                        case "tag":
                            tagIndex = colIndex;
                            break;
                        case "status":
                            statusIndex = colIndex;
                            break;
                        case "description":
                        case "desc":
                            descIndex = colIndex;
                            break;
                        case "category":
                            categoryIndex = colIndex;
                            break;
                        case "partial":
                            partialIndex = colIndex;
                            break;
                    }

                    //Language ==> english 이렇게 바꿔야함
                    foreach (string configLanguage in configData.Languages)
                    {
                        if (fieldName == configLanguage.ToLower())
                        {
                            translationIndex = colIndex;
                            language = configLanguage;
                            break;
                        }
                    }
                }

                //header 매칭 실패시
                try
                {
                    if (keyIndex != -1 || sourceTextIndex != -1 || tagIndex != -1 || statusIndex != -1 || descIndex != -1
                        || categoryIndex == -1 || partialIndex == -1) { }
                }
                catch (Exception e)
                {
                    log.Error(e.Message);
                    return false;
                }

                for (int rowIdx = 1; rowIdx < sheet.LastRowNum + 1; rowIdx++)
                {
                    IRow row = sheet.GetRow(rowIdx);
                    if (row == null)
                    {
                        continue;
                    }

                    ICell statusCell = row.GetCell(statusIndex);
                    if (statusCell == null) continue;
                    string status = statusCell.StringCellValue;

                    ICell keyCell = row.GetCell(keyIndex);
                    if (keyCell == null) continue;
                    string key = keyCell.StringCellValue;

                    ICell translationCell = row.GetCell(translationIndex);
                    if (translationCell == null) continue;
                    string translation = translationCell.StringCellValue;

                    ICell categoryCell = row.GetCell(categoryIndex);
                    if (categoryCell == null) continue;
                    string category = categoryCell.StringCellValue;

                    ICell partialCell = row.GetCell(partialIndex);
                    if (partialCell == null) continue;
                    int partial = Int32.Parse(partialCell.StringCellValue);

                    ICell originTextCell = row.GetCell(sourceTextIndex);
                    if (originTextCell == null) continue;
                    string originText = originTextCell.StringCellValue;

                    Dictionary<string, FileLine> fileLineDic = localData.categoryInfos[category].partialInfos[partial].languageInfos[language].fileInfo;
                    ImportLocalizationData(category, partial, language, fileLineDic, importType);
                }
            }

            return true;
        }

        public bool ImportZipFile(string filePath, Action<string> SetMessage, int importType)
        {
            using (Stream fsInput = File.OpenRead(filePath))
            using (var zf = new ZipFile(fsInput))
            {
                foreach (ZipEntry zipEntry in zf)
                {
                    if (!zipEntry.IsFile)
                    {
                        continue;
                    }

                    string category = string.Empty;
                    int partial = -1;
                    string language = string.Empty;

                    ExportFileType fileNameStatus = GetFileType(Path.GetFileNameWithoutExtension(zipEntry.Name),
                        ref category, ref partial, ref language, importType);

                    using (Stream s = zf.GetInputStream(zipEntry))
                    {
                        switch (fileNameStatus)
                        {
                            case ExportFileType.NONE:
                                continue;
                            case ExportFileType.REGULAR_FILE:
                                switch (Path.GetExtension(zipEntry.Name))
                                {
                                    case ".csv":
                                        SetMessage(string.Format("Import {0}", zipEntry.Name));
                                        ImportRegularCSV(s, category, partial, language, importType);
                                        break;
                                    case ".xlsx":
                                        SetMessage(string.Format("Import {0}", zipEntry.Name));
                                        ImportRegularXLSXZip(s, category, partial, language, importType);
                                        break;
                                    default:
                                        break;
                                }
                                break;
                            case ExportFileType.EXTRA_FILE:
                                switch (Path.GetExtension(zipEntry.Name))
                                {
                                    case ".csv":
                                        SetMessage(string.Format("Import {0}", zipEntry.Name));
                                        ImportExtraCSV(s, category, partial, language, importType);
                                        break;
                                    case ".xlsx":
                                        SetMessage(string.Format("Import {0}", zipEntry.Name));
                                        ImportExtraXLSXZip(s, importType);
                                        break;
                                    default:
                                        break;
                                }
                                break;
                        }
                    }
                }
            }

            return true;
        }

        bool ImportRegularXLSXZip(Stream stream, string category, int partial, string language, int importType)
        {
            XSSFWorkbook workBook;

            try
            {
                workBook = new XSSFWorkbook(stream);
            }
            catch (Exception e)
            {
                log.Error(e.ToString());
                return true;
            }
            finally
            {
                stream.Close();
            }


            return ImportRegularXLSX(workBook, category, partial, language, importType);
        }

        bool ImportExtraXLSXZip(Stream stream, int importType)
        {
            XSSFWorkbook workBook;

            try
            {
                workBook = new XSSFWorkbook(stream);
            }
            catch (Exception e)
            {
                log.Error(e.ToString());
                return true;
            }
            finally
            {
                stream.Close();
            }


            return ImportExtraXLSX(workBook, importType);
        }

        bool ImportRegularCSV(Stream stream, string category, int partial, string language, int importType)
        {
            //정규번역파일 Import
            using (var reader = new StreamReader(stream))
            using (var csv = new CsvParser(reader, CultureInfo.InvariantCulture))
            {
                Dictionary<string, FileLine> fileLineDic = new Dictionary<string, FileLine>();
                bool isLangKorean = language.Equals("Korean");
                string[] header = csv.Read();

                int keyIndex = -1;
                int sourceTextIndex = -1;
                int translationIndex = -1;
                int tagIndex = -1;
                int statusIndex = -1;
                int descIndex = -1;
                int translationStatusIndex = -1;

                //Header 분석
                for (int i = 0; i < header.Length; i++)
                {
                    switch (header[i].ToLower())
                    {
                        case "key":
                            keyIndex = i;
                            break;
                        case "korean":
                            sourceTextIndex = i;
                            break;
                        case "tag":
                            tagIndex = i;
                            break;
                        case "status":
                            statusIndex = i;
                            break;
                        case "description":
                        case "desc":
                            descIndex = i;
                            break;
                        case "translationstatus":
                            translationStatusIndex = i;
                            break;
                    }

                    //Language ==> english 이렇게 바꿔야함
                    foreach (string configLanguage in configData.Languages)
                    {
                        if (header[i].ToLower() == configLanguage.ToLower())
                        {
                            translationIndex = i;
                            break;
                        }
                    }
                }

                //header 매칭 실패시
                try
                {
                    if (keyIndex != -1 || sourceTextIndex != -1 || tagIndex != -1 || statusIndex != -1 || descIndex != -1) { }
                    if (isLangKorean != false && translationIndex != -1) { }
                }
                catch (Exception e)
                {
                    log.Error(e.Message);
                    return false;
                }

                while (true)
                {
                    string[] record = csv.Read();
                    if (record == null)
                        break;

                    //빈 key가 입력되는 것 방지
                    if (string.IsNullOrEmpty(record[keyIndex]) == true)
                        continue;

                    FileLine line = new FileLine();

                    line.key = record[keyIndex];
                    line.sourceText = record[sourceTextIndex];
                    line.tag = record[tagIndex];
                    line.status = record[statusIndex];
                    line.desc = record[descIndex];
                    if (isLangKorean == false)
                    {
                        line.translation = record[translationIndex];
                    }
                    else //Korean
                    {
                        if (translationStatusIndex != -1)
                        {
                            line.translationStatus = record[translationStatusIndex];
                        }
                    }

                    var addKey = line.key;
                    while (fileLineDic.ContainsKey(addKey))
                    {
                        addKey = string.Format("{0} (1)", addKey);
                    }

                    fileLineDic.Add(addKey, line);
                }
                //localData.AddFileInfo(category, partial, language, fileLineDic);
                ImportLocalizationData(category, partial, language, fileLineDic, importType);
            }

            return true;
        }

        bool ImportExtraCSV(Stream stream, string category, int partial, string language, int importType)
        {
            //추가번역파일 Import
            using (var reader = new StreamReader(stream))
            using (var csv = new CsvParser(reader, CultureInfo.InvariantCulture))
            {
                Dictionary<string, FileLine> fileLineDic = new Dictionary<string, FileLine>();
                bool isLangKorean = language.Equals("Korean");
                string[] header = csv.Read();

                int keyIndex = -1;
                int sourceTextIndex = -1;
                int translationIndex = -1;
                int tagIndex = -1;
                int statusIndex = -1;
                int descIndex = -1;
                int translationStatusIndex = -1;

                //Header 분석
                for (int i = 0; i < header.Length; i++)
                {
                    switch (header[i].ToLower())
                    {
                        case "key":
                            keyIndex = i;
                            break;
                        case "korean":
                            sourceTextIndex = i;
                            break;
                        case "tag":
                            tagIndex = i;
                            break;
                        case "status":
                            statusIndex = i;
                            break;
                        case "description":
                        case "desc":
                            descIndex = i;
                            break;
                        case "translationstatus":
                            translationStatusIndex = i;
                            break;
                    }

                    //Language ==> english 이렇게 바꿔야함
                    foreach (string configLanguage in configData.Languages)
                    {
                        if (header[i].ToLower() == configLanguage.ToLower())
                        {
                            translationIndex = i;
                            break;
                        }
                    }
                }

                //header 매칭 실패시
                try
                {
                    if (keyIndex != -1 || sourceTextIndex != -1 || tagIndex != -1 || statusIndex != -1 || descIndex != -1) { }
                    if (isLangKorean != false && translationIndex != -1) { }
                }
                catch (Exception e)
                {
                    log.Error(e.Message);
                    return false;
                }

                while (true)
                {
                    string[] record = csv.Read();
                    if (record == null)
                        break;

                    //빈 key가 입력되는 것 방지
                    if (string.IsNullOrEmpty(record[keyIndex]) == true)
                        continue;

                    FileLine line = new FileLine();

                    line.key = record[keyIndex];
                    line.sourceText = record[sourceTextIndex];
                    line.tag = record[tagIndex];
                    line.status = record[statusIndex];
                    line.desc = record[descIndex];
                    if (isLangKorean == false)
                    {
                        line.translation = record[translationIndex];
                    }
                    else //Korean
                    {
                        if (translationStatusIndex != -1)
                        {
                            line.translationStatus = record[translationStatusIndex];
                        }
                    }

                    var addKey = line.key;
                    while (fileLineDic.ContainsKey(addKey))
                    {
                        addKey = string.Format("{0} (1)", addKey);
                    }

                    fileLineDic.Add(addKey, line);
                }
                //localData.AddFileInfo(category, partial, language, fileLineDic);
                ImportLocalizationData(category, partial, language, fileLineDic, importType);
            }

            return true;
        }

        #endregion

        #region Export

        public void ExportFiles(string directory, string exportType, string collectType, bool isZip, string tag, List<string> selectedTemplateList,
            bool isExportNewlyUpdatedTBTOnly, LocalizationFileType exportFileType, string dirName)
        {
            Dictionary<string, List<string>> totalExportLanaugeList = new Dictionary<string, List<string>>();

            switch (collectType)
            {
                case "All Language":
                    totalExportLanaugeList = ExportAllLanguage(directory, exportType, isZip, tag, isExportNewlyUpdatedTBTOnly, exportFileType);
                    break;
                case "By Language":
                    totalExportLanaugeList = ExportByLanguage(directory, exportType, isZip, tag, isExportNewlyUpdatedTBTOnly, exportFileType);
                    break;
                case "By Custom Template":
                    totalExportLanaugeList = ExportCustomLanguage(directory, exportType, isZip, tag, selectedTemplateList, isExportNewlyUpdatedTBTOnly, exportFileType);
                    break;
                default:
                    break;
            }

            //Export 경로에 폴더 생성
            string exportDirName = string.IsNullOrEmpty(dirName) == true ? GetExportDirectoryName(exportType, tag) : dirName;

            foreach (KeyValuePair<string, List<string>> languageList in totalExportLanaugeList)
            {
                //폴더 안에 zip 또는 Export 파일 생성
                if (!isZip)
                {
                    string exportDir = GetExportDirectoryNameByCollectType(collectType, directory, exportDirName, languageList.Key);
                    Directory.CreateDirectory(exportDir);

                    //Export All이면 Regular 파일 형태, 나머지는 Extra 파일 형태로 Export
                    if (exportType.Equals("ExportAll"))
                    {
                        ExportRegularFile(exportDir, exportType, tag, languageList.Value, exportFileType);
                    }
                    else
                    {
                        ExportExtraFile(exportDir, exportType, tag, languageList.Value, isExportNewlyUpdatedTBTOnly, exportFileType);
                        ExportTBTNumList(exportDir, isExportNewlyUpdatedTBTOnly);
                    }
                }
                else
                {
                    MemoryStream outputMemStream;

                    //Export All이면 Regular 파일 형태, 나머지는 Extra 파일 형태로 Export
                    if (exportType.Equals("ExportAll"))
                    {
                        outputMemStream = CreateToExportZipMemStream(ExportRegularZipEntry(exportType, tag, languageList.Value, exportFileType));
                    }
                    else
                    {
                        outputMemStream = CreateToExportZipMemStream(ExportExtraZipEntry(exportType, tag, languageList.Value, isExportNewlyUpdatedTBTOnly, exportFileType));
                    }

                    var zipName = string.Format("{0}.zip", GetExportDirectoryNameByCollectType(collectType, directory, exportDirName, languageList.Key));
                    using (FileStream file = new FileStream(Path.Combine(directory, zipName), FileMode.Create, System.IO.FileAccess.Write))
                    {
                        byte[] bytes = new byte[outputMemStream.Length];
                        outputMemStream.Read(bytes, 0, (int)outputMemStream.Length);
                        file.Write(bytes, 0, bytes.Length);
                        outputMemStream.Close();
                    }
                }
            }

            if (!isZip)
            {
                ExportTBTNumList(directory, isExportNewlyUpdatedTBTOnly);
            }

            //최근 collectType, zip 여부 기록
            RegistryManager.Instance.StoreStr(ChooseExportView.GetRecentCollectTypeKey(exportType), collectType);
            RegistryManager.Instance.StoreStr(ChooseExportView.GetRecentIsZipKey(exportType), isZip.ToString());
            RegistryManager.Instance.StoreStr(ChooseExportView.GetRecentExportFileTypeKey(exportType), exportFileType.ToString());
        }

        public string GetExportDirectoryNameByCollectType(string collectType, string path, string dirName, string name)
        {
            string directory = string.Empty;
            switch (collectType)
            {
                case "All Language":
                    directory = Path.Combine(path, dirName);
                    break;
                case "By Language":
                case "By Custom Template":
                    directory = Path.Combine(path, string.Format("{0}_{1}", dirName, name));
                    break;
                default:
                    break;
            }

            return directory;
        }

        public string GetExportDirectoryName(string exportType, string tag = null, bool isExistTime = true)
        {
            string directory = string.Empty;
            switch (exportType)
            {
                case "ExportAll":
                    directory = EXPORT_ALL;
                    break;
                case "ExportTBT":
                    directory = EXPORT_TBT;
                    break;
                case "ExportTag":
                    if (!string.IsNullOrEmpty(tag))
                    {
                        directory = string.Format("{0}_{1}", EXPORT_TAG, tag);
                    }
                    break;
                default:
                    break;
            }

            if (!isExistTime)
            {
                return string.IsNullOrEmpty(directory) ?
                string.Empty : directory;
            }

            return string.IsNullOrEmpty(directory) ?
                string.Empty : string.Format("{0}_{1}", DateTime.Now.ToString("yyMMdd_HHmmss"), directory);
        }

        public Dictionary<string, List<string>> ExportAllLanguage(string directory, string exportType, bool isZip, string tag, bool isExportNewlyUpdatedTBTOnly, LocalizationFileType exportFileType)
        {
            Dictionary<string, List<string>> toalLanugageList = new Dictionary<string, List<string>>();
            List<string> languageList = new List<string>();
            foreach (string language in configData.Languages)
            {
                if (!exportType.Equals("ExportTag") && language.Equals("Korean")) continue;
                languageList.Add(language);
            }

            toalLanugageList.Add("All", languageList);

            return toalLanugageList;
        }

        public Dictionary<string, List<string>> ExportByLanguage(string directory, string exportType, bool isZip, string tag, bool isExportNewlyUpdatedTBTOnly, LocalizationFileType exportFileType)
        {
            Dictionary<string, List<string>> toalLanugageList = new Dictionary<string, List<string>>();

            //LanguageList에 들어간 language만 Export
            foreach (string language in configData.Languages)
            {
                if (!exportType.Equals("ExportTag") && language.Equals("Korean")) continue;

                List<string> languageList = new List<string>();
                languageList.Add(language);
                toalLanugageList.Add(language, languageList);
            }

            return toalLanugageList;
        }

        public Dictionary<string, List<string>> ExportCustomLanguage(string directory, string exportType, bool isZip, string tag, List<string> selectedTemplateList,
            bool isExportNewlyUpdatedTBTOnly, LocalizationFileType exportFileType)
        {
            Dictionary<string, List<string>> toalLanugageList = new Dictionary<string, List<string>>();

            foreach (string templateName in selectedTemplateList)
            {
                toalLanugageList.Add(templateName, configData.GetExportTemplate(templateName).languageList);
            }

            return toalLanugageList;
        }

        //정규번역파일 형식으로 export
        public bool ExportRegularFile(string directory, string exportType, string tag, List<string> exportLanguageList, LocalizationFileType exportFileType)
        {
            bool isPartiallyExport = localData._isPartiallyExport;

            foreach (KeyValuePair<string, CategoryInfo> category in localData.categoryInfos)
            {
                //특정 category만 export할 때
                if (isPartiallyExport == true && category.Value.isExport == false) continue;

                foreach (PartialInfo partialInfo in category.Value.partialInfos)
                {
                    //특정 category_partial만 export할 때
                    if (isPartiallyExport == true && partialInfo.isExport == false) continue;

                    foreach (KeyValuePair<string, LangFile> language in partialInfo.languageInfos)
                    {
                        bool isExportLanguage = false;
                        foreach (string exportLang in exportLanguageList)
                        {
                            if (language.Key.Equals(exportLang))
                            {
                                isExportLanguage = true;
                                break;
                            }
                        }

                        //export 대상이 아닌 언어인 경우 continue;
                        if (!isExportLanguage) continue;

                        List<FileLine> exportFile = new List<FileLine>();
                        foreach (KeyValuePair<string, FileLine> line in language.Value.fileInfo)
                        {
                            //Export All : Regular, Extra : TBT, Tag 형태
                            //Regular -> Export All만 해당하므로 TBT, Tag 확인해서 넣을 필요가 없다.
                            FileLine exportLine = line.Value.ToNewFileLine();
                            exportLine.key = line.Key;
                            exportFile.Add(exportLine);
                        }

                        //파일내용이 없으면 continue
                        if (exportFile.Count == 0) continue;


                        try
                        {
                            string filepath = Path.Combine(directory, LocalizationData.GetFileName(category.Key, partialInfo._partial, language.Key, exportFileType));

                            switch (exportFileType)
                            {
                                case LocalizationFileType.CSV:
                                    SaveData.SaveRegularCSV(filepath, language.Key, exportFile);
                                    break;
                                case LocalizationFileType.XLSX:
                                    string sheetName = LocalizationData.GetFileName(category.Key, partialInfo._partial, language.Key, LocalizationFileType.NONE);
                                    SaveData.SaveRegularXLSX(filepath, sheetName, language.Key, exportFile);
                                    break;
                                default:
                                    break;
                            }
                        }
                        catch (Exception e)
                        {
                            log.Error(e.ToString());
                        }
                    }
                }
            }

            return true;
        }

        public List<ExportZipContent> ExportRegularZipEntry(string exportType, string tag, List<string> exportLanguageList, LocalizationFileType exportFileType)
        {
            List<ExportZipContent> exportZipContents = new List<ExportZipContent>();
            bool isPartiallyExport = localData._isPartiallyExport;

            foreach (KeyValuePair<string, CategoryInfo> category in localData.categoryInfos)
            {
                //특정 category만 export할 때
                if (isPartiallyExport == true && category.Value.isExport == false) continue;

                foreach (PartialInfo partialInfo in category.Value.partialInfos)
                {
                    //특정 category_partial만 export할 때
                    if (isPartiallyExport == true && partialInfo.isExport == false) continue;

                    foreach (KeyValuePair<string, LangFile> language in partialInfo.languageInfos)
                    {
                        bool isExportLanguage = false;
                        foreach (string exportLang in exportLanguageList)
                        {
                            if (language.Key.Equals(exportLang))
                            {
                                isExportLanguage = true;
                                break;
                            }
                        }

                        //export 대상이 아닌 언어인 경우 continue;
                        if (!isExportLanguage) continue;

                        List<FileLine> exportFile = new List<FileLine>();
                        foreach (KeyValuePair<string, FileLine> line in language.Value.fileInfo)
                        {
                            //Export All : Regular, Extra : TBT, Tag 형태
                            //Regular -> Export All만 해당하므로 TBT, Tag 확인해서 넣을 필요가 없다.
                            FileLine exportLine = line.Value.ToNewFileLine();
                            exportLine.key = line.Key;
                            exportFile.Add(exportLine);
                        }

                        //파일내용이 없으면 continue
                        if (exportFile.Count == 0) continue;

                        var fileName = LocalizationData.GetFileName(category.Key, partialInfo._partial, language.Key, exportFileType);

                        ExportZipContent content = null;
                        switch (exportFileType)
                        {
                            case LocalizationFileType.CSV:
                                content = SaveData.ZipRegularCSV(fileName, language.Key, exportFile);
                                break;
                            case LocalizationFileType.XLSX:
                                string sheetName = LocalizationData.GetFileName(category.Key, partialInfo._partial, language.Key, LocalizationFileType.NONE);
                                content = SaveData.ZipRegularXLSX(fileName, sheetName, language.Key, exportFile);
                                break;
                            default:
                                break;
                        }

                        if (content != null)
                        {
                            exportZipContents.Add(content);
                        }
                    }
                }
            }

            //TBTNumList 추가
            ExportZipContent exportZipContent = ExportTBTNumListZip();

            if (exportZipContent != null)
            {
                exportZipContents.Add(exportZipContent);
            }

            return exportZipContents;
        }

        //Extra파일 형식으로 export
        public bool ExportExtraFile(string directory, string exportType, string tag, List<string> exportLanguageList,
            bool isExportNewlyUpdatedTBTOnly, LocalizationFileType exportFileType)
        {
            Dictionary<string, List<TBTLine>> extraFileLineDic = new Dictionary<string, List<TBTLine>>();
            bool isPartiallyExport = localData._isPartiallyExport;

            foreach (KeyValuePair<string, CategoryInfo> category in localData.categoryInfos)
            {
                //특정 category만 export할 때
                if (isPartiallyExport == true && category.Value.isExport == false) continue;

                foreach (PartialInfo partialInfo in category.Value.partialInfos)
                {
                    //특정 category_partial만 export할 때
                    if (isPartiallyExport == true && partialInfo.isExport == false) continue;

                    foreach (KeyValuePair<string, LangFile> language in partialInfo.languageInfos)
                    {
                        bool isExportLanguage = false;
                        foreach (string exportLang in exportLanguageList)
                        {
                            if (language.Key.Equals(exportLang))
                            {
                                isExportLanguage = true;
                                break;
                            }
                        }

                        //export 대상이 아닌 언어인 경우 continue;
                        if (!isExportLanguage) continue;

                        //ExtraDic 없으면 생성
                        if (!extraFileLineDic.ContainsKey(language.Key))
                        {
                            List<TBTLine> fileLines = new List<TBTLine>();
                            extraFileLineDic.Add(language.Key, fileLines);
                        }

                        List<TBTLine> exportFile = extraFileLineDic[language.Key];
                        foreach (KeyValuePair<string, FileLine> line in language.Value.fileInfo)
                        {
                            TBTLine tbtLine = line.Value.ToTBTLine(category.Key, partialInfo._partial);
                            tbtLine.key = line.Key;

                            switch (exportType)
                            {
                                //Export All : Regular, Extra : TBT, Tag 형태
                                //Extra -> Export TBT, Tag만 해당하므로 All 확인해서 넣을 필요가 없다.
                                case "ExportTBT":
                                    if (line.Value.status.Equals(STATUS_NEW) ||
                                        line.Value.status.Equals(STATUS_NEW_ALT) ||
                                        line.Value.status.Equals(STATUS_UPDATE) ||
                                        line.Value.status.Equals(STATUS_UPDATE_ALT))
                                    {
                                        if (isExportNewlyUpdatedTBTOnly == true && line.Value.isModified == false)
                                        {
                                            break;
                                        }

                                        //Korean만 수정된 것이면 Export 대상 아님.
                                        if (line.Value.isModifiedNotExport == true)
                                        {
                                            break;
                                        }

                                        exportFile.Add(tbtLine);
                                    }
                                    break;
                                case "ExportTag":
                                    if (!string.IsNullOrEmpty(tag) && line.Value.tag.Equals(tag))
                                    {
                                        exportFile.Add(tbtLine);
                                    }
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                }
            }

            //Extra File 형태로 save
            foreach (KeyValuePair<string, List<TBTLine>> extraFileLines in extraFileLineDic)
            {
                //파일내용이 없으면 continue
                if (extraFileLines.Value.Count == 0) continue;

                string fileName = exportType.Equals("ExportTag")
                    ? string.Format("{0}Tag{1}{2}", LOCALIZATION_FILENAME_PREFIX, tag, extraFileLines.Key)
                    : string.Format("{0}Extra{1}", LOCALIZATION_FILENAME_PREFIX, extraFileLines.Key);

                string filepath = string.Empty;
                switch (exportFileType)
                {
                    case LocalizationFileType.CSV:
                        filepath = string.Format("{0}.csv", fileName);
                        filepath = Path.Combine(directory, filepath);
                        SaveData.SaveExtraCSV(filepath, extraFileLines.Key, extraFileLines.Value);
                        break;
                    case LocalizationFileType.XLSX:
                        filepath = string.Format("{0}.xlsx", fileName);
                        filepath = Path.Combine(directory, filepath);
                        SaveData.SaveExtraXLSX(filepath, fileName, extraFileLines.Key, extraFileLines.Value);
                        break;
                    default:
                        break;
                }
            }

            return true;
        }

        public List<ExportZipContent> ExportExtraZipEntry(string exportType, string tag, List<string> exportLanguageList,
            bool isExportNewlyUpdatedTBTOnly, LocalizationFileType exportFileType)
        {
            List<ExportZipContent> exportZipContents = new List<ExportZipContent>();
            Dictionary<string, List<TBTLine>> extraFileLineDic = new Dictionary<string, List<TBTLine>>();
            bool isPartiallyExport = localData._isPartiallyExport;

            foreach (KeyValuePair<string, CategoryInfo> category in localData.categoryInfos)
            {
                //특정 category만 export할 때
                if (isPartiallyExport == true && category.Value.isExport == false) continue;

                foreach (PartialInfo partialInfo in category.Value.partialInfos)
                {
                    //특정 category_partial만 export할 때
                    if (isPartiallyExport == true && partialInfo.isExport == false) continue;

                    foreach (KeyValuePair<string, LangFile> language in partialInfo.languageInfos)
                    {
                        bool isExportLanguage = false;
                        foreach (string exportLang in exportLanguageList)
                        {
                            if (language.Key.Equals(exportLang))
                            {
                                isExportLanguage = true;
                                break;
                            }
                        }

                        //export 대상이 아닌 언어인 경우 continue;
                        if (!isExportLanguage) continue;

                        //ExtraDic 없으면 생성
                        if (!extraFileLineDic.ContainsKey(language.Key))
                        {
                            List<TBTLine> fileLines = new List<TBTLine>();
                            extraFileLineDic.Add(language.Key, fileLines);
                        }

                        List<TBTLine> exportFile = extraFileLineDic[language.Key];
                        foreach (KeyValuePair<string, FileLine> line in language.Value.fileInfo)
                        {
                            TBTLine tbtLine = line.Value.ToTBTLine(category.Key, partialInfo._partial);
                            tbtLine.key = line.Key;

                            switch (exportType)
                            {
                                //Export All : Regular, Extra : TBT, Tag 형태
                                //Extra -> Export TBT, Tag만 해당하므로 All 확인해서 넣을 필요가 없다.
                                case "ExportTBT":
                                    if (line.Value.status.Equals(STATUS_NEW) ||
                                        line.Value.status.Equals(STATUS_NEW_ALT) ||
                                        line.Value.status.Equals(STATUS_UPDATE) ||
                                        line.Value.status.Equals(STATUS_UPDATE_ALT))
                                    {
                                        if (isExportNewlyUpdatedTBTOnly == true && line.Value.isModified == false)
                                        {
                                            break;
                                        }

                                        //Korean만 수정된 것이면 Export 대상 아님.
                                        if (line.Value.isModifiedNotExport == true)
                                        {
                                            break;
                                        }

                                        exportFile.Add(tbtLine);
                                    }
                                    break;
                                case "ExportTag":
                                    if (!string.IsNullOrEmpty(tag) && line.Value.tag.Equals(tag))
                                    {
                                        exportFile.Add(tbtLine);
                                    }
                                    break;
                                default:
                                    break;
                            }
                        }
                    }

                }
            }

            //Extra File 형태로 save
            foreach (KeyValuePair<string, List<TBTLine>> extraFileLines in extraFileLineDic)
            {
                //파일내용이 없으면 continue
                if (extraFileLines.Value.Count == 0) continue;

                string sheetName = exportType.Equals("ExportTag")
                    ? string.Format("{0}Tag{1}{2}", LOCALIZATION_FILENAME_PREFIX, tag, extraFileLines.Key)
                    : string.Format("{0}Extra{1}", LOCALIZATION_FILENAME_PREFIX, extraFileLines.Key);

                string fileName = sheetName;
                ExportZipContent content = null;
                switch (exportFileType)
                {
                    case LocalizationFileType.CSV:
                        fileName = string.Format("{0}.csv", fileName);
                        content = SaveData.ZipExtraCSV(fileName, extraFileLines.Key, extraFileLines.Value);
                        break;
                    case LocalizationFileType.XLSX:
                        fileName = string.Format("{0}.xlsx", fileName);
                        content = SaveData.ZipExtraXLSX(fileName, sheetName, extraFileLines.Key, extraFileLines.Value);
                        break;
                    default:
                        break;
                }

                if (content != null)
                {
                    exportZipContents.Add(content);
                }
            }

            //TBTNumList 추가
            ExportZipContent exportZipContent = ExportTBTNumListZip(isExportNewlyUpdatedTBTOnly);
            if (exportZipContent != null)
            {
                exportZipContents.Add(exportZipContent);
            }

            return exportZipContents;
        }

        //Export TBTNum List
        public bool ExportTBTNumList(string directory, bool isExportNewlyUpdatedTBTOnly)
        {
            var fileName = string.Format("To_be_translated_List_{0}.txt", DateTime.Now.ToString("yyMMdd_HHmmss"));
            var filePath = Path.Combine(directory, fileName);

            string tbtNumListStr = GetTBTNumListStr(isExportNewlyUpdatedTBTOnly);
            if (string.IsNullOrEmpty(tbtNumListStr) == false)
            {
                File.WriteAllText(filePath, tbtNumListStr);
            }

            return true;
        }

        //Export TBTNum List Zip
        public ExportZipContent ExportTBTNumListZip(bool isExportNewlyUpdatedTBTOnly = true)
        {
            var fileName = string.Format("To_be_translated_List_{0}.txt", DateTime.Now.ToString("yyMMdd_HHmmss"));

            string tbtNumListStr = GetTBTNumListStr(isExportNewlyUpdatedTBTOnly);
            if (string.IsNullOrEmpty(tbtNumListStr) == true)
            {
                return null;
            }
            return new ExportZipContent(fileName, Encoding.UTF8.GetBytes(tbtNumListStr));
        }

        //Category, Partial 별 Add, Fix 개수 string
        public string GetTBTNumListStr(bool isExportNewlyUpdatedTBTOnly)
        {
            //[Localization_Basic0], Add, Fix 개수
            Dictionary<string, TBTNum> TBTNumDic = new Dictionary<string, TBTNum>();

            foreach (string category in configData.Categories)
            {
                CategoryInfo categoryInfo = localData.categoryInfos[category];
                if (localData._isPartiallyExport == true && categoryInfo.isExport == false) continue;

                foreach (var partialInfo in categoryInfo.partialInfos)
                {
                    if (localData._isPartiallyExport == true && partialInfo.isExport == false) continue;

                    LangFile koreanInfo = partialInfo.languageInfos["Korean"];
                    foreach (FileLine line in koreanInfo.fileInfo.Values)
                    {
                        //if (isExportNewlyUpdatedTBTOnly == true && line.isModified == false)
                        //{
                        //	continue;
                        //}

                        //Korean만 수정된 것이면 Export 대상 아님.
                        if (line.isModifiedNotExport == true)
                        {
                            continue;
                        }

                        string key = partialInfo._partial == 0 ? string.Format("[Localization_{0}]", category)
                            : string.Format("[Localization_{0}{1}]", category, partialInfo._partial);

                        if (!TBTNumDic.ContainsKey(key))
                        {
                            TBTNumDic.Add(key, new TBTNum());
                        }


                        switch (line.translationStatus)
                        {
                            case ATS_ADD:
                                TBTNumDic[key].addNum++;
                                break;
                            case ATS_UPDATE:
                            case ATS_PT:
                                TBTNumDic[key].updateNum++;
                                break;
                            default:
                                break;
                        }
                        //switch (line.status)
                        //{
                        //	case STATUS_NEW:
                        //	case STATUS_NEW_ALT:
                        //		TBTNumDic[key].addNum++;
                        //		break;
                        //	case STATUS_UPDATE:
                        //	case STATUS_UPDATE_ALT:
                        //		TBTNumDic[key].updateNum++;
                        //		break;
                        //	default:
                        //		break;
                        //}
                    }
                }
            }

            string tbtNumListStr = "[TBT List]\n";
            foreach (KeyValuePair<string, TBTNum> TBTNumPair in TBTNumDic)
            {
                if (TBTNumPair.Value.addNum == 0 && TBTNumPair.Value.updateNum == 0) continue;

                tbtNumListStr = string.Format("{0}{1} {2}\n", tbtNumListStr, TBTNumPair.Key, TBTNumPair.Value.ToTBTNumStr());
            }

            return tbtNumListStr == "[TBT List]\n" ? string.Empty : tbtNumListStr;
        }

        public MemoryStream CreateToExportZipMemStream(List<ExportZipContent> exportZipContents)
        {
            var outputMemStream = new MemoryStream();
            using (var zipStream = new ZipOutputStream(outputMemStream))
            {

                // 0-9, 9 being the highest level of compression
                zipStream.SetLevel(3);

                foreach (var exportZipContent in exportZipContents)
                {
                    ZipEntry newEntry = new ZipEntry(exportZipContent.entryName);
                    newEntry.DateTime = DateTime.Now;

                    zipStream.PutNextEntry(newEntry);

                    StreamUtils.Copy(new MemoryStream(exportZipContent.content), zipStream, new byte[4096]);
                    zipStream.CloseEntry();
                }

                // Stop ZipStream.Dispose() from also Closing the underlying stream.
                zipStream.IsStreamOwner = false;
            }

            outputMemStream.Position = 0;
            return outputMemStream;
        }

        #endregion

        //Korean파일의 Status 수정
        public void SetTranslationStatus()
        {
            foreach (string category in configData.Categories)
            {
                CategoryInfo categoryInfo = localData.categoryInfos[category];
                foreach (PartialInfo partialInfo in categoryInfo.partialInfos)
                {
                    //Korean 파일을 읽어서 다른 Language 파일 순서로 Status 수정
                    Dictionary<string, FileLine> sourceLangInfo = partialInfo.languageInfos[ConfigData.SourceLanguage].fileInfo;

                    foreach (KeyValuePair<string, FileLine> sourceLangLine in sourceLangInfo)
                    {
                        int AddCnt = 0;
                        int FixCnt = 0;
                        int translatedCnt = 0;

                        foreach (KeyValuePair<string, LangFile> languageInfo in partialInfo.languageInfos)
                        {
                            if (languageInfo.Value.IsSourceLanguage())
                                continue;

                            Dictionary<string, FileLine> targetLangFileInfo = languageInfo.Value.fileInfo;

                            if (targetLangFileInfo.ContainsKey(sourceLangLine.Key))
                            {
                                FileLine targetLangLFileLine = targetLangFileInfo[sourceLangLine.Key];
                                if (!string.IsNullOrEmpty(targetLangLFileLine.translation) &&
                                    string.CompareOrdinal(sourceLangLine.Value.sourceText, targetLangLFileLine.sourceText) == 0 &&
                                    targetLangLFileLine.IsStatusUpdate() == false)
                                {
                                    targetLangLFileLine.status = STATUS_TRANSLATED;
                                }

                                switch (targetLangLFileLine.status)
                                {
                                    case STATUS_NEW:
                                    case STATUS_NEW_ALT:
                                        if (configData.ProjectVersion < 1)
                                            targetLangLFileLine.status = STATUS_NEW_ALT;
                                        else
                                            targetLangLFileLine.status = STATUS_NEW;
                                        AddCnt++;
                                        break;

                                    case STATUS_UPDATE:
                                    case STATUS_UPDATE_ALT:
                                        if (configData.ProjectVersion < 1)
                                            targetLangLFileLine.status = STATUS_UPDATE_ALT;
                                        else
                                            targetLangLFileLine.status = STATUS_UPDATE;
                                        FixCnt++;
                                        break;
                                    case STATUS_TRANSLATED:
                                        translatedCnt++;
                                        break;
                                    default:
                                        break;
                                }
                            }
                        }

                        if (translatedCnt > 0 && translatedCnt < partialInfo.languageInfos.Count - 1)
                        {
                            sourceLangLine.Value.translationStatus = ATS_PT;
                        }
                        else if (translatedCnt == partialInfo.languageInfos.Count - 1)
                        {
                            sourceLangLine.Value.translationStatus = ATS_TRANSLATED;
                        }
                        else if (AddCnt > 0)
                        {
                            sourceLangLine.Value.translationStatus = ATS_ADD;
                        }
                        else if (FixCnt > 0)
                        {
                            sourceLangLine.Value.translationStatus = ATS_UPDATE;
                        }
                        else
                        {
                            sourceLangLine.Value.translationStatus = string.Empty;
                        }
                    }
                }

            }
        }

        public ExportFileType GetFileType(string fileName, ref string category, ref int partial, ref string language, int importType)
        {
            string categoryPartialLanguageName = fileName.Split(new string[] { LOCALIZATION_FILENAME_PREFIX }, StringSplitOptions.RemoveEmptyEntries)[0];
            if (categoryPartialLanguageName.StartsWith("Extra") || categoryPartialLanguageName.StartsWith("TBT") || categoryPartialLanguageName.StartsWith("Tag"))
            {
                return ExportFileType.EXTRA_FILE;
            }

            foreach (string c in configData.Categories)
            {
                if (categoryPartialLanguageName.StartsWith(c))
                {
                    string partialLanguageName = categoryPartialLanguageName.Split(new string[] { c }, StringSplitOptions.RemoveEmptyEntries)[0];

                    bool isExistLanguage = false;
                    foreach (string l in configData.Languages)
                    {
                        if (partialLanguageName.EndsWith(l))
                        {
                            language = l;
                            isExistLanguage = true;
                            break;
                        }
                    }

                    //파일명에 해당 언어가 없으면 잘못된 파일명
                    if (!isExistLanguage)
                    {
                        continue;
                    }

                    string[] partialNames = partialLanguageName.Split(new string[] { language }, StringSplitOptions.RemoveEmptyEntries);
                    if (partialNames.Length == 0)
                    {
                        partial = 0;
                        category = c;
                        return ExportFileType.REGULAR_FILE;
                    }

                    if (partialNames.Length == 1 &&
                        Int32.TryParse(partialNames[0], out partial) == true)
                    {
                        category = c;
                        return ExportFileType.REGULAR_FILE;
                    }
                }
            }

            return ExportFileType.NONE;
        }

        // TODO: Finding duplicated keys 로직 다시 잡아야 함. 언어별로 Segment 전체돌면서 Key를 Dictionary에 등록하고, 해당 Key가 Dictionary에 존재하면 중복처리하는 방식.
        public void SetDupKeyDic()
        {
            localData.dupKeyDic.Clear();

            foreach (string category in configData.Categories)
            {
                CategoryInfo categoryInfo = localData.categoryInfos[category];
                foreach (PartialInfo partialInfo in categoryInfo.partialInfos)
                {
                    //Korean 파일을 읽어서 다른 Category, Partial에 있는 KEY인지 비교 
                    Dictionary<string, FileLine> koreanInfo = partialInfo.languageInfos["Korean"].fileInfo;

                    foreach (var fileLinePair in koreanInfo)
                    {
                        string key = fileLinePair.Value.key;

                        //이미 포함된 key면 continue;
                        if (localData.dupKeyDic.ContainsKey(key)) continue;

                        //다른 파일 내 중복키 검사
                        List<TBTLine> dupList = FindDupKey(key, category, partialInfo._partial);

                        //같은 파일 내 중복키 검사
                        foreach (KeyValuePair<string, FileLine> copyFileLinePair in koreanInfo)
                        {
                            string copyKey = copyFileLinePair.Value.key;

                            if ((key == copyKey) && (fileLinePair.Key != copyFileLinePair.Key))
                            {
                                dupList.Insert(0, copyFileLinePair.Value.ToTBTLine(category, partialInfo._partial));
                            }
                        }

                        if (dupList.Count == 0) continue;
                        dupList.Insert(0, fileLinePair.Value.ToTBTLine(category, partialInfo._partial));

                        //dupKeyList에 추가
                        localData.dupKeyDic.Add(key, dupList);

                    }
                }
            }
        }

        private List<TBTLine> FindDupKey(string key, string keyCategory, int keyPartial)
        {
            List<TBTLine> dupList = new List<TBTLine>();

            foreach (string category in configData.Categories)
            {
                CategoryInfo categoryInfo = localData.categoryInfos[category];
                foreach (PartialInfo partialInfo in categoryInfo.partialInfos)
                {
                    //같은 category, partial인 경우 continue
                    if (category.Equals(keyCategory) && partialInfo._partial == keyPartial) continue;

                    //Korean 파일을 읽어서 다른 Category, Partial에 있는 KEY인지 비교 
                    Dictionary<string, FileLine> koreanInfo = partialInfo.languageInfos["Korean"].fileInfo;

                    if (koreanInfo.ContainsKey(key))
                    {
                        dupList.Add(koreanInfo[key].ToTBTLine(category, partialInfo._partial));
                    }
                }
            }

            return dupList;
        }

        //List들 Dictionary 형태로 json으로 추출한다
        public void ExportListJson(List<PrefixTemplate> prefixExportTemplate)
        {
            Dictionary<string, Dictionary<string, PrefixExportJsonTemplate>> ExportLanaugeDic = new Dictionary<string, Dictionary<string, PrefixExportJsonTemplate>>();
            foreach (string language in configData.Languages)
            {
                ExportLanaugeDic.Add(language, new Dictionary<string, PrefixExportJsonTemplate>());

                foreach (PrefixTemplate prefixTemplate in prefixExportTemplate)
                {
                    ExportLanaugeDic[language].Add(prefixTemplate.prefixStr, new PrefixExportJsonTemplate(prefixTemplate.prefixStr.ToUpper()));
                }
            }

            foreach (PrefixTemplate prefixTemplate in prefixExportTemplate)
            {
                string prefixStr = prefixTemplate.prefixStr;
                string regexStr = prefixTemplate.regexStr;

                foreach (KeyValuePair<string, CategoryInfo> category in localData.categoryInfos)
                {
                    foreach (PartialInfo partialInfo in category.Value.partialInfos)
                    {
                        //Korean 파일로 적합한 Key인지 체크 후에 language를 돈다
                        LangFile koreanLanguageInfo = partialInfo.languageInfos["Korean"];
                        foreach (KeyValuePair<string, FileLine> line in koreanLanguageInfo.fileInfo)
                        {
                            string exportKey = line.Value.key;
                            string exportIndexStr = string.Empty;
                            if (CheckValidKey(prefixStr, regexStr, exportKey, out exportIndexStr) == false) continue;

                            foreach (KeyValuePair<string, LangFile> language in partialInfo.languageInfos)
                            {
                                if (language.Value.fileInfo.ContainsKey(exportKey) == false) continue;

                                if (language.Key == "Korean")
                                {
                                    //ExportLanaugeDic[language.Key][exportIndexStr] = language.Value.fileInfo[exportKey].korean;
                                    ExportLanaugeDic[language.Key][prefixStr].exportPrefixDic[exportIndexStr] = language.Value.fileInfo[exportKey].sourceText;
                                }
                                else
                                {
                                    //ExportLanaugeDic[language.Key][exportIndexStr] = language.Value.fileInfo[exportKey].translation;
                                    ExportLanaugeDic[language.Key][prefixStr].exportPrefixDic[exportIndexStr] = language.Value.fileInfo[exportKey].translation;
                                }
                            }
                        }
                    }
                }
            }



            //Language Dic json으로 export
            foreach (var exportDic in ExportLanaugeDic)
            {
                string exportFileName = string.Format("prefix{0}.json", exportDic.Key.ToLower());
                JObject exportJsonObj = new JObject();

                foreach (var exportDicKeyValues in exportDic.Value)
                {
                    JArray jArray = new JArray();
                    JObject jObject = new JObject();
                    foreach (var prefixDic in exportDicKeyValues.Value.exportPrefixDic)
                    {
                        jObject.Add(prefixDic.Key, prefixDic.Value);
                    }

                    jArray.Add(jObject);

                    string prefixKey = (exportDicKeyValues.Key.EndsWith("_") == true) ?
                            (exportDicKeyValues.Key.Substring(0, exportDicKeyValues.Key.Length - 1)) : exportDicKeyValues.Key;
                    exportJsonObj.Add(prefixKey, jArray);
                }

                string exportDirPath = Path.Combine(configData.Directory, "PrefixExport");
                if (Directory.Exists(exportDirPath) == false)
                {
                    Directory.CreateDirectory(exportDirPath);
                }

                string exportFilePath = Path.Combine(exportDirPath, exportFileName);

                string contents = exportJsonObj.ToString();

                try
                {
                    File.WriteAllText(exportFilePath, contents, Encoding.UTF8);
                }
                catch (Exception e)
                {
                    log.Error(e.ToString());
                    //저장 실패 시?
                }
            }
        }

        //해당 시작하는 string 전부 추출
        private bool CheckValidKey(string prefix, string regexStr, string key, out string index)
        {
            Regex regex = new Regex(regexStr, RegexOptions.IgnoreCase);
            if (regex.IsMatch(key) == false)
            {
                index = string.Empty;
                return false;
            }

            index = key.Replace(prefix.ToUpper(), "");
            return true;
        }

        public void UpdateStatus(string category, int partial, string language, string key, string newStatus)
        {
            FileLine toolLine = LocalizationDataManager.GetFileLine(category, partial, language, key);
            if (toolLine == null)
                return;
            UpdateStatus(toolLine, newStatus);
            localData.SetIsModified(true, category, partial, language);
        }

        public void UpdateStatus(FileLine fileLine, string newStatus)
        {
            string category = fileLine.file._category;
            int partial = fileLine.file._partial;
            string language = fileLine.file._language;
            fileLine.status = newStatus;
            fileLine.isModified = true;
            localData.SetIsModified(true, category, partial, language);
        }

        public void PartialPreTranslate(string provider, string category, int partial, string sourceLang, string targetLang,
            Dictionary<string, bool> isNotSelectedStatus, string tag)
        {
            Dictionary<string, LangFile> LangFiles = GetLangFiles(category, partial);

            Dictionary<string, FileLine> sourceFileLInes = LangFiles[sourceLang].fileInfo;
            Dictionary<string, FileLine> targetFileLines = LangFiles[targetLang].fileInfo;
            LangFile targetLangFile = LangFiles[targetLang];

            FileLine[] targetFileLineArr = targetFileLines.Values.ToArray();            

            string sourceLangCode = ConfigData.GetLanguageCode(sourceLang);
            string targetLangCode = ConfigData.GetLanguageCode(targetLang);

            ITranslate translator = null;

            if (string.CompareOrdinal(provider, "Google") == 0) translator = new GoogleTranslate();
            if (string.CompareOrdinal(provider, "Microsoft") == 0) translator = new MicrosoftTranslate();
            if (string.CompareOrdinal(provider, "Naver") == 0) translator = new NaverTranslate();

            foreach (FileLine sourceFL in sourceFileLInes.Values)
            {
                foreach (FileLine targetFL in targetFileLineArr)
                {
                    string sourceTranslation = string.Empty;

                    if (sourceLang.Equals(ConfigData.SourceLanguage, StringComparison.OrdinalIgnoreCase) == true)
                        sourceTranslation = sourceFL.sourceText;
                    else
                        sourceTranslation = sourceFL.translation;

                    if (string.IsNullOrEmpty(sourceTranslation)) continue;

                    try
                    {
                        if (string.CompareOrdinal(sourceFL.key, targetFL.key) == 0)
                        {
                            // PreTranslationWindow에서 Status 체크 안된 것들 번역X
                            if (isNotSelectedStatus.ContainsKey(targetFL.status)) continue;

                            // PreTranslationWindow에서 tag 번역
                            if (!string.IsNullOrEmpty(tag))
                            {
                                if (string.CompareOrdinal(tag, targetFL.tag) != 0) continue;
                            }

                            if (targetLang.Equals(ConfigData.SourceLanguage, StringComparison.OrdinalIgnoreCase) == true)
                                targetFL.sourceText = translator.Translate(sourceLangCode, targetLangCode, sourceTranslation);
                            else
                                targetFL.translation = translator.Translate(sourceLangCode, targetLangCode, sourceTranslation);

                            targetFL.status = STATUS_PRE_TRANSLATED;

                            // 번역한 targetFL를 targetFileLineArr에서 제거
                            int numIndex = Array.IndexOf(targetFileLineArr, targetFL);
                            targetFileLineArr = targetFileLineArr.Where((val, idx) => idx != numIndex).ToArray();

                            break;
                        }
                    }
                    catch (Exception e)
                    {
                        log.Error(e.Message);
                    }
                }
            }
            localData.SetIsModified(true, targetLangFile._category, targetLangFile._partial, targetLangFile._language);
        }

        public void CategoryPreTranslate(string provider, string category, string sourceLang, string targetLang, 
            Dictionary<string, bool> isNotSelectedStatus, string tag)
        {
            List<PartialInfo> partialInfos = GetPartialInfos(category);

            try
            {
                foreach (PartialInfo partialInfo in partialInfos)
                {
                    foreach (KeyValuePair<string, LangFile> langFilePair in partialInfo.languageInfos)
                    {
                        string _category = langFilePair.Value._category;
                        int _partial = langFilePair.Value._partial;
                        string curLangFileName = langFilePair.Key;

                        if (sourceLang.Equals(curLangFileName, StringComparison.OrdinalIgnoreCase)
                            || !targetLang.Equals(curLangFileName, StringComparison.OrdinalIgnoreCase))
                            continue;

                        PartialPreTranslate(provider, _category, _partial, sourceLang, targetLang, isNotSelectedStatus, tag);
                    }
                }
            }
            catch (Exception e)
            {
                log.Error(e.Message);
                PreTranslationWindow preTranslationWindow = new PreTranslationWindow();
                preTranslationWindow.Close();
            }
        }
    }
}
