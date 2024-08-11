using System;
using System.Collections;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LocalizationManager
{
    /// <summary>
    /// ManageConfigView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ManageConfigView : UserControl
    {
        //event
        public event RoutedEventHandler ClickCancel;
        public event RoutedEventHandler ClickApply;
        public event RoutedEventHandler ClickSetConfig;


        ManageConfigType manageType = ManageConfigType.NONE;
        public List<string> manageConfigList;
        public string selectedItemName;
        public bool isNew = false;

        public ManageConfigView(ManageConfigType manageType)
        {
            InitializeComponent();

            this.manageType = manageType;
            SetConfigView();
        }

        private void SetConfigView()
        {
            SetManageTabItemName();
            LoadManageListView();
            SetEditEnabled(false);
        }

        private void SetManageTabItemName()
        {
            switch (manageType)
            {
                case ManageConfigType.MNG_CATEGORY:
                    ManageTabItem.Header = "Category";
                    break;
                case ManageConfigType.MNG_LANGUAGE:
                    ManageTabItem.Header = "Language";
                    break;
                default:
                    break;
            }
        }

        public void LoadManageListView()
        {
            switch (manageType)
            {
                case ManageConfigType.MNG_CATEGORY:
                    manageConfigList = LocalizationDataManager.Instance.configData.Categories;
                    break;
                case ManageConfigType.MNG_LANGUAGE:
                    manageConfigList = LocalizationDataManager.Instance.configData.Languages;
                    break;
                default:
                    break;
            }

            ManageItemView.Items.Clear();
            foreach (var configName in manageConfigList)
            {
                ListViewItem item = new ListViewItem();
                item.Content = configName;
                item.Selected += ManageItemView_Selected;

                ManageItemView.Items.Add(item);
            }
        }

        private void ManageItemView_Selected(object sender, RoutedEventArgs e)
        {
            ListViewItem item = (ListViewItem)sender;
            selectedItemName = item.Content.ToString();

            bool isEdit = true;
            switch (manageType)
            {
                case ManageConfigType.MNG_CATEGORY:
                    if (selectedItemName.Equals("Basic"))
                    {
                        isEdit = false;
                    }
                    break;
                case ManageConfigType.MNG_LANGUAGE:
                    if (selectedItemName.Equals("Korean"))
                    {
                        isEdit = false;
                    }
                    break;
                default:
                    break;
            }

            SetEditEnabled(isEdit);
        }

        //Edit, Delete 버튼 enable 설정
        private void SetEditEnabled(bool isEdit)
        {
            btnEdit.IsEnabled = isEdit;
            btnDelete.IsEnabled = isEdit;
        }

        private void btnClickSetConfig(object sender, RoutedEventArgs e)
        {
            if (ClickSetConfig != null)
            {
                ClickSetConfig(sender, e);
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            IList items = ManageItemView.SelectedItems;
            foreach (ListViewItem item in items)
			{
                string itemName = item.Content.ToString();

                switch (manageType)
                {
                    case ManageConfigType.MNG_CATEGORY:
                        //LocalizationDataManager.Instance.configData.DeleteCategory(selectedItemName);
                        manageConfigList.Remove(itemName);
                        break;
                    case ManageConfigType.MNG_LANGUAGE:
                        //LocalizationDataManager.Instance.configData.DeleteLanguage(selectedItemName);
                        manageConfigList.Remove(itemName);
                        break;
                    default:
                        break;
                }
            }

            LoadManageListView();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (ClickCancel != null)
            {
                ClickCancel(sender, e);
            }
        }

        private void btnApply_Click(object sender, RoutedEventArgs e)
        {
            if (ClickApply != null)
            {
                ApplyConfigData();

                ClickApply(sender, e);
            }
        }

        void ApplyConfigData()
        {
            switch (manageType)
            {
                case ManageConfigType.MNG_CATEGORY:
                    LocalizationDataManager.Instance.configData.Categories = manageConfigList;
                    break;
                case ManageConfigType.MNG_LANGUAGE:
                    LocalizationDataManager.Instance.configData.Languages = manageConfigList;
                    break;
                default:
                    break;
            }

            LocalizationDataManager.Instance.configData.SaveConfigData();
        }

    }
}
