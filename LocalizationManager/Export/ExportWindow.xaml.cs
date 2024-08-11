using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MahApps.Metro.Controls;
using MahApps.Metro.SimpleChildWindow;

namespace LocalizationManager
{
    /// <summary>
    /// ExportWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ExportWindow : ChildWindow
    {
        public string _exportType = "";
        public string _collectType = "";
        public List<string> _selectedTemplateList = new List<string>();
        public bool _isZip = false;
        public string _tag = "";
        public string _location = "";
        public bool _isExportNewlyUpdatedTBTOnly = true;
        public bool _isPartiallyExport = false;
        public LocalizationFileType _exportFileType = LocalizationFileType.CSV;
        public string ExportDir = string.Empty;

        public bool _isCanceled = false;

        private ChooseExportView chooseExportView;
        private ChooseCategoryPartialView chooseCategoryPartialView;
        private ZipTemplateView zipTemplateView;
        private SetZipTemplateView setZipTemplateView;

        public ExportWindow(string name)
        {
            InitializeComponent();

            InitChooseExportView(name);

            ExportGrid.Children.Add(chooseExportView);
        }

        public void InitChooseExportView(string name)
        {
            chooseExportView = new ChooseExportView(name);
            chooseExportView.ClickCancel += ClickBtnCancel;
            chooseExportView.ClickExport += ClickBtnExport;
        }

        public void InitChooseCategoryPartialView()
        {
            chooseCategoryPartialView = new ChooseCategoryPartialView();
            chooseCategoryPartialView.ClickCancel += ClickBtnCancel;
            chooseCategoryPartialView.ClickExport += ClickBtnExport;
        }

        public void InitZipTemplateView()
        {
            zipTemplateView = new ZipTemplateView();
            zipTemplateView.ClickCancel += ClickBtnCancel;
            zipTemplateView.ClickExport += ClickBtnExport;
            zipTemplateView.ClickSetTemplate += SetZipTemplates;
        }

        public void InitSetZipTemplateView(bool isNew, ExportTemplate template = null)
        {
            setZipTemplateView = new SetZipTemplateView(isNew, template);
            setZipTemplateView.ClickCancel += ClickBtnCancel;
            setZipTemplateView.ClickApply += ClickBtnApply;
        }

        private void ClickBtnCancel(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;

            switch (btn.Tag)
            {
                case "ChooseExport":
                    _exportType = string.Empty;
                    _collectType = string.Empty;
                    _isZip = false;
                    _tag = _exportType.Equals("ExportTag") ? chooseExportView.TagName.Text : string.Empty;
                    _location = string.Empty;

                    this.Close();
                    _isCanceled = true;
                    break;
                case "ChooseCategoryParitalExport":
                    ExportGrid.Children.Clear();
                    ExportGrid.Children.Add(chooseExportView);
                    break;
                case "ChooseZip":
                    if (_isPartiallyExport == true)
                    {
                        ExportGrid.Children.Clear();
                        ExportGrid.Children.Add(chooseCategoryPartialView);
                    }
                    else
                    {
                        ExportGrid.Children.Clear();
                        ExportGrid.Children.Add(chooseExportView);
                    }
                    break;
                case "SetZipTemplate":
                    ExportGrid.Children.Clear();
                    ExportGrid.Children.Add(zipTemplateView);
                    break;
                default:
                    break;
            }

        }

        private void ClickBtnExport(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;

            switch (btn.Tag)
            {
                case "ChooseExport":
                    TabItem selectedTab = (TabItem)chooseExportView.ExportTab.SelectedItem;
                    _exportType = selectedTab.Name;
                    _isZip = chooseExportView.CompressZip.IsOn;
                    _tag = _exportType.Equals("ExportTag") ? chooseExportView.TagName.Text : string.Empty;
                    _location = chooseExportView.Location_Combobox.Text;
                    _isExportNewlyUpdatedTBTOnly = chooseExportView.checkBoxSyncKeysOnly.IsChecked.Value;
                    _isPartiallyExport = chooseExportView.PartialSwitch.IsOn;
                    ExportDir = chooseExportView.TextBoxFileName.Text;
                    ComboBoxItem exportFileTypeItem = chooseExportView.ExportFileType.SelectedItem as ComboBoxItem;
                    string exportFileType = exportFileTypeItem.Content as string;
                    switch (exportFileType)
                    {
                        case "CSV":
                            _exportFileType = LocalizationFileType.CSV;
                            break;
                        case "XLSX":
                            _exportFileType = LocalizationFileType.XLSX;
                            break;
                        default:
                            _exportFileType = LocalizationFileType.CSV;
                            break;
                    }

                    var selectedItem = (ComboBoxItem)chooseExportView.CollectType.SelectedItem;
                    _collectType = (string)selectedItem.Content;

                    if (_isPartiallyExport == true)
                    {
                        LocalizationDataManager.Instance.localData._isPartiallyExport = true;

                        ExportGrid.Children.Clear();
                        InitChooseCategoryPartialView();
                        ExportGrid.Children.Add(chooseCategoryPartialView);
                    }
                    else
                    {
                        LocalizationDataManager.Instance.localData._isPartiallyExport = false;

                        switch (_collectType)
                        {
                            case "All Language":
                                this.Close();
                                break;
                            case "By Language":
                                this.Close();
                                break;
                            case "By Custom Template":
                                ExportGrid.Children.Clear();
                                InitZipTemplateView();
                                ExportGrid.Children.Add(zipTemplateView);
                                break;
                            default:
                                break;
                        }

                    }
                    break;
                case "ChooseCategoryParitalExport":
                    switch (_collectType)
                    {
                        case "All Language":
                            this.Close();
                            break;
                        case "By Language":
                            this.Close();
                            break;
                        case "By Custom Template":
                            ExportGrid.Children.Clear();
                            InitZipTemplateView();
                            ExportGrid.Children.Add(zipTemplateView);
                            break;
                        default:
                            break;
                    }
                    break;
                case "ChooseZip":
                    _selectedTemplateList.Clear();
                    foreach (var box in zipTemplateView.checkBoxList)
                    {
                        if (box.IsChecked.Value)
                        {
                            _selectedTemplateList.Add(box.Content.ToString());
                        }
                    }
                    this.Close();
                    break;
                default:
                    break;
            }
        }

        private void ClickBtnApply(object sender, RoutedEventArgs e)
        {
            string name = setZipTemplateView.TemplateName.Text;

            if (string.IsNullOrEmpty(name))
            {
                //빈 이름
                ShowDialog("Empty Template Name", string.Empty);
                return;
            }
            //Template 이름 중복 검사
            if (LocalizationDataManager.Instance.configData.ExportTemplateList != null)
            {
                foreach (var template in LocalizationDataManager.Instance.configData.ExportTemplateList)
                {
                    if (template.name.Equals(name))
                    {
                        if (setZipTemplateView.isNewTempate)//New
                        {
                            //중복
                            ShowDialog("Duplicated Template Name", string.Empty);
                            return;
                        }
                        else //Edit
                        {
                            if (!setZipTemplateView.selectedTemplate.name.Equals(name))
                            {
                                //중복
                                ShowDialog("Duplicated Template Name", string.Empty);
                                return;
                            }
                        }
                    }
                }
            }

            List<string> languages = new List<string>();
            foreach (KeyValuePair<string, CheckBox> box in setZipTemplateView.languageCheck)
            {
                if (box.Value.IsChecked.Value)
                {
                    languages.Add(box.Key);
                }
            }

            var exportTemplates = LocalizationDataManager.Instance.configData.ExportTemplateList;

            if (setZipTemplateView.isNewTempate)
            {
                ExportTemplate template = new ExportTemplate();
                template.name = name;
                template.languageList = languages;

                if (exportTemplates == null)
                    LocalizationDataManager.Instance.configData.ExportTemplateList = new List<ExportTemplate>();
                LocalizationDataManager.Instance.configData.ExportTemplateList.Add(template);
                LocalizationDataManager.Instance.configData.SaveConfigData();
            }
            else
            {
                foreach (ExportTemplate template in exportTemplates)
                {
                    if (template.name.Equals(setZipTemplateView.selectedTemplate.name))
                    {
                        template.name = name;
                        template.languageList = languages;
                        break;
                    }
                }
                LocalizationDataManager.Instance.configData.SaveConfigData();
            }

            //이전 화면(zipTemplateView)으로 전환
            InitZipTemplateView();
            ExportGrid.Children.Clear();
            ExportGrid.Children.Add(zipTemplateView);
        }

        private void SetZipTemplates(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;

            string btnContent = (string)btn.Content;

            switch (btnContent)
            {
                case "New":
                    ExportGrid.Children.Clear();
                    InitSetZipTemplateView(true);
                    ExportGrid.Children.Add(setZipTemplateView);
                    break;
                case "Edit":
                    var selectedTemplate = LocalizationDataManager.Instance.configData.GetExportTemplate(zipTemplateView.selectedTemplateName);
                    ExportGrid.Children.Clear();
                    InitSetZipTemplateView(false, selectedTemplate);
                    ExportGrid.Children.Add(setZipTemplateView);
                    break;
                default:
                    break;
            }
        }

        private void ChildWindow_Loaded(object sender, RoutedEventArgs e)
        {
            chooseExportView.CheckValidateExport(sender, e);
        }

        private void ShowDialog(string title, string content)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;

            mainWindow.ShowBasicDialog(title, content);
        }

    }
}
