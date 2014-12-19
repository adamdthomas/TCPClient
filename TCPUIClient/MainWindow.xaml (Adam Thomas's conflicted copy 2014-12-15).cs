using SharpDX.DirectInput;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;



namespace TCPUIClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
               

   
    public partial class MainWindow : Window
    {
        #region Variables
        public static string ServerAddress;
        public static Int16 Port;
        public static string MyMessage;
        public static Socket Sender;
        public static byte[] bytes = new byte[1024];
        public static string data = "";
        public static char c1 = (char)10;
        public static string L = c1.ToString();
        public static bool CurrentlyConnected = false;
        public static Stopwatch watch = Stopwatch.StartNew();
        public static bool GamePadEnabled = false;
        public static bool cbOutputGPDataB = false;
        public static string GPID = "";
        public static string LogPath = @"C:\Logs\";
        public static string LogName = ("TCPClientLog-" + DateTime.Now.ToString("D") + ".txt").Replace(@"/",".").Replace(":",".");
        public static string LogFullPath = LogPath + LogName;
        public static Int16 DeadZone = 0;
        public static string ConfigPath = @"C:\Logs\config.txt";
        public static Dictionary<string, string> dicConfig = new Dictionary<string, string>();
        #endregion

                
        public MainWindow()
        {
            
            InitializeComponent();

        }

        public void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            Connect();
        }

        public void Connect()
        {
            if (CurrentlyConnected)
            {
                DeadZone = Int16.Parse(dicConfig["deadzone"]);
            }
            else
            {
                DeadZone = Int16.Parse(dicConfig["deadzone"]);
                try
                {
                    ServerAddress = txServername.Text;
                    Port = Int16.Parse(txPort.Text);
                    IPHostEntry ipHostInfo = Dns.Resolve(ServerAddress);
                    IPAddress ipAddress = ipHostInfo.AddressList[0];
                    IPEndPoint remoteEP = new IPEndPoint(ipAddress, Port);
                    Sender = new Socket(AddressFamily.InterNetwork,
                    SocketType.Stream, ProtocolType.Tcp);

                    // Connect the socket to the remote endpoint. Catch any errors.

                    Sender.SendTimeout = 1000;
                    Sender.ReceiveTimeout = 1000;
                    Sender.Connect(remoteEP);
                    WriteToLog("Socket connected to " + Sender.RemoteEndPoint.ToString());
                    CurrentlyConnected = true;
                    txStatus.Text = "Connected";
                }
                catch (Exception ex)
                {
                    string exc = ex.ToString();
                }
            }
        }

        private void btnDisconnect_Click(object sender, RoutedEventArgs e)
        {
            SetConfigData();
         try 
	       {	        
		         if (GamePadEnabled)
                {
                    cbGameEnabled.IsChecked = false;
                    WriteToLog("Gamepad disconnected.");
                    GamePadEnabled = false;
                }
                
                byte[] msg = Encoding.ASCII.GetBytes("<SERVERKILLCONNECTION>");
                // Send the data through the socket.
                int bytesSent = Sender.Send(msg);
                Sender.Shutdown(SocketShutdown.Both);
                Sender.Close();
                WriteToLog("Socket disconnected...");
                CurrentlyConnected = false;
                txStatus.Text = "Disconnected";

             	}
	            catch (Exception ec)
	            {
		             WriteToLog("Socket was already disconnected...");
	            }
            }

         #region ConfigData

        public void GetConfigData()
        {
            System.IO.Directory.CreateDirectory(LogPath);
            if (!File.Exists(ConfigPath))
            {
                // Create a file to write to. 
                using (StreamWriter sw = File.CreateText(ConfigPath))
                {
                    sw.Close();
                }
            }

            System.Collections.Generic.IEnumerable<String> lines = File.ReadLines(ConfigPath);
            

            foreach (var item in lines)
            {
                string[] KeyPair = item.Split('=');
                if (dicConfig.ContainsKey(KeyPair[0]))
                {
                    dicConfig[KeyPair[0]] = KeyPair[1];
                }
                else
                {
                    dicConfig.Add(KeyPair[0], KeyPair[1]);	  
                }
           
            }
        
        }

        public void LoadConfigToUI()
        {
            if (dicConfig["writetolog"] == "true")
            {
                cbOutputGPData.IsChecked = true;
            }
            else
            {
                cbOutputGPData.IsChecked = false;
            }
           
            txServername.Text = dicConfig["servername"];
            txPort.Text = dicConfig["port"];
            slDeadZone.Value = double.Parse(dicConfig["deadzone"]);


        }

        public void SetConfigData()
        {
            //Delete/erase old file
            File.WriteAllText(ConfigPath, String.Empty);
       
            using (StreamWriter configwriter = File.AppendText(ConfigPath))
            {             
                foreach (var pair in dicConfig)
                {
                    configwriter.WriteLine(pair.Key + "=" + pair.Value);
                }
                configwriter.Close();
            }
        }

        private void txServername_LostFocus(object sender, RoutedEventArgs e)
        {
            dicConfig["servername"] = txServername.Text;
        }

        private void txPort_TextChanged(object sender, TextChangedEventArgs e)
        {
            dicConfig["port"] = txPort.Text;
        }

        private void txDeadZone_TextChanged(object sender, TextChangedEventArgs e)
        {
          
        }

        private void cbOutputGPData_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void Grid_Initialized(object sender, EventArgs e)
        {
            GetConfigData();
            LoadConfigToUI();
        }

        #endregion

        private void txMessage_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        public void SendMessage()
        {
            txStatus.Text = "";
            watch.Reset();
            watch.Start();
            byte[] msg = Encoding.UTF8.GetBytes(txMessage.Text);
            data = "";
          

            if (CurrentlyConnected && !GamePadEnabled)
            {
                // Send the data through the socket.
                try
                {
                    if (txMessage.Text.IndexOf("*") > -1)
                    {
                        string[] qty = txMessage.Text.Split('*');
                        int SendQty = int.Parse(qty[1]);
                        byte[] msgs = Encoding.ASCII.GetBytes(qty[0]);
                       // byte[] msgs = Encoding.ASCII.GetBytes(qty[0]);
                        for (int i = 0; i < SendQty; i++)
                        {
                            int bytesSent = Sender.Send(msgs); 
                        }
                       
                    }
                    else
                    {
                        int bytesSent = Sender.Send(msg);
                    }
                  
                    int bytesRec = Sender.Receive(bytes);
                    data = Encoding.UTF8.GetString(bytes, 0, bytesRec);
                }
                catch (Exception ex)
                {

                    WriteToLog("Uh oh..." + ex.ToString());
                }

                if (data == "...")
                {
                    watch.Stop();
                    var elapsedMs = watch.Elapsed;
                    txStatus.Text = "Total Transaction Time: " + elapsedMs.ToString();
                }
                else
                {
                    txStatus.Text = "No response";

                }

                WriteToLog("Client Says: " + txMessage.Text);
                txMessage.Text = "";
            }
            else if (!GamePadEnabled)
            {
                WriteToLog("Server not connected!");
        
            }
            else if (GamePadEnabled)
            {
                WriteToLog("Cannot send messages while gamepad is connected!");
             
            }
        }

        public static void SendGPData()
        {
     
            if (CurrentlyConnected && GamePadEnabled)
            {
                try
                {


                using (StreamWriter sw = File.AppendText(LogFullPath))
                {
                    string GPD = "";
                    var MyJS = StartGamePad();
                    while (GamePadEnabled)
                    {
                        MyJS.Poll();
                        var datas = MyJS.GetBufferedData();
                        foreach (var state in datas)
                        {
                            GPD = GamePadDataFilter(state.ToString());
                            if (GPD != "")
                            {
                                if (cbOutputGPDataB)
                                {
                                    sw.WriteLine(GPD);
                                }
                                byte[] GPMsg = Encoding.ASCII.GetBytes(GPD);
                                int bytesSent = Sender.Send(GPMsg);
                            }
                         
                        }


                    }
                    sw.Close();
                }
                }
                catch (Exception sgp)
                {

                    sgp.ToString();
                }
            } 
        }

        private void txMessage_KeyDown(object sender, KeyEventArgs e)
        
        {
            if (e.Key == System.Windows.Input.Key.Return && txMessage.Text != "")
            {
                SendMessage();

             }
        }

        private void lstMaine_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            SendMessage();
        }

       public static string GamePadDataFilter(string DataToFilter)
        {
            string s = DataToFilter;
            string[] cmds = s.Split(' ');
            string ButLabel = cmds[1].Replace(",", "");
            int ButVal = int.Parse(cmds[3]);
            DataToFilter = ButLabel + ":" + ButVal;
            string IO = "";
            double ButValDec;

            switch (ButLabel)
            {
                case "Sliders0":
                case "Sliders1":
                case "RotationZ":
                case "X":
                case "Y":
                case "Z":
                    if (ButVal < (30750 + (DeadZone / 2)) && ButVal > (30750 - (DeadZone / 2)))
                    {
                        DataToFilter = "";
                    }
                    else
                    {
                        ButValDec = ButVal / 361.11;
                        ButValDec = Math.Round(ButValDec, 0);
                        DataToFilter = ButLabel + ":" + ButValDec.ToString() + "~";
                    }
                    break;
                case "Buttons0":
                case "Buttons1":
                case "Buttons2":
                case "Buttons3":
                case "Buttons4":
                case "Buttons5":
                case "Buttons6":
                case "Buttons7":
                case "Buttons8":
                case "Buttons9":
                case "Buttons10":
                case "Buttons11":
                case "Buttons12":
                case "Buttons13":
                case "Buttons14":
                case "Buttons15":
                    {
                        if (ButVal == 0)
                        {
                            IO = "OFF";
                        }
                        else
                        {
                            IO = "ON";
                        }
                        DataToFilter = ButLabel + ":" + IO + "~";
                        break;
                    }
                default:
                    {
                        DataToFilter = "";
                        break;
                    }
            }
            return DataToFilter;
        }

        public static Joystick StartGamePad()
      {
            // Initialize DirectInput
            var directInput = new DirectInput();

            // Find a Joystick Guid
            var joystickGuid = Guid.Empty;

            foreach (var deviceInstance in directInput.GetDevices(DeviceType.Gamepad,
                        DeviceEnumerationFlags.AllDevices))
                joystickGuid = deviceInstance.InstanceGuid;

            // If Gamepad not found, look for a Joystick
            if (joystickGuid == Guid.Empty)
                foreach (var deviceInstance in directInput.GetDevices(DeviceType.Joystick,
                        DeviceEnumerationFlags.AllDevices))
                    joystickGuid = deviceInstance.InstanceGuid;

            // If Joystick not found, throws an error
            if (joystickGuid == Guid.Empty)
            {
                MainWindow.GPID = "No Gamepad found.";
                Environment.Exit(1);
            }

            // Instantiate the joystick
            var joystick = new Joystick(directInput, joystickGuid);

            MainWindow.GPID = "Gamepad GUID: " + joystickGuid;

            // Query all suported ForceFeedback effects
            var allEffects = joystick.GetEffects();
            foreach (var effectInfo in allEffects)
                Console.WriteLine("Effect available {0}", effectInfo.Name);

            // Set BufferSize in order to use buffered data.
            joystick.Properties.BufferSize = 128;

            // Acquire the joystick
            joystick.Acquire();

            // Poll events from joystick
          //  while (true)
           // {
            //    joystick.Poll();
            //    var datas = joystick.GetBufferedData();
              //  foreach (var state in datas)
                //    Console.WriteLine(state);
            //}
         
            return joystick;
        }

        public void WriteToLog(string Message)
        {
            txMain.AppendText(Message + L);
            txMain.ScrollToEnd();
            if (cbOutputGPDataB && !GamePadEnabled)
            {
                try
                {
                 using (StreamWriter sw = File.AppendText(LogFullPath))
                {
                    sw.WriteLine(Message);
                    sw.Close();
                }
                }
                catch (Exception l)
                {
                    l.ToString();
                }
               
            }
        }

        private void cbGameEnabled_Click(object sender, RoutedEventArgs e)
        {
            GamePadEnabled = cbGameEnabled.IsChecked.Value;

            if (CurrentlyConnected)
            {


                if (GamePadEnabled)
                {
                    txStatus.Text = "Gamepad Connected!";
                    //  MessageBox.Show("Test GP");

                    Thread GPThread = new Thread(MainWindow.SendGPData);
                    GPThread.Start();
                    Thread.Sleep(1000);
                    WriteToLog(GPID);

                }
                else


                    txStatus.Text = "Gamepad Disconnected!";
            }
            else
            {
                cbGameEnabled.IsChecked = false;
                WriteToLog("Please connect to the server before connecting a gamepad.");
                txStatus.Text = "Warning!";
            }
        }

        private void cbOutputGPData_Click(object sender, RoutedEventArgs e)
        {
            dicConfig["writetolog"] = "true";
            cbOutputGPDataB = cbOutputGPData.IsChecked.Value;
            if (cbOutputGPDataB)
            {
                System.IO.Directory.CreateDirectory(LogPath);
                if (!File.Exists(LogFullPath))
                {
                    // Create a file to write to. 
                    using (StreamWriter sw = File.CreateText(LogFullPath))
                    {
                        sw.WriteLine(DateTime.Now.ToString());
                        sw.Close();
                    }
                }

             
                WriteToLog("Logging to file: " + LogFullPath);
            }
        }

        private void txDeadZone_LostFocus(object sender, RoutedEventArgs e)
        {
            
        }

        private void txServername_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Return)
            {
                Connect();
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            txMain.Text = "";
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            DeadZone = Int16.Parse(Math.Round(slDeadZone.Value, 0).ToString());
            txStatus.Text = "DeadZone set to: " + Math.Round(slDeadZone.Value, 0).ToString();
            dicConfig["deadzone"] = Math.Round(slDeadZone.Value,0).ToString();
        }

            
        }

      
    }



        public static class StringCipher
        {
            // This constant string is used as a "salt" value for the PasswordDeriveBytes function calls.
            // This size of the IV (in bytes) must = (keysize / 8).  Default keysize is 256, so the IV must be
            // 32 bytes long.  Using a 16 character string here gives us 32 bytes when converted to a byte array.
            private static readonly byte[] initVectorBytes = Encoding.ASCII.GetBytes("jfhur9482kdlkj45");

            // This constant is used to determine the keysize of the encryption algorithm.
            private const int keysize = 256;

            public static string Encrypt(string plainText, string passPhrase)
            {
                byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);
                using (PasswordDeriveBytes password = new PasswordDeriveBytes(passPhrase, null))
                {
                    byte[] keyBytes = password.GetBytes(keysize / 8);
                    using (RijndaelManaged symmetricKey = new RijndaelManaged())
                    {
                        symmetricKey.Mode = CipherMode.CBC;
                        using (ICryptoTransform encryptor = symmetricKey.CreateEncryptor(keyBytes, initVectorBytes))
                        {
                            using (MemoryStream memoryStream = new MemoryStream())
                            {
                                using (CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                                {
                                    cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                                    cryptoStream.FlushFinalBlock();
                                    byte[] cipherTextBytes = memoryStream.ToArray();
                                    return Convert.ToBase64String(cipherTextBytes);
                                }
                            }
                        }
                    }
                }
            }

            public static string Decrypt(string cipherText, string passPhrase)
            {
                byte[] cipherTextBytes = Convert.FromBase64String(cipherText);
                using (PasswordDeriveBytes password = new PasswordDeriveBytes(passPhrase, null))
                {
                    byte[] keyBytes = password.GetBytes(keysize / 8);
                    using (RijndaelManaged symmetricKey = new RijndaelManaged())
                    {
                        symmetricKey.Mode = CipherMode.CBC;
                        using (ICryptoTransform decryptor = symmetricKey.CreateDecryptor(keyBytes, initVectorBytes))
                        {
                            using (MemoryStream memoryStream = new MemoryStream(cipherTextBytes))
                            {
                                using (CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                                {
                                    byte[] plainTextBytes = new byte[cipherTextBytes.Length];
                                    int decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
                                    return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount);
                                }
                            }
                        }
                    }
                }
            }
        }




