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
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.SimpleChildWindow;

namespace LocalizationManager
{
    /// <summary>
    /// MoveKeyWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MoveKeyWindow : ChildWindow
    {
        List<string> categoryList;

        public bool isSuccessAll = true;
        public string errorStr = string.Empty;

        public MoveKeyWindow()
        {
            InitializeComponent();

            categoryList = LocalizationDataManager.Instance.configData.Categories;
            SetCategoryBox();
            SetPartialBox();
        }

        private void SetCategoryBox()
        {
            CategoryBox.Items.Clear();
            foreach (string category in categoryList)
            {
                CategoryBox.Items.Add(category);
            }

            CategoryBox.SelectedIndex = 0;
        }

        private void CategoryBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var category = e.AddedItems[0] as string;

            SetPartialBox(category);
        }

        private void SetPartialBox(string selectedCategory = null)
        {
            PartialBox.Items.Clear();
            var category = string.IsNullOrEmpty(selectedCategory) ? categoryList[0] : selectedCategory;

            foreach (PartialInfo partialInfo in LocalizationDataManager.Instance.localData.categoryInfos[category].partialInfos)
            {
                PartialBox.Items.Add(partialInfo._partial);
            }

            PartialBox.SelectedIndex = 0;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnApply_Click(object sender, RoutedEventArgs e)
        {
            string[] moveKeyArr = MoveKey.Text.Split('\n');

            var category = CategoryBox.SelectedItem as string;
            var partial = PartialBox.SelectedIndex;

            List<string> errorList = new List<string>();
            for (int i = 0; i < moveKeyArr.Length; i++)
            {
                string moveKey = moveKeyArr[i].Trim();
                string errorStr;

                if (string.IsNullOrEmpty(moveKey))
                {
                    errorStr = string.Format("key : {0} :: Empty Key.", moveKey);
                    errorList.Add(errorStr);
                    continue;

                    //ShowDialog(string.Format("Empty Key.", moveKey), string.Empty);
                    //this.Close();
                    //return;
                }

                switch (LocalizationDataManager.Instance.localData.MoveKey(moveKey, category, partial))
                {
                    case 0: // success
                        break;
                    case 1:
                        errorStr = string.Format("key : {0} :: Key {0} is not exist.", moveKey);
                        errorList.Add(errorStr);

                        //ShowDialog(string.Format("Key {0} is not exist.", moveKey), string.Empty);
                        break;
                    case 2:
                        errorStr = string.Format("key : {0} :: Cannot move Key {0} to same files.", moveKey);
                        errorList.Add(errorStr);

                        //ShowDialog(string.Format("Cannot move Key {0} to same files.", moveKey), string.Empty);
                        break;
                    default:
                        break;
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

            LocalizationDataManager.Instance.SetSummaryInfo();

            this.Close();
        }

        private async void ShowDialog(string title, string content)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;

            await mainWindow.ShowMessageAsync(title, content);
        }

    }
}
