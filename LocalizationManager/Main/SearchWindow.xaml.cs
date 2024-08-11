using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MahApps.Metro.Controls;

namespace LocalizationManager
{
    /// <summary>
    /// SearchWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SearchWindow : MetroWindow
    {
        public const string SELECTED_ALL_STR = "Selected All";
        public bool isOpened = false;
        public DataTable dataTable;

        public Dictionary<string, CheckBox> categoryBox = new Dictionary<string, CheckBox>();
        public Dictionary<string, CheckBox> languageBox = new Dictionary<string, CheckBox>();

        public SearchWindow()
        {
            InitializeComponent();

            isOpened = true;

            InitializeDataTable();
            InitCategoryLanguage();
        }

        private void InitCategoryLanguage()
        {
            CategoryCombobox.Items.Clear();
            LanguageCombobox.Items.Clear();
            categoryBox.Clear();
            languageBox.Clear();

            CheckBox categorySelectBox = new CheckBox();
            categorySelectBox.IsChecked = true;
            categorySelectBox.Content = SELECTED_ALL_STR;
            categorySelectBox.Click += CategorySelectBox_Click;
            CategoryCombobox.Items.Add(categorySelectBox);
            categoryBox.Add(SELECTED_ALL_STR, categorySelectBox);

            foreach (string category in LocalizationDataManager.Instance.configData.Categories)
            {
                CheckBox checkBox = new CheckBox();
                checkBox.IsChecked = true;
                checkBox.Content = category;
                checkBox.Click += CategoryCheckbox_CheckChanged;
                CategoryCombobox.Items.Add(checkBox);
                categoryBox.Add(category, checkBox);
            }

            CheckBox languageSelectBox = new CheckBox();
            languageSelectBox.IsChecked = true;
            languageSelectBox.Content = SELECTED_ALL_STR;
            languageSelectBox.Click += LanguageSelectBox_Click;
            LanguageCombobox.Items.Add(languageSelectBox);
            languageBox.Add(SELECTED_ALL_STR, languageSelectBox);

            foreach (string language in LocalizationDataManager.Instance.configData.Languages)
            {
                CheckBox checkBox = new CheckBox();
                checkBox.IsChecked = true;
                checkBox.Content = language;
                checkBox.Click += LanguageCheckbox_CheckChanged;
                LanguageCombobox.Items.Add(checkBox);
                languageBox.Add(language, checkBox);
            }
        }

        private void CategorySelectBox_Click(object sender, RoutedEventArgs e)
        {
            bool isChecked = categoryBox[SELECTED_ALL_STR].IsChecked.Value;
            foreach (var boxPair in categoryBox)
            {
                boxPair.Value.IsChecked = isChecked;
            }
        }

        private void LanguageSelectBox_Click(object sender, RoutedEventArgs e)
        {
            bool isChecked = languageBox[SELECTED_ALL_STR].IsChecked.Value;
            foreach (var boxPair in languageBox)
            {
                boxPair.Value.IsChecked = isChecked;
            }
        }

        public void InitializeDataTable()
        {
            dataTable = new DataTable();

            //Cateogory, Partial, Language, Key, Korean, Translation, Tag, Status, Desc
            dataTable.Columns.Add("Category");
            dataTable.Columns.Add("Partial");
            dataTable.Columns.Add("Key");
            foreach (string language in LocalizationDataManager.Instance.configData.Languages)
            {
                dataTable.Columns.Add(language);
            }
            dataTable.Columns.Add("Tag");
            dataTable.Columns.Add("Status");
            dataTable.Columns.Add("Desc");

            searchDataGrid.ItemsSource = dataTable.DefaultView;
        }

        private void SearchWindow_Closed(object sender, EventArgs e)
        {
            isOpened = false;
        }

        private void searchBtn_Click(object sender, RoutedEventArgs e)
        {
            searchBtn_Click();
        }

        private void searchBtn_Click()
        {
            dataTable.Rows.Clear();

            string searchText = searchBox.Text;
            bool isKey = searchKey.IsChecked.Value;
            bool isValue = searchValue.IsChecked.Value;
            bool isMatchCase = matchCase.IsChecked.Value;
            bool isMatchWholeWord = matchWord.IsChecked.Value;

            if (string.IsNullOrEmpty(searchText)) return;

            var localData = LocalizationDataManager.Instance.localData;

            foreach (var categoryPair in localData.categoryInfos)
            {
                if (categoryBox[categoryPair.Key].IsChecked.Value == false) continue;

                foreach (PartialInfo partialInfo in categoryPair.Value.partialInfos)
                {
                    //Korean key, value부터 확인
                    var koreanfileInfo = partialInfo.languageInfos["Korean"].fileInfo;
                    foreach (var koreanFileIinePair in koreanfileInfo)
                    {
                        var krLine = koreanFileIinePair.Value;
                        var key = krLine.key;
                        var value = krLine.sourceText;
                        if (isKey)
                        {
                            if (GetIsMatchText(key, searchText, isMatchCase, isMatchWholeWord) == true)
                            {
                                //Cateogory, Partial, Key, Korean, Translation, Tag, Status, Desc
                                dataTable.Rows.Add(GetDataTableRow(categoryPair.Key, partialInfo._partial, krLine, partialInfo, koreanFileIinePair));

                                continue;
                            }
                        }

                        if (isValue)
                        {
                            if (languageBox["Korean"].IsChecked.Value)
                            {
                                if (GetIsMatchText(value, searchText, isMatchCase, isMatchWholeWord) == true)
                                {
                                    //Cateogory, Partial, Key, Korean, Translation, Tag, Status, Desc
                                    dataTable.Rows.Add(GetDataTableRow(categoryPair.Key, partialInfo._partial, krLine, partialInfo, koreanFileIinePair));

                                    continue;
                                }
                            }


                            //Korean 제외 다른 언어 value(Translation 검사)
                            foreach (var languagePair in partialInfo.languageInfos)
                            {
                                if (languageBox[languagePair.Key].IsChecked.Value) continue;
                                if (languagePair.Key.Equals("Korean") || languagePair.Value.fileInfo.ContainsKey(koreanFileIinePair.Key) == false) continue;

                                var transLine = languagePair.Value.fileInfo[koreanFileIinePair.Key];
                                var transValue = transLine.translation;

                                if (GetIsMatchText(transValue, searchText, isMatchCase, isMatchWholeWord) == true)
                                {
                                    //Cateogory, Partial, Key, Korean, Translation, Tag, Status, Desc
                                    dataTable.Rows.Add(GetDataTableRow(categoryPair.Key, partialInfo._partial, krLine, partialInfo, koreanFileIinePair));

                                    break;
                                }
                            }
                        }

                    }
                }

            }
        }

        public bool GetIsMatchText(string value, string text, bool isMatchCase, bool isMatchWholeWord)
        {
            var matchCaseComparison = isMatchCase ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase;
            var matchCaseRegex = isMatchCase ? RegexOptions.None : RegexOptions.IgnoreCase;

            if (!isMatchWholeWord)
            {
                if (value.IndexOf(text, matchCaseComparison) >= 0)
                {
                    return true;
                }
            }
            else
            {

                if (Regex.IsMatch(value, string.Format("\\b{0}\\b", text), matchCaseRegex))
                {
                    return true;
                }
            }

            return false;
        }

        public string[] GetDataTableRow(string category, int partial, FileLine krLine, PartialInfo partialInfo, KeyValuePair<string, FileLine> koreanFileIinePair)
        {
            // DataTable 데이터 생성
            List<string> lineList = new List<string>();
            lineList.Add(category);
            lineList.Add(partial.ToString());
            lineList.Add(krLine.key);
            lineList.Add(krLine.sourceText);
            foreach (string language in LocalizationDataManager.Instance.configData.Languages)
            {
                if (language.Equals("Korean")) continue;

                var transfileInfo = partialInfo.languageInfos[language].fileInfo;

                //여기도 중복key일 때 korean 비교해서 add
                if (transfileInfo.ContainsKey(koreanFileIinePair.Key) && transfileInfo[koreanFileIinePair.Key].sourceText.Equals(krLine.sourceText))
                {
                    lineList.Add(transfileInfo[koreanFileIinePair.Key].translation);
                }
                else
                {
                    lineList.Add(string.Empty);
                }
            }
            lineList.Add(krLine.tag);
            lineList.Add(krLine.status);
            lineList.Add(krLine.desc);

            return lineList.ToArray();
        }

        private void DataGridLoaded(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < searchDataGrid.Columns.Count; i++)
            {
                switch (searchDataGrid.Columns[i].Header.ToString())
                {
                    case "Category":
                        searchDataGrid.Columns[i].Width = 130;
                        break;
                    case "Partial":
                        searchDataGrid.Columns[i].Width = 72;
                        break;
                    case "Key":
                        searchDataGrid.Columns[i].Width = 280;
                        break;
                    default:
                        break;
                }

                var col = searchDataGrid.Columns[i] as DataGridTextColumn;

                col.ElementStyle = getWrapTextBolckStyle();
            }
        }

