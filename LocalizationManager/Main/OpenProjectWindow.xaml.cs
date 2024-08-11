using MahApps.Metro.SimpleChildWindow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LocalizationManager
{
    /// <summary>
    /// OpenProjectWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class OpenProjectWindow : ChildWindow
    {
        public const string FindDupKeysLoadKeyStr = "FIND_DUP_KEYS_LOAD";

        MainWindow MainWindow;

        public OpenProjectWindow(MainWindow window)
        {
            MainWindow = window;

            InitializeComponent();

            Init();
        }

        private void Init()
        {
            SetRecentFindDupKeyValue();
            SetRecentProjectList();
        }

        private void SetRecentFindDupKeyValue()
        {
            SwitchCheckDupKeys.IsOn = GetRecentFindDupKeySettting();
        }

        public static bool GetRecentFindDupKeySettting()
        {
            string isFindDupKeys = RegistryManager.Instance.LoadStr(FindDupKeysLoadKeyStr, RegistryManager.Instance.REGISTRY_KEY_STARTS);

            if (string.IsNullOrEmpty(isFindDupKeys) == true)
                return true;

            return (isFindDupKeys == "True");
        }

        public void SetRecentProjectList()
        {
            ProjectListPanel.Children.Clear();
            var recentFiles = RecentProjectInfo.Instance.GetRecentFiles();

            foreach (ProjectInfo info in recentFiles)
            {
                ProjectListPanel.Children.Add(CreateProjectButton(info));
            }
        }

        private Button CreateProjectButton(ProjectInfo info)
        {
            Button button = new Button();
            button.Name = "project1";
            button.Width = 650;
            button.Margin = new Thickness(50, 0, 50, 0);
            button.Click += btnProject_Click;
            button.Tag = info;

            WrapPanel panel = new WrapPanel();
            panel.Margin = new Thickness(5);
            panel.Orientation = Orientation.Vertical;
            panel.HorizontalAlignment = HorizontalAlignment.Left;
            panel.Width = 600;

            TextBlock block1 = new TextBlock();
            block1.Text = info.Name;
            block1.FontWeight = FontWeights.Bold;
            block1.FontSize = 15;

            TextBlock block2 = new TextBlock();
            block2.Text = info.Directory;
            block2.TextWrapping = TextWrapping.Wrap;
            block2.FontWeight = FontWeights.Light;
            block2.FontSize = 12;

            panel.Children.Add(block1);
            panel.Children.Add(block2);

            button.Content = panel;

            return button;
        }

        private void btnProject_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            ProjectInfo info = (ProjectInfo)btn.Tag;

            RegistryManager.Instance.StoreStr(FindDupKeysLoadKeyStr, SwitchCheckDupKeys.IsOn.ToString(), RegistryManager.Instance.REGISTRY_KEY_STARTS);
            LocalizationDataManager.Instance.LoadConfigData(info);

            this.Close();
        }

        private void btnNewProject_Click(object sender, RoutedEventArgs e)
        {
            //this.Close();
            ShowNewProjectWindow(sender, e);
        }

        private void btnOpenProject_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
            openFileDialog.Title = "Open Project";
            openFileDialog.Filter = "VLM Project Files|*.lmp";

            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var fileName = Path.GetFileNameWithoutExtension(openFileDialog.FileName);
                var dir = Path.GetDirectoryName(openFileDialog.FileName);
                ProjectInfo projectInfo = new ProjectInfo(fileName, dir);
                RecentProjectInfo.Instance.AddRecentFile(projectInfo);

                RegistryManager.Instance.StoreStr(FindDupKeysLoadKeyStr, SwitchCheckDupKeys.IsOn.ToString(), RegistryManager.Instance.REGISTRY_KEY_STARTS);
                LocalizationDataManager.Instance.LoadConfigData(projectInfo);

                this.Close();
            }
        }

        private async void ShowNewProjectWindow(object sender, RoutedEventArgs e)
        {
            NewProjectWindow newProjectWindow = new NewProjectWindow() { IsModal = true };

            await MainWindow.ShowChildWindowAsync(newProjectWindow);

            switch (newProjectWindow.resultType)
            {
                case CreateProjectResultType.Cancel:
                case CreateProjectResultType.Success:
                    if (LocalizationDataManager.Instance.configData != null)
                    {
                        this.Close();
                    }
                    break;
                case CreateProjectResultType.ExistProjectFile:
                    ShowDialog("해당 경로에 이미 프로젝트 파일이 존재합니다.", string.Empty);
                    break;
            }
        }

        private void ShowDialog(string title, string content)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;

            mainWindow.ShowBasicDialog(title, content);
        }
    }
}
