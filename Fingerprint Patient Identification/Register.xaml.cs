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
using System.Windows.Threading;
using Newtonsoft.Json;
using System.Data.SQLite;
using System.Configuration;
using System.Drawing;
using System.IO;
using Microsoft.Win32;

namespace Fingerprint_Patient_Identification
{
    /// <summary>
    /// Interaction logic for Register.xaml
    /// </summary>
    public partial class Register : Window
    {
        private SerialPort ComPort = new SerialPort(); //Initialise ComPort Variable as SerialPort
        string Query;
        long lastInsertedId;
        string indata2;
        int clever_variable;
        int p;
        int Fing_ID;

      


        string connectionString = ConfigurationManager.ConnectionStrings["patientsDb"].ConnectionString;
        private string imageName;
        private byte[] imageData;



        public Register()
        {
            InitializeComponent();
        }


        private void Register_Loaded(object sender, RoutedEventArgs e)
        {
            checkForDb();
            updatePorts();

        }
        private void updatePorts()
        {
            string[] ports = SerialPort.GetPortNames();
            foreach (string port in ports)
            {
                comboBox.Items.Add(port);
            }
        }
       private void checkForDb()
        {
            // Check if the database file exists, create it if not
            if (!File.Exists("patientsDb.db"))
            {
                // Create a new SQLite database file
                SQLiteConnection.CreateFile("patientsDb.db");

                // Define the connection string for the SQLite database


                // Define SQL query to create the Patients table
                string createTableSql = "CREATE TABLE Patients (" +
                                        "finger_id INTEGER PRIMARY KEY AUTOINCREMENT," +
                                        "hospital_number TEXT," +
                                        "name TEXT," +
                                        "surname TEXT," +
                                        "sex TEXT," +
                                        "dob TEXT," +
                                        "tplan TEXT," +
                                        "phone TEXT," +
                                        "sessions TEXT," +
                                        "prevtreatments TEXT," +
                                        "diagnosis TEXT," +
                                        "id TEXT," +
                                        "oncologist TEXT," +
                                        "image_name TEXT," +
                                        "image_data BLOB)";

                // Create the Patients table
                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();
                    using (var command = new SQLiteCommand(createTableSql, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                    connection.Close();
                }
            }
        }

        private void insertToDB()
        {
            //checkForDb();

            // Insert data into the Patients table
            string insertDataSql = "INSERT INTO Patients (name, surname, sex, dob, tplan, phone, sessions, prevtreatments, diagnosis, image_name, image_data, id, oncologist  ) " +
                                    "VALUES (@name, @surname, @sex, @dob, @tplan, @phone, @sessions, @prevtreatments, @diagnosis, @image_name, @image_data, @id, @oncologist)";

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(insertDataSql, connection))
                {
                    command.Parameters.AddWithValue("@name", nameTB.Text);
                    command.Parameters.AddWithValue("@surname", surnameTB.Text);
                    command.Parameters.AddWithValue("@sex", sexCB.Text);
                    command.Parameters.AddWithValue("@dob", datePicker.Text);
                    command.Parameters.AddWithValue("@tplan", tpTB.Text);
                    command.Parameters.AddWithValue("@phone", phoneTB.Text);
                    command.Parameters.AddWithValue("@sessions", sessionsTB.Text);
                    command.Parameters.AddWithValue("@prevtreatments", ptTB.Text);
                    command.Parameters.AddWithValue("@diagnosis", diagnosisTB.Text);
                    command.Parameters.AddWithValue("@image_name", imageName);
                    command.Parameters.AddWithValue("@image_data", imageData);
                    command.Parameters.AddWithValue("@id", idTB.Text);
                    command.Parameters.AddWithValue("@oncologist", oncologistTB.Text);


                    command.ExecuteNonQuery();
                }

                // Retrieve the last inserted ID
                string selectSql = "SELECT last_insert_rowid()";
                using (var cmd = new SQLiteCommand(selectSql, connection))
                {
                    string lastInsertedIdString = cmd.ExecuteScalar().ToString();
                    fingerIdLabel.Content = lastInsertedIdString;
                    if (!string.IsNullOrEmpty(lastInsertedIdString))
                    {
                        lastInsertedId = long.Parse(lastInsertedIdString);
                    }
                    else
                    {
                        lastInsertedId = 0;
                    }
          
                }

                // Generate hospital_number based on lastInsertedId
                string hospitalNumber = "PTH" + lastInsertedId;
                hospitalNumberLabel.Content = hospitalNumber;
               

                // Update hospital_number in the database
                string updateHospitalNumberSql = "UPDATE Patients SET hospital_number = @hospital_number WHERE finger_id = @lastInsertedId";
                using (var updateCmd = new SQLiteCommand(updateHospitalNumberSql, connection))
                {
                    updateCmd.Parameters.AddWithValue("@hospital_number", hospitalNumber);
                    updateCmd.Parameters.AddWithValue("@lastInsertedId", lastInsertedId);
                    updateCmd.ExecuteNonQuery();
                }

                connection.Close();
            }
        }

    


