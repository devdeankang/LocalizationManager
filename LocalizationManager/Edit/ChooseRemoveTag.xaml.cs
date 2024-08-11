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
    /// ChooseRemoveTag.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ChooseRemoveTag : UserControl
    {
        public event RoutedEventHandler ClickCancel;
        public event RoutedEventHandler ClickRemove;
        
        [System.ComponentModel.Browsable(false)]
        public int SelectedIndex { get; set; }

        public ChooseRemoveTag(string selectedKey)
        {
            InitializeComponent();

            if (string.IsNullOrEmpty(selectedKey) == false)
            {
                if (this.SelectedIndex == 0) KeyName.Text = selectedKey; 
                //if(this.SelectedIndex == 1) TagName.Text = selectedKey;
            }
        }

        private void ClickBtnCancel(object sender, RoutedEventArgs e)
        {
            if (ClickCancel != null)
            {
                ClickCancel(sender, e);
            }
        }

        private void ClickBtnRemove(object sender, RoutedEventArgs e)
        {
            if (ClickRemove != null)
            {
                ClickRemove(sender, e);
            }

            ClickCancel(sender, e);
        }
    }
}
