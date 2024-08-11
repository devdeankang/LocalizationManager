using log4net;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.SimpleChildWindow;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace LocalizationManager
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(MainWindow));

        ConfigData config = LocalizationDataManager.Instance.configData;
        LocalizationData localData = LocalizationDataManager.Instance.localData;
        [System.ComponentModel.Browsable(false)]
        public int SelectedIndex { get; set; }

        //현재 DataView 저장용도
        Type currentType = typeof(Dictionary<string, CategoryInfo>);
        public string currentCategory = string.Empty;
        public int currentpartial = -1;
        public string currentLanguage = string.Empty;

        //SubWindow 종류
        FindDupKeyWindow findDupKeyWindow;
        SearchWindow searchWindow;

        public MainWindow()
        {
            InitializeComponent();
            TextBoxAppender.AppenderTextBox = consoleLog;
        }

        private async void ShowOpenProjectWindow(object sender, RoutedEventArgs e)
        {
            MessageDialogResult result = await CheckModifiedSaved();

            switch (result)
            {
                case MessageDialogResult.Affirmative:
                    LocalizationDataManager.Instance.localData._isPartiallyExport = false;
                    await SaveLocalizationData();
                    break;
                case MessageDialogResult.Negative:
                    break;
                case MessageDialogResult.FirstAuxiliary:
                    return;
            }

            //데이터 초기화
            if (LocalizationDataManager.Instance.configData != null)
            {
                LocalizationDataManager.Instance.configData = null;
                LocalizationDataManager.Instance.localData.InitLocalizationData();
                TableTreeView.Items.Clear();
                dataView.Children.Clear();
            }

            OpenProjectWindow openProjectWindow = new OpenProjectWindow(this) { IsModal = true };
            await this.ShowChildWindowAsync(openProjectWindow);

            if (LocalizationDataManager.Instance.configData != null)
            {
                LoadLocalFiles();
                btnPanel.IsEnabled = true;
                MenuEdit.IsEnabled = true;
                MenuSync.IsEnabled = true;
                SetFileMenuItemEnable(true);
            }
            else
            {
                btnPanel.IsEnabled = false;
                MenuEdit.IsEnabled = false;
                MenuSync.IsEnabled = false;
                SetFileMenuItemEnable(false);
            }
        }

        private void SetFileMenuItemEnable(bool isEnabled)
        {
            var fileItems = MenuFile.Items;
            for (int i = 0; i < fileItems.Count; i++)
            {
                if ((fileItems[i]).GetType().Equals(typeof(Separator))) continue;

                var item = (MenuItem)fileItems[i];
                if (!item.Header.Equals("New Project") && !item.Header.Equals("Open Project") && !item.Header.Equals("Exit"))
                {
                    item.IsEnabled = isEnabled;
                }
            }
        }

        /// <summary>
        /// Tree View 설정
        /// </summary>
        void SetTreeView()
        {
            TableTreeView.Items.Clear();

            config = LocalizationDataManager.Instance.configData;
            localData = LocalizationDataManager.Instance.localData;
            localData.projectName = config.ProjectName;
            var item = new TreeViewItem();
            item.Header = localData.projectName;
            item.Tag = localData.categoryInfos;
            TableTreeView.Items.Add(item);

            foreach (string category in config.Categories)
            {
                CategoryInfo categoryInfo = localData.categoryInfos[category];

                var categoryItem = new TreeViewItem();
                categoryItem.Header = category;
                categoryItem.Tag = categoryInfo;
                item.Items.Add(categoryItem);

                foreach (PartialInfo partialInfo in categoryInfo.partialInfos)
                {
                    var partialItem = new TreeViewItem();
                    partialItem.Header = string.Format("{0} {1}", category, partialInfo._partial);
                    partialItem.Tag = partialInfo;
                    categoryItem.Items.Add(partialItem);

                    foreach (KeyValuePair<string, LangFile> language in partialInfo.languageInfos)
                    {
                        var languageItem = new TreeViewItem();

                        languageItem.Header = language.Key;
                        languageItem.Tag = language.Value;

                        partialItem.Items.Add(languageItem);
                    }
                }
            }

            item.IsExpanded = true;

            RefreshDataView();
        }

        /// <summary>
        /// CPL = Category Partial Language
        /// </summary>
        void SetCPLView(string fileName, LangFile languageInfo)
        {
            if (dataView.Children.Count > 0 && dataView.Children[0].GetType().Equals(typeof(DataGridView)))
            {
                var dataGridView = dataView.Children[0] as DataGridView;
                if (!dataGridView.isBackgroundWorkerCompleted)
                {
                    dataGridView._backgroundWorker.CancelAsync();
                }
            }
            dataView.Children.Clear();

            //DataGridView cplView = new DataGridView();
            //cplView.SetCPLView(fileName, languageInfo);

            DataGridView cplView = new DataGridView(fileName, languageInfo);
            cplView.SetCPLView();

            //dataView에 추가
            dataView.Children.Add(cplView);
        }

        /// <summary>
        /// CP = Category Partial
        /// </summary>
        void SetCPView(string fileName, PartialInfo partialInfo)
        {
            if (dataView.Children.Count > 0 && dataView.Children[0].GetType().Equals(typeof(DataGridView)))
            {
                var dataGridView = dataView.Children[0] as DataGridView;
                if (!dataGridView.isBackgroundWorkerCompleted)
                {
                    dataGridView._backgroundWorker.CancelAsync();
                }
            }
            dataView.Children.Clear();

            //Background Load
            DataGridView cpView = new DataGridView(fileName, partialInfo);
            cpView.ClickLangFilter += ShowLanguageFilterWindow;
            cpView.SetCPView();

            //dataView에 추가
            dataView.Children.Add(cpView);
        }

        ///// <summary>
        ///// 해당 카테고리의 요약정보를 보여준다.
        ///// </summary>
        void SetSummaryView(string fileName, CategoryInfo categoryInfo)
        {
            if (dataView.Children.Count > 0 && dataView.Children[0].GetType().Equals(typeof(DataGridView)))
            {
                var dataGridView = dataView.Children[0] as DataGridView;
                if (!dataGridView.isBackgroundWorkerCompleted)
                {
                    dataGridView._backgroundWorker.CancelAsync();
                }
            }
            dataView.Children.Clear();

            SummaryView categoryView = new SummaryView();
            categoryView.SetSummaryView(fileName, categoryInfo);

            dataView.Children.Add(categoryView);
        }

        ///// <summary>
        ///// 해당 카테고리의 요약정보를 보여준다.
        ///// </summary>
        void SetProjectView(string projectName)
        {
            if (dataView.Children.Count > 0 && dataView.Children[0].GetType().Equals(typeof(DataGridView)))
            {
                var dataGridView = dataView.Children[0] as DataGridView;
                if (!dataGridView.isBackgroundWorkerCompleted)
                {
                    dataGridView._backgroundWorker.CancelAsync();
                }
            }
            dataView.Children.Clear();

            SummaryView categoryView = new SummaryView();
            categoryView.SetSummaryView(projectName);

            dataView.Children.Add(categoryView);
        }

        private DataGridTextColumn CreateDataGridTextColumn(string field)
        {
            DataGridTextColumn textColumn = new DataGridTextColumn();
            textColumn.Header = field;
            textColumn.Binding = new Binding(field.ToLower());
            textColumn.Width = 110;

            return textColumn;
        }

        private void TableTreeSelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null) return;

            var fileName = ((TreeViewItem)e.NewValue).Header.ToString();
            var info = ((TreeViewItem)e.NewValue).Tag;
            if (info == null)
            {
                return;
            }

            SetDataView(fileName, ((TreeViewItem)e.NewValue).Tag);
        }

        void SetDataView(string fileName, object info)
        {
            var infoType = info.GetType();

            if (infoType.Equals(typeof(Dictionary<string, CategoryInfo>)))
            {
                //이후에 작업
                //프로젝트 이름 누르면 전체 Summary View
                currentType = infoType;

                currentCategory = string.Empty;
                currentpartial = -1;
                currentLanguage = string.Empty;
            }
            else
            {
                currentType = infoType;

                var dataInfo = (DataInfo)info;
                currentCategory = dataInfo._category;
                currentpartial = dataInfo._partial;
                currentLanguage = dataInfo._language;
            }

            if (infoType.Equals(typeof(LangFile)))
            {
                SetCPLView(fileName, (LangFile)info);
            }
            else if (infoType.Equals(typeof(PartialInfo)))
            {
                SetCPView(fileName, (PartialInfo)info);
            }
            else if (infoType.Equals(typeof(CategoryInfo)))
            {
                SetSummaryView(fileName, (CategoryInfo)info);
            }
            else if (infoType.Equals(typeof(Dictionary<string, CategoryInfo>)))
            {
                SetProjectView(fileName);
            }
        }

        public void RefreshDataView()
        {
            string fileName;
            if (currentType.Equals(typeof(Dictionary<string, CategoryInfo>)))
            {
                fileName = LocalizationDataManager.Instance.configData.ProjectName;
                SetProjectView(fileName);
            }
            else if (currentType.Equals(typeof(CategoryInfo)))
            {
                fileName = currentCategory;
                SetSummaryView(fileName, localData.categoryInfos[currentCategory]);
            }
            else if (currentType.Equals(typeof(PartialInfo)))
            {
                fileName = string.Format("{0} {1}", currentCategory, currentpartial);
                SetCPView(fileName, localData.categoryInfos[currentCategory].partialInfos[currentpartial]);
            }
            else if (currentType.Equals(typeof(LangFile)))
            {
                SetCPLView(currentLanguage,
                    localData.categoryInfos[currentCategory].partialInfos[currentpartial].languageInfos[currentLanguage]);
            }
        }

        private async void LoadLocalFiles()
        {
            //backgroundWorker 작동중이면 취소.
            if (dataView.Children.Count > 0 && dataView.Children[0].GetType().Equals(typeof(DataGridView)))
            {
                var dataGridView = dataView.Children[0] as DataGridView;
                if (!dataGridView.isBackgroundWorkerCompleted)
                {
                    dataGridView._backgroundWorker.CancelAsync();
                }
            }

            var mySettings = new MetroDialogSettings()
            {
                AnimateShow = false,
                AnimateHide = false,
                ColorScheme = this.MetroDialogOptions.ColorScheme
            };

            var controller = await this.ShowProgressAsync("Loading localization files...", "", settings: mySettings);
            controller.SetIndeterminate();
            controller.SetCancelable(false);

            LocalizationDataManager.Instance.localData.InitLocalizationData();

            // TODO: Two phase 구조로 변경. 첫번째 phase에서는 읽을 파일목록을 얻어오고, 두번째 phase에서 읽도록 하여서 로딩중에 파일개수를 보여줄 수 있게 함.
            var loadTaskList = new List<Task>();
            foreach (string category in LocalizationDataManager.Instance.configData.Categories)
            {
                switch (LocalizationDataManager.Instance.configData.LoadFileExtensionType)
                {
                    case LocalizationFileType.CSV:
                        Task taskCSV = Task.Run(() =>
                        {
                            try
                            {
                                LocalizationDataManager.Instance.LoadLocalizationDataCSV_Category(category, controller.SetMessage);
                            }
                            catch (Exception e)
                            {
                                log.Error(e.ToString());
                            }
                        });
                        loadTaskList.Add(taskCSV);
                        break;
                    case LocalizationFileType.XLSX:
                        Task taskXLSX = Task.Run(() =>
                        {
                            try
                            {
                                LocalizationDataManager.Instance.LoadLocalizationDataXLSX_Category(category, controller.SetMessage);
                            }
                            catch (Exception e)
                            {
                                log.Error(e.ToString());
                            }
                        });
                        loadTaskList.Add(taskXLSX);
                        break;
                    default:
                        break;
                }
            }

            await Task.WhenAll(loadTaskList);

            controller.SetMessage("Checking translation status...");
            await Task.Run(() => { LocalizationDataManager.Instance.SetTranslationStatus(); });

            controller.SetMessage("Summarizing information...");
            await Task.Run(() => { LocalizationDataManager.Instance.SetSummaryInfo(); });

            string checkFindDupKeysStr = RegistryManager.Instance.LoadStr(OpenProjectWindow.FindDupKeysLoadKeyStr, RegistryManager.Instance.REGISTRY_KEY_STARTS);
            bool checkFindDupKeys = (string.IsNullOrEmpty(checkFindDupKeysStr) == true || checkFindDupKeysStr == "True");

            if (checkFindDupKeys)
            {
                controller.SetMessage("Finding duplicated keys...");
                await Task.Run(() => { LocalizationDataManager.Instance.SetDupKeyDic(); });
            }

            await controller.CloseAsync();

            InitCurrentInfos();
            SetTreeView();

            if (localData.dupKeyDic.Count > 0)
            {
                findDupKeyWindow = new FindDupKeyWindow();
                //findDupKeyWindow.Owner = this;
                findDupKeyWindow.SetDataGrid();
                findDupKeyWindow.Show();
            }
        }

        public void InitCurrentInfos()
        {
            currentType = typeof(Dictionary<string, CategoryInfo>);
            currentCategory = string.Empty;
            currentpartial = -1;
            currentLanguage = string.Empty;
        }

        private async void SyncKeys()
        {
            var mySettings = new MetroDialogSettings()
            {
                AnimateShow = false,
                AnimateHide = false,
                ColorScheme = this.MetroDialogOptions.ColorScheme
            };

            var controller = await this.ShowProgressAsync("Synchronizing keys...", "Synchronizing keys...", settings: mySettings);
            controller.SetIndeterminate();
            controller.SetCancelable(false);

            await Task.Run(() => { LocalizationDataManager.Instance.SyncKeys(); });

            controller.SetMessage("Synchronizing key orders...");

            var syncKeyOrdersTaskList = new List<Task>();
            foreach (string category in LocalizationDataManager.Instance.configData.Categories)
            {
                Task task = Task.Run(() => { LocalizationDataManager.Instance.SyncKeyOrders_Category(category); });
                syncKeyOrdersTaskList.Add(task);
            }

            await Task.WhenAll(syncKeyOrdersTaskList);

            await controller.CloseAsync();

            ShowBasicDialog("Synchronizing completed!", string.Empty);

            RefreshDataView();
        }

        private async void SyncKeyOrders()
        {
            var mySettings = new MetroDialogSettings()
            {
                AnimateShow = false,
                AnimateHide = false,
                ColorScheme = this.MetroDialogOptions.ColorScheme
            };

            var controller = await this.ShowProgressAsync("Synchronizing key orders...", "", settings: mySettings);
            controller.SetIndeterminate();
            controller.SetCancelable(false);

            var syncKeyOrdersTaskList = new List<Task>();
            foreach (string category in LocalizationDataManager.Instance.configData.Categories)
            {
                Task task = Task.Run(() => { LocalizationDataManager.Instance.SyncKeyOrders_Category(category); });
                syncKeyOrdersTaskList.Add(task);
            }

            await Task.WhenAll(syncKeyOrdersTaskList);

            await controller.CloseAsync();

            ShowBasicDialog("Synchronizing completed!", string.Empty);

            RefreshDataView();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var btn = (FrameworkElement)sender;

            switch (btn.Name)
            {
                case "btnReload":
                case "menuReload":
                    //reload 시 find.dup key, search 화면이 열려있으면 닫는다.
                    if (findDupKeyWindow != null && findDupKeyWindow.isOpened)
                    {
                        findDupKeyWindow.Close();
                    }
                    if (searchWindow != null && searchWindow.isOpened)
                    {
                        searchWindow.Close();
                    }
                    LoadLocalFiles();
                    break;
                //case "btnImportFiles":
                //case "menuImportFiles":
                //    ImportFiles();
                //    break;
                case "btnSyncKeys":
                case "menuSyncKeys":
                    SyncKeys();
                    break;
                case "btnSyncKeyOrders":
                case "menuSyncKeyOrders":
                    SyncKeyOrders();
                    break;
                //case "btnSyncStatuses":
                //case "menuSyncStatuses":
                //    SyncStatuses();
                //    break;
                default:
                    break;
            }

        }

        private async void ShowNewProjectWindow(object sender, RoutedEventArgs e)
        {
            MessageDialogResult result = await CheckModifiedSaved();

            switch (result)
            {
                case MessageDialogResult.Affirmative:
                    LocalizationDataManager.Instance.localData._isPartiallyExport = false;
                    await SaveLocalizationData();
                    break;
                case MessageDialogResult.Negative:
                    break;
                case MessageDialogResult.FirstAuxiliary:
                    return;
            }

            var newProjectWindow = new NewProjectWindow() { IsModal = true };
            await this.ShowChildWindowAsync(newProjectWindow);

            if (newProjectWindow.isOK)
            {
                if (LocalizationDataManager.Instance.configData != null)
                {
                    LoadLocalFiles();
                    btnPanel.IsEnabled = true;
                    MenuEdit.IsEnabled = true;
                    MenuSync.IsEnabled = true;
                    SetFileMenuItemEnable(true);
                }
                else
                {
                    btnPanel.IsEnabled = false;
                    MenuEdit.IsEnabled = false;
                    MenuSync.IsEnabled = false;
                    SetFileMenuItemEnable(false);
                }
            }

        }

        private async void SaveAll(object sender, RoutedEventArgs e)
        {
            LocalizationDataManager.Instance.localData._isPartiallyExport = false;
            await SaveLocalizationData();
        }

        private async Task SaveLocalizationData()
        {
            var mySettings = new MetroDialogSettings()
            {
                AnimateShow = false,
                AnimateHide = false,
                ColorScheme = this.MetroDialogOptions.ColorScheme
            };

            var controller = await this.ShowProgressAsync("Save Localization Files...", "", settings: mySettings);
            controller.SetIndeterminate();
            controller.SetCancelable(false);

            await Task.Run(() => { LocalizationDataManager.Instance.SaveLocalizationData(); });

            await controller.CloseAsync();
        }

        private async void ImportFiles(object sender, RoutedEventArgs e)
        {
            var importWindow = new ImportWindow() { IsModal = true };
            await this.ShowChildWindowAsync(importWindow);

            RefreshDataView();
        }

        private async void ExportFiles(object sender, RoutedEventArgs e)
        {
            //Sync Key 하지 않았으면 확인창을 띄운다.
            if (!LocalizationDataManager.Instance.localData._isSynchronized)
            {
                await ShowCheckDialog("'Sync Key' has not been run. Do you want to run 'Sync Key'?", string.Empty,
                () =>
                {
                    SyncKeys();
                });
            }

            var btn = (FrameworkElement)sender;

            ExportWindow exportWindow = new ExportWindow(btn.Name) { IsModal = true };
            await this.ShowChildWindowAsync(exportWindow);

            if (exportWindow._isCanceled == true)
            {
                return;
            }

            var mySettings = new MetroDialogSettings()
            {
                AnimateShow = false,
                AnimateHide = false,
                ColorScheme = this.MetroDialogOptions.ColorScheme
            };

            var controller = await this.ShowProgressAsync("Export All Localization Files...", "", settings: mySettings);
            controller.SetIndeterminate();
            controller.SetCancelable(false);

            await Task.Run(() => LocalizationDataManager.Instance.ExportFiles(
                exportWindow._location, exportWindow._exportType, exportWindow._collectType, exportWindow._isZip,
                exportWindow._tag, exportWindow._selectedTemplateList, exportWindow._isExportNewlyUpdatedTBTOnly, exportWindow._exportFileType, exportWindow.ExportDir));

            await controller.CloseAsync();
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ShowOpenProjectWindow(sender, e);
        }

        private async void ShowManageConfigs(object sender, RoutedEventArgs e)
        {
            string manageType = string.Empty;
            var btn = (FrameworkElement)sender;

            switch (btn.Name)
            {
                case "menuManageCategory":
                    manageType = "Category";
                    break;
                case "menuManageLanguage":
                    manageType = "Language";
                    break;
            }

            var manageWindow = new ManageConfigWindow(manageType) { IsModal = true };
            await this.ShowChildWindowAsync(manageWindow);

            if (manageWindow.isApply)
            {
                LoadLocalFiles();
            }
        }

        private async void ShowLanguageFilterWindow(object sender, RoutedEventArgs e)
        {
            await this.ShowChildWindowAsync(new LanguageFilterWindow() { IsModal = true });

            RefreshDataView();
        }

        private async void RenameKey(object sender, RoutedEventArgs e)
        {
            string selectedKey = string.Empty;
            if (dataView.Children[0].GetType().Equals(typeof(DataGridView)))
            {
                DataGridView currentDataGridView = dataView.Children[0] as DataGridView;
                selectedKey = currentDataGridView.GetCurrentSelectedKey();
            }

            var renameKeyWindow = new RenameKeyWindow(selectedKey) { IsModal = true };
            await this.ShowChildWindowAsync(renameKeyWindow);

            if (renameKeyWindow.isSuccessAll == false)
            {
                string title = renameKeyWindow.SwapCheckbox.IsChecked.Value == true ? "Error Exist Swap Key." : "Error Exist Rename Key.";
                string content = renameKeyWindow.errorStr;

                await this.ShowChildWindowAsync(new DialogWindow(title, content) { IsModal = true });
            }

            RefreshDataView();
        }

        private async void ModifyTranslation(object sender, RoutedEventArgs e)
        {
            bool isSelectedTranslation = false;
            ModifyTranslationInfo modifyTranslationInfo = null;

            if (dataView.Children[0].GetType().Equals(typeof(DataGridView)))
            {
                DataGridView currentDataGridView = dataView.Children[0] as DataGridView;
                isSelectedTranslation = currentDataGridView.GetCurrentSelectedTranslation(out modifyTranslationInfo);
            }

            var modifyTranslationWindow = new ModifyTranslationWindow(modifyTranslationInfo) { IsModal = true };
            await this.ShowChildWindowAsync(modifyTranslationWindow);

            RefreshDataView();
        }

        private async void RemoveTag(object sender, RoutedEventArgs e)
        {
            string selectedKey = string.Empty;

            if (dataView.Children[0].GetType().Equals(typeof(DataGridView)))
            {
                DataGridView currentDataGridView = dataView.Children[0] as DataGridView;
                selectedKey = currentDataGridView.GetCurrentSelectedKey();
            }

            if (!string.IsNullOrEmpty(selectedKey)) SelectedIndex = 0;

            var removeTagWindow = new RemoveTagWindow(selectedKey) { IsModal = true };
            await this.ShowChildWindowAsync(removeTagWindow);
            
            if (removeTagWindow.isSuccessAll == false)
            {
                string title = "Error Exist Remove Tag.";
                string content = removeTagWindow.errorStr;

                await this.ShowChildWindowAsync(new DialogWindow(title, content) { IsModal = true });
            }

            RefreshDataView();

            ////////////////////////////////////////////////////////////////////////////////////////////////



            // var removeTagWindow = new RemoveTagWindow() { IsModal = true };
            // await this.ShowChildWindowAsync(removeTagWindow);

            //// if (removeTagWindow.isSuccessAll == false)
            //// {
            ////     string title = "Error Exist Remove Tag.";
            ////     string content = removeTagWindow.errorStr;
            ////
            ////     await this.ShowChildWindowAsync(new DialogWindow(title, content) { IsModal = true });
            //// }

            // RefreshDataView();
        }

        private async void MoveKey(object sender, RoutedEventArgs e)
        {
            var moveKeyWindow = new MoveKeyWindow() { IsModal = true };
            await this.ShowChildWindowAsync(moveKeyWindow);

            if (moveKeyWindow.isSuccessAll == false)
            {
                string title = "Error Exist Move Key.";
                string content = moveKeyWindow.errorStr;

                await this.ShowChildWindowAsync(new DialogWindow(title, content) { IsModal = true });
            }

            RefreshDataView();
        }

        public async void ShowBasicDialog(string title, string content)
        {
            log.Info(title);
            await this.ShowMessageAsync(title, content);
        }

        public async Task ShowCheckDialog(string title, string content, Action affirmativeAction)
        {
            var mySettings = new MetroDialogSettings()
            {
                AffirmativeButtonText = "OK",
                NegativeButtonText = "Cancel",
                ColorScheme = MetroDialogOptions.ColorScheme
            };

            MessageDialogResult result = await this.ShowMessageAsync(title, content,
                MessageDialogStyle.AffirmativeAndNegative, mySettings);

            switch (result)
            {
                case MessageDialogResult.Affirmative:
                    affirmativeAction();
                    break;
                case MessageDialogResult.Negative:
                    break;
            }

        }

        private async void FindDupKeys(object sender, RoutedEventArgs e)
        {
            if (findDupKeyWindow != null && findDupKeyWindow.isOpened)
            {
                findDupKeyWindow.Focus();
                return;
            }

            var mySettings = new MetroDialogSettings()
            {
                AnimateShow = false,
                AnimateHide = false,
                ColorScheme = this.MetroDialogOptions.ColorScheme
            };

            var controller = await this.ShowProgressAsync("Finding duplicated keys...", "", settings: mySettings);
            controller.SetIndeterminate();
            controller.SetCancelable(false);


            await Task.Run(() => LocalizationDataManager.Instance.SetDupKeyDic());

            await controller.CloseAsync();

            findDupKeyWindow = new FindDupKeyWindow();
            //findDupKeyWindow.Owner = this;
            findDupKeyWindow.SetDataGrid();
            findDupKeyWindow.Show();
        }

        private async void MetroWindow_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;

            await CheckWindowClosing();
        }

        private async Task CheckWindowClosing()
        {
            MessageDialogResult result = await CheckModifiedSaved();

            switch (result)
            {
                case MessageDialogResult.Affirmative:
                    LocalizationDataManager.Instance.localData._isPartiallyExport = false;
                    await SaveLocalizationData();
                    break;
                case MessageDialogResult.Negative:
                    break;
                case MessageDialogResult.FirstAuxiliary:
                    return;
            }

            if (findDupKeyWindow != null && findDupKeyWindow.isOpened)
            {
                findDupKeyWindow.Close();
            }

            if (searchWindow != null && searchWindow.isOpened)
            {
                searchWindow.Close();
            }

            Application.Current.Shutdown();
        }

        private async Task<MessageDialogResult> CheckModifiedSaved()
        {
            //저장했는지 확인
            if (LocalizationDataManager.Instance.localData.GetLocalDataModified())
            {
                string title = "Localization data has been changed. Do you want to save All changes?";
                string content = string.Empty;

                foreach (var categoryInfo in LocalizationDataManager.Instance.localData.categoryInfos)
                {
                    if (categoryInfo.Value.isModified == true)
                    {
                        content = (string.IsNullOrEmpty(content)) ? categoryInfo.Key : string.Format("{0}, {1}", content, categoryInfo.Key);
                    }
                }

                var mySettings = new MetroDialogSettings()
                {
                    AffirmativeButtonText = "Save",
                    NegativeButtonText = "Don't Save",
                    FirstAuxiliaryButtonText = "Cancel",
                    ColorScheme = MetroDialogOptions.ColorScheme
                };

                MessageDialogResult result = await this.ShowMessageAsync(title, content,
                    MessageDialogStyle.AffirmativeAndNegativeAndSingleAuxiliary, mySettings);

                switch (result)
                {
                    case MessageDialogResult.Affirmative:
                        LocalizationDataManager.Instance.localData._isPartiallyExport = false;
                        await SaveLocalizationData();
                        break;
                    case MessageDialogResult.Negative:
                        break;
                    case MessageDialogResult.FirstAuxiliary:
                        break;
                }

                return result;
            }

            return MessageDialogResult.Canceled;
        }

        private void OpenSearchWindow(object sender, RoutedEventArgs e)
        {
            if (searchWindow != null && searchWindow.isOpened)
            {
                searchWindow.Focus();
                return;
            }

            searchWindow = new SearchWindow();
            //searchWindow.Owner = this;
            searchWindow.Show();
        }

        private async void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            await CheckWindowClosing();
        }

        private async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            LocalizationDataManager.Instance.localData._isPartiallyExport = true;

            SaveWindow saveWindow = new SaveWindow() { IsModal = true };
            await this.ShowChildWindowAsync(saveWindow);

            if (saveWindow._isCanceled == true)
            {
                return;
            }

            await SaveLocalizationData();
        }

        private async void FixKorean(object sender, RoutedEventArgs e)
        {
            await this.ShowChildWindowAsync(new FixKoreanWindow() { IsModal = true });

            RefreshDataView();
        }

        private async void btnModifyStatus(object sender, RoutedEventArgs e)
        {   
            FileLine fileLine = null;

            if (dataView.Children[0].GetType().Equals(typeof(DataGridView)))
            {
                DataGridView curDataGridView = dataView.Children[0] as DataGridView;
                bool toolLineValid = curDataGridView.GetCurrentSelectedData(out fileLine);

                var modifyStatusWindow = new ModifyStatusWindow() { IsModal = true };
                modifyStatusWindow.UpdateStatusWindow(fileLine, true);
                await this.ShowChildWindowAsync(modifyStatusWindow);
            }

            RefreshDataView();
        }

        private async void btnPreTranslation(object sender, RoutedEventArgs e)
        {
            var preTranslationWindow = new PreTranslationWindow() { IsModal = true };
            await this.ShowChildWindowAsync(preTranslationWindow);
            RefreshDataView();
        }
        private async void OpenOptionWindow(object sender, RoutedEventArgs e)
        {
            await this.ShowChildWindowAsync(new OptionWindow() { IsModal = true });
        }

        //아이템리스트 json으로 추출
        private async void menuItemListJson_Click(object sender, RoutedEventArgs e)
        {
            PrefixExportJsonWindow exportListJsonWindow = new PrefixExportJsonWindow() { IsModal = true };
            await this.ShowChildWindowAsync(exportListJsonWindow);

            if (exportListJsonWindow.isCanceled == true)
            {
                return;
            }

            var mySettings = new MetroDialogSettings()
            {
                AnimateShow = false,
                AnimateHide = false,
                ColorScheme = this.MetroDialogOptions.ColorScheme
            };

            var controller = await this.ShowProgressAsync("Exporting items to json files...", "", settings: mySettings);
            controller.SetIndeterminate();
            controller.SetCancelable(false);

            await Task.Run(() => LocalizationDataManager.Instance.ExportListJson(exportListJsonWindow.prefixExportTemplateList));

            await controller.CloseAsync();
        }
    }

}
