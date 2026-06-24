using System;
using System.Data;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using MySql.Data.MySqlClient;

namespace HotelManagementApp
{
    public partial class MainWindow : Window
    {
        private int _selectedRoomId;
        private int _selectedClientId;
        private int _selectedReservationId;

        public MainWindow()
        {
            InitializeComponent();
            LoadAllData();
            cmbReservationStatus.SelectedIndex = 0;
        }

        private void LoadAllData()
        {
            LoadRooms();
            LoadClients();
            LoadReservationCombos();
            LoadReservations();
            ClearSelections();
        }

        private void ClearSelections()
        {
            _selectedRoomId = 0;
            _selectedClientId = 0;
            _selectedReservationId = 0;
            dgRooms.SelectedItem = null;
            dgClients.SelectedItem = null;
            dgReservations.SelectedItem = null;
        }

        private static MySqlParameter P(string name, object value) => new MySqlParameter(name, value ?? DBNull.Value);

        private void ExecuteNonQuery(string sql, params MySqlParameter[] parameters)
        {
            HotelDb.ExecuteNonQuery(sql, parameters);
        }

        private DataTable ExecuteSelect(string sql, params MySqlParameter[] parameters)
        {
            return HotelDb.ExecuteSelect(sql, parameters);
        }

        private void LoadRooms()
        {
            LoadRooms(string.Empty, string.Empty, null, null);
        }

        private void LoadRooms(string number, string type, int? capacity, decimal? price)
        {
            const string sql = @"
                SELECT IdCamera, NumarCamera, TipCamera, Capacitate, PretNoapte
                FROM Camera
                WHERE (@number = '' OR NumarCamera LIKE @number)
                  AND (@type = '' OR TipCamera LIKE @type)
                  AND (@capacity IS NULL OR Capacitate = @capacity)
                  AND (@price IS NULL OR PretNoapte <= @price)
                ORDER BY NumarCamera";

            dgRooms.ItemsSource = ExecuteSelect(sql,
                P("@number", string.IsNullOrWhiteSpace(number) ? string.Empty : "%" + number.Trim() + "%"),
                P("@type", string.IsNullOrWhiteSpace(type) ? string.Empty : "%" + type.Trim() + "%"),
                P("@capacity", capacity.HasValue ? (object)capacity.Value : DBNull.Value),
                P("@price", price.HasValue ? (object)price.Value : DBNull.Value)).DefaultView;
        }

        private void LoadClients()
        {
            LoadClients(string.Empty);
        }

        private void LoadClients(string search)
        {
            const string sql = @"
                SELECT IdClient, Nume, Prenume, Telefon, SeriaNumarPasaport
                FROM Client
                WHERE (@search = '' OR Nume LIKE @search OR Prenume LIKE @search OR Telefon LIKE @search)
                ORDER BY Nume, Prenume";

            dgClients.ItemsSource = ExecuteSelect(sql, P("@search", string.IsNullOrWhiteSpace(search) ? string.Empty : "%" + search.Trim() + "%")).DefaultView;
        }

