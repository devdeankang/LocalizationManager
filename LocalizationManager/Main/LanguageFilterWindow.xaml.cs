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
    /// LanguageFilterWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class LanguageFilterWindow : ChildWindow
    {
        //public Dictionary<string, bool> languageFilterDic = new Dictionary<string, bool>();
        public Dictionary<string, CheckBox> languageCheck = new Dictionary<string, CheckBox>();

        public LanguageFilterWindow()
        {
            InitializeComponent();

            SetLanguageList();
        }

        //Language Filter 언어 Set
        public void SetLanguageList()
        {
            foreach (string language in LocalizationDataManager.Instance.configData.Languages)
            {
                if (language.Equals("Korean")) continue;

                CheckBox box = new CheckBox();
                box.Content = language;
                box.Margin = new Thickness(5);
                box.Click += ClickLanguage;

                var languageFilterKey = GetLanguageFilterKey(language);
                var languageFilterValue = RegistryManager.Instance.LoadStr(languageFilterKey);

                bool isLangaugeSet = true;
                switch (languageFilterValue)
                {
                    case "True":
                        isLangaugeSet = true;
                        break;
                    case "False":
                        isLangaugeSet = false;
                        break;
                    default:
                        isLangaugeSet = true;
                        break;
                }

                box.IsChecked = isLangaugeSet;
                languageCheck.Add(languageFilterKey, box);
                LanguageList.Children.Add(box);
            }

            //SelectAll Check
            SelectAll.IsChecked = CheckAllSelected();
        }

        private void ClickLanguage(object sender, RoutedEventArgs e)
        {
            //SelectAll Check
            SelectAll.IsChecked = CheckAllSelected();
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

        private void ClickSelectAll(object sender, RoutedEventArgs e)
        {
            CheckBox select = (CheckBox)sender;
            bool isSelected = select.IsChecked.Value;

            foreach (CheckBox box in languageCheck.Values)
            {
                box.IsChecked = isSelected;
            }
        }

        //Language Filter 언어 Save
        public void SaveLanguageList()
        {
            foreach (KeyValuePair<string, CheckBox> pair in languageCheck)
            {
                var languageFilterKey = pair.Key;
                bool isLangaugeSet = pair.Value.IsChecked.Value;

                var languageFilterValue = isLangaugeSet ? "True" : "False";

                RegistryManager.Instance.StoreStr(languageFilterKey, languageFilterValue);
            }
        }

        public static string GetLanguageFilterKey(string language)
        {
            return string.Format("{0}{1}", typeof(LanguageFilterWindow).FullName, language);
        }

        private void btnApply_Click(object sender, RoutedEventArgs e)
        {
            SaveLanguageList();
            this.Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
