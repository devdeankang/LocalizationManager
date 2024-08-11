using MahApps.Metro.SimpleChildWindow;
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

namespace LocalizationManager
{
    /// <summary>
    /// ImportWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ImportWindow : ChildWindow
    {
        private ChooseImportView chooseImportView;

        public ImportWindow()
        {
            InitializeComponent();
            InitChooseImportView();
            ImportGrid.Children.Add(chooseImportView);
        }
        
        public void InitChooseImportView()
        {
            chooseImportView = new ChooseImportView();
            chooseImportView.ClickCancel += ClickBtnCancel;
            chooseImportView.ClickImport += ClickBtnImport;
        }

        private void ClickBtnCancel(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;            
            if(string.CompareOrdinal((string)btn.Tag, "ChooseImport") == 0)
            {
                this.Close();
            }
        }

        private void ClickBtnImport(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            int curImportSetting = -1;

            if (string.CompareOrdinal((string)btn.Tag, "ChooseImport") == 0)
            {
                string _importSetting = chooseImportView.ImportSetting.Text;
                foreach (ImportFileType fileType in System.Enum.GetValues(typeof(ImportFileType)))
                {
                    string desc = LocalizationDataManager.Instance.localData.GetImportTypeDesc(fileType);
                    if (string.CompareOrdinal(desc, _importSetting) == 0) 
                        curImportSetting = (int)fileType;
                }

                if (chooseImportView.Files != null)
                {
                    LocalizationDataManager.Instance.ImportFiles(chooseImportView.Files, curImportSetting, null);                    
                }
                else
                {
                    // Import File목록이 비어있을 때
                    string errorStr = string.Format("Files are empty.");
                    ShowDialog(errorStr, string.Empty);
                    return;
                }
            }
        }

        private void ShowDialog(string title, string content)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;

            mainWindow.ShowBasicDialog(title, content);
        }
    }
}