    private void connect()
        {
            bool error = false;

            // Check if all settings have been selected

            if (comboBox.SelectedIndex != -1)
            {  //if yes then Set The Port's settings
                ComPort.PortName = comboBox.Text;
                ComPort.BaudRate = 57600;
                ComPort.Parity = Parity.None;

                ComPort.DataBits = 8;
                ComPort.StopBits = StopBits.One;

                try //always try to use this try and catch method to open your port.
                    //if there is an error your program will display a message instead of freezing.
                {
                    //Open Port
                    ComPort.Open();

                    btnCollect.IsEnabled = true;
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
            if (ComPort.IsOpen)
            {
                btnConnect.Content = "Disconnect";

            }


        }

        // Call this function to close the port.
        private void disconnect()
        {
            ComPort.Close();
            btnConnect.Content = "Connect";

            btnCollect.IsEnabled = false;

        }
        //whenever the connect button is clicked, it will check if the port is already open, call the disconnect function.
        // if the port is closed, call the connect function.

        private void btnBack_Click(object sender, EventArgs e)
        {
          //  Hide();
           // Home f2 = new Home();
           // f2.ShowDialog();
        }

        private void btnHome_Click(object sender, EventArgs e)
        {
           // Hide();
            //Home f2 = new Home();
            //f2.ShowDialog();
        }

        private void btnCollect_Click(object sender, RoutedEventArgs e)
        {
            // Check if any of the required fields are empty
            if (nameTB.Text.Length == 0 ||
                surnameTB.Text.Length == 0 ||
                datePicker.Text.Length == 0 ||
                idTB.Text.Length == 0 ||
                sexCB.Text.Length == 0 ||
                tpTB.Text.Length == 0 ||
                phoneTB.Text.Length == 0 ||
                sessionsTB.Text.Length == 0 || 
                ptTB.Text.Length == 0 || 
                diagnosisTB.Text.Length == 0 ||
                oncologistTB.Text.Length == 0) 
            {
                MessageBox.Show("Please fill all entries");
                return; // Exit the method if any field is empty
            }

            try
            {
                enroll();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ComPort.IsOpen)
                {
                    try
                    {
                        disconnect();
                        statusLabel.Foreground = Brushes.Red;
                        statusLabel.Content = "System Offline";
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, $"Error disconnecting: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
                else
                {
                    try
                    {
                        connect();
                        statusLabel.Foreground = Brushes.Green;
                        statusLabel.Content = "System Online";
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, $"Error connecting: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

              
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Unexpected error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void btnHome_Click(object sender, RoutedEventArgs e)
        {
            //  disconnect();
            // Hide();
            //Home w1 = new Home();
            //w1.ShowDialog();
           // statusLabel.Content = "Hello";
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
           disconnect();
            Hide();
            Search w1 = new Search();
            w1.ShowDialog(); 
        }

        public void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {

            indata2 = ComPort.ReadExisting();
            Dispatcher.Invoke(new EventHandler(ShowData));

        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            comboBox.Items.Clear();
            string[] ports = SerialPort.GetPortNames();
            foreach (string port in ports)
            { 
            comboBox.Items.Add(port);
            }
        }

        private void btnUpload_Click(object sender, EventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "Image Files (*.png;*.jpg;*.jpeg;*.gif;*.bmp)|*.png;*.jpg;*.jpeg;*.gif;*.bmp";
            try
            {
                if (openFileDialog.ShowDialog() == true)
                {
                    imageName = System.IO.Path.GetFileName(openFileDialog.FileName);
                    imageData = File.ReadAllBytes(openFileDialog.FileName);

                    // Update statusLabel on the UI thread
                    Dispatcher.Invoke(() =>
                    {
                       label2.Content = $"{imageName} ({imageData.Length} bytes)";
                    });

                    // Convert byte array to BitmapImage
                    BitmapImage bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = new MemoryStream(imageData);
                    bitmapImage.EndInit();

                    // Display the image in imgBox
                    imgBox.Source = bitmapImage;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }





        private void ShowData(object sender, EventArgs e)
        {
       

        }
        private async void areYouRegistered()
        {

            ComPort.Write(new byte[] { 0xEF, 0x1, 0xFF, 0xFF, 0xFF, 0xFF, 0x1, 0x0, 0x8, 0x1B, 0x1, 0x0, 0x0, 0x0, 0xA3, 0x0, 0xC8 }, 0, 17);
            //See if finger isnot already available
            await Task.Delay(700);
            byte[] bufferAYR = new byte[16];
            ComPort.Read(bufferAYR, 0, 16);


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
                    string display = "Already Registered. FingerID: " + y + "" + z;
                    label2.Content = display;
                    string display2 = "Match Score: " + p + "" + q;
                    label1.Content = display2;
                    fingerIdLabel.Content = z;


                    //label31.Text = z;

                    clever_variable = 1;
                    break;


                case 2:
                    label2.Foreground = Brushes.Red;
                    label2.Content = "Error when receiving package";
                    label1.Content = "Are You Registered Package";
                    clever_variable = 1;
                    break;

                default:
                    clever_variable = 0;

                    break;

            }


        }


        private async void enroll()
        {
            btnCollect.Content = "Registering";
            int count = -1;
            TryFirstTake:
            ComPort.Write(new byte[] { 0xEF, 0x1, 0xFF, 0xFF, 0xFF, 0xFF, 0x1, 0x0, 0x3, 0x1, 0x0, 0x5 }, 0, 12);
            //First finger stored in buffer
            await Task.Delay(500);
            byte[] buffer = new byte[12];
            ComPort.Read(buffer, 0, 12);
            //label2.Content = "Collecting and Storing First Fingerprint";
            int x = Convert.ToUInt16(buffer[9] + 1);
            // richTextBox2.AppendText(x + "");


            switch (x)
            {
                case 1:
                    ComPort.Write(new byte[] { 0xEF, 0x1, 0xFF, 0xFF, 0xFF, 0xFF, 0x1, 0x0, 0x4, 0x2, 0x1, 0x0, 0x8 }, 0, 13);
                    //generate first character from first finger take stored in buffer 
                    await Task.Delay(700);
                    label2.Content = "Generating First Character";


                    goto SecondTake;


                case 2:


                    label2.Foreground = Brushes.Red;
                    label2.Content = "Error when receiving package, First Take";
                    goto PlaceOfError;
                case 3:
                    label2.Foreground = Brushes.Orange;

                    if (count == 0) label2.Content = "Waiting for the finger.";
                    else if (count == 1) label2.Content = "Waiting for the finger..";
                    else if (count == 2) label2.Content = "Waiting for the finger...";
                    else if (count == 3) label2.Content = "Waiting for the finger....";
                    else count = -1 ;
                    count++;



                    goto TryFirstTake;
                default:
                    label2.Foreground = Brushes.Orange;
                    label2.Content = "Failed to collect finger, First Take";
                    goto PlaceOfError;

            }



            SecondTake:
            byte[] buffer1 = new byte[12];
            ComPort.Read(buffer1, 0, 12);
            int y = Convert.ToUInt16(buffer1[9] + 1);
            switch (y)
            {
                case 1:
                    label2.Content = "First Character successfully created";
                    label1.Foreground = Brushes.Green;
                    label1.Content = "Success, Insert Finger Again";
                    await Task.Delay(2000);

                    ComPort.Write(new byte[] { 0xEF, 0x1, 0xFF, 0xFF, 0xFF, 0xFF, 0x1, 0x0, 0x3, 0x1, 0x0, 0x5 }, 0, 12);
                    label2.Content = "Waiting For Second Finger";
                    label1.Content = "";
                    //Second Finger Take to Buffer
                    await Task.Delay(500);
                    goto GenerateSecChar;
                case 2:
                    label2.Foreground = Brushes.Red;
                    label2.Content = "Error when receiving package, Try Again GenChar1";
                    goto PlaceOfError;
                case 7:
                    label2.Foreground = Brushes.OrangeRed;
                    label2.Content = "fail to generate character file due to the over-disorderly fingerprint image";
                    goto PlaceOfError;
                case 8:
                    label2.Foreground = Brushes.OrangeRed;
                    label2.Content = "fail to generate character file due to lackness of character point or over-smallness of fingerprint image";
                    goto PlaceOfError;
                default:
                    label2.Foreground = Brushes.Orange;
                    label2.Content = "fail to generate the image for the lackness of valid primary image";
                    goto PlaceOfError;

            }
            GenerateSecChar:
            byte[] buffer2 = new byte[12];
            ComPort.Read(buffer2, 0, 12);
            int z = Convert.ToUInt16(buffer[9] + 1);
            switch (z)
            {
                case 1:
                    ComPort.Write(new byte[] { 0xEF, 0x1, 0xFF, 0xFF, 0xFF, 0xFF, 0x1, 0x0, 0x4, 0x2, 0x2, 0x0, 0x9 }, 0, 13);
                    //generate second character from second finger take stored in  buffer 
                    label2.Content = "Generating Second Character";
                    await Task.Delay(700);

                    goto GenerateModel;

                case 2:
                    label2.Foreground = Brushes.Red;
                    label2.Content = "Error when receiving package, Second Take";
                    goto PlaceOfError;
                case 3://If no finger is detected resend till chigunwe chabatwa;
                    ComPort.Write(new byte[] { 0xEF, 0x1, 0xFF, 0xFF, 0xFF, 0xFF, 0x1, 0x0, 0x3, 0x1, 0x0, 0x5 }, 0, 12);
                    //Second Finger Take to Buffer
                    await Task.Delay(500);
                    goto GenerateSecChar;
                default:
                    label2.Foreground = Brushes.Orange;
                    label2.Content = "Failed to collect finger, Try Again";
                    goto PlaceOfError;

            }
            GenerateModel:
            byte[] buffer3 = new byte[12];
            ComPort.Read(buffer3, 0, 12);
            int j = Convert.ToUInt16(buffer3[9] + 1);

            switch (z)
            {
                case 1:


                    ComPort.Write(new byte[] { 0xEF, 0x1, 0xFF, 0xFF, 0xFF, 0xFF, 0x1, 0x0, 0x3, 0x5, 0x0, 0x9 }, 0, 12);
                    //generating finger model from char1 and char2
                    label2.Content = "Generating Finger Model";
                    await Task.Delay(700);


                    goto StoreSinhi;

                /* case 2:
                     label23.ForeColor = System.Drawing.Color.Red;
                     label23.Text = "Error when receiving package, Try Again";
                     goto PlaceOfError;
                 case 3:
                     label23.ForeColor = System.Drawing.Color.OrangeRed;
                     label23.Text = "Can’t detect finger, Try Again";
                     goto PlaceOfError;*/
                default:
                    label2.Foreground = Brushes.Orange;
                    label2.Content = "Error generating sec char";
                    goto PlaceOfError;

            }


            StoreSinhi:
            byte[] buffer4 = new byte[12];
            ComPort.Read(buffer4, 0, 12);
            int s = Convert.ToUInt16(buffer4[9] + 1);
            switch (s)
            {
                case 1:
                    // statusStrip1.Text = "Successful";
                    areYouRegistered();
                    if (clever_variable == 1)
                    {
                        goto PlaceOfError;
                    }
                    else
                    {


                    }

                    try
                    {
                        insertToDB();


                        int id = Convert.ToInt32(lastInsertedId);
                        fingerIdLabel.Content = Convert.ToString(id);
                        byte idToFinger = Convert.ToByte(id);


                        int id2 = 14 + id;
                        byte checksum = Convert.ToByte(id2);
                        ComPort.Write(new byte[] { 0xEF, 0x1, 0xFF, 0xFF, 0xFF, 0xFF, 0x1, 0x0, 0x6, 0x6, 0x1, 0x0, idToFinger, 0x0, checksum }, 0, 15);
                        //takustowa manje NB Byte 12 and 13 are the Location to sto the finger
                        label2.Content = "Storing Fingerprint Model To Flash";
                        await Task.Delay(700);

                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                        goto PlaceOfError;
                    }








                    //cmd.ExecuteNonQuery();

                    // Return the id of the new record. Convert from Int64 to Int32 (int).




                    goto FinalResult;

                /* case 2:
                     label23.ForeColor = System.Drawing.Color.Red;
                     label23.Text = "Error when receiving package, Try Again";
                     goto PlaceOfError;
                 case 3:
                     label23.ForeColor = System.Drawing.Color.OrangeRed;
                     label23.Text = "Can’t detect finger, Try Again";
                     goto PlaceOfError;*/
                default:
                    label2.Foreground = Brushes.Orange;
                    label2.Content = "Error generating Model";
                    goto PlaceOfError;

            }

            FinalResult:
            byte[] buffer5 = new byte[12];
            ComPort.Read(buffer4, 0, 12);
            int k = Convert.ToUInt16(buffer4[9] + 1);
            switch (k)
            {
                case 1:
                    //statusStrip1.Text = "Successfully Stored, Saved To Database";

                    label2.Foreground = Brushes.Green;
                    label2.Content = "Successfully Stored";
                    p = 1;
                    goto PlaceOfJoy;





                case 2:
                    label2.Foreground = Brushes.Red;
                    label2.Content = "Error when receiving package, Storing";
                    goto PlaceOfError;
                case 25:
                    label2.Foreground = Brushes.OrangeRed;
                    label2.Content = "Error writing flash, Try Again";
                    goto PlaceOfError;
                default:
                    label2.Foreground = Brushes.Orange;
                    label2.Content = "Error storing Model";
                    goto PlaceOfError;

            }
            PlaceOfError:
            btnCollect.Content = "Try Again";
            p = 0;
            PlaceOfJoy:

            if (p == 1)
            {
                MessageBox.Show("Saved Data");
                btnCollect.Content = "Register Again"; 
            }

        }



    }
}
