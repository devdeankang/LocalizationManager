using MahApps.Metro.Controls.Dialogs;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace LocalizationManager
{
    public class ImportFile
    {
        public string fileName { get; set; }
    }

    /// <summary>
    /// ChooseImportView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ChooseImportView : UserControl
    {
        public event RoutedEventHandler ClickCancel;
        public event RoutedEventHandler ClickImport;

        private string[] fileNames;
        public string[] Files { get => fileNames; }

        public List<ImportFile> ImportFileList = new List<ImportFile>();
        public List<string> ImportDupList = new List<string>();

        public ChooseImportView()
        {
            InitializeComponent();
            GetImportSetting();
        }

        private void GetImportSetting()
        {
            bool _dispoable = true;
            
            foreach (ImportFileType fileType in System.Enum.GetValues(typeof(ImportFileType)))
            {                
                string desc = LocalizationDataManager.Instance.localData.GetImportTypeDesc(fileType);
                ImportSetting.Items.Add(desc);
                if (_dispoable == true)
                {
                    _dispoable = false;
                    ImportSetting.SelectedItem = desc;
                }
            }
        }

        private void ClickBtnCancel(object sender, RoutedEventArgs e)
        {
            if (ClickCancel != null)
            {
                ClickCancel(sender, e);
            }
        }

        private void ClickBtnOpen(object sender, RoutedEventArgs e)
        {
            if (ClickImport != null)
            {
                ClickImport(sender, e);
            }

            ClickCancel(sender, e);
        }

        private void GetDeleteFiles(object sender, RoutedEventArgs e)
        {
            foreach (ImportFile item in ImportViewList.SelectedItems)
            {
                ImportFileList.Remove(item);
                ImportDupList.Remove(item.fileName);
            }
            ObservableCollection<ImportFile> isAdd = new ObservableCollection<ImportFile>(ImportFileList);
            ImportViewList.ItemsSource = isAdd;
            ImportViewList.ItemsSource = ImportFileList;
        }

        private void GetImportFIles(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
            openFileDialog.Title = "Import Files";
            openFileDialog.Multiselect = true;
            openFileDialog.Filter = "Localization files|Localization_*.csv;Localization_*.xlsx;*.zip";

            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                fileNames = openFileDialog.FileNames;

                var mySettings = new MetroDialogSettings()
                {
                    AnimateShow = false,
                    AnimateHide = false,
                };

                foreach (string file in fileNames)
                {
                    if (ImportDupList.Contains(file)) continue;
                    ImportDupList.Add(file);
                    ImportFileList.Add(new ImportFile() { fileName = file });
                }
                ObservableCollection<ImportFile> isAdd = new ObservableCollection<ImportFile>(ImportFileList);
                ImportViewList.ItemsSource = isAdd;
                ImportViewList.ItemsSource = ImportFileList;
            }
            return;
        }

        private async void ShowDialog(string title, string content)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            await mainWindow.ShowMessageAsync(title, content);
        }
    }
}
