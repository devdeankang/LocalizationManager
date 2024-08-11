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
    /// SummaryView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SummaryView : UserControl
    {
        public SummaryView()
        {
            InitializeComponent();
        }

        public void SetSummaryView(string fileName, CategoryInfo categoryInfo)
        {
            SummaryTabItem.Header = string.Format("{0} (Summary)", fileName);
            KeysNum.Text = categoryInfo.totalKeys.ToString();

            //Duplicated key
            DupKeysNum.Text = GetDupKeyNum(categoryInfo).ToString();

            var tagItem = GetmainTreeViewItem("Tags", true);

            var tagKeysDic = categoryInfo.tagKeysDic.OrderBy(pair => pair.Key);
            foreach (KeyValuePair<string, Dictionary<string, FileLine>> tagKeys in tagKeysDic)
            {
               var tagSubItem = GetCustomTreeViewItem(tagKeys.Key, tagKeys.Value.Count.ToString(), false);

                var tagStatusDic = GetTagTranslationStatusInfo(tagKeys.Value);

                foreach (KeyValuePair<string, int> pair in tagStatusDic)
                {
                    var tagSubItem2 = GetCustomTreeViewItem(pair.Key, pair.Value.ToString(), false);

                    tagSubItem.Items.Add(tagSubItem2);
                }

                tagItem.Items.Add(tagSubItem);
            }

            TagTreeView.Items.Add(tagItem);


            //DupKeys랑 Translantion Status 추후 작업
            var statusItem = GetmainTreeViewItem("Translation Status", true);

            foreach (KeyValuePair<string, Dictionary<string, FileLine>> tagKeys in categoryInfo.translationStatusDic)
            {
                var statusSubItem = GetCustomTreeViewItem(tagKeys.Key, tagKeys.Value.Count.ToString(), false);

                statusItem.Items.Add(statusSubItem);
            }

            StatusTreeView.Items.Add(statusItem);
        }

        public void SetSummaryView(string projectName)
        {
            Dictionary<string, CategoryInfo> cateogoryInfos = LocalizationDataManager.Instance.localData.categoryInfos;

            SummaryTabItem.Header = string.Format("{0} (Summary)", projectName);


            //전체 Key 개수
            int totalKeyNum = 0;
            foreach (CategoryInfo categoryInfo in cateogoryInfos.Values)
            {
                totalKeyNum += categoryInfo.totalKeys;
            }
            KeysNum.Text = totalKeyNum.ToString();

            //Duplicated key
            int totalDupKeyNum = 0;
            foreach (CategoryInfo categoryInfo in cateogoryInfos.Values)
            {
                totalDupKeyNum += GetDupKeyNum(categoryInfo);
            }

            DupKeysNum.Text = totalDupKeyNum.ToString();

            var tagItem = GetmainTreeViewItem("Tags", true);

            //전체 local데이터에서 tag별 숫자
            Dictionary<string, int> totalTagNumDic = new Dictionary<string, int>();
            //전체 로컬데이터에서 태그별 translation status
            Dictionary<string, Dictionary<string, int>> totalTagStatusDic = new Dictionary<string, Dictionary<string, int>>();

            foreach (CategoryInfo categoryInfo in cateogoryInfos.Values)
            {
               foreach (KeyValuePair<string, Dictionary<string, FileLine>> tagKeys in categoryInfo.tagKeysDic)
                {
                    //var tagSubItem = GetCustomTreeViewItem(tagKeys.Key, tagKeys.Value.Count.ToString(), false);
                    //tag별 숫자 계산
                    if (totalTagNumDic.ContainsKey(tagKeys.Key) == false)
                    {
                        totalTagNumDic[tagKeys.Key] = tagKeys.Value.Count;
                    }
                    else
                    {
                        totalTagNumDic[tagKeys.Key] += tagKeys.Value.Count;
                    }

                    var tagStatusDic = GetTagTranslationStatusInfo(tagKeys.Value);
                    if (totalTagStatusDic.ContainsKey(tagKeys.Key) == false)
                    {
                        totalTagStatusDic[tagKeys.Key] = tagStatusDic;
                    }
                    else
                    {
                        foreach (KeyValuePair<string, int> pair in tagStatusDic)
                        {
                            totalTagStatusDic[tagKeys.Key][pair.Key] += pair.Value;
                        }
                    }
                }
            }

            //구해낸 Tag별 totalNum, translation Status tree에 추가
            foreach (var tagPair in totalTagStatusDic.OrderBy(pair => pair.Key))
            {
                var tagSubItem = GetCustomTreeViewItem(tagPair.Key, totalTagNumDic[tagPair.Key].ToString(), false);

                foreach (KeyValuePair<string, int> tagStatusPair in tagPair.Value)
                {
                    var tagSubItem2 = GetCustomTreeViewItem(tagStatusPair.Key, tagStatusPair.Value.ToString(), false);

                    tagSubItem.Items.Add(tagSubItem2);
                }

                tagItem.Items.Add(tagSubItem);
            }

            TagTreeView.Items.Add(tagItem);

            //Translation Status
            var statusItem = GetmainTreeViewItem("Translation Status", true);
            Dictionary<string, int> totalTranslationStatusDic = new Dictionary<string, int>();
            foreach (CategoryInfo categoryInfo in cateogoryInfos.Values)
            {
                foreach (KeyValuePair<string, Dictionary<string, FileLine>> translationStatusPair in categoryInfo.translationStatusDic)
                {
                    if (totalTranslationStatusDic.ContainsKey(translationStatusPair.Key) == false)
                    {
                        totalTranslationStatusDic[translationStatusPair.Key] = translationStatusPair.Value.Count;
                    }
                    else
                    {
                        totalTranslationStatusDic[translationStatusPair.Key] += translationStatusPair.Value.Count;
                    }
                }
            }

            foreach (KeyValuePair<string, int> translationStatusNumPair in totalTranslationStatusDic)
            {
                var statusSubItem = GetCustomTreeViewItem(translationStatusNumPair.Key, translationStatusNumPair.Value.ToString(), false);

                statusItem.Items.Add(statusSubItem);
            }

            StatusTreeView.Items.Add(statusItem);
        }

        private Dictionary<string, int> GetTagTranslationStatusInfo(Dictionary<string, FileLine> tagInfo)
        {
            Dictionary<string, int> dic = new Dictionary<string, int>();

            dic["(None)"] = 0;
            dic[LocalizationDataManager.ATS_ADD] = 0;
            dic[LocalizationDataManager.ATS_UPDATE] = 0;
            dic[LocalizationDataManager.ATS_PT] = 0;
            dic[LocalizationDataManager.ATS_TRANSLATED] = 0;

            foreach (KeyValuePair<string, FileLine> line in tagInfo)
            {
                switch (line.Value.translationStatus)
                {
                    case LocalizationDataManager.ATS_ADD:
                        dic[LocalizationDataManager.ATS_ADD]++;
                        break;
                    case LocalizationDataManager.ATS_UPDATE:
                        dic[LocalizationDataManager.ATS_UPDATE]++;
                        break;
                    case LocalizationDataManager.ATS_PT:
                        dic[LocalizationDataManager.ATS_PT]++;
                        break;
                    case LocalizationDataManager.ATS_TRANSLATED:
                        dic[LocalizationDataManager.ATS_TRANSLATED]++;
                        break;
                    default:
                        dic["(None)"]++;
                        break;
                }
            }

            return dic;
        }

        private TreeViewItem GetmainTreeViewItem(string text, bool isExpand)
        {
            TreeViewItem item = new TreeViewItem();

            item.IsExpanded = isExpand;

            // create stack panel
            StackPanel stack = new StackPanel();
            stack.Orientation = Orientation.Horizontal;

            //mainBlock
            TextBlock mainBlock = new TextBlock();
            mainBlock.Width = 170;
            mainBlock.TextWrapping = TextWrapping.Wrap;
            mainBlock.FontWeight = FontWeights.Bold;
            mainBlock.FontSize = 16;
            mainBlock.Text = text;

            // Add into stack
            stack.Children.Add(mainBlock);

            // assign stack to header
            item.Header = stack;
            return item;
        }

        private TreeViewItem GetCustomTreeViewItem(string mainText, string subText, bool isExpand)
        {
            TreeViewItem item = new TreeViewItem();

            item.IsExpanded = isExpand;

            // create stack panel
            StackPanel stack = new StackPanel();
            stack.Orientation = Orientation.Horizontal;

            //mainBlock
            TextBlock mainBlock = new TextBlock();
            mainBlock.Width = 170;
            mainBlock.TextWrapping = TextWrapping.Wrap;
            mainBlock.FontWeight = FontWeights.Bold;
            mainBlock.FontSize = 14;
            mainBlock.Text = mainText;

            //subBlock
            TextBlock subBlock = new TextBlock();
            subBlock.TextWrapping = TextWrapping.Wrap;
            subBlock.FontSize = 13;
            subBlock.Text = subText;


            // Add into stack
            stack.Children.Add(mainBlock);
            stack.Children.Add(subBlock);

            // assign stack to header
            item.Header = stack;
            return item;
        }

        private int GetDupKeyNum(CategoryInfo categoryInfo)
        {
            int dupKey = 0;
            var category = categoryInfo._category;
            var dupKeyDic = LocalizationDataManager.Instance.localData.dupKeyDic;
            foreach (var dupkeyPair in dupKeyDic)
            {
                var dupkeyList = dupkeyPair.Value;
                foreach (var dupLine in dupkeyList)
                {
                    if (dupLine.category.Equals(category))
                    {
                        dupKey++;
                    }
                }
            }

            return dupKey;
        }

    }
}
