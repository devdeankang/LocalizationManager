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
    /// SetZipTemplateView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SetZipTemplateView : UserControl
    {
        public bool isNewTempate = true;
        public ExportTemplate selectedTemplate;

        public event RoutedEventHandler ClickCancel;
        public event RoutedEventHandler ClickApply;

        public Dictionary<string, CheckBox> languageCheck = new Dictionary<string, CheckBox>();


        public SetZipTemplateView(bool isNew, ExportTemplate template = null)
        {
            InitializeComponent();

            isNewTempate = isNew;

            if (template != null)
                selectedTemplate = template;

            SetLanguages();

            //Edit의 경우, 기존 정보 set
            if (!isNewTempate)
                SetEditTemplates();

            //Language가 하나라도 선택되어야 Apply 가능
            btnApply.IsEnabled = CheckAllUnselected() ? false : true;
        }

        private void SetEditTemplates()
        {
            TemplateName.Text = selectedTemplate.name;
            
            foreach (string language in selectedTemplate.languageList)
            {
                languageCheck[language].IsChecked = true;
            }
        }

        public void SetLanguages()
        {
            foreach (string language in LocalizationDataManager.Instance.configData.Languages)
            {
                //if (language.Equals("Korean")) continue;

                CheckBox box = new CheckBox();
                box.Content = language;
                box.Margin = new Thickness(5);
                box.Click += ClickLanguage;
                languageCheck.Add(language, box);
                LanguageList.Children.Add(box);
            }
        }

        private void ClickSelectAll(object sender, RoutedEventArgs e)
        {
            CheckBox select = (CheckBox)sender;
            bool isSelected = select.IsChecked.Value;
            
            foreach (CheckBox box in languageCheck.Values)
            {
                box.IsChecked = isSelected;
            }

            btnApply.IsEnabled = CheckAllUnselected() ? false : true;
        }

        private void ClickLanguage(object sender, RoutedEventArgs e)
        {
            btnApply.IsEnabled = CheckAllUnselected() ? false : true;

            //SelectAll Check
            SelectAll.IsChecked = CheckAllSelected();
        }

        private bool CheckAllUnselected()
        {
            foreach (CheckBox box in languageCheck.Values)
            {
                if (box.IsChecked.Value)
                {
                    return false;
                }
            }

            return true;
        }

        private bool CheckAllSelected()
        {
            foreach (CheckBox box in languageCheck.Values)
            {
                if (!box.IsChecked.Value)
                {
                    return false;
                }
            }

            return true;
        }

        private void ClickBtnCancel(object sender, RoutedEventArgs e)
        {
            if (ClickCancel != null)
            {
                ClickCancel(sender, e);
            }
        }

        private void ClickBtnApply(object sender, RoutedEventArgs e)
        {
            if (ClickApply != null)
            {
                ClickApply(sender, e);
            }
        }
    }
}
