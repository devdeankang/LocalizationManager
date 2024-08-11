using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace LocalizationManager
{
    /// <summary>
    /// ViewTemplate.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class DataGridView : UserControl
    {
        public event RoutedEventHandler ClickLangFilter;

        ConfigData config = LocalizationDataManager.Instance.configData;

        public string DataCategory { get; private set; }
        public int DataPartial { get; private set; }
        public string DataLanguage { get; private set; }

        string fileName;
        LangFile languageInfo;
        PartialInfo partialInfo;
        DataTable dataTable;

        //BackgroundWorker
        public BackgroundWorker _backgroundWorker;
        public bool isBackgroundWorkerCompleted = true;

        public DataGridView()
        {
            InitializeComponent();
        }

        public DataGridView(string fileName, LangFile languageInfo)
        {
            InitializeComponent();

            DataCategory = languageInfo._category;
            DataPartial = languageInfo._partial;
            DataLanguage = languageInfo._language;

            this.fileName = fileName;
            this.languageInfo = languageInfo;
        }

        public DataGridView(string fileName, PartialInfo partialInfo)
        {
            InitializeComponent();

            DataCategory = partialInfo._category;
            DataPartial = partialInfo._partial;
            DataLanguage = string.Empty;

            this.fileName = fileName;
            this.partialInfo = partialInfo;
        }

        void _backgroundWorker_LoadCPLData(object sender, DoWorkEventArgs e)
        {
            bool isLanguageKorean = fileName.EndsWith("Korean");

            //Row 생성
            foreach (KeyValuePair<string, FileLine> linePair in languageInfo.fileInfo)
            {
                if (_backgroundWorker.CancellationPending == true)
                {
                    e.Cancel = true;
                    return;
                }

                //Korean 파일일 때
                if (isLanguageKorean)
                {
                    // DataTable 데이터 생성
                    Dispatcher.BeginInvoke(new AddDataTableDelegate(AddDataTable), dataTable,
                        new string[] { linePair.Value.key, linePair.Value.status, linePair.Value.sourceText, linePair.Value.tag, linePair.Value.desc });
                }
                else
                {
                    // DataTable 데이터 생성
                    Dispatcher.BeginInvoke(new AddDataTableDelegate(AddDataTable), dataTable,
                        new string[] { linePair.Value.key, linePair.Value.status, linePair.Value.sourceText, linePair.Value.translation, linePair.Value.tag, linePair.Value.desc });
                }
            }
        }

        void _backgroundWorker_LoadCPData(object sender, DoWorkEventArgs e)
        {
            //Korean 기준 FileInfo
            Dictionary<string, FileLine> koreanFileInfo = partialInfo.languageInfos["Korean"].fileInfo;
            //Row 생성
            foreach (KeyValuePair<string, FileLine> linePair in koreanFileInfo)
            {
                if (_backgroundWorker.CancellationPending == true)
                {
                    e.Cancel = true;
                    return;
                }

                FileLine line = linePair.Value;

                // DataTable 데이터 생성
                List<string> lineList = new List<string>();
                lineList.Add(line.key);
                lineList.Add(line.translationStatus);
                lineList.Add(line.sourceText);
                foreach (KeyValuePair<string, LangFile> pair in partialInfo.languageInfos)
                {
                    if (pair.Key.Equals("Korean")) continue;

                    //Language Filter
                    var langFilterKey = LanguageFilterWindow.GetLanguageFilterKey(pair.Key);
                    if (RegistryManager.Instance.LoadStr(langFilterKey).Equals("False")) continue;

                    if (pair.Value.fileInfo.ContainsKey(linePair.Key) && pair.Value.fileInfo[linePair.Key] != null)
                    {
                        lineList.Add(pair.Value.fileInfo[linePair.Key].translation);
                    }
                    else
                    {
                        lineList.Add(string.Empty);
                    }
                }
                lineList.Add(line.tag);
                lineList.Add(line.desc);

                Dispatcher.BeginInvoke(new AddDataTableDelegate(AddDataTable), dataTable, lineList.ToArray());
                lineList.Clear();
            }
        }

        void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            isBackgroundWorkerCompleted = true;
        }

        /// <summary>
        /// CP = Category Partial
        /// </summary>
        public void SetCPView()
        {
            //Header
            DaaGridTabItem.Header = fileName;

            // dataTable 바인딩
            dataTable = new DataTable();
            DataGrid.ItemsSource = dataTable.DefaultView;

            //Language Filter
            btnLangFilter.Visibility = Visibility.Visible;

            //Column 생성
            foreach (string field in LineInfo.CPLine)
            {
                if (!field.Equals("Translation"))
                {
                    //DataTable Column 생성
                    dataTable.Columns.Add(field.ToLower(), typeof(string));
                }
                else
                {
                    foreach (KeyValuePair<string, LangFile> pair in partialInfo.languageInfos)
                    {
                        if (pair.Key.Equals("Korean")) continue;

                        //Language Filter
                        var langFilterKey = LanguageFilterWindow.GetLanguageFilterKey(pair.Key);
                        if (RegistryManager.Instance.LoadStr(langFilterKey).Equals("False")) continue;

                        //DataTable Column 생성
                        dataTable.Columns.Add(pair.Key.ToLower(), typeof(string));
                    }
                }

            }

            if (_backgroundWorker != null)
            {
                _backgroundWorker.CancelAsync();
            }
            _backgroundWorker = new BackgroundWorker();
            isBackgroundWorkerCompleted = false;
            _backgroundWorker.DoWork += _backgroundWorker_LoadCPData;
            _backgroundWorker.RunWorkerCompleted += worker_RunWorkerCompleted;
            _backgroundWorker.WorkerReportsProgress = true;
            _backgroundWorker.WorkerSupportsCancellation = true;
            // BackgroundWorker 실행
            _backgroundWorker.RunWorkerAsync();
        }

        /// <summary>
        /// CPL = Category Partial Language
        /// </summary>
        public void SetCPLView()
        {
            //Header 설정
            DaaGridTabItem.Header = languageInfo.fileName;

            // dataTable 바인딩
            dataTable = new DataTable();
            DataGrid.ItemsSource = dataTable.DefaultView;

            //Language Filter
            btnLangFilter.Visibility = Visibility.Hidden;

            bool isLanguageKorean = fileName.EndsWith(ConfigData.SourceLanguage);

            //Column 생성
            foreach (string field in LineInfo.CPLLine)
            {
                //Translation -> 각 언어로 표기
                if (field.Equals("Translation"))
                {
                    //Korean 파일일 때
                    if (isLanguageKorean) continue;

                    dataTable.Columns.Add(languageInfo._language.ToLower());
                }
                else
                {
                    dataTable.Columns.Add(field.ToLower());
                }
            }

            if (_backgroundWorker != null)
            {
                _backgroundWorker.CancelAsync();
            }
            _backgroundWorker = new BackgroundWorker();
            isBackgroundWorkerCompleted = false;
            _backgroundWorker.DoWork += _backgroundWorker_LoadCPLData;
            _backgroundWorker.RunWorkerCompleted += worker_RunWorkerCompleted;
            _backgroundWorker.WorkerReportsProgress = true;
            _backgroundWorker.WorkerSupportsCancellation = true;
            // BackgroundWorker 실행
            _backgroundWorker.RunWorkerAsync();
        }


        private void DataGridLoaded(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < DataGrid.Columns.Count; i++)
            {
                if (DataGrid.Columns[i].Header.Equals("key"))
                {
                    DataGrid.Columns[i].Width = 280;
                }

                var col = DataGrid.Columns[i] as DataGridTextColumn;

                col.ElementStyle = getWrapTextBolckStyle();
            }
        }

        //return Wrap style
        private Style getWrapTextBolckStyle()
        {
            var style = new Style(typeof(TextBlock));
            style.Setters.Add(new Setter(TextBlock.TextWrappingProperty, TextWrapping.Wrap));
            style.Setters.Add(new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center));

            return style;
        }

        private void btnLangFilter_Click(object sender, RoutedEventArgs e)
        {
            if (ClickLangFilter != null)
            {
                ClickLangFilter(sender, e);
            }
        }

        private delegate void AddDataTableDelegate(DataTable dataTable, string[] strList);
        private void AddDataTable(DataTable dataTable, string[] strList)
        {
            dataTable.Rows.Add(strList);
        }

        public string GetCurrentSelectedKey()
        {
            if (DataGrid.SelectedCells.Count < 1)
                return null;

            string selectedKeyStr = string.Empty;
            for (int i = 0; i < DataGrid.SelectedCells.Count; i++)
            {
                DataGridCellInfo cellInfo = DataGrid.SelectedCells[i];
                if (cellInfo.IsValid == false)
                    continue;

                DataGridColumn dataColumn = cellInfo.Column;

                var headerName = (string)dataColumn.Header;

                //선택한 cell이 KEY값인 경우에만 선택
                if (headerName.Equals("KEY", StringComparison.OrdinalIgnoreCase) == false)
                    continue;

                var content = dataColumn.GetCellContent(cellInfo.Item);
                string selectedText = (content as TextBlock).Text;


                if (string.IsNullOrEmpty(selectedText) == false)
                {
                    selectedKeyStr = string.IsNullOrEmpty(selectedKeyStr) == true ? selectedText
                        : string.Format("{0}\n{1}", selectedKeyStr, selectedText);
                }
            }

            return selectedKeyStr;
        }                


        public bool GetCurrentSelectedData(out FileLine fileLine)
        {
            fileLine = null;

            if (DataGrid.SelectedItem == null)
                return false;

            // Get Cell Key
            DataGridCellInfo cellKeyInfo = DataGrid.SelectedCells.ElementAt((int)ToolLineIndex.KEY);
            if (cellKeyInfo.IsValid == false) return false;
            DataGridColumn KeyColumn = cellKeyInfo.Column;
            FrameworkElement content = KeyColumn.GetCellContent(cellKeyInfo.Item);
            string selectionKey = (content as TextBlock).Text;

            // Get Cell Language
            DataGridCellInfo cellLangInfo = DataGrid.SelectedCells.ElementAt((int)ToolLineIndex.TRANSLATION);
            if (cellLangInfo.IsValid == false) return false;
            DataGridColumn LangColumn = cellLangInfo.Column;
            string headerName = (string)LangColumn.Header;
            string curLanguage = string.Empty;

            foreach (string language in config.Languages)
            {
                if (language.Equals(ConfigData.SourceLanguage, StringComparison.OrdinalIgnoreCase)) continue;

                if (headerName.Equals(language, StringComparison.OrdinalIgnoreCase))
                    curLanguage = language;
            }

            fileLine = LocalizationDataManager.GetFileLine(DataCategory, DataPartial, curLanguage, selectionKey);

            return true;
        }

        public bool GetCurrentSelectedTranslation(out ModifyTranslationInfo translationInfo)
        {
            translationInfo = null;

            if (DataGrid.SelectedCells.Count < 1) return false;

            DataGridCellInfo cellInfo = DataGrid.SelectedCells.ElementAt((int)ToolLineIndex.TRANSLATION);
            if (cellInfo.IsValid == false)
                return false;

            DataGridColumn dataColumn = cellInfo.Column;
            string headerName = (string)dataColumn.Header;
            string curLanguage = string.Empty;

            foreach (string language in config.Languages)
            {
                if (language.Equals("Korean", StringComparison.OrdinalIgnoreCase)
                    || !headerName.Equals(language, StringComparison.OrdinalIgnoreCase)) continue;

                if (headerName.Equals(language, StringComparison.OrdinalIgnoreCase))
                    curLanguage = language;
                else
                    return false;
            }

            FrameworkElement content = dataColumn.GetCellContent(cellInfo.Item);
            string selectedTranslation = (content as TextBlock).Text;

            DataRowView row = (DataRowView)content.DataContext;
            string selectedKey = row.Row.ItemArray[0].ToString();

            translationInfo = new ModifyTranslationInfo(DataCategory, DataPartial, curLanguage, selectedKey, selectedTranslation);
            return true;
        }
    }
}
