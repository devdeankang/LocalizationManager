using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
using MahApps.Metro.SimpleChildWindow;

namespace LocalizationManager
{
    /// <summary>
    /// OptionWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class OptionWindow : ChildWindow
    {
        public OptionWindow()
        {
            InitializeComponent();

            InitSetConfigLoadFileType();
            InitSetConfigSaveFileType();
            InitSwitchCheckDupKeys();
        }

        private void InitSetConfigLoadFileType()
        {
            switch (LocalizationDataManager.Instance.configData.LoadFileExtensionType)
            {
                case LocalizationFileType.CSV:
                    LoadFileType.SelectedIndex = 0;
                    break;
                case LocalizationFileType.XLSX:
                    LoadFileType.SelectedIndex = 1;
                    break;
                default:
                    LoadFileType.SelectedIndex = 0;
                    break;
            }
        }

        private void InitSetConfigSaveFileType()
        {
            switch (LocalizationDataManager.Instance.configData.SaveFileExtensionType)
            {
                case LocalizationFileType.CSV:
                    SaveFileType.SelectedIndex = 0;
                    break;
                case LocalizationFileType.XLSX:
                    SaveFileType.SelectedIndex = 1;
                    break;
                default:
                    SaveFileType.SelectedIndex = 0;
                    break;
            }
        }

        private void InitSwitchCheckDupKeys()
        {
            SwitchCheckDupKeys.IsOn = OpenProjectWindow.GetRecentFindDupKeySettting();
        }

        private void btnApply_Click(object sender, RoutedEventArgs e)
        {
            SetConfigLoadFileType();
            SetConfigSaveFileType();
            RegistryManager.Instance.StoreStr(OpenProjectWindow.FindDupKeysLoadKeyStr, 
                SwitchCheckDupKeys.IsOn.ToString(), RegistryManager.Instance.REGISTRY_KEY_STARTS);
            LocalizationDataManager.Instance.configData.SaveConfigData();

            this.Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void SetConfigLoadFileType()
        {
            ComboBoxItem loadFileTypeItem = LoadFileType.SelectedItem as ComboBoxItem;
            string loadFileType = loadFileTypeItem.Content as string;
            switch (loadFileType)
            {
                case "CSV":
                    LocalizationDataManager.Instance.configData.LoadFileExtensionType = LocalizationFileType.CSV;
                    break;
                case "XLSX":
                    LocalizationDataManager.Instance.configData.LoadFileExtensionType = LocalizationFileType.XLSX;
                    break;
                default:
                    LocalizationDataManager.Instance.configData.LoadFileExtensionType = LocalizationFileType.CSV;
                    break;
            }
        }

        private void SetConfigSaveFileType()
        {
            ComboBoxItem saveFileTypeItem = SaveFileType.SelectedItem as ComboBoxItem;
            string saveFileType = saveFileTypeItem.Content as string;
            switch (saveFileType)
            {
                case "CSV":
                    LocalizationDataManager.Instance.configData.SaveFileExtensionType = LocalizationFileType.CSV;
                    break;
                case "XLSX":
                    LocalizationDataManager.Instance.configData.SaveFileExtensionType = LocalizationFileType.XLSX;
                    break;
                default:
                    LocalizationDataManager.Instance.configData.SaveFileExtensionType = LocalizationFileType.CSV;
                    break;
            }
        }
    }
}
