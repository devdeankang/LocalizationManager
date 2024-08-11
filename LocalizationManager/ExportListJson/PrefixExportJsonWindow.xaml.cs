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
    /// 

    public class PrefixTemplate
    {
        public string prefixStr { get; set; }
        public string regexStr { get; set; }

        public PrefixTemplate(string prefixStr, string regexStr)
        {
            this.prefixStr = prefixStr;
            this.regexStr = regexStr;
        }
    }

    public partial class PrefixExportJsonWindow : ChildWindow
    {
        public const string PREFIX_STR = "prefix";
        public const string REGEX_STR = "regex";

        public bool isCanceled = false;
        public List<PrefixTemplate> prefixExportTemplateList;

        public PrefixExportJsonWindow()
        {
            InitializeComponent();

            isCanceled = false;
            prefixExportTemplateList = new List<PrefixTemplate>();

            //최근 기록이 있으면 기록 불러와서 저장
            List<string> registryPrefixTemplateList = RegistryManager.Instance.LoadMultiStr(PREFIX_STR);
            if (registryPrefixTemplateList != null && registryPrefixTemplateList.Count > 0)
            {
                foreach (string template in registryPrefixTemplateList)
                {
                    string[] tempArr = template.Split(',');
                    AddNewPrefixTemplate(true, tempArr[0], tempArr[1]);
                }
            }
            else
            {
                //없으면 default 하나만 보여준다
                AddNewPrefixTemplate();
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            isCanceled = true;
            this.Close();
        }

        private void btnApply_Click(object sender, RoutedEventArgs e)
        {
            prefixExportTemplateList.Clear();
            List<string> registryStrList = new List<string>();

            for (int i = 0; i < PrefixList.Children.Count; i++)
            {
                WrapPanel panel = PrefixList.Children[i] as WrapPanel;
                string prefixStr = string.Empty;
                string regexStr = string.Empty;

                for (int j = 0; j < panel.Children.Count; j++)
                {
                    if (panel.Children[j].GetType() != typeof(TextBox)) continue;

                    TextBox box = panel.Children[j] as TextBox;

                    if (box.Tag.ToString() == PREFIX_STR)
                    {
                        if (string.IsNullOrEmpty(box.Text) == true)
                        {
                            return;
                        }

                        prefixStr = box.Text;
                    }
                    if (box.Tag.ToString() == REGEX_STR)
                    {
                        regexStr = box.Text;
                    }
                }

                prefixExportTemplateList.Add(new PrefixTemplate(prefixStr, regexStr));
                registryStrList.Add(string.Format("{0},{1}", prefixStr, regexStr));
            }

            RegistryManager.Instance.StoreMultiStr(PREFIX_STR, registryStrList.ToArray());

            this.Close();
        }

        private void AddInput_Click(object sender, RoutedEventArgs e)
        {
            AddNewPrefixTemplate();
        }

        public void DeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            FrameworkElement btn = (FrameworkElement)sender;
            WrapPanel deleteWrapPanel = btn.Tag as WrapPanel;
            PrefixList.Children.Remove(deleteWrapPanel);
        }

        private void AddNewPrefixTemplate(bool enableDeleteBtn = true, string prefix = null, string regex = null)
        {
            PrefixList.Children.Add(CreateNewPrefixTemplate(enableDeleteBtn, prefix, regex));
        }

        private WrapPanel CreateNewPrefixTemplate(bool enableDeleteBtn = true, string prefix = null, string regex = null)
        {
            WrapPanel wrapPanel = new WrapPanel();
            wrapPanel.Orientation = Orientation.Horizontal;
            wrapPanel.Margin = new Thickness(0, 5, 0, 0);

            wrapPanel.Children.Add(CreateNewTextBox(PREFIX_STR, 220, prefix));
            wrapPanel.Children.Add(CreateNewTextBox(REGEX_STR, 640, regex));
            if (enableDeleteBtn)
            {
                Button deleteBtn = CreateDeleteBtn();
                deleteBtn.Tag = wrapPanel;
                deleteBtn.Click += DeleteBtn_Click;
                wrapPanel.Children.Add(deleteBtn);
            }

            return wrapPanel;
        }

        private TextBox CreateNewTextBox(string tag, int width, string text = null)
        {
            TextBox textBox = new TextBox();
            textBox.Text = text;
            textBox.Tag = tag;
            textBox.Width = width;
            textBox.Margin = new Thickness(10, 0, 0, 0);
            return textBox;
        }

        private Button CreateDeleteBtn()
        {
            Button btn = new Button();
            btn.Content = "X";
            btn.FontWeight = FontWeights.UltraBold;
            btn.Margin = new Thickness(20, 0, 0, 0);
            return btn;
        }
    }
}