        private void LoadReservationCombos()
        {
            cmbReservationRoom.ItemsSource = ExecuteSelect(@"
                SELECT IdCamera,
                       CONCAT(NumarCamera, ' • ', TipCamera, ' • ', Capacitate, ' чел. • ', PretNoapte, ' lei') AS DisplayName
                FROM Camera
                ORDER BY NumarCamera").DefaultView;

            cmbReservationClient.ItemsSource = ExecuteSelect(@"
                SELECT IdClient,
                       CONCAT(Nume, ' ', Prenume, ' • ', Telefon) AS DisplayName
                FROM Client
                ORDER BY Nume, Prenume").DefaultView;

            cmbReservationFilterRoom.ItemsSource = ExecuteSelect(@"
                SELECT IdCamera,
                       CONCAT(NumarCamera, ' • ', TipCamera) AS DisplayName
                FROM Camera
                ORDER BY NumarCamera").DefaultView;

            cmbReservationFilterClient.ItemsSource = ExecuteSelect(@"
                SELECT IdClient,
                       CONCAT(Nume, ' ', Prenume) AS DisplayName
                FROM Client
                ORDER BY Nume, Prenume").DefaultView;
        }

        private void LoadReservations()
        {
            LoadReservations(0, 0, null, null);
        }

        private void LoadReservations(int roomId, int clientId, DateTime? fromDate, DateTime? toDate)
        {
            const string sql = @"
                SELECT r.IdRezervare,
                       cam.NumarCamera,
                       cam.TipCamera,
                       CONCAT(cl.Nume, ' ', cl.Prenume) AS Client,
                       r.DataCheckIn,
                       r.DataCheckOut,
                       r.NumarNopti,
                       r.CostTotal,
                       r.StatusRezervare,
                       r.IdCamera,
                       r.IdClient
                FROM Rezervare r
                INNER JOIN Camera cam ON cam.IdCamera = r.IdCamera
                INNER JOIN Client cl ON cl.IdClient = r.IdClient
                WHERE (@roomId = 0 OR r.IdCamera = @roomId)
                  AND (@clientId = 0 OR r.IdClient = @clientId)
                  AND (@fromDate IS NULL OR r.DataCheckOut >= @fromDate)
                  AND (@toDate IS NULL OR r.DataCheckIn <= @toDate)
                ORDER BY r.DataCheckIn DESC, cam.NumarCamera";

            dgReservations.ItemsSource = ExecuteSelect(sql,
                P("@roomId", roomId),
                P("@clientId", clientId),
                P("@fromDate", fromDate.HasValue ? (object)fromDate.Value.Date : DBNull.Value),
                P("@toDate", toDate.HasValue ? (object)toDate.Value.Date : DBNull.Value)).DefaultView;
        }

        private void BtnAddRoom_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateRoom(out int capacity, out decimal price))
            {
                return;
            }

            if (RoomNumberExists(txtRoomNumber.Text.Trim(), 0))
            {
                MessageBox.Show("Номер комнаты должен быть уникальным.");
                return;
            }

            try
            {
                ExecuteNonQuery(@"
                    INSERT INTO Camera (NumarCamera, TipCamera, Capacitate, PretNoapte)
                    VALUES (@number, @type, @capacity, @price)",
                    P("@number", txtRoomNumber.Text.Trim()),
                    P("@type", txtRoomType.Text.Trim()),
                    P("@capacity", capacity),
                    P("@price", price));
                LoadAllData();
                ClearRoomInputs();
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Ошибка сохранения номера: " + ex.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения номера: " + ex.Message);
            }
        }

        private void BtnUpdateRoom_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRoomId == 0)
            {
                MessageBox.Show("Выберите номер для изменения.");
                return;
            }

            if (!ValidateRoom(out int capacity, out decimal price))
            {
                return;
            }

            if (RoomNumberExists(txtRoomNumber.Text.Trim(), _selectedRoomId))
            {
                MessageBox.Show("Номер комнаты должен быть уникальным.");
                return;
            }

