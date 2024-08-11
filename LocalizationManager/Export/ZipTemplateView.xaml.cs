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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LocalizationManager
{
    /// <summary>
    /// ZipTemplateView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ZipTemplateView : UserControl
    {
        const string GRID_STR = "Grid_";
        const string CHECKBOX_STR = "Chk_";

        public event RoutedEventHandler ClickCancel;
        public event RoutedEventHandler ClickExport;
        public event RoutedEventHandler ClickSetTemplate;

        public Dictionary<string, ExportTemplate> templates;
        public List<CheckBox> checkBoxList = new List<CheckBox>();

        public string selectedTemplateName = string.Empty;

        public ZipTemplateView()
        {
            InitializeComponent();

            SetExportTemplate();
        }

        public void SetExportTemplate()
        {
            TemplateCheckList.Children.Clear();
            checkBoxList.Clear();

            if (LocalizationDataManager.Instance.configData.ExportTemplateList == null) return;

            templates = LocalizationDataManager.Instance.configData.ExportTemplateList.ToDictionary(x=>x.name, x => x);

            int i = 0;
            foreach (ExportTemplate template in templates.Values)
            {
                Grid grid = new Grid();
                grid.Name = GetGridName(i.ToString());
                CheckBox checkBox = new CheckBox();
                checkBox.Name = GetCheckBoxName(i.ToString());
                checkBox.Tag = grid;
                checkBox.Content = template.name;
                checkBox.Margin = new Thickness(5);
                checkBox.Click += ClickTemplate;

                checkBoxList.Add(checkBox);
                grid.Children.Add(checkBox);
                TemplateCheckList.Children.Add(grid);
            }

            //Export Check
            CheckBtnExportEnable();
        }

        private string GetGridName(string templateName)
        {
            return string.Format("{0}{1}", GRID_STR, templateName);
        }

        private string GetCheckBoxName(string templateName)
        {
            return string.Format("{0}{1}", CHECKBOX_STR, templateName);
        }

        private void ClickTemplate(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
           
            foreach(CheckBox box in checkBoxList)
            {
                var grid = (Grid)box.Tag;
                grid.Background = Brushes.Transparent;
            }

            selectedTemplateName = checkBox.Content.ToString();
            var selectedGrid = (Grid)checkBox.Tag;
            selectedGrid.Background = Brushes.SkyBlue;

            string content = (string)checkBox.Content;
            TemplateName.Text = templates[content].name;
            TemplateLanguaes.Text = GetTemplateLanguageTxt(templates[content].languageList);

            //selectAll Check
            SelectAll.IsChecked = CheckAllSelected();

            //edit, delete set
            btnEdit.IsEnabled = true;
            btnDelete.IsEnabled = true;

            //Export Check
            CheckBtnExportEnable();
        }

        private string GetTemplateLanguageTxt(List<string> languageList)
        {
            string value = "";
            for (int i = 0; i < languageList.Count; i++)
            {
                string lang = languageList[i];

                value = (i == 0) ? lang : string.Format("{0}{1}", value, lang);
                value = (i == languageList.Count - 1) ? value : string.Format("{0}{1}", value, Environment.NewLine);
            }

            return value;
        }

        //all template choice
        private void ClickSelectAll(object sender, RoutedEventArgs e)
        {
            CheckBox selectAll = (CheckBox)sender;

            foreach (CheckBox box in checkBoxList)
            {
                box.IsChecked = selectAll.IsChecked;
            }

            //Export Check
            CheckBtnExportEnable();
        }

        private bool CheckAllSelected()
        {
            foreach (CheckBox box in checkBoxList)
            {
                if (!box.IsChecked.Value)
                {
                    return false;
                }
            }

            return true;
        }

        private void CheckBtnExportEnable()
        {
            //checkbox중 하나라도 export되어있으면 export 버튼 활성화
            foreach (CheckBox box in checkBoxList)
            {
                if (box.IsChecked.Value)
                {
                    btnExport.IsEnabled = true;
                    return;
                }
            }

            btnExport.IsEnabled = false;
            return;
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

        private void SetTemplates(object sender, RoutedEventArgs e)
        {
            if (ClickSetTemplate != null)
            {
                ClickSetTemplate(sender, e);
            }
        }

        private void DelteTemplate(object sender, RoutedEventArgs e)
        {
            var exportList = LocalizationDataManager.Instance.configData.ExportTemplateList;

            int i = 0;
            foreach (ExportTemplate template in exportList)
            {
                if (template.name.Equals(selectedTemplateName))
                {
                    exportList.RemoveAt(i);
                    break;
                }
                i++;
            }

            LocalizationDataManager.Instance.configData.SaveConfigData();
            SetExportTemplate();

            TemplateName.Text = string.Empty;
            TemplateLanguaes.Text = string.Empty;

            //edit, delete set
            btnEdit.IsEnabled = false;
            btnDelete.IsEnabled = false;
        }
    }
}
