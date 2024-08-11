using System;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.SimpleChildWindow;

namespace LocalizationManager
{
    /// <summary>
    /// FixKoreanWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ModifyTranslationWindow : ChildWindow
    {
        List<string> categoryList;
        List<string> languageList;

        public ModifyTranslationWindow()
        {
            InitializeComponent();
        }

        public ModifyTranslationWindow(ModifyTranslationInfo modifyTranslationInfo)
        {
            InitializeComponent();
            InitModifyTranslationInfo(modifyTranslationInfo);
        }

        private void InitModifyTranslationInfo(ModifyTranslationInfo modifyTranslationInfo)
        {
            categoryList = LocalizationDataManager.Instance.configData.Categories;
            languageList = LocalizationDataManager.Instance.configData.Languages;

            SetCategoryBox(modifyTranslationInfo != null ? modifyTranslationInfo.Category : null);
            SetPartialBox(null, modifyTranslationInfo != null ? modifyTranslationInfo.Partial : 0);
            SetLanguageBox(modifyTranslationInfo != null ? modifyTranslationInfo.Language : null);
            TranslationText.Text = modifyTranslationInfo != null ? modifyTranslationInfo.Translation : string.Empty;
            KeyText.Text = modifyTranslationInfo != null ? modifyTranslationInfo.Key : string.Empty;
        }

        private void SetLanguageBox(string selectedLanguage)
        {
            int selectedIndex = 0;
            LanguageBox.Items.Clear();
            int index = 0;
            for (int i = 0; i < languageList.Count; i++)
            {
                string language = languageList[i];

                if (language.Equals(ConfigData.SourceLanguage, StringComparison.OrdinalIgnoreCase) == true)
                    continue;

                LanguageBox.Items.Add(language);
                if (string.IsNullOrEmpty(selectedLanguage) == false && selectedLanguage == language)
                {
                    selectedIndex = index;
                }
                index++;
            }

            LanguageBox.SelectedIndex = selectedIndex;
        }

        private void SetCategoryBox(string selectedCategory)
        {
            int selectedIndex = 0;
            CategoryBox.Items.Clear();
            for (int i = 0; i < categoryList.Count; i++)
            {
                string category = categoryList[i];
                CategoryBox.Items.Add(category);
                if (string.IsNullOrEmpty(selectedCategory) == false && selectedCategory == category)
                {
                    selectedIndex = i;
                }
            }

            CategoryBox.SelectedIndex = selectedIndex;
        }

        private void SetPartialBox(string selectedCategory = null, int selectedPartial = 0)
        {
            int selectedIndex = 0;
            PartialBox.Items.Clear();
            var category = string.IsNullOrEmpty(selectedCategory) ? categoryList[0] : selectedCategory;

            foreach (PartialInfo partialInfo in LocalizationDataManager.GetPartialInfos(category))
            {
                PartialBox.Items.Add(partialInfo._partial);
                if (selectedPartial == partialInfo._partial)
                {
                    selectedIndex = partialInfo._partial;
                }
            }

            PartialBox.SelectedIndex = selectedIndex;
        }


        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnApply_Click(object sender, RoutedEventArgs e)
        {
            //기존 Key 이름이 비어있을 때
            if (string.IsNullOrEmpty(KeyText.Text))
            {
                string errorStr = string.Format("Key name cannot be empty.");
                ShowDialog(errorStr, string.Empty);
                return;
            }

            string category = CategoryBox.SelectedItem.ToString();
            int partial = PartialBox.SelectedIndex;
            string language = LanguageBox.SelectedItem.ToString();
            string key = KeyText.Text;

            //존재하는 Key가 아닐 때
            FileLine fileLine = LocalizationDataManager.GetFileLine(category, partial, language, key);
            if (fileLine == null)
            {
                string errorStr = string.Format("Cannot find key.");
                ShowDialog(errorStr, string.Empty);
                return;
            }

            string newText = ModifyTranslationText.Text.Trim();

            //Modify Translation
            LocalizationDataManager.Instance.localData.ModifyTranslation(category, partial, language, key, newText);

            this.Close();
        }

        private async void ShowDialog(string title, string content)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;

            await mainWindow.ShowMessageAsync(title, content);
        }

        private void CategoryBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var category = e.AddedItems[0] as string;

            UpdateChangedCombobox();
            SetPartialBox(category);
        }

        private void PartialBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateChangedCombobox();
        }

        private void LanguageBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateChangedTextBox();
        }

        private void KeyText_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateChangedTextBox();
        }

        private void UpdateChangedTextBox()
        {
            var fileinfo = LocalizationDataManager.Instance.localData.categoryInfos[CategoryBox.SelectedItem.ToString()].
                partialInfos[PartialBox.SelectedIndex].
                languageInfos[LanguageBox.SelectedItem.ToString()].fileInfo;

            TranslationText.Text = fileinfo.ContainsKey(KeyText.Text) == true ? fileinfo[KeyText.Text].translation : string.Empty;
            ModifyTranslationText.IsReadOnly = string.IsNullOrEmpty(TranslationText.Text) == true;
        }

        private void UpdateChangedCombobox()
        {
            KeyText.Text = string.Empty;
            TranslationText.Text = string.Empty;
            ModifyTranslationText.IsReadOnly = true;
        }
    }
}
