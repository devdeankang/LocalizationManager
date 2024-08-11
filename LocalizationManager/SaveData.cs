using CsvHelper;
using NPOI.XSSF.UserModel;
using NPOI.SS.UserModel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalizationManager
{
    public class SaveData
    {
        private static readonly char[] quoteChars = new char[] { '\r', '\n' };

        public static bool ShouldQuote(string field, WritingContext context)
        {
            var shouldQuote = !string.IsNullOrEmpty(field) &&
            (
                field.Contains(context.WriterConfiguration.QuoteString) // Contains quote
                || field.IndexOfAny(quoteChars) > -1 // Contains chars that require quotes
                || (context.WriterConfiguration.Delimiter.Length > 0 && field.Contains(context.WriterConfiguration.Delimiter)) // Contains delimiter
            );

            return shouldQuote;
        }

        //Regular 필드
        //KEY, Korean, (Translation), Tag, Status, Desc
        //Category, Partial, Language 별 하나의 파일
        //Extra 필드
        //KEY, Korean, (Translation), Tag, Status, Desc, Category, Partial
        //각 Language 별 하나의 파일

        public static void SaveRegularCSV(string filePath, string language, List<FileLine> fileLineList)
        {
            using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                FileLineMap fileLineMap = new FileLineMap(language);
                csv.Configuration.RegisterClassMap(fileLineMap);
                csv.Configuration.ShouldQuote = ShouldQuote;

                var records = fileLineList.ToList();
                csv.WriteRecords(records);
            }
        }

        public static ExportZipContent ZipRegularCSV(string fileName, string language, List<FileLine> fileLineList)
        {
            ExportZipContent zipContent;

            using (var inputMemoryStream = new MemoryStream())
            {
                using (var writer = new StreamWriter(inputMemoryStream, Encoding.UTF8))
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    FileLineMap fileLineMap = new FileLineMap(language);
                    csv.Configuration.RegisterClassMap(fileLineMap);
                    csv.Configuration.ShouldQuote = ShouldQuote;

                    var records = fileLineList.ToList();
                    csv.WriteRecords(records);
                }

                zipContent = new ExportZipContent(fileName, inputMemoryStream.ToArray());
            }

            return zipContent;
        }

        private static XSSFWorkbook GetFileLineWorkbook(string sheetName, string language, List<FileLine> fileLIneList)
        {
            XSSFWorkbook workBook = new XSSFWorkbook();

            var sheet = workBook.GetSheet(sheetName);
            if (sheet == null)
            {
                sheet = workBook.CreateSheet(sheetName);
            }

            ICellStyle colStyle = workBook.CreateCellStyle();
            colStyle.VerticalAlignment = VerticalAlignment.Center;
            colStyle.WrapText = true;

            int rowIndex = 0;

            var firstRow = sheet.GetRow(rowIndex);
            if (firstRow == null)
            {
                firstRow = sheet.CreateRow(rowIndex);
            }
            int firstColIndex = 0;
            foreach (string fieldName in LineInfo.saveRegularFileLine)
            {
                ICell cell;
                //"KEY", "Korean", "Translation", "Tag", "Status", "Description"
                if (fieldName == "Translation")
                {
                    if (language == "Korean") continue;

                    cell = firstRow.CreateCell(firstColIndex);
                    cell.SetCellType(CellType.String);
                    cell.SetCellValue(language);
                }
                else
                {
                    cell = firstRow.CreateCell(firstColIndex);
                    cell.SetCellType(CellType.String);
                    cell.SetCellValue(fieldName);
                }

                switch (fieldName)
                {
                    case "KEY":
                        sheet.SetColumnWidth(firstColIndex, 61 * 256);
                        break;
                    case "Korean":
                        sheet.SetColumnWidth(firstColIndex, 43 * 256);
                        break;
                    case "Translation":
                        sheet.SetColumnWidth(firstColIndex, 43 * 256);
                        break;
                    case "Tag":
                        sheet.SetColumnWidth(firstColIndex, 17 * 256);
                        break;
                    case "Status":
                        sheet.SetColumnWidth(firstColIndex, 17 * 256);
                        break;
                    case "Description":
                        sheet.SetColumnWidth(firstColIndex, 25 * 256);
                        break;
                }
                
                sheet.SetDefaultColumnStyle(firstColIndex, colStyle);

                firstColIndex++;
            }
            rowIndex++;

            foreach (FileLine fileLine in fileLIneList)
            {
                var row = sheet.GetRow(rowIndex);
                if (row == null)
                {
                    row = sheet.CreateRow(rowIndex);
                }

                int colIndex = 0;
                //key korean (language) tag status desc
                ICell cell;

                cell = row.CreateCell(colIndex++);
                cell.SetCellType(CellType.String);
                cell.SetCellValue(fileLine.key);
                cell.CellStyle = colStyle;

                cell = row.CreateCell(colIndex++);
                cell.SetCellType(CellType.String);
                cell.SetCellValue(fileLine.sourceText);
                cell.CellStyle = colStyle;

                if (language != "Korean")
                {
                    cell = row.CreateCell(colIndex++);
                    cell.SetCellType(CellType.String);
                    cell.SetCellValue(fileLine.translation);
                    cell.CellStyle = colStyle;
                }

                cell = row.CreateCell(colIndex++);
                cell.SetCellType(CellType.String);
                cell.SetCellValue(fileLine.tag);
                cell.CellStyle = colStyle;

                cell = row.CreateCell(colIndex++);
                cell.SetCellType(CellType.String);
                cell.SetCellValue(fileLine.status);
                cell.CellStyle = colStyle;

                cell = row.CreateCell(colIndex++);
                cell.SetCellType(CellType.String);
                cell.SetCellValue(fileLine.desc);
                cell.CellStyle = colStyle;

                rowIndex++;
            }

            return workBook;
        }

        public static void SaveRegularXLSX(string filePath, string sheetName, string language, List<FileLine> fileLIneList)
        {
            XSSFWorkbook workBook = GetFileLineWorkbook(sheetName, language, fileLIneList);

            //xlsx 저장
            using (FileStream stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
            {
                workBook.Write(stream);
                workBook.Close();
                stream.Close();
            }
        }

        public static ExportZipContent ZipRegularXLSX(string fileName, string sheetName, string language, List<FileLine> fileLIneList)
        {
            ExportZipContent zipContent;
            XSSFWorkbook workBook = GetFileLineWorkbook(sheetName, language, fileLIneList);

            using (var inputMemoryStream = new MemoryStream())
            {
                workBook.Write(inputMemoryStream);
                workBook.Close();

                zipContent = new ExportZipContent(fileName, inputMemoryStream.ToArray());
                inputMemoryStream.Close();
            }

            return zipContent;
        }

        public static void SaveExtraCSV(string filePath, string language, List<TBTLine> TBTLineList)
        {
            using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                TBTMap fileLineMap = new TBTMap(language);
                csv.Configuration.RegisterClassMap(fileLineMap);
                csv.Configuration.ShouldQuote = ShouldQuote;

                var records = TBTLineList.ToList();
                csv.WriteRecords(records);
            }
        }

        public static ExportZipContent ZipExtraCSV(string fileName, string language, List<TBTLine> TBTLineList)
        {
            ExportZipContent zipContent;

            using (var inputMemoryStream = new MemoryStream())
            {
                using (var writer = new StreamWriter(inputMemoryStream, Encoding.UTF8))
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    TBTMap fileLineMap = new TBTMap(language);
                    csv.Configuration.RegisterClassMap(fileLineMap);
                    csv.Configuration.ShouldQuote = ShouldQuote;

                    var records = TBTLineList.ToList();
                    csv.WriteRecords(records);
                }

                zipContent = new ExportZipContent(fileName, inputMemoryStream.ToArray());
            }

            return zipContent;
        }

        private static XSSFWorkbook GetTBTLineWorkbook(string sheetName, string language, List<TBTLine> TBTLineList)
        {
            XSSFWorkbook workBook = new XSSFWorkbook();

            var sheet = workBook.GetSheet(sheetName);
            if (sheet == null)
            {
                sheet = workBook.CreateSheet(sheetName);
            }

            ICellStyle colStyle = workBook.CreateCellStyle();
            colStyle.WrapText = true;

            int rowIndex = 0;

            var firstRow = sheet.GetRow(rowIndex);
            if (firstRow == null)
            {
                firstRow = sheet.CreateRow(rowIndex);
            }
            int firstColIndex = 0;
            foreach (var fieldName in LineInfo.saveExtraFileLine)
            {
                ICell cell;
                if (fieldName == "Translation")
                {
                    if (language == "Korean") continue;

                    cell = firstRow.CreateCell(firstColIndex);
                    cell.SetCellType(CellType.String);
                    cell.SetCellValue(language);
                }
                else
                {
                    cell = firstRow.CreateCell(firstColIndex);
                    cell.SetCellType(CellType.String);
                    cell.SetCellValue(fieldName);
                }

                switch (fieldName)
                {
                    case "KEY":
                        sheet.SetColumnWidth(firstColIndex, 61 * 256);
                        break;
                    case "Korean":
                        sheet.SetColumnWidth(firstColIndex, 43 * 256);
                        break;
                    case "Translation":
                        sheet.SetColumnWidth(firstColIndex, 43 * 256);
                        break;
                    case "Tag":
                        sheet.SetColumnWidth(firstColIndex, 17 * 256);
                        break;
                    case "Status":
                        sheet.SetColumnWidth(firstColIndex, 17 * 256);
                        break;
                    case "Description":
                        sheet.SetColumnWidth(firstColIndex, 25 * 256);
                        break;
                }

                sheet.SetDefaultColumnStyle(firstColIndex, colStyle);

                firstColIndex++;
            }
            rowIndex++;

            foreach (TBTLine tbtLine in TBTLineList)
            {
                var row = sheet.GetRow(rowIndex);
                if (row == null)
                {
                    row = sheet.CreateRow(rowIndex);
                }

                int colIndex = 0;
                //key korean (language) tag status desc category partial
                ICell cell;

                cell = row.CreateCell(colIndex++);
                cell.SetCellType(CellType.String);
                cell.SetCellValue(tbtLine.key);
                cell.CellStyle = colStyle;

                cell = row.CreateCell(colIndex++);
                cell.SetCellType(CellType.String);
                cell.SetCellValue(tbtLine.sourceText);
                cell.CellStyle = colStyle;

                if (language != "Korean")
                {
                    cell = row.CreateCell(colIndex++);
                    cell.SetCellType(CellType.String);
                    cell.SetCellValue(tbtLine.translation);
                    cell.CellStyle = colStyle;
                }

                cell = row.CreateCell(colIndex++);
                cell.SetCellType(CellType.String);
                cell.SetCellValue(tbtLine.tag);
                cell.CellStyle = colStyle;

                cell = row.CreateCell(colIndex++);
                cell.SetCellType(CellType.String);
                cell.SetCellValue(tbtLine.status);
                cell.CellStyle = colStyle;

                cell = row.CreateCell(colIndex++);
                cell.SetCellType(CellType.String);
                cell.SetCellValue(tbtLine.desc);
                cell.CellStyle = colStyle;

                cell = row.CreateCell(colIndex++);
                cell.SetCellType(CellType.String);
                cell.SetCellValue(tbtLine.category);
                cell.CellStyle = colStyle;

                cell = row.CreateCell(colIndex++);
                cell.SetCellType(CellType.String);
                cell.SetCellValue(tbtLine.partial.ToString());
                cell.CellStyle = colStyle;

                rowIndex++;
            }

            return workBook;
        }

        public static void SaveExtraXLSX(string filePath, string sheetName, string language, List<TBTLine> TBTLineList)
        {
            XSSFWorkbook workBook = GetTBTLineWorkbook(sheetName, language, TBTLineList);

            using (FileStream stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
            {
                workBook.Write(stream);
                workBook.Close();
                stream.Close();
            }
        }

        public static ExportZipContent ZipExtraXLSX(string fileName, string sheetName, string language, List<TBTLine> TBTLineList)
        {
            ExportZipContent zipContent;
            XSSFWorkbook workBook = GetTBTLineWorkbook(sheetName, language, TBTLineList);

            using (var inputMemoryStream = new MemoryStream())
            {
                workBook.Write(inputMemoryStream);
                workBook.Close();

                zipContent = new ExportZipContent(fileName, inputMemoryStream.ToArray());
                inputMemoryStream.Close();
            }

            return zipContent;
        }
    }
}
