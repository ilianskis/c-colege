using System;
using System.Data;
using System.IO;
using System.Text;
using System.Windows;
using Microsoft.Win32;

namespace HotelManagementApp
{
    public partial class ReportWindow : Window
    {
        private DataTable _reportData;

        public ReportWindow()
        {
            InitializeComponent();
            LoadReport();
        }

        private void LoadReport()
        {
            _reportData = HotelDb.ExecuteSelect(@"
                SELECT r.IdRezervare,
                       cam.NumarCamera,
                       cam.TipCamera,
                       CONCAT(cl.Nume, ' ', cl.Prenume) AS Client,
                       r.DataCheckIn,
                       r.DataCheckOut,
                       r.NumarNopti,
                       r.StatusRezervare,
                       r.CostTotal
                FROM Rezervare r
                INNER JOIN Camera cam ON cam.IdCamera = r.IdCamera
                INNER JOIN Client cl ON cl.IdClient = r.IdClient
                ORDER BY r.DataCheckIn DESC, cam.NumarCamera");

            dgReport.ItemsSource = _reportData.DefaultView;

            decimal revenue = Convert.ToDecimal(HotelDb.ExecuteSelect(@"
                SELECT IFNULL(SUM(CostTotal), 0)
                FROM Rezervare
                WHERE StatusRezervare IN ('Подтверждено', 'Завершено')").Rows[0][0]);

            lblRevenue.Text = revenue.ToString("0.00") + " lei";

            DataTable topRoom = HotelDb.ExecuteSelect(@"
                SELECT cam.NumarCamera, COUNT(*) AS TotalCount
                FROM Rezervare r
                INNER JOIN Camera cam ON cam.IdCamera = r.IdCamera
                GROUP BY cam.IdCamera, cam.NumarCamera
                ORDER BY TotalCount DESC, cam.NumarCamera
                LIMIT 1");

            lblTopRoom.Text = topRoom.Rows.Count > 0
                ? topRoom.Rows[0]["NumarCamera"] + " (" + topRoom.Rows[0]["TotalCount"] + ")"
                : "Нет данных";

            int confirmed = Convert.ToInt32(HotelDb.ExecuteSelect(@"
                SELECT COUNT(*)
                FROM Rezervare
                WHERE StatusRezervare = 'Подтверждено'").Rows[0][0]);

            lblConfirmedCount.Text = confirmed.ToString();
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadReport();
        }

        private void BtnExportTxt_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    Filter = "Text file (*.txt)|*.txt",
                    FileName = "HotelReport.txt"
                };

                if (dialog.ShowDialog() != true)
                {
                    return;
                }

                var builder = new StringBuilder();
                builder.AppendLine("Краткий отчет по бронированиям");
                builder.AppendLine("Дата формирования: " + DateTime.Now);
                builder.AppendLine();

                foreach (DataRow row in _reportData.Rows)
                {
                    builder.AppendLine(
                        $"{row["NumarCamera"]} | {row["TipCamera"]} | {row["Client"]} | {Convert.ToDateTime(row["DataCheckIn"]):dd.MM.yyyy} - {Convert.ToDateTime(row["DataCheckOut"]):dd.MM.yyyy} | {row["NumarNopti"]} ноч. | {row["StatusRezervare"]} | {Convert.ToDecimal(row["CostTotal"]):0.00} lei");
                }

                builder.AppendLine();
                builder.AppendLine("Общая сумма дохода: " + lblRevenue.Text);
                builder.AppendLine("Номер с наибольшим количеством бронирований: " + lblTopRoom.Text);
                builder.AppendLine("Подтвержденных бронирований: " + lblConfirmedCount.Text);

                File.WriteAllText(dialog.FileName, builder.ToString(), Encoding.UTF8);
                MessageBox.Show("Отчет сохранен.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка экспорта: " + ex.Message);
            }
        }
    }
}
