using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Data;
using System.Runtime.InteropServices;

namespace UpdateChecker
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            textBoxSource.Text = "C:\\data\\volleyball scheduling data.xlsm";
            dateTimePickerEnd.Value = DateTime.Now.AddYears(1);
            //AllocConsole();
        }

        //[DllImport("kernel32.dll", SetLastError = true)]
        //[return: MarshalAs(UnmanagedType.Bool)]
        //static extern bool AllocConsole();

        private void selectSourceButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Title = "Choose source file";
            openFileDialog1.Filter = "Excel files (*.xlsx;*.xlsm)|*.xlsx;*.xlsm";
            openFileDialog1.ShowDialog();
            textBoxSource.Text = openFileDialog1.FileName;
        }

        private void selectUpdateButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Title = "Choose update file";
            openFileDialog1.Filter = "Excel files (*.xlsx;*.xlsm)|*.xlsx;*.xlsm";
            openFileDialog1.ShowDialog();
            textBoxUpdate.Text = openFileDialog1.FileName;
        }

        private void processButton_Click(object sender, EventArgs e)
        {
            dataGridView1.Rows.Clear();
            string sourceFilePath = textBoxSource.Text;

            DataTable sourceData = GetDataFromExcel(sourceFilePath);
            sourceData = sourceData.AsEnumerable().Where(r => r.Field<string>("Locatie") == "Kruisboog, Houten" && DateTime.Parse(r.Field<string>("Datum")) > dateTimePickerStart.Value && DateTime.Parse(r.Field<string>("Datum")) < dateTimePickerEnd.Value).CopyToDataTable();

            string updateFilePath = textBoxUpdate.Text;
            DataTable updateData = GetDataFromExcel(updateFilePath);
            updateData = updateData.AsEnumerable().Where(r => r.Field<string>("Locatie") == "Kruisboog, Houten" && DateTime.Parse(r.Field<string>("Datum")) > dateTimePickerStart.Value && DateTime.Parse(r.Field<string>("Datum")) < dateTimePickerEnd.Value).CopyToDataTable();

            sourceData.AsEnumerable().ToList().ForEach(s =>
            {
                var temp = updateData.AsEnumerable().Where(u => s.Field<string>("Code") == u.Field<string>("Code"));

                // removed entries
                if (temp.Count() < 1)
                {
                    string[] newRow = { s["Code"].ToString(), s["Datum"].ToString(), s["Tijd"].ToString(), "Not Found", "Not Found" };
                    dataGridView1.Rows.Add(newRow);
                    dataGridView1.Rows[dataGridView1.Rows.Count - 1].Cells[0].Style.BackColor = System.Drawing.Color.DarkRed;
                }

                // changed entries
                temp.Where(u => (s.Field<string>("Datum") != u.Field<string>("Datum") ||
                                 s.Field<string>("Tijd") != u.Field<string>("Tijd"))).ToList().ForEach(update =>
                {
                    string[] newRow = { s["Code"].ToString(), s["Datum"].ToString(), s["Tijd"].ToString(), update["Datum"].ToString(), update["Tijd"].ToString() };
                    dataGridView1.Rows.Add(newRow);

                    for (int i = 1; i < 3; i++)
                    {
                        if (newRow[i] != newRow[i + 2])
                        {
                            dataGridView1.Rows[dataGridView1.Rows.Count - 1].Cells[i + 2].Style.BackColor = System.Drawing.Color.Red;

                        }
                    }
                });
            });

            // new entries
            updateData.AsEnumerable().ToList().ForEach(u =>
            {
                if (sourceData.AsEnumerable().Where(s => s.Field<string>("Code") == u.Field<string>("Code")).Count() < 1)
                {
                    string[] newRow = { u["Code"].ToString(), "Not Found", "Not Found", u["Datum"].ToString(), u["Tijd"].ToString() };
                    dataGridView1.Rows.Add(newRow);
                    dataGridView1.Rows[dataGridView1.Rows.Count - 1].Cells[0].Style.BackColor = System.Drawing.Color.Green;
                }
            });


        }

        private DataTable GetDataFromExcel(string path)
        {
            List<string> fields = new List<string>() { "Datum", "Tijd", "Code", "Locatie" };
            List<Type> fieldTypes = new List<Type>() { typeof(DateTime), typeof(TimeSpan), typeof(string), typeof(string) };

            using (var workBook = new XLWorkbook(path))
            {
                var workSheet = workBook.Worksheet(1);
                var firstRowUsed = workSheet.FirstRowUsed();
                var firstPossibleAddress = workSheet.Row(firstRowUsed.RowNumber()).FirstCell().Address;
                var lastPossibleAddress = workSheet.LastCellUsed().Address;

                workSheet.SetAutoFilter(false);

                // Get a range with the remainder of the worksheet data (the range used)
                var range = workSheet.Range(firstPossibleAddress, lastPossibleAddress).AsRange(); //.RangeUsed();
                                                                                                  // Treat the range as a table (to be able to use the column names)
                var table = range.AsTable();

                var dataList = new List<string[]>();
                foreach (var (fieldName, fieldType) in fields.Zip(fieldTypes))
                {
                    string[] values = table.DataRange.Rows().Select(tableRow => tableRow.Field(fieldName).GetString()).ToArray();

                    if (fieldType != typeof(string))
                    {
                        for (int i = 0; i < values.Length; i++)
                        {
                            if (values[i].Length < 1)
                            {
                                continue;
                            }

                            if (values[i] == "Vervallen" || values[i][0] == '#')
                            {
                                continue;
                            }

                            var dt = DateTime.Parse(values[i]);

                            if (fieldType == typeof(DateTime))
                            {
                                values[i] = dt.ToShortDateString();

                            }
                            else if (fieldType == typeof(TimeSpan))
                            {
                                values[i] = dt.ToShortTimeString();

                            }
                            else
                            {
                                throw new NotImplementedException();
                            }

                            //Console.Out.WriteLine(dt);
                        }

                    }


                    dataList.Add(values);
                }
                //Convert List to DataTable
                return ConvertListToDataTable(dataList, fields);
            }
        }


        private static DataTable ConvertListToDataTable(IReadOnlyList<string[]> list, IReadOnlyList<string> fields)
        {
            var table = new DataTable("CustomTable");
            var rows = list.Select(array => array.Length).Concat(new[] { 0 }).Max();

            foreach (var field in fields)
            {
                table.Columns.Add(field);
            }

            for (var j = 0; j < rows; j++)
            {
                var row = table.NewRow();
                int i = 0;
                foreach (var field in fields)
                {
                    row[field] = list[i][j];
                    i++;
                }
                table.Rows.Add(row);
            }
            return table;
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void dateTimePickerStart_ValueChanged(object sender, EventArgs e)
        {

        }
    }
}