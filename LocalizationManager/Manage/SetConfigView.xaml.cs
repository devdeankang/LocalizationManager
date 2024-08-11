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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LocalizationManager
{
    /// <summary>
    /// SetConfigView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SetConfigView : UserControl
    {
        //event
        public event RoutedEventHandler ClickCancel;
        public event RoutedEventHandler ClickApply;

        ManageConfigType configType = ManageConfigType.NONE;
        bool isNew = false;
        string defaultName = string.Empty;
        //bool isValidateName = true;

        public SetConfigView(ManageConfigType configType, bool isNew, string defaultName = null)
        {
            InitializeComponent();

            this.configType = configType;
            this.isNew = isNew;
            ConfigType.Text = configType.Equals(ManageConfigType.MNG_CATEGORY) ? "Category" : "Language";
            if (!isNew)
            {
                this.defaultName = defaultName == null ? string.Empty : defaultName;
                ConfigName.Text = this.defaultName;
            }
        }

        private bool CheckValidConfigName()
        {
            if (string.IsNullOrEmpty(ConfigName.Text))
            {
                ErrorText.Visibility = Visibility;

                switch (configType)
                {
                    case ManageConfigType.MNG_CATEGORY:
                        ErrorText.Text = "Empty Category Name";
                        break;
                    case ManageConfigType.MNG_LANGUAGE:
                        ErrorText.Text = "Empty Language";
                        break;
                }

                return false;
            }

            switch (configType)
            {
                case ManageConfigType.MNG_CATEGORY:
                    if (!defaultName.Equals(ConfigName.Text) && LocalizationDataManager.Instance.configData.isExistCategoryName(ConfigName.Text))
                    {
                        ErrorText.Visibility = Visibility;
                        ErrorText.Text = "Duplicate Category Name";
                        return false;
                    }
                    break;
                case ManageConfigType.MNG_LANGUAGE:
                    if (!defaultName.Equals(ConfigName.Text) && LocalizationDataManager.Instance.configData.isExistLanguageName(ConfigName.Text))
                    {
                        ErrorText.Visibility = Visibility;
                        ErrorText.Text = "Duplicate Language Name";
                        return false;
                    }
                    break;
            }

            return true;
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
            //유효한 이름인지 체크
            if (CheckValidConfigName() == false)
                return;

            if (ClickApply != null)
            {
                Button btn = (Button)sender;
                btn.Tag = ConfigName.Text;

                ClickApply(sender, e);
            }
        }
    }
}
