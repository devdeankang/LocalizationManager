using System;
using System.Collections.Generic;
using System.Data;
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
using MahApps.Metro.Controls;

namespace LocalizationManager
{
    /// <summary>
    /// FindDupKeyWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class FindDupKeyWindow : MetroWindow
    {
        private string[] dupKeyCol = { "KEY", "Korean", "Tag", "Status", "Desc", "Category", "Partial" };
        public bool isOpened = false;

        public FindDupKeyWindow()
        {
            InitializeComponent();
        }

        public void  SetDataGrid()
        {
            DataTable dataTable = new DataTable();

            //Column 생성
            foreach (string field in dupKeyCol)
            {
                dataTable.Columns.Add(field.ToLower());
            }

            //Row 생성
            foreach (var dupList in LocalizationDataManager.Instance.localData.dupKeyDic)
            {
                foreach (var dupLine in dupList.Value)
                {
                    // DataTable 데이터 생성
                    dataTable.Rows.Add(new string[] { dupList.Key, dupLine.sourceText, dupLine.tag, dupLine.status, dupLine.desc, dupLine.category, dupLine.partial.ToString() });
                }
            }
            
            // dataTable 바인딩
            DataGrid.ItemsSource = dataTable.DefaultView;

            isOpened = true;
        }

        private void DataGridLoaded(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < DataGrid.Columns.Count; i++)
            {
                if (DataGrid.Columns[i].Header.Equals("key"))
                {
                    DataGrid.Columns[i].Width = 280;
                }

                var col = DataGrid.Columns[i] as DataGridTextColumn;

                col.ElementStyle = GetWrapTextBolckStyle();
            }
        }

        //return Wrap style
        private Style GetWrapTextBolckStyle()
        {
            var style = new Style(typeof(TextBlock));
            style.Setters.Add(new Setter(TextBlock.TextWrappingProperty, TextWrapping.Wrap));
            style.Setters.Add(new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center));

            return style;
        }

        private void FindDupKeyWindow_Closed(object sender, EventArgs e)
        {
            isOpened = false;
        }
    }
}
