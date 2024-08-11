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
    /// FixKoreanWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class FixKoreanWindow : ChildWindow
    {
        public FixKoreanWindow()
        {
            InitializeComponent();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnApply_Click(object sender, RoutedEventArgs e)
        {
            string category = string.Empty;
            int partial = -1;

            //존재하는 Key가 아닐 때
            if (!LocalizationDataManager.Instance.localData.isExistKey(FixKey.Text, ref category, ref partial))
            {
                ShowDialog(string.Format("Cannot find Key '{0}'.", FixKey.Text), string.Empty);
                return;
            }

            //Korean Text가 비어있을 때
            if (string.IsNullOrEmpty(FixKorean.Text.Trim()))
            {
                ShowDialog(string.Format("Korean Text is empty.", FixKorean.Text), string.Empty);
                return;
            }

            //Fix Korean
            LocalizationDataManager.Instance.localData.FixKorean(category, partial, FixKey.Text, FixKorean.Text);

            this.Close();
        }

        private async void ShowDialog(string title, string content)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;

            await mainWindow.ShowMessageAsync(title, content);
        }
    }
}
