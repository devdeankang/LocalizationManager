using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.SimpleChildWindow;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace LocalizationManager
{
    /// <summary>
    /// PreTranslationWindow.xaml에 대한 상호 작용 논리
    /// </summary>    
    public partial class PreTranslationWindow : ChildWindow
    {
        private readonly string[] PlatformList = { "Google", "Microsoft", "Naver" };

        public PreTranslationWindow()
        {
            InitializeComponent();
            InitComboBox();
        }

        private void InitComboBox()
        {
            // Cloud Platform
            ProviderBox.Items.Clear();
            foreach (string item in PlatformList) ProviderBox.Items.Add(item);
            ProviderBox.SelectedIndex = 0;

            // Category
            CategoryBox.Items.Clear();
            foreach (string category in LocalizationDataManager.Instance.configData.Categories) CategoryBox.Items.Add(category);
            CategoryBox.SelectedIndex = 0;

            // SourceLang
            SourceLangBox.Items.Clear();
            foreach (string language in LocalizationDataManager.Instance.configData.Languages) SourceLangBox.Items.Add(language);
            SourceLangBox.SelectedIndex = 0;

            // TargetLang
            TargetLangBox.Items.Clear();
            foreach (string language in LocalizationDataManager.Instance.configData.Languages) TargetLangBox.Items.Add(language);
            TargetLangBox.SelectedIndex = 1;

            // InclusionTag
            InclusionTagBox.Items.Clear();
            InclusionTagBox.Items.Add("");
            List<string> TagList = new List<string>();
            Dictionary<string, CategoryInfo> categoryInfos = LocalizationDataManager.Instance.localData.categoryInfos;
            foreach (CategoryInfo categoryInfo in categoryInfos.Values)
            {
                foreach (KeyValuePair<string, Dictionary<string, FileLine>> tagKeys in categoryInfo.tagKeysDic)
                {
                    if (TagList.Contains(tagKeys.Key)) continue;
                    TagList.Add(tagKeys.Key);
                }
            }
            foreach (string tag in TagList) InclusionTagBox.Items.Add(tag);
            InclusionTagBox.SelectedIndex = 0;
        }


        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnTranslate_Click(object sender, RoutedEventArgs e)
        {
            if (string.CompareOrdinal(SourceLangBox.Text, TargetLangBox.Text) == 0)
            {
                string errorStr = "The selected Languages are the same Language.";
                ShowDialog(errorStr, string.Empty);
                this.Close();
            }

            if (TranslatedBox.IsChecked == false && EmptyBox.IsChecked == false && NewBox.IsChecked == false &&
                UpdateBox.IsChecked == false && PreTranslatedBox.IsChecked == false)
            {
                string errorStr = "There are no selected Status Items.";
                ShowDialog(errorStr, string.Empty);
                this.Close();
            }

            string tagBox = InclusionTagBox.Text;

            if (PartialBox.Text.Equals("All", StringComparison.OrdinalIgnoreCase))
                LocalizationDataManager.Instance.CategoryPreTranslate(ProviderBox.Text, CategoryBox.Text, SourceLangBox.Text,
                    TargetLangBox.Text, curSelectedStatus(), tagBox);
            else
                LocalizationDataManager.Instance.PartialPreTranslate(ProviderBox.Text, CategoryBox.Text, Convert.ToInt32(PartialBox.Text),
                    SourceLangBox.Text, TargetLangBox.Text, curSelectedStatus(), tagBox);
            
            this.Close();
        }

        public Dictionary<string, bool> curSelectedStatus()
        {
            string EMPTY = LocalizationDataManager.STATUS_EMPTY;
            string NEW = LocalizationDataManager.STATUS_NEW;
            string ADD = LocalizationDataManager.STATUS_NEW_ALT;
            string UPDATE = LocalizationDataManager.STATUS_UPDATE;
            string FIX = LocalizationDataManager.STATUS_UPDATE_ALT;
            string TRANSLATED = LocalizationDataManager.STATUS_TRANSLATED;
            string PRETRANSLATED = LocalizationDataManager.STATUS_PRE_TRANSLATED;
            Dictionary<string, bool> isNotSelectedStatus = new Dictionary<string, bool>();

            bool emptyStatus = EmptyBox.IsChecked != null ? (bool)EmptyBox.IsChecked : false;
            bool newStatus = NewBox.IsChecked != null ? (bool)NewBox.IsChecked : false;
            bool updateStatus = UpdateBox.IsChecked != null ? (bool)UpdateBox.IsChecked : false;
            bool translatedStatus = TranslatedBox.IsChecked != null ? (bool)TranslatedBox.IsChecked : false;
            bool preTranslatedStatus = PreTranslatedBox.IsChecked != null ? (bool)PreTranslatedBox.IsChecked : false;

            if (emptyStatus == false) isNotSelectedStatus.Add(EMPTY, emptyStatus);
            if (newStatus == false) { isNotSelectedStatus.Add(NEW, newStatus); isNotSelectedStatus.Add(ADD, newStatus); }
            if (updateStatus == false) { isNotSelectedStatus.Add(UPDATE, updateStatus); isNotSelectedStatus.Add(FIX, updateStatus); }
            if (translatedStatus == false) isNotSelectedStatus.Add(TRANSLATED, translatedStatus);
            if (preTranslatedStatus == false) isNotSelectedStatus.Add(PRETRANSLATED, preTranslatedStatus);

            return isNotSelectedStatus;
        }

        private void Category_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PartialBox.Items.Clear();
            PartialBox.Items.Add("All");
            string selectedCategory = CategoryBox.SelectedItem.ToString();
            List<PartialInfo> partialInfos = LocalizationDataManager.GetPartialInfos(selectedCategory);
            if (partialInfos.Count == 1) PartialBox.Items.Clear();
            foreach (PartialInfo partialInfo in partialInfos) PartialBox.Items.Add(partialInfo._partial);
            PartialBox.SelectedIndex = 0;
        }


        private async void ShowDialog(string title, string content)
        {
            MainWindow mainWindow = Window.GetWindow(this) as MainWindow;
            await mainWindow.ShowMessageAsync(title, content);
        }
    }
}
