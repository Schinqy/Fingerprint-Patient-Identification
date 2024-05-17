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
using System.IO.Ports;
using System.Data;
using System.Configuration;
using Newtonsoft.Json;
using System.Data.SQLite;
using System.IO;


namespace Fingerprint_Patient_Identification
{
    /// <summary>
    /// Interaction logic for Search.xaml
    /// </summary>
    public partial class Search : Window
    {

        string connectionString = ConfigurationManager.ConnectionStrings["patientsDb"].ConnectionString;
        // DataTable ds = new DataTable();
        DataSet ds = new DataSet();

        private SerialPort comPort = new SerialPort(); //Initialise comPort Variable as SerialPort

        // Weights //
        int Fing_ID;
        int clever_variable;
        int p;
        bool isDeleted = false;   

        public Search()
        {
            InitializeComponent();
            try
            {

                using (var connection = new SQLiteConnection(connectionString))
                {


                    connection.Open();

                    //Display query  
                    string Query = "select * from Patients;";
                    using (var command = new SQLiteCommand(Query, connection))
                    {

                        //  MyConn2.Open();  
                        //For offline connection we weill use  MySqlDataAdapter class.  
                        SQLiteDataAdapter MyAdapter = new SQLiteDataAdapter();
                        MyAdapter.SelectCommand = command;


                        MyAdapter.Fill(ds);
                        DataTable dT = ds.Tables[0];
                        DataView DV = new DataView(dT);
                        // DV.RowFilter = string.Format("last_name LIKE '%{0}%'", textBox.Text);
                        dataGrid.ItemsSource = DV;
                        //  dataGrid.DataContext = ds; // here i have assigned dataset object to the dataGridView object to display data.               
                    }
                }//MyConn2.Close();  
            }
            catch (SQLiteException ex)
            {
                MessageBox.Show(ex.Message);
            }


        }

        private void Search_Loaded(object sender, RoutedEventArgs e)
        {
            updatePorts();
        }

        void selectedCellChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            var cellInfos = dataGrid.SelectedCells;
            var list1 = new List<string>();
            var errors = new List<string>();

            foreach (DataGridCellInfo cellInfo in cellInfos)
            {
                if (cellInfo.IsValid)
                {
                    var content = cellInfo.Column.GetCellContent(cellInfo.Item);
                    if (content != null)
                    {
                        var dataContext = content.DataContext as DataRowView;
                        if (dataContext != null)
                        {
                            try
                            {
                                object[] obj = dataContext.Row.ItemArray;
                                for (int i = 0; i < Math.Min(obj.Length, 15); i++)
                                {
                                    list1.Add(obj[i]?.ToString() ?? string.Empty);
                                }
                            }
                            catch (Exception ex)
                            {
                                errors.Add($"Error processing row data: {ex.Message}");
                            }
                        }
                        else
                        {
                            errors.Add("The data context is not a DataRowView.");
                        }
                    }
                    else
                    {
                        errors.Add("The cell content is null.");
                    }
                }
            }

            if (list1.Count > 0)
            {
                try
                {
                    fingerLabel.Content = list1[0];
                    Fing_ID = Convert.ToInt32(list1[0]);
                    hospitalNumberLabel.Content = list1[1];
                    nameTB.Text = list1[2];
                    surnameTB.Text = list1[3];
                   // textBox.Text = surnameTB.Text;
                    sexTB.Text = list1[4];
                    dobTB.Text = list1[5];
                    tpTB.Text = list1[6];
                    phoneTB.Text = list1[7];
                    sessionsTB.Text = list1[8];
                    ptTB.Text = list1[9];
                    diagnosisTB.Text = list1[10];
                    idTB.Text = list1[11];
                    oncologistTB.Text = list1[12];
                    string element = list1[14];
                    setImageFromDatagridClick(Fing_ID);
                }
                catch (Exception ex)
                {
                    errors.Add($"Error setting UI elements: {ex.Message}");
                }
            }

