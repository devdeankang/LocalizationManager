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
using MahApps.Metro.SimpleChildWindow;

namespace LocalizationManager
{
    public enum ManageConfigType
    {
        NONE,
        MNG_CATEGORY,
        MNG_LANGUAGE
    }

    public enum ManageViewType
    {
        NONE,
        MVT_MAIN,
        MVT_SET
    }

    /// <summary>
    /// ManageConfigWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    /// 
    public partial class ManageConfigWindow : ChildWindow
    {
        public ManageViewType manageViewType = ManageViewType.NONE;
        public ManageConfigType configType = ManageConfigType.NONE;

        public ManageConfigView manageConfigView;
        public SetConfigView setConfigView;

        public bool isApply = false;

        public ManageConfigWindow(string manageTypeStr)
        {
            InitializeComponent();

            //ManageConfigType Set
            switch (manageTypeStr)
            {
                case "Category":
                    configType = ManageConfigType.MNG_CATEGORY;
                    break;
                case "Language":
                    configType = ManageConfigType.MNG_LANGUAGE;
                    break;
            }

            InitManageConfigView();
            manageViewType = ManageViewType.MVT_MAIN;
            ManageGrid.Children.Add(manageConfigView);
            SetManageWindowMainTitle();
        }

        private void SetManageWindowMainTitle()
        {
            switch (configType)
            {
                case ManageConfigType.MNG_CATEGORY:
                    this.Title = "Manage Category";
                    break;
                case ManageConfigType.MNG_LANGUAGE:
                    this.Title = "Manage Language";
                    break;
                default:
                    break;
            }
        }

        private void SetManageWindowSetTitle(string setType)
        {
            switch (configType)
            {
                case ManageConfigType.MNG_CATEGORY:
                    this.Title = string.Format("{0} Category", setType);
                    break;
                case ManageConfigType.MNG_LANGUAGE:
                    this.Title = string.Format("{0} Language", setType);
                    break;
                default:
                    break;
            }
        }

        public void InitManageConfigView()
        {
            manageConfigView = new ManageConfigView(configType);
            
            manageConfigView.ClickApply += ClickBtnApply;
            manageConfigView.ClickCancel += ClickBtnCancel;
            manageConfigView.ClickSetConfig += ClickBtnSelect;
        }

        public void InitSetConfigView(bool isNew, string defaultName = null)
        {
            setConfigView = new SetConfigView(configType, isNew, defaultName);

            setConfigView.ClickApply += ClickBtnApply;
            setConfigView.ClickCancel += ClickBtnCancel;
        }

        private void ClickBtnSelect(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            string btnName = btn.Content.ToString();

            SetManageWindowSetTitle(btnName);
            var isNew = btnName.Equals("New") ? true : false;
            manageConfigView.isNew = isNew;
            InitSetConfigView(isNew, manageConfigView.selectedItemName);
            ManageGrid.Children.Clear();
            ManageGrid.Children.Add(setConfigView);
            manageViewType = ManageViewType.MVT_SET;
        }

        public void ApplySetConfig(string newConfigName)
        {
            var isNew = manageConfigView.isNew;
            var originalName = manageConfigView.selectedItemName;

            if (isNew)
            {
                manageConfigView.manageConfigList.Add(newConfigName);
                manageConfigView.manageConfigList.Sort();

                switch (configType)
                {
                    case ManageConfigType.MNG_CATEGORY:
                        manageConfigView.manageConfigList.Remove("Basic");
                        manageConfigView.manageConfigList.Insert(0, "Basic");
                        break;
                    case ManageConfigType.MNG_LANGUAGE:
                        manageConfigView.manageConfigList.Remove("Korean");
                        manageConfigView.manageConfigList.Insert(0, "Korean");
                        break;
                }
            }
            else
            {
                for (int i = 0; i < manageConfigView.manageConfigList.Count; i++)
                {
                    if (manageConfigView.manageConfigList[i] == originalName)
                    {
                        manageConfigView.manageConfigList[i] = newConfigName;
                    }
                }
            }
        }


        private void ClickBtnCancel(object sender, RoutedEventArgs e)
        {
            switch (manageViewType)
            {
                case ManageViewType.MVT_MAIN:
                    this.Close();
                    break;
                case ManageViewType.MVT_SET:
                    ManageGrid.Children.Clear();
                    ManageGrid.Children.Add(manageConfigView);
                    manageViewType = ManageViewType.MVT_MAIN;
                    break;
            }
        }

        private void ClickBtnApply(object sender, RoutedEventArgs e)
        {
            switch (manageViewType)
            {
                case ManageViewType.MVT_MAIN:
                    isApply = true;
                    this.Close();
                    break;
                case ManageViewType.MVT_SET:
                    ManageGrid.Children.Clear();
                    ManageGrid.Children.Add(manageConfigView);

                    var btn = (Button)sender;
                    string newConfigName = btn.Tag.ToString();
                    ApplySetConfig(newConfigName);
                    
                    manageConfigView.LoadManageListView();
                    manageViewType = ManageViewType.MVT_MAIN;
                    break;
            }
        }
    }
}
