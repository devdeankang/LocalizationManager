using MahApps.Metro.SimpleChildWindow;
using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.IO;
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
using Microsoft.WindowsAPICodePack.Dialogs;

namespace LocalizationManager
{
    public enum CreateProjectResultType
    {
        None,
        Success,
        Cancel,
        ExistProjectFile,
    }

    /// <summary>
    /// OpenProjectWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class NewProjectWindow : ChildWindow
    {
        public bool isOK = false;
        public CreateProjectResultType resultType = CreateProjectResultType.None;

        public NewProjectWindow()
        {
            InitializeComponent();
        }

        private void CheckValidateProject(object sender, RoutedEventArgs e)
        {
            if (isValidateName(ProjectName.Text) && isValidateDirectory(Location.Text))
            {
                btnCreate.IsEnabled = true;
            }
            else
            {
                btnCreate.IsEnabled = false;
            }
        }

        private bool isValidateName(string name)
        {
            return string.IsNullOrEmpty(name) ? false : true;
        }

        private bool isValidateDirectory(string directory)
        {
            return Directory.Exists(directory) ? true : false;
        }

        private void SetProjectLocation(object sender, RoutedEventArgs e)
        {
            var location = OpenDirectory();
            if (!string.IsNullOrEmpty(location))
            {
                Location.Text = location;
            }
        }

        private void ClickBtnCancel(object sender, RoutedEventArgs e)
        {
            isOK = false;
            resultType = CreateProjectResultType.Cancel;
            this.Close();
        }

        private void ClickBtnCreate(object sender, RoutedEventArgs e)
        {
            //해당 경로에 프로젝트 파일이 있는지 검사
            string[] existProjectFiles = Directory.GetFiles(Location.Text, "*.lmp");
            if (existProjectFiles != null && existProjectFiles.Length > 0)
            {
                resultType = CreateProjectResultType.ExistProjectFile;
                this.Close();
                return;
            }

            ProjectInfo info = new ProjectInfo(ProjectName.Text, Location.Text);
            RecentProjectInfo.Instance.AddRecentFile(info);
            LocalizationDataManager.Instance.configData = new ConfigData(ProjectName.Text, Location.Text);
            LocalizationDataManager.Instance.configData.SaveConfigData();

            isOK = true;
            resultType = CreateProjectResultType.Success;
            this.Close();
        }

        private void ChildWindow_Loaded(object sender, RoutedEventArgs e)
        {
            CheckValidateProject(sender, e);
        }

        private string OpenDirectory()
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.InitialDirectory = "C:\\Users";
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                return dialog.FileName;
            }

            return string.Empty;
        }
    }
}
