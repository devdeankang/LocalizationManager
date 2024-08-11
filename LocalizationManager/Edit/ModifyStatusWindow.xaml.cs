using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.SimpleChildWindow;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace LocalizationManager
{
    /// <summary>
    /// ModifyStatusWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ModifyStatusWindow : ChildWindow
    {
        public ModifyStatusWindow()
        {
            InitializeComponent();
            InitNewStatusBox();
        }

        private void InitLanguageBox(string selectedLanguage = null)
        {
            List<string> languagesList = LocalizationDataManager.Instance.configData.Languages;
            int selectedIndex = 0;
            int index = 0;

            LanguageBox.Items.Clear();
            for (int i = 0; i < languagesList.Count; i++)
            {
                string language = languagesList[i];
                if (language.Equals(ConfigData.SourceLanguage, StringComparison.OrdinalIgnoreCase) == true)
                    continue;
                LanguageBox.Items.Add(language);

                if (string.IsNullOrEmpty(selectedLanguage) == false && selectedLanguage.Equals(language, StringComparison.OrdinalIgnoreCase) == true)
                    selectedIndex = index;
                index++;
            }
            LanguageBox.Items.Add("All");
            LanguageBox.SelectedIndex = selectedIndex;
        }

        private void InitNewStatusBox()
        {
            ModifyStatusBox.Items.Clear();
            string[] statusArr = LocalizationDataManager.STATUS_ARR;

            for (int i = 0; i < statusArr.Length; i++)
            {
                string status = statusArr[i];

                ModifyStatusBox.Items.Add(status);
            }
            ModifyStatusBox.SelectedIndex = 0;
        }

        public void UpdateStatusWindow(string key, string category = null, int partial = -1, string language = null, bool initialUpdate = false)
        {
            FileLine itemToFind = null;
            if (category != null && partial != -1)
            {
                itemToFind = LocalizationDataManager.GetFileLine(category, partial, language, key);
            }

            if (itemToFind == null)
            {
                itemToFind = LocalizationDataManager.FindFileLine(key, category, partial, language);
            }

            UpdateStatusWindow(itemToFind, initialUpdate);
        }

        public void UpdateStatusWindow(FileLine toolLine, bool initialUpdate = false)
        {
            if (initialUpdate)
            {
                InitLanguageBox(toolLine != null ? toolLine.file._language : null);
                KeyText.Text = toolLine != null ? toolLine.key : null;
            }
            StatusText.Text = toolLine != null ? toolLine.status : null;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnApply_Click(object sender, RoutedEventArgs e)
        {
            string key = KeyText.Text;
            string newStatus  = ModifyStatusBox.Text;                        
            string language = LanguageBox.SelectedItem.ToString();

            //기존 Key 이름이 비어있을 때
            if (string.IsNullOrEmpty(key))
            {
                string errorStr = string.Format("The Key is empty.");
                ShowDialog(errorStr, string.Empty);
                return;
            }

            List<FileLine> fileLines = LocalizationDataManager.FindFileLines(key, null, -1, (language == "All") ? null : language);
            if (fileLines.Count == 0)
            {
                string errorStr = string.Format("The key does not exist.");
                ShowDialog(errorStr, string.Empty);
                return;
            }

            foreach(FileLine fileLine in fileLines)
            {
                LocalizationDataManager.Instance.UpdateStatus(fileLine, newStatus);
            }

            this.Close();
        }

        private async void ShowDialog(string title, string content)
        {
            MainWindow mainWindow = Window.GetWindow(this) as MainWindow;

            await mainWindow.ShowMessageAsync(title, content);
        }

        private void Keys_OnTextChanged(object sender, TextChangedEventArgs args)
        {
            string language = LanguageBox.SelectedItem.ToString();
            UpdateStatusWindow(KeyText.Text, null, -1, (language != "All") ? language : null);
        } // end textChangedEventHandler
    }
}