            try
            {
                ExecuteNonQuery(@"
                    UPDATE Camera
                    SET NumarCamera = @number,
                        TipCamera = @type,
                        Capacitate = @capacity,
                        PretNoapte = @price
                    WHERE IdCamera = @id",
                    P("@number", txtRoomNumber.Text.Trim()),
                    P("@type", txtRoomType.Text.Trim()),
                    P("@capacity", capacity),
                    P("@price", price),
                    P("@id", _selectedRoomId));

                LoadAllData();
                ClearRoomInputs();
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Ошибка обновления номера: " + ex.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка обновления номера: " + ex.Message);
            }
        }

        private void BtnDeleteRoom_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRoomId == 0)
            {
                MessageBox.Show("Выберите номер для удаления.");
                return;
            }

            if (MessageBox.Show("Удалить номер и связанные бронирования?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                ExecuteNonQuery("DELETE FROM Camera WHERE IdCamera = @id", P("@id", _selectedRoomId));
                LoadAllData();
                ClearRoomInputs();
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Ошибка удаления номера: " + ex.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка удаления номера: " + ex.Message);
            }
        }

        private void BtnClearRoom_Click(object sender, RoutedEventArgs e)
        {
            ClearRoomInputs();
            dgRooms.SelectedItem = null;
            _selectedRoomId = 0;
        }

        private void BtnFilterRooms_Click(object sender, RoutedEventArgs e)
        {
            int? capacity = null;
            decimal? price = null;

            if (!string.IsNullOrWhiteSpace(txtRoomFilterCapacity.Text))
            {
                if (!int.TryParse(txtRoomFilterCapacity.Text.Trim(), out int parsedCapacity) || parsedCapacity <= 0)
                {
                    MessageBox.Show("Вместимость для фильтра должна быть числом больше 0.");
                    return;
                }

                capacity = parsedCapacity;
            }

            if (!string.IsNullOrWhiteSpace(txtRoomFilterPrice.Text))
            {
                if (!decimal.TryParse(txtRoomFilterPrice.Text.Trim(), out decimal parsedPrice) || parsedPrice <= 0)
                {
                    MessageBox.Show("Цена для фильтра должна быть числом больше 0.");
                    return;
                }

                price = parsedPrice;
            }

            LoadRooms(txtRoomFilterNumber.Text, txtRoomFilterType.Text, capacity, price);
        }

        private void BtnClearRoomFilter_Click(object sender, RoutedEventArgs e)
        {
            txtRoomFilterNumber.Clear();
            txtRoomFilterType.Clear();
            txtRoomFilterCapacity.Clear();
            txtRoomFilterPrice.Clear();
            LoadRooms();
        }

        private void DgRooms_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgRooms.SelectedItem is DataRowView row)
            {
                _selectedRoomId = Convert.ToInt32(row["IdCamera"]);
                txtRoomNumber.Text = row["NumarCamera"].ToString();
                txtRoomType.Text = row["TipCamera"].ToString();
                txtRoomCapacity.Text = row["Capacitate"].ToString();
                txtRoomPrice.Text = row["PretNoapte"].ToString();
            }
        }

        private bool ValidateRoom(out int capacity, out decimal price)
        {
            capacity = 0;
            price = 0;

            if (string.IsNullOrWhiteSpace(txtRoomNumber.Text) ||
                string.IsNullOrWhiteSpace(txtRoomType.Text) ||
                string.IsNullOrWhiteSpace(txtRoomCapacity.Text) ||
                string.IsNullOrWhiteSpace(txtRoomPrice.Text))
            {
                MessageBox.Show("Все поля номера обязательны.");
                return false;
            }

            if (!int.TryParse(txtRoomCapacity.Text.Trim(), out capacity) || capacity <= 0)
            {
                MessageBox.Show("Вместимость должна быть числом больше 0.");
                return false;
            }

            if (!decimal.TryParse(txtRoomPrice.Text.Trim(), out price) || price <= 0)
            {
                MessageBox.Show("Цена за ночь должна быть числом больше 0.");
                return false;
            }

            return true;
        }

        private bool RoomNumberExists(string roomNumber, int excludeId)
        {
            object result = ExecuteSelect(@"
                SELECT COUNT(*)
                FROM Camera
                WHERE NumarCamera = @number AND IdCamera <> @id",
                P("@number", roomNumber),
                P("@id", excludeId)).Rows[0][0];

            return Convert.ToInt32(result) > 0;
        }

        private void BtnAddClient_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateClient())
            {
                return;
            }

            if (PassportExists(txtClientPassport.Text.Trim(), 0))
            {
                MessageBox.Show("Серия и номер паспорта должны быть уникальными.");
                return;
            }

            try
            {
                ExecuteNonQuery(@"
                    INSERT INTO Client (Nume, Prenume, Telefon, SeriaNumarPasaport)
                    VALUES (@lastName, @firstName, @phone, @passport)",
                    P("@lastName", txtClientLastName.Text.Trim()),
                    P("@firstName", txtClientFirstName.Text.Trim()),
                    P("@phone", txtClientPhone.Text.Trim()),
                    P("@passport", txtClientPassport.Text.Trim()));

                LoadAllData();
                ClearClientInputs();
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Ошибка сохранения клиента: " + ex.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения клиента: " + ex.Message);
            }
        }

        private void BtnUpdateClient_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedClientId == 0)
            {
                MessageBox.Show("Выберите клиента для изменения.");
                return;
            }

            if (!ValidateClient())
            {
                return;
            }

            if (PassportExists(txtClientPassport.Text.Trim(), _selectedClientId))
            {
                MessageBox.Show("Серия и номер паспорта должны быть уникальными.");
                return;
            }

            try
            {
                ExecuteNonQuery(@"
                    UPDATE Client
                    SET Nume = @lastName,
                        Prenume = @firstName,
                        Telefon = @phone,
                        SeriaNumarPasaport = @passport
                    WHERE IdClient = @id",
                    P("@lastName", txtClientLastName.Text.Trim()),
                    P("@firstName", txtClientFirstName.Text.Trim()),
                    P("@phone", txtClientPhone.Text.Trim()),
                    P("@passport", txtClientPassport.Text.Trim()),
                    P("@id", _selectedClientId));

                LoadAllData();
                ClearClientInputs();
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Ошибка обновления клиента: " + ex.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка обновления клиента: " + ex.Message);
            }
        }

        private void BtnDeleteClient_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedClientId == 0)
            {
                MessageBox.Show("Выберите клиента для удаления.");
                return;
            }

            if (MessageBox.Show("Удалить клиента и связанные бронирования?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                ExecuteNonQuery("DELETE FROM Client WHERE IdClient = @id", P("@id", _selectedClientId));
                LoadAllData();
                ClearClientInputs();
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Ошибка удаления клиента: " + ex.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка удаления клиента: " + ex.Message);
            }
        }

        private void BtnClearClient_Click(object sender, RoutedEventArgs e)
        {
            ClearClientInputs();
            dgClients.SelectedItem = null;
            _selectedClientId = 0;
        }

        private void TxtClientSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoadClients(txtClientSearch.Text);
        }

        private void BtnClearClientSearch_Click(object sender, RoutedEventArgs e)
        {
            txtClientSearch.Clear();
            LoadClients();
        }

        private void DgClients_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgClients.SelectedItem is DataRowView row)
            {
                _selectedClientId = Convert.ToInt32(row["IdClient"]);
                txtClientLastName.Text = row["Nume"].ToString();
                txtClientFirstName.Text = row["Prenume"].ToString();
                txtClientPhone.Text = row["Telefon"].ToString();
                txtClientPassport.Text = row["SeriaNumarPasaport"].ToString();
            }
        }

        private bool ValidateClient()
        {
            if (string.IsNullOrWhiteSpace(txtClientLastName.Text) ||
                string.IsNullOrWhiteSpace(txtClientFirstName.Text) ||
                string.IsNullOrWhiteSpace(txtClientPhone.Text) ||
                string.IsNullOrWhiteSpace(txtClientPassport.Text))
            {
                MessageBox.Show("Все поля клиента обязательны.");
                return false;
            }

            if (!Regex.IsMatch(txtClientPhone.Text.Trim(), @"^(0\d{8}|\+373\d{8})$"))
            {
                MessageBox.Show("Телефон должен иметь формат 0XXXXXXXX или +373XXXXXXXX.");
                return false;
            }

            return true;
        }

        private bool PassportExists(string passport, int excludeId)
        {
            object result = ExecuteSelect(@"
                SELECT COUNT(*)
                FROM Client
                WHERE SeriaNumarPasaport = @passport AND IdClient <> @id",
                P("@passport", passport),
                P("@id", excludeId)).Rows[0][0];

            return Convert.ToInt32(result) > 0;
        }

        private void BtnAddReservation_Click(object sender, RoutedEventArgs e)
        {
            SaveReservation(0);
        }

        private void BtnUpdateReservation_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedReservationId == 0)
            {
                MessageBox.Show("Выберите бронирование для изменения.");
                return;
            }

            SaveReservation(_selectedReservationId);
        }

        private void SaveReservation(int reservationId)
        {
            if (!ValidateReservation(out int roomId, out int clientId, out DateTime checkIn, out DateTime checkOut, out string status, out int nights, out decimal total))
            {
                return;
            }

            if (!string.Equals(status, "Отменено", StringComparison.OrdinalIgnoreCase) && !IsRoomAvailable(roomId, checkIn, checkOut, reservationId))
            {
                MessageBox.Show("Выбранный номер уже занят на этот период.");
                return;
            }

            try
            {
                if (reservationId == 0)
                {
                    ExecuteNonQuery(@"
                        INSERT INTO Rezervare (IdCamera, IdClient, DataCheckIn, DataCheckOut, NumarNopti, CostTotal, StatusRezervare)
                        VALUES (@roomId, @clientId, @checkIn, @checkOut, @nights, @total, @status)",
                        P("@roomId", roomId),
                        P("@clientId", clientId),
                        P("@checkIn", checkIn.Date),
                        P("@checkOut", checkOut.Date),
                        P("@nights", nights),
                        P("@total", total),
                        P("@status", status));
                }
                else
                {
                    ExecuteNonQuery(@"
                        UPDATE Rezervare
                        SET IdCamera = @roomId,
                            IdClient = @clientId,
                            DataCheckIn = @checkIn,
                            DataCheckOut = @checkOut,
                            NumarNopti = @nights,
                            CostTotal = @total,
                            StatusRezervare = @status
                        WHERE IdRezervare = @id",
                        P("@roomId", roomId),
                        P("@clientId", clientId),
                        P("@checkIn", checkIn.Date),
                        P("@checkOut", checkOut.Date),
                        P("@nights", nights),
                        P("@total", total),
                        P("@status", status),
                        P("@id", reservationId));
                }

                LoadAllData();
                ClearReservationInputs();
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Ошибка сохранения бронирования: " + ex.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения бронирования: " + ex.Message);
            }
        }

        private bool ValidateReservation(out int roomId, out int clientId, out DateTime checkIn, out DateTime checkOut, out string status, out int nights, out decimal total)
        {
            roomId = 0;
            clientId = 0;
            checkIn = DateTime.MinValue;
            checkOut = DateTime.MinValue;
            status = string.Empty;
            nights = 0;
            total = 0;

            if (cmbReservationRoom.SelectedValue == null ||
                cmbReservationClient.SelectedValue == null ||
                dpCheckIn.SelectedDate == null ||
                dpCheckOut.SelectedDate == null ||
                cmbReservationStatus.SelectedItem == null)
            {
                MessageBox.Show("Все поля бронирования обязательны.");
                return false;
            }

            roomId = Convert.ToInt32(cmbReservationRoom.SelectedValue);
            clientId = Convert.ToInt32(cmbReservationClient.SelectedValue);
            checkIn = dpCheckIn.SelectedDate.Value.Date;
            checkOut = dpCheckOut.SelectedDate.Value.Date;
            status = ((ComboBoxItem)cmbReservationStatus.SelectedItem).Content.ToString();

            if (checkOut <= checkIn)
            {
                MessageBox.Show("Дата выезда должна быть позже даты заезда.");
                return false;
            }

            nights = (checkOut - checkIn).Days;
            if (nights <= 0)
            {
                MessageBox.Show("Количество ночей должно быть больше 0.");
                return false;
            }

            decimal roomPrice = GetRoomPrice(roomId);
            if (roomPrice <= 0)
            {
                MessageBox.Show("Не удалось определить цену номера.");
                return false;
            }

            total = nights * roomPrice;
            return true;
        }

        private decimal GetRoomPrice(int roomId)
        {
            object result = ExecuteSelect("SELECT PretNoapte FROM Camera WHERE IdCamera = @id", P("@id", roomId)).Rows[0][0];
            return Convert.ToDecimal(result);
        }

        private bool IsRoomAvailable(int roomId, DateTime checkIn, DateTime checkOut, int excludeReservationId)
        {
            object result = ExecuteSelect(@"
                SELECT COUNT(*)
                FROM Rezervare
                WHERE IdCamera = @roomId
                  AND StatusRezervare <> 'Отменено'
                  AND IdRezervare <> @id
                  AND DataCheckIn < @checkOut
                  AND DataCheckOut > @checkIn",
                P("@roomId", roomId),
                P("@id", excludeReservationId),
                P("@checkIn", checkIn.Date),
                P("@checkOut", checkOut.Date)).Rows[0][0];

            return Convert.ToInt32(result) == 0;
        }

        private void BtnCancelReservation_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedReservationId == 0)
            {
                MessageBox.Show("Выберите бронирование для отмены.");
                return;
            }

            try
            {
                ExecuteNonQuery("UPDATE Rezervare SET StatusRezervare = 'Отменено' WHERE IdRezervare = @id", P("@id", _selectedReservationId));
                LoadAllData();
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Ошибка отмены бронирования: " + ex.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка отмены бронирования: " + ex.Message);
            }
        }

        private void BtnDeleteReservation_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedReservationId == 0)
            {
                MessageBox.Show("Выберите бронирование для удаления.");
                return;
            }

            if (MessageBox.Show("Удалить бронирование?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                ExecuteNonQuery("DELETE FROM Rezervare WHERE IdRezervare = @id", P("@id", _selectedReservationId));
                LoadAllData();
                ClearReservationInputs();
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Ошибка удаления бронирования: " + ex.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка удаления бронирования: " + ex.Message);
            }
        }

        private void BtnClearReservation_Click(object sender, RoutedEventArgs e)
        {
            ClearReservationInputs();
            dgReservations.SelectedItem = null;
            _selectedReservationId = 0;
        }

        private void BtnFilterReservations_Click(object sender, RoutedEventArgs e)
        {
            int roomId = cmbReservationFilterRoom.SelectedValue == null ? 0 : Convert.ToInt32(cmbReservationFilterRoom.SelectedValue);
            int clientId = cmbReservationFilterClient.SelectedValue == null ? 0 : Convert.ToInt32(cmbReservationFilterClient.SelectedValue);
            DateTime? fromDate = dpReservationFrom.SelectedDate?.Date;
            DateTime? toDate = dpReservationTo.SelectedDate?.Date;

            if (fromDate.HasValue && toDate.HasValue && toDate.Value < fromDate.Value)
            {
                MessageBox.Show("Дата конца фильтра должна быть позже даты начала.");
                return;
            }

            LoadReservations(roomId, clientId, fromDate, toDate);
        }

        private void BtnClearReservationFilter_Click(object sender, RoutedEventArgs e)
        {
            cmbReservationFilterRoom.SelectedIndex = -1;
            cmbReservationFilterClient.SelectedIndex = -1;
            dpReservationFrom.SelectedDate = null;
            dpReservationTo.SelectedDate = null;
            LoadReservations();
        }

        private void DgReservations_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgReservations.SelectedItem is DataRowView row)
            {
                _selectedReservationId = Convert.ToInt32(row["IdRezervare"]);
                cmbReservationRoom.SelectedValue = row["IdCamera"];
                cmbReservationClient.SelectedValue = row["IdClient"];
                dpCheckIn.SelectedDate = Convert.ToDateTime(row["DataCheckIn"]);
                dpCheckOut.SelectedDate = Convert.ToDateTime(row["DataCheckOut"]);

                string status = row["StatusRezervare"].ToString();
                foreach (ComboBoxItem item in cmbReservationStatus.Items)
                {
                    if (string.Equals(item.Content?.ToString(), status, StringComparison.OrdinalIgnoreCase))
                    {
                        cmbReservationStatus.SelectedItem = item;
                        break;
                    }
                }
            }
        }

        private void BtnOpenReport_Click(object sender, RoutedEventArgs e)
        {
            var window = new ReportWindow
            {
                Owner = this
            };

            window.ShowDialog();
        }

        private void ClearRoomInputs()
        {
            txtRoomNumber.Clear();
            txtRoomType.Clear();
            txtRoomCapacity.Clear();
            txtRoomPrice.Clear();
        }

        private void ClearClientInputs()
        {
            txtClientLastName.Clear();
            txtClientFirstName.Clear();
            txtClientPhone.Clear();
            txtClientPassport.Clear();
        }

        private void ClearReservationInputs()
        {
            cmbReservationRoom.SelectedIndex = -1;
            cmbReservationClient.SelectedIndex = -1;
            dpCheckIn.SelectedDate = null;
            dpCheckOut.SelectedDate = null;
            cmbReservationStatus.SelectedIndex = 0;
        }
    }
}
