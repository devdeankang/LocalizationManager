using MahApps.Metro.Controls;
using MahApps.Metro.SimpleChildWindow;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace LocalizationManager
{
    public enum ExportType
    {
        ExportAll,
        ExportTBT,
        ExportTag,
    }

    /// <summary>
    /// ChooseExportView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ChooseExportView : UserControl
    {
        public event RoutedEventHandler ClickCancel;
        public event RoutedEventHandler ClickExport;

        public ExportType _exportType;

        public ChooseExportView(string name)
        {
            InitializeComponent();

            switch (name)
            {
                case "btnExportAll":
                case "menuExportAll":
                    ExportAll.IsSelected = true;
                    _exportType = ExportType.ExportAll;
                    break;
                case "btnExportTBT":
                case "menuExportTBT":
                    ExportTBT.IsSelected = true;
                    _exportType = ExportType.ExportTBT;
                    break;
                case "btnExportTag":
                case "menuExportTag":
                    ExportTag.IsSelected = true;
                    _exportType = ExportType.ExportTag;
                    break;
                default:
                    break;
            }

            //Recent Selected zip, collectType
            SetCompressZip();
            SetCollectType();
            SetExportFileType();

            //Recent Registry
            SetLocationComboboxItems();
        } 

        public void CheckValidateExport(object sender, RoutedEventArgs e)
        {
            if (isValidateDirectory(Location_Combobox.Text))
            {
                btnExport.IsEnabled = true;
            }
            else
            {
                btnExport.IsEnabled = false;
            }
        }

        private bool isValidateDirectory(string directory)
        {
            return Directory.Exists(directory) ? true : false;
        }

        private void SetExportLocation(object sender, RoutedEventArgs e)
        {
            var location = OpenDirectory();
            if (!string.IsNullOrEmpty(location))
            {
                RecentDirectory.Instance.AddRecentDir(location);
                SetLocationComboboxItems();
                Location_Combobox.Text = location;
            }
        }

        private void ClickBtnCancel(object sender, RoutedEventArgs e)
        {
            if (ClickCancel != null)
            {
                ClickCancel(sender, e);
            }
        }

        private void ClickBtnExport(object sender, RoutedEventArgs e)
        {
            if (ClickExport != null)
            {
                ClickExport(sender, e);
            }
        }

        private string OpenDirectory()
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.InitialDirectory = string.IsNullOrEmpty(Location_Combobox.Text) ? "" : Location_Combobox.Text;
            dialog.IsFolderPicker = true;

            foreach (string path in RecentDirectory.Instance.GetRecentDirs())
            {
                if (Directory.Exists(path) == false) continue;
                dialog.AddPlace(path, Microsoft.WindowsAPICodePack.Shell.FileDialogAddPlaceLocation.Bottom);
            }

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                return dialog.FileName;
            }

            return string.Empty;
        }

        private void SetLocationComboboxItems()
        {
            var recentDirectory = RecentDirectory.Instance.GetRecentDirs();
            Location_Combobox.Items.Clear();
            foreach (string dir in recentDirectory)
            {
                if (Directory.Exists(dir) == false) continue;
                Location_Combobox.Items.Add(dir);
            }
        }

        private void SetCollectType()
        {
            string recentCollectType = RegistryManager.Instance.LoadStr(GetRecentCollectTypeKey(_exportType.ToString()));
           
            switch (recentCollectType)
            {
                case "All Language":
                    CollectType.SelectedIndex = 0;
                    break;
                case "By Language":
                    CollectType.SelectedIndex = 1;
                    break;
                case "By Custom Template":
                    CollectType.SelectedIndex = 2;
                    break;
                default:
                    CollectType.SelectedIndex = 0;
                    break;
            }
        }

        private void SetCompressZip()
        {
            bool recentIsCompress = (RegistryManager.Instance.LoadStr(GetRecentIsZipKey(_exportType.ToString())) == "True") ?  true : false;

            CompressZip.IsOn = recentIsCompress;
        }

        private void SetExportFileType()
        {
            string recentCollectType = RegistryManager.Instance.LoadStr(GetRecentExportFileTypeKey(_exportType.ToString()));

            switch (recentCollectType)
            {
                case "CSV":
                    ExportFileType.SelectedIndex = 0;
                    break;
                case "XLSX":
                    ExportFileType.SelectedIndex = 1;
                    break;
                default:
                    CollectType.SelectedIndex = 0;
                    break;
            }
        }

        public static string GetRecentIsZipKey(string exportType)
        {
            return string.Format("{0}IsZip", exportType);
        }

        public static string GetRecentCollectTypeKey(string exportType)
        {
            return string.Format("{0}CollectType", exportType);
        }

        public static string GetRecentExportFileTypeKey(string exportType)
        {
            return string.Format("{0}ExportFileType", exportType);
        }

        private void ExportTab_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MetroTabItem metroTabItem = (MetroTabItem) ExportTab.SelectedItem;

            switch (metroTabItem.Name)
            {
                case "ExportAll":
                    _exportType = ExportType.ExportAll;
                    break;
                case "ExportTBT":
                    _exportType = ExportType.ExportTBT;
                    break;
                case "ExportTag":
                    _exportType = ExportType.ExportTag;
                    break;
                default:
                    return;
            }

            //Recent Selected zip, collectType, exportFileType
            SetCompressZip();
            SetCollectType();
            SetExportFileType();
        }
    }
}