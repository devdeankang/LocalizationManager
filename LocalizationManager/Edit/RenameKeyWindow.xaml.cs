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
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.SimpleChildWindow;

namespace LocalizationManager
{
    /// <summary>
    /// RenameKeyWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class RenameKeyWindow : ChildWindow
    {
        public bool isSuccessAll = true;
        public string errorStr = string.Empty;

        public RenameKeyWindow(string selectedKey)
        {
            InitializeComponent();

            if (string.IsNullOrEmpty(selectedKey) == false)
            {
                OriginalKey.Text = selectedKey;
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnApply_Click(object sender, RoutedEventArgs e)
        {
            string[] originalKeys = OriginalKey.Text.Split('\n');
            string[] renameKeys = RenameKey.Text.Split('\n');

            //original key 수와 rename key 수가 맞지 않을 때
            if (originalKeys.Length != renameKeys.Length)
            {
                ShowDialog("Not Equals Original, Rename Key's Total Nums.",
                    string.Format("Original Num : {0}, Rename Num : {1}", originalKeys.Length, renameKeys.Length));
                return;
            }

            List<string> errorList = new List<string>();
            for (int i = 0; i < originalKeys.Length; i++)
            {
                string originalKey = originalKeys[i].Trim();
                string renameKey = renameKeys[i].Trim();

                string originCategory = string.Empty;
                int originPartial = -1;
                string renameCategory = string.Empty;
                int renamePartial = -1;

                bool isRename = true;

                //기존 Key 이름이 비어있을 때
                if (string.IsNullOrEmpty(originalKey))
                {
                    string errorStr = string.Format("original : {0}, rename : {1} :: Original Key is empty.", originalKey, renameKey);
                    isRename = false;
                    errorList.Add(errorStr);
                    continue;
                }

                //존재하는 Key가 아닐 때
                if (!LocalizationDataManager.Instance.localData.isExistKey(originalKey, ref originCategory, ref originPartial))
                {
                    string errorStr = string.Format("original : {0}, rename : {1} :: Cannot find Key {0}.", originalKey, renameKey);
                    isRename = false;
                    errorList.Add(errorStr);
                    continue;
                    //ShowDialog(string.Format("{0}, {1} : Cannot find Key.", OriginalKey.Text), string.Empty);
                    //return;
                }

                //바꾸는 Key 이름이 비어있을 때
                if (string.IsNullOrEmpty(renameKey))
                {
                    string errorStr = string.Format("original : {0}, rename : {1} :: Rename Key is empty.", originalKey, renameKey);
                    isRename = false;
                    errorList.Add(errorStr);
                    continue;
                    //ShowDialog(string.Format("Rename Key is empty.", RenameKey.Text, category, partial), string.Empty);
                    //return;
                }

                //바꾸는 Key 이름이 존재할 때
                if (LocalizationDataManager.Instance.localData.isExistKey(renameKey, ref renameCategory, ref renamePartial))
                {
                    if (SwapCheckbox.IsChecked.Value == false)
                    {
                        string errorStr = string.Format("original : {0}, rename : {1} :: Duplicate Key '{1}' exists in {2}_{3}.", originalKey, renameKey, renameCategory, renamePartial);
                        isRename = false;
                        errorList.Add(errorStr);
                        continue;
                    }
                    
                    //ShowDialog(string.Format("Duplicate Key '{0}' exists in {1}_{2}.", RenameKey.Text, category, partial), string.Empty);
                    //return;
                }
                else
                {
                    if (SwapCheckbox.IsChecked.Value == true)
                    {
                        string errorStr = string.Format("original : {0}, rename : {1} :: Cannot find Key {1}.", originalKey, renameKey);
                        isRename = false;
                        errorList.Add(errorStr);
                        continue;
                    }
                }

                if (isRename == true)
                {
                    if (SwapCheckbox.IsChecked.Value == true)
                    {
                        //Swap Key
                        LocalizationDataManager.Instance.localData.SwapKey(originCategory, originPartial, renameCategory, renamePartial, originalKey, renameKey);
                    }
                    else
                    {
                        //Rename Key
                        LocalizationDataManager.Instance.localData.RenameKey(originCategory, originPartial, originalKey, renameKey);
                    }
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

            this.Close();
        }

        private async void ShowDialog(string title, string content)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;

            await mainWindow.ShowMessageAsync(title, content);
        }
    }
}
