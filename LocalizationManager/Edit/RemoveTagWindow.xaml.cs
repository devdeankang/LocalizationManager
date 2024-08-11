using MahApps.Metro.Controls;
using MahApps.Metro.SimpleChildWindow;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace LocalizationManager
{
    /// <summary>
    /// RemoveTagWindow.xaml에 대한 상호 작용 논리
    /// </summary>

    public partial class RemoveTagWindow : ChildWindow
    {
        private ChooseRemoveTag chooseRemoveTag;
        public bool isSuccessAll = true;
        public string errorStr = string.Empty;
        private const string TagByTag = "TagByTag";
        private const string TagByKey = "TagByKey";


        public RemoveTagWindow(string selectedKey)
        {
            InitializeComponent();
            InitChooseImportView(selectedKey);
            RemoveTagGrid.Children.Add(chooseRemoveTag);
        }

        public void InitChooseImportView(string selectedKey)
        {
            chooseRemoveTag = new ChooseRemoveTag(selectedKey);
            chooseRemoveTag.ClickCancel += ClickBtnCancel;
            chooseRemoveTag.ClickRemove += ClickBtnRemove;
        }

        private void ClickBtnCancel(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            if (string.CompareOrdinal((string)btn.Tag, "ChooseRemoveTag") == 0)
            {
                this.Close();
            }
        }

        private void ClickBtnRemove(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            MetroTabItem metroTabItem = (MetroTabItem)chooseRemoveTag.RemoveTag.SelectedItem; ;


            if (string.CompareOrdinal((string)btn.Tag, "ChooseRemoveTag") == 0)
            {
                switch (metroTabItem.Name)
                {
                    case TagByTag:
                        RemoveTag(TagByTag, chooseRemoveTag.TagName.Text);
                        break;
                    case TagByKey:
                        RemoveTag(TagByKey, chooseRemoveTag.KeyName.Text);
                        break;
                    default:
                        break;
                }
            }
        }

        private void RemoveTag(string RemoveBy, string text)
        {            
            string[] removeTagArr = text.Split('\n');
            List<string> errorList = new List<string>();

            for (int i = 0; i < removeTagArr.Length; i++)
            {
                string removeTag = removeTagArr[i].Trim();

                if (string.IsNullOrEmpty(removeTag))
                {
                    errorStr = string.Format("tag : {0} :: Empty Tag.", removeTag);
                    errorList.Add(errorStr);
                }

                //TagByKey
                if (string.CompareOrdinal(RemoveBy, TagByKey) == 0)
                {
                    LocalizationDataManager.Instance.localData.RemoveTagByKey(removeTag);
                }

                // TagByTag
                else
                {
                    if (LocalizationDataManager.Instance.localData.RemoveTag(removeTag))
                    {
                        // success
                    }
                    else
                    {
                        errorStr = string.Format("tag : {0} :: Not exist Tag '{0}' in localData.", removeTag);
                        errorList.Add(errorStr);
                    }
                }

                if (errorList.Count > 0)
                {
                    isSuccessAll = false;
                    errorStr = string.Empty;
                    foreach (string str in errorList)
                    {
                        if (string.IsNullOrEmpty(errorStr) == true)
                            errorStr = str;
                        else
                            errorStr = string.Format("{0}\n{1}", errorStr, str);
                    }
                }
            }
            // TAG가 TBRT가 아니고 번역이 되어 있을 경우, Translated 처리를 위해
            LocalizationDataManager.Instance.SetTranslationStatus();

            LocalizationDataManager.Instance.SetSummaryInfo();
            this.Close();

        }

        private void ShowDialog(string title, string content)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;

            mainWindow.ShowBasicDialog(title, content);
        }
    }
}