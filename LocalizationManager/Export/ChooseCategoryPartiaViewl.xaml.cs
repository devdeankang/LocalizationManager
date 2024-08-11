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
    /// ChooseCategoryPartial.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ChooseCategoryPartialView : UserControl
    {
        public event RoutedEventHandler ClickCancel;
        public event RoutedEventHandler ClickExport;

        public ChooseCategoryPartialView()
        {
            InitializeComponent();

            SetPartialTreeView();
            SetExportTreeView();
        }

        private void SetPartialTreeView()
        {
            var item = new TreeViewItem();
            CheckBox ProjectCheckBox = CreateCheckBox("Select All", false, PartialCheckBoxType.All);
            item.Header = ProjectCheckBox;
            item.IsExpanded = true;
            PartialTreeView.Items.Add(item);

            foreach (string category in LocalizationDataManager.Instance.configData.Categories)
            {
                CategoryInfo categoryInfo = LocalizationDataManager.Instance.localData.categoryInfos[category];

                var categoryItem = new TreeViewItem();
                CheckBox categoryCheckBox = CreateCheckBox(category, false, PartialCheckBoxType.Category);
                categoryCheckBox.Tag = item;
                categoryItem.Header = categoryCheckBox;
                item.Items.Add(categoryItem);

                if (categoryInfo.partialInfos.Count <= 1) continue;
                foreach (PartialInfo partialInfo in categoryInfo.partialInfos)
                {
                    var partialItem = new TreeViewItem();
                    string partialContent = string.Format("{0} {1}", category, partialInfo._partial);
                    CheckBox partialCheckBox = CreateCheckBox(partialContent, false, PartialCheckBoxType.Partial);
                    partialCheckBox.Tag = categoryItem;
                    partialItem.Header = partialCheckBox;
                    categoryItem.Items.Add(partialItem);
                }
            }
        }

        private void SetExportTreeView()
        {
            PartialExportTreeView.Items.Clear();

            TreeViewItem projectItem = PartialTreeView.Items[0] as TreeViewItem;
            foreach (TreeViewItem categoryItem in projectItem.Items)
            {
                CheckBox categoryCheckBox = categoryItem.Header as CheckBox;
                if (categoryCheckBox.IsChecked == true)
                {
                    TreeViewItem categoryExportitem = new TreeViewItem();
                    categoryExportitem.Header = categoryCheckBox.Content as string;
                    categoryExportitem.FontSize = 14;
                    categoryExportitem.IsExpanded = true;
                    PartialExportTreeView.Items.Add(categoryExportitem);

                    if (categoryItem.Items.Count < 1) continue;

                    foreach (TreeViewItem partialItem in categoryItem.Items)
                    {
                        CheckBox partialCheckBox = partialItem.Header as CheckBox;
                        if (partialCheckBox.IsChecked == true)
                        {
                            TreeViewItem partialExportItem = new TreeViewItem();
                            partialExportItem.Header = partialCheckBox.Content as string;
                            partialExportItem.FontSize = 14;
                            categoryExportitem.Items.Add(partialExportItem);
                        }
                    }
                }
            }

            CheckBtnExportEnable();
        }

        private CheckBox CreateCheckBox(string content, bool isChecked, PartialCheckBoxType partialCheckBoxType = PartialCheckBoxType.None)
        {
            CheckBox checkbox = new CheckBox();
            checkbox.IsChecked = isChecked;
            checkbox.Content = content;
            checkbox.FontSize = 14;
            switch (partialCheckBoxType)
            {
                case PartialCheckBoxType.All:
                    checkbox.Click += PartialCheckbox_ClickAll;
                    break;
                case PartialCheckBoxType.Category:
                    checkbox.Click += PartialCheckbox_ClickCategory;
                    break;
                case PartialCheckBoxType.Partial:
                    checkbox.Click += PartialCheckbox_ClickParatial;
                    break;
                default:
                    break;
            }

            return checkbox;
        }

        private void PartialCheckbox_ClickAll(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;
            bool isChecked = checkBox.IsChecked.Value;

            TreeViewItem projectItem = PartialTreeView.Items[0] as TreeViewItem;
            foreach (TreeViewItem categoryItem in projectItem.Items)
            {
                CheckBox categoryCheckBox = categoryItem.Header as CheckBox;
                categoryCheckBox.IsChecked = isChecked;

                foreach (TreeViewItem partialItem in categoryItem.Items)
                {
                    CheckBox partialCheckBox = partialItem.Header as CheckBox;
                    partialCheckBox.IsChecked = isChecked;
                }
            }

            SetExportTreeView();
        }

        private void PartialCheckbox_ClickCategory(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;
            bool isChecked = checkBox.IsChecked.Value;
            string categoryName = (string)checkBox.Content;

            CategoryInfo categoryInfo = LocalizationDataManager.Instance.localData.categoryInfos[categoryName];

            if (categoryInfo.partialInfos.Count > 1)
            {
                //해당 카테고리 내에 있는 partial checkbox 설정
                TreeViewItem categoryItem = GetCategoryTreeViewItem(categoryName);
                foreach (TreeViewItem partialItem in categoryItem.Items)
                {
                    CheckBox partialCheckBox = partialItem.Header as CheckBox;
                    partialCheckBox.IsChecked = isChecked;
                }
            }

            //모든 checkbox가 체크되어있으면 select all에도 체크
            SetTreeViewItem_ProjectCheckBox();

            SetExportTreeView();
        }

        private void PartialCheckbox_ClickParatial(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;
            TreeViewItem categoryItem = checkBox.Tag as TreeViewItem;
            CheckBox categoryCheckBox = categoryItem.Header as CheckBox;

            //해당 Partial Checkbox중 하나라도 체크되어있으면 Category Checkbox 체크
            bool isCategoryChecked = false;
            foreach (TreeViewItem partialItem in categoryItem.Items)
            {
                CheckBox partialCheckBox = partialItem.Header as CheckBox;
                if (partialCheckBox.IsChecked == true)
                {
                    isCategoryChecked = true;
                    break;
                }
            }

            categoryCheckBox.IsChecked = isCategoryChecked;

            //모든 checkbox가 체크되어있으면 select all에도 체크
            SetTreeViewItem_ProjectCheckBox();

            SetExportTreeView();
        }

        public void SetTreeViewItem_ProjectCheckBox()
        {
            TreeViewItem projectItem = PartialTreeView.Items[0] as TreeViewItem;
            CheckBox projectCheckBox = projectItem.Header as CheckBox;
            projectCheckBox.IsChecked = IsAllTreeViewItemChecked();
        }

        private bool IsAllTreeViewItemChecked()
        {
            TreeViewItem projectItem = PartialTreeView.Items[0] as TreeViewItem;
            foreach (TreeViewItem categoryItem in projectItem.Items)
            {
                CheckBox categoryCheckBox = categoryItem.Header as CheckBox;
                if (categoryCheckBox.IsChecked.Value == false)
                {
                    return false;
                }

                foreach (TreeViewItem partialItem in categoryItem.Items)
                {
                    CheckBox partialCheckBox = partialItem.Header as CheckBox;
                    if (partialCheckBox.IsChecked.Value == false)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private TreeViewItem GetCategoryTreeViewItem(string category)
        {
            TreeViewItem projectItem = PartialTreeView.Items[0] as TreeViewItem;
            foreach (TreeViewItem categoryItem in projectItem.Items)
            {
                CheckBox categoryCheckBox = categoryItem.Header as CheckBox;
                if (categoryCheckBox.Content as string == category)
                {
                    return categoryItem;
                }
            }

            return null;
        }

        private TreeViewItem GetPartialTreeViewItem(string category, int partial)
        {
            TreeViewItem projectItem = PartialTreeView.Items[0] as TreeViewItem;
            foreach (TreeViewItem categoryItem in projectItem.Items)
            {
                CheckBox categoryCheckBox = categoryItem.Header as CheckBox;
                if (categoryCheckBox.Content as string != category) continue;

                foreach (TreeViewItem partialItem in categoryItem.Items)
                {
                    CheckBox partialCheckBox = partialItem.Header as CheckBox;
                    if (partialCheckBox.Content as string == GetPartialFormatName(category, partial))
                    {
                        return partialItem;
                    }
                }
            }

            return null;
        }

        private static string GetPartialFormatName(string cateogry, int partial)
        {
            return string.Format("{0} {1}", cateogry, partial);
        }

        private void ClickBtnCancel(object sender, RoutedEventArgs e)
        {
            if (ClickCancel != null)
            {
                ClickCancel(sender, e);
            }
        }

        private void ClickBtnExport(object sender, RoutedEventArgs e)
        {
            if (ClickExport != null)
            {
                //Export List 저장
                SetPartiallyExport();

                ClickExport(sender, e);
            }
        }

        private void SetPartiallyExport()
        {
            LocalizationDataManager.Instance.localData.ClearIsExportInfo();

            TreeViewItem projectItem = PartialTreeView.Items[0] as TreeViewItem;
            foreach (TreeViewItem categoryItem in projectItem.Items)
            {
                CheckBox categoryCheckBox = categoryItem.Header as CheckBox;
                string categoryName = categoryCheckBox.Content as string;
                if (categoryCheckBox.IsChecked.Value == true)
                {
                    if (categoryItem.Items.Count < 1)
                    {
                        LocalizationDataManager.Instance.localData.SetIsExport(true, categoryName, 0);
                    }
                    else
                    {
                        foreach (TreeViewItem partialItem in categoryItem.Items)
                        {
                            CheckBox partialCheckBox = partialItem.Header as CheckBox;
                            string partialName = partialCheckBox.Content as string;
                            int partial = Int32.Parse(partialName.Split(' ')[1]);
                            if (partialCheckBox.IsChecked.Value == true)
                            {
                                LocalizationDataManager.Instance.localData.SetIsExport(true, categoryName, partial);
                            }
                        }
                    }
                }

                
            }
        }

        //Export 버튼 Enable 설정
        private void CheckBtnExportEnable()
        {
            if (PartialExportTreeView.Items.Count > 0)
            {
                btnExport.IsEnabled = true;
            }
            else
            {
                btnExport.IsEnabled = false;
            }
        }

    }
}