        //return Wrap style
        private Style getWrapTextBolckStyle()
        {
            var style = new Style(typeof(TextBlock));
            style.Setters.Add(new Setter(TextBlock.TextWrappingProperty, TextWrapping.Wrap));
            style.Setters.Add(new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center));

            return style;
        }

        private void CategoryCheckbox_CheckChanged(object sender, RoutedEventArgs e)
        {
            bool isAllChecked = true;
            foreach (var boxPair in categoryBox)
            {
                if (boxPair.Key == SELECTED_ALL_STR) continue;
                
                if (boxPair.Value.IsChecked == false)
                {
                    isAllChecked = false;
                    break;
                }
            }

            categoryBox[SELECTED_ALL_STR].IsChecked = isAllChecked;
        }

        private void LanguageCheckbox_CheckChanged(object sender, RoutedEventArgs e)
        {
            bool isAllChecked = true;
            foreach (var boxPair in languageBox)
            {
                if (boxPair.Key == SELECTED_ALL_STR) continue;

                if (boxPair.Value.IsChecked == false)
                {
                    isAllChecked = false;
                    break;
                }
            }

            languageBox[SELECTED_ALL_STR].IsChecked = isAllChecked;
        }

        private void CategoryCombobox_DropDownClosed(object sender, EventArgs e)
        {
            CategoryCombobox.Text = "Category";
        }

        private void LanguageCombobox_DropDownClosed(object sender, EventArgs e)
        {
            LanguageCombobox.Text = "Language";
        }

        private void searchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key.Equals(Key.Enter))
            {
                searchBtn_Click();
            }
        }
    }

}