            if (errors.Count > 0)
            {
                MessageBox.Show(this, string.Join("\n", errors), "Errors", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }



        private void Records_Loaded(object sender, RoutedEventArgs e)
        {

           
        }

        private void updatePorts()
        {
            string[] ports = SerialPort.GetPortNames();
            foreach (string port in ports)
            {
                comboBox.Items.Add(port);
            }
        }


        private void connect()
        {
            bool error = false;

            // Check if all settings have been selected

            if (comboBox.SelectedIndex != -1)
            {  //if yes then Set The Port's settings
                comPort.PortName = comboBox.Text;
                comPort.BaudRate = 57600;
                comPort.Parity = Parity.None;

                comPort.DataBits = 8;
                comPort.StopBits = StopBits.One;

                try //always try to use this try and catch method to open your port.
                    //if there is an error your program will display a message instead of freezing.
                {
                    //Open Port
                    comPort.Open();

                    //btnCollect.IsEnabled = true;
                }
                catch (UnauthorizedAccessException) { error = true; }
                catch (System.IO.IOException) { error = true; }
                catch (ArgumentException) { error = true; }

                if (error) MessageBox.Show(this, "Could not open the COM port. Most likely it is already in use, has been removed, or is unavailable.", "COM Port unavailable", MessageBoxButton.OK, MessageBoxImage.Stop);


            }
            else
            {
                MessageBox.Show("Please select the COM Port", "Serial Port Interface", MessageBoxButton.OK, MessageBoxImage.Stop);
            }
            //if the port is open, Change the Connect button to disconnect, enable the send button.
            //and disable the groupBox to prevent changing configuration of an open port.
            if (comPort.IsOpen)
            {
                btnConnect.Content = "Disconnect";

            }


        }

        // Call this function to close the port.
        private void disconnect()
        {
            comPort.Close();
            btnConnect.Content = "Connect";

            //btnCollect.IsEnabled = false;

        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            if (comPort.IsOpen)
            {
                disconnect();
            }
            else
            {
                connect();
            }
        }

        private void btn_Click(object sender, RoutedEventArgs e)
        {
            dataGrid.ItemsSource = null;
        }

        private void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            try

            {
                using (var connection = new SQLiteConnection(connectionString))
                {


                    connection.Open();
                    string Query = "update Patients set name='" + nameTB.Text
                                   + "',surname='" + surnameTB.Text 
                                   + "',sex='" + sexTB.Text 
                                   + "',id='" + idTB.Text 
                                   + "',dob='" + dobTB.Text 
                                   + "', phone='" + phoneTB.Text 
                                   + "', prevtreatments='" + ptTB.Text 
                                   + "', sessions='" + sessionsTB.Text 
                                   + "', diagnosis='" + diagnosisTB.Text 
                                   + "', oncologist='" + oncologistTB.Text 
                                   + "', tplan='" + tpTB.Text + "' WHERE finger_id = " + fingerLabel.Content;

                    using (var commandx = new SQLiteCommand(Query, connection))
                    {



                        SQLiteDataReader MyReader2;



                        MyReader2 = commandx.ExecuteReader();
                        ds.Clear();
                        dataGrid.ItemsSource = null;

                        MessageBox.Show("Data Updated");

                        while (MyReader2.Read())

                        {



                        }

                        connection.Close();//Connection closed here 

                    }
                }
            }

            catch (Exception ex)

            {



                MessageBox.Show(ex.Message);

            }
            try
            {

                using (var connection = new SQLiteConnection(connectionString))
                {

                    string Query = "select * from Patients;";


                    using (var command = new SQLiteCommand(Query, connection))
                    {


                        connection.Open();
                        //For offline connection we weill use  MySqlDataAdapter class.  
                        SQLiteDataAdapter MyAdapter = new SQLiteDataAdapter();
                        MyAdapter.SelectCommand = command;



                        MyAdapter.Fill(ds);
                        DataTable dT = ds.Tables[0];
                        DataView DV = new DataView(dT);
                        // DV.RowFilter = string.Format("last_name LIKE '%{0}%'", textBox.Text);
                        // dataGrid.Items.Refresh();
                        // dataGrid.ItemsSource = null;
                        dataGrid.ItemsSource = DV;
                        //  dataGrid.DataContext = ds; // here i have assigned dataset object to the dataGridView object to display data.               
                        connection.Close();
                    }
                }
            }
            catch (SQLiteException ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        private async void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UInt32 fingerID = Convert.ToUInt32(fingerLabel.Content);

                // Wait for the deleteFinger task to complete
                bool isDeleted = await deleteFinger(fingerID);

                if (isDeleted)
                {
                    using (var connection = new SQLiteConnection(connectionString))
                    {
                        string deleteQuery = "DELETE FROM Patients WHERE finger_id=@fingerID";
                        using (var deleteCommand = new SQLiteCommand(deleteQuery, connection))
                        {
                            deleteCommand.Parameters.AddWithValue("@fingerID", fingerID);
                            connection.Open();
                            deleteCommand.ExecuteNonQuery();
                            connection.Close();
                            MessageBox.Show("Data Deleted");
                        }

                        // Reload data into the DataSet
                        try
                        {
                            string selectQuery = "SELECT * FROM Patients";
                            using (var selectCommand = new SQLiteCommand(selectQuery, connection))
                            {
                                connection.Open();
                                SQLiteDataAdapter adapter = new SQLiteDataAdapter(selectCommand);
                                ds.Clear(); // Clear existing data before filling with new data
                                adapter.Fill(ds);
                                connection.Close();
                            }

                            // Update the DataGrid
                            DataTable dT = ds.Tables[0];
                            DataView DV = new DataView(dT);
                            dataGrid.ItemsSource = DV;
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }



        private void btnHome_Click(object sender, RoutedEventArgs e)
        {

            try
            {
                disconnect();
                Hide();
               // Home w1 = new Home();
               // w1.ShowDialog();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnReport_Click(object sender, RoutedEventArgs e)
        {

            disconnect();
            Hide();
            //Home w1 = new Home();
            //w1.ShowDialog();

        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {

        }

        private void textBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            DataTable dT = ds.Tables[0];
            DataView DV = new DataView(dT);
            DV.RowFilter = string.Format("surname LIKE '%{0}%'", textBox.Text);
            dataGrid.ItemsSource = DV;
        }

        private async void searchFingerX()
        {
            try
            {
                if (!comPort.IsOpen)
                {
                    MessageBox.Show(this, "Serial port is not open.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                TryFirstTake:
                comPort.Write(new byte[] { 0xEF, 0x1, 0xFF, 0xFF, 0xFF, 0xFF, 0x1, 0x0, 0x3, 0x1, 0x0, 0x5 }, 0, 12);
                //First finger stored in buffer
                await Task.Delay(500);
                byte[] buffer = new byte[12];
                comPort.Read(buffer, 0, 12);
                label2.Content = "Collecting and Storing First Fingerprint";
                label1.Content = "";
                int x = Convert.ToUInt16(buffer[9] + 1);

                switch (x)
                {
                    case 1:
                        comPort.Write(new byte[] { 0xEF, 0x1, 0xFF, 0xFF, 0xFF, 0xFF, 0x1, 0x0, 0x4, 0x2, 0x1, 0x0, 0x8 }, 0, 13);
                        await Task.Delay(700);
                        label2.Content = "Generating First Character";
                        label1.Content = "";
                        goto SecondTake;

                    default:
                        label2.Foreground = Brushes.Orange;
                        label2.Content = "... Waiting for Finger";
                        label1.Content = "";
                        goto TryFirstTake;
                }

                SecondTake:
                byte[] buffer1 = new byte[12];
                comPort.Read(buffer1, 0, 12);
                int y = Convert.ToUInt16(buffer1[9] + 1);

                switch (y)
                {
                    case 1:
                        label2.Content = "First Character successfully created";
                        label1.Foreground = Brushes.Green;
                        label1.Content = "";
                        await Task.Delay(200);
                        goto checkFinger;

                    case 2:
                        label2.Foreground = Brushes.Red;
                        label2.Content = "Error when receiving package, Try Again GenChar1";
                        label1.Content = "";
                        goto PlaceOfError;

                    case 7:
                        label1.Content = "";
                        label2.Foreground = Brushes.OrangeRed;
                        label2.Content = "fail to generate character file due to the over-disorderly fingerprint image";
                        goto PlaceOfError;

                    case 8:
                        label1.Content = "";
                        label2.Foreground = Brushes.OrangeRed;
                        label2.Content = "fail to generate character file due to lackness of character point or over-smallness of fingerprint image";
                        goto PlaceOfError;

                    default:
                        label1.Content = "";
                        label2.Foreground = Brushes.Orange;
                        label2.Content = "fail to generate the image for the lackness of valid primary image";
                        goto PlaceOfError;
                }

                checkFinger:
                searchFinger();
                if (clever_variable == 0)
                    goto PlaceOfError;
                else
                    goto PlaceOfJoy;

                PlaceOfError:
                MessageBox.Show("Finger Not Registered. Try Again or Get Registered");
                p = 0;
                PlaceOfJoy:

                if (p == 1)
                {
                    MessageBox.Show("Search Done");
                    btnFingerSearch.Content = "Search Again";
                }
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(this, $"Invalid operation: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (IOException ex)
            {
                MessageBox.Show(this, $"I/O error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Unexpected error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void searchFinger()
        {

            comPort.Write(new byte[] { 0xEF, 0x1, 0xFF, 0xFF, 0xFF, 0xFF, 0x1, 0x0, 0x8, 0x1B, 0x1, 0x0, 0x0, 0x0, 0xA3, 0x0, 0xC8 }, 0, 17);
            //See if finger isnot already available
            System.Threading.Thread.Sleep(700);
            byte[] bufferAYR = new byte[16];
            comPort.Read(bufferAYR, 0, 16);


            int x = Convert.ToUInt16(bufferAYR[9] + 1);
            string y = Convert.ToString(bufferAYR[10]);
            string z = Convert.ToString(bufferAYR[11]);
            string p = Convert.ToString(bufferAYR[12]);
            string q = Convert.ToString(bufferAYR[13]);


            // richTextBox2.AppendText(x + "");


            switch (x)
            {
                case 1:
                    label2.Foreground = Brushes.Red;
                    label1.Foreground = Brushes.DarkOrange;
                    string display = "Registered. FingerID: " + y + "" + z;
                    label2.Content = display;
                    string display2 = "Match Score: " + p + "" + q;
                    label1.Content = display2;
                    fingerLabel.Content = z;

                    fillUI();

                    //label31.Text = z;

                    clever_variable = 1;
                    break;


                case 2:
                    label2.Foreground = Brushes.Red;
                    label2.Content = "Error when receiving package";
                    label1.Content = "Try again";
                    clever_variable = 0;
                    break;

                default:

                    label2.Content = "Didnt find match";
                    label1.Content = "Kindly get registered";
                    clever_variable = 0;

                    break;

            }


        }


        private async Task<bool> deleteFinger(UInt32 fingerID)
        {
            if (!comPort.IsOpen)
            {
                MessageBox.Show(this, "Serial port is not open.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            try
            {
                byte higherByte = (byte)(fingerID >> 8);
                byte lowerByte = (byte)(fingerID & 0xFF);

                uint id2 = 21 + fingerID;
                byte checksum = Convert.ToByte(id2);

                comPort.Write(new byte[] { 0xEF, 0x1, 0xFF, 0xFF, 0xFF, 0xFF, 0x1, 0x0, 0x7, 0xC, higherByte, lowerByte, 0x0, 0x1, 0x0, checksum }, 0, 16);
                await Task.Delay(800);

                byte[] bufferAYR = new byte[12];
                comPort.Read(bufferAYR, 0, 12);

                int x = Convert.ToUInt16(bufferAYR[9] + 1);

                switch (x)
                {
                    case 1:
                        label2.Foreground = Brushes.Green;
                        label1.Foreground = Brushes.DarkOrange;
                        label2.Content = "Deleted Successfully";
                        label1.Content = "";
                        isDeleted = true;
                        return true;

                    case 2:
                        label2.Foreground = Brushes.Red;
                        label2.Content = "Error when receiving package";
                        label1.Content = "Try again";
                        isDeleted = false;
                        return false;

                    default:
                        label2.Content = "Fail to clear fingerprint";
                        label1.Content = "Try again";
                        isDeleted = false;
                        return false;
                }
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(this, $"Invalid operation: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            catch (IOException ex)
            {
                MessageBox.Show(this, $"I/O error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Unexpected error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }


        private void fillUI()
        {


            using (var connection = new SQLiteConnection(connectionString))
            {
                // Open the connection
                connection.Open();

                // Create a command
                using (var command = new SQLiteCommand(connection))
                {
                    // Set the command text to a SQL query
                    command.CommandText = "SELECT * FROM Patients WHERE finger_id = @fingerid";

                    // Add a parameter for the fingerid value
                    command.Parameters.AddWithValue("@fingerid", fingerLabel.Content);

                    // Execute the command and get a reader
                    using (var reader = command.ExecuteReader())
                    {
                        // Read the data from the reader
                        if (reader.Read())
                        {
                            // Assign the data to the textboxes
                            nameTB.Text = reader["name"].ToString();
                            surnameTB.Text = reader["surname"].ToString();
                            textBox.Text = surnameTB.Text;
                            idTB.Text = reader["id"].ToString();
                            sexTB.Text = reader["sex"].ToString();
                            phoneTB.Text = reader["phone"].ToString();
                            dobTB.Text = reader["dob"].ToString();
                            tpTB.Text = reader["tplan"].ToString();            
                            sessionsTB.Text = reader["sessions"].ToString();
                            ptTB.Text = reader["prevtreatments"].ToString();
                            diagnosisTB.Text = reader["diagnosis"].ToString();
                            oncologistTB.Text = reader["oncologist"].ToString();
                            hospitalNumberLabel.Content = reader["hospital_number"].ToString();
                            byte[] imageData = (byte[])reader["image_data"];
                            setImageFromBlob(imageData, imgBox);
                        }
                    }
                }
            }


        }

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            searchFingerX();
        }

       private void setImageFromDatagridClick(int fingerId)
        {
         

            using (var connection = new SQLiteConnection(connectionString))
            {
                // Open the connection
                connection.Open();

                // Create a command
                using (var command = new SQLiteCommand(connection))
                {
                    // Set the command text to a SQL query
                    command.CommandText = "SELECT image_data FROM Patients WHERE finger_id = @fingerid";

                    // Add a parameter for the fingerid value
                    command.Parameters.AddWithValue("@fingerid", fingerId);

                    // Execute the command and get a reader
                    using (var reader = command.ExecuteReader())
                    {
                        // Read the data from the reader
                        if (reader.Read())
                        {
                 
                           
                            byte[] imageData = (byte[])reader["image_data"];
                            setImageFromBlob(imageData, imgBox);
                        }
                    }
                }
            


        }
    }

public void setImageFromBlob(byte[] blobData, System.Windows.Controls.Image imageBox)
    {
        if (blobData == null || blobData.Length == 0)
        {
            // If the BLOB data is null or empty, clear the image box
            imageBox.Source = null;
            return;
        }

        // Convert the BLOB data to a BitmapImage
        BitmapImage bitmapImage = new BitmapImage();
        using (MemoryStream memoryStream = new MemoryStream(blobData))
        {
            memoryStream.Position = 0;
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = memoryStream;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
        }

        // Set the BitmapImage as the source of the Image control
        imageBox.Source = bitmapImage;
    }

}
}
