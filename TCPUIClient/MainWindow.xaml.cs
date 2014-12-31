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
        public static Int32 Port;
        public static string MyMessage;

        public static Socket MainSocket;
        public static Socket GamePadSocket;
        public static Socket GamePadSocketUDP;
        public static Socket VideoSocket;
        public static Socket AudioFromMCSocket;
        public static Socket AudioToMCSocket;

        public static IPEndPoint GameUDPEndPoint;

        public static byte[] bytes = new byte[1024];
        public static string data = "";
        public static char c1 = (char)10;
        public static string L = c1.ToString();
        
        public static bool CurrentlyConnected = false;
        public static bool GamePadEnabled = false;
        public static bool cbOutputGPDataB = false;
        public static bool TranslateGPD = false;
        public static bool VideoEnabled = false;
        
        public static Stopwatch watch = Stopwatch.StartNew();

        public static string GPID = "";
        public static string LogPath = @"C:\Logs\";
        public static string VideoBat = @"C:\Logs\vid.bat";
        public static string LogName = ("TCPClientLog-" + DateTime.Now.ToString("D") + ".txt").Replace(@"/",".").Replace(":",".");
        public static string LogFullPath = LogPath + LogName;
        public static string cmd;

        public static Int32 DeadZone = 0;
        public static Int32 txRate = 0;
        public static Int32 Center = 0;
        public static string ConfigPath = @"C:\Logs\config.txt";
        public static Dictionary<string, string> dicConfig = new Dictionary<string, string>();


        #endregion

                
        public MainWindow()
        {
            
            InitializeComponent();

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

                //Load defaults for new machine
                dicConfig["writetolog"] = "true";
                dicConfig["translategpd"] = "true";
                dicConfig["servername"] = "www.thomasworkshop.com";
                dicConfig["port"] = "3333";
                dicConfig["deadzone"] = "7500";
                dicConfig["txrate"] = "0";
                dicConfig["center"] = "30750";
                dicConfig["gamepadmode"] = "0";
                SetConfigData();

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
            if (dicConfig["writetolog"].ToUpper() == "TRUE")
            {
                cbOutputGPDataB = true;
                cbOutputGPData.IsChecked = true;
            }
            else
            {
                cbOutputGPDataB = false;
                cbOutputGPData.IsChecked = false;
            }

            if (dicConfig["translategpd"].ToUpper() == "TRUE")
            {
                TranslateGPD = true;
                cbTranslate.IsChecked = true;
            }
            else
            {
                TranslateGPD = false;
                cbTranslate.IsChecked = false;
            }
           
            //Set UI
            txServername.Text = dicConfig["servername"];
            txPort.Text = dicConfig["port"];
            slDeadZone.Value = double.Parse(dicConfig["deadzone"]);
            slTXRate.Value = double.Parse(dicConfig["txrate"]);
            slCenter.Value = double.Parse(dicConfig["center"]);
            cbGamepadType.SelectedIndex = int.Parse(dicConfig["gamepadmode"]);
            ShowGamePadAdvancedControls(false);


            //Set Variables
            DeadZone = Int32.Parse(dicConfig["deadzone"]);
            txRate = Int32.Parse(dicConfig["txrate"]);
            Center = Int32.Parse(dicConfig["center"]);

            ShowGamePadAdvancedControls(false);
            
            

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

       

        private void Grid_Initialized(object sender, EventArgs e)
        {
            GetConfigData();
            LoadConfigToUI();
        }

        #endregion

        #region VideoFunctions

        public void RunVideo()
        {
            if (CurrentlyConnected)
            {
                try
                {
                    cmd = "<VIDEOINFO>"; //vid cmd
                    byte[] msg = Encoding.UTF8.GetBytes(cmd);
                    data = "";
                    int bytesSent = MainSocket.Send(msg);
                    WriteToLog("Client says: " + cmd);
                    int bytesRec = MainSocket.Receive(bytes);
                    data = Encoding.UTF8.GetString(bytes, 0, bytesRec);
                    WriteToLog("Server says: " + data);
                    string[] AddressParts = data.Split(':');
                    string VideoCMDString = @"cd c:\gstreamer\1.0\x86\bin & gst-launch-1.0 -v tcpclientsrc host=" + AddressParts[0] + " port=" + AddressParts[1] + "  ! gdpdepay !  rtph264depay ! avdec_h264 ! videoconvert ! videoflip method=horizontal-flip ! autovideosink sync=false";
                    RunCMD(VideoCMDString);
                    WriteToLog("Attempting to connect to video server now...");

                }
                catch (Exception ex)
                {
                    txStatus.Text = ex.ToString();
                    WriteToLog("Poo... Something bad happened when I tried to pull the video stream. Maybe someone doesn't want to be watched?");
                }
            }
            else
            {
                txStatus.Text = "Warning!";
                WriteToLog("You must be connected to the main server before you can stream video!");
            }
        }

        public void DisconnectVideo()
        {
            try
            {
                //Test Github
                cmd = "<VIDEOKILL>";
                byte[] msg = Encoding.UTF8.GetBytes(cmd);
                data = "";
                int bytesSent = MainSocket.Send(msg);
                WriteToLog("Client says: " + cmd);
                int bytesRec = MainSocket.Receive(bytes);
                data = Encoding.UTF8.GetString(bytes, 0, bytesRec);
                WriteToLog("Server says: " + data);
                KillProcessByName("cmd.exe");
                txStatus.Text = "Video discconected successfully!";
            }
            catch (Exception ex)
            {
                txStatus.Text = ex.ToString();
                WriteToLog("Awwww snap... Could not stop the video feed. Are we still connected to the main server?");
            }

            cbVideo.IsChecked = false;

        }

        #endregion

        #region GamePadFunctions

        public void RunGamePad()
        {
            GamePadEnabled = cbGameEnabled.IsChecked.Value;

            //Ask Server for IP and port

            //<GAMEPADINFO>




            if (CurrentlyConnected)
            {
                if (GamePadEnabled)
                {
                    txStatus.Text = "Gamepad Connected!";
                    //  MessageBox.Show("Test GP");
                    //Thread GPThread = new Thread(new ThreadStart(MainWindow.SendGPData));
                    Thread GPThread = new Thread(MainWindow.SendGPData);
                    GPThread.Start();
                    Thread.Sleep(1000);
                    WriteToLog(GPID);
                    
                }
                else
                {
                   
                    txStatus.Text = "Gamepad Disconnected!";
                }
            }
            else
            {
                cbGameEnabled.IsChecked = false;
                WriteToLog("Please connect to the server before connecting a gamepad.");
                txStatus.Text = "Warning!";
            }
        }

        public void RunGamePadIndependent()
        {
            GamePadEnabled = cbGameEnabled.IsChecked.Value;

            //Ask Server for IP and port
            try
            {
                //<GAMEPADINFO>
                cmd = "<GAMEPADINFO>";
                byte[] msg = Encoding.UTF8.GetBytes(cmd);
                data = "";
                int bytesSent = MainSocket.Send(msg);
                WriteToLog("Client says: " + cmd);
                int bytesRec = MainSocket.Receive(bytes);
                data = Encoding.UTF8.GetString(bytes, 0, bytesRec);
                WriteToLog("Server says: " + data);
                string[] AddressParts = data.Split(':');
                GamePadSocket = ConnectIndependent(AddressParts[0], int.Parse(AddressParts[1]));
                WriteToLog("Attempting to connect to gamepad server now...");
                

                if (CurrentlyConnected)
                {
                    if (GamePadEnabled)
                    {
                        //  MessageBox.Show("Test GP");
                        //Thread GPThread = new Thread(new ThreadStart(MainWindow.SendGPData));
                        Thread GPThread = new Thread(MainWindow.SendGPDataIndependent);
                        GPThread.Start();
                        Thread.Sleep(1000);
                        WriteToLog(GPID);

                        WriteToLog("Gamepad connected to: " + data);
                        txStatus.Text = "Gamepad Connected!";
                    }
                    else
                    {

                        txStatus.Text = "Gamepad Disconnected!";
                    }
                }
                else
                {
                    cbGameEnabled.IsChecked = false;
                    WriteToLog("Please connect to the server before connecting a gamepad.");
                    txStatus.Text = "Warning!";
                }
            }
            catch (Exception ec)
            {

                WriteToLog("Gamepad thread failed to connect.");
                txStatus.Text = ec.ToString();
            }
        }

        public void RunGamePadIndependentUDP()
        {
            GamePadEnabled = cbGameEnabled.IsChecked.Value;

            //Ask Server for IP and port
            try
            {
                if (CurrentlyConnected)
                {
                    if (GamePadEnabled)
                    {           //<GAMEPADINFO>
                        cmd = "<GAMEPADINFO>";
                        byte[] msg = Encoding.UTF8.GetBytes(cmd);
                        data = "";
                        int bytesSent = MainSocket.Send(msg);
                        WriteToLog("Client says: " + cmd);
                        int bytesRec = MainSocket.Receive(bytes);
                        data = Encoding.UTF8.GetString(bytes, 0, bytesRec);
                        WriteToLog("Server says: " + data);
                        string[] AddressParts = data.Split(':');
                        GamePadSocketUDP = ConnectIndependentUDP(AddressParts[0], int.Parse(AddressParts[1]));

                        //Thread GPThread = new Thread(new ThreadStart(MainWindow.SendGPData));
                        Thread GPThread = new Thread(MainWindow.SendGPDataIndependentUDP);
                        GPThread.Start();
                        Thread.Sleep(1000);
                        WriteToLog(GPID);

                        WriteToLog("Gamepad connected to: " + data);
                        txStatus.Text = "Gamepad Connected!";
                    }
                    else
                    {

                        txStatus.Text = "Gamepad Disconnected!";
                    }
                }
                else
                {
                    cbGameEnabled.IsChecked = false;
                    WriteToLog("Please connect to the server before connecting a gamepad.");
                    txStatus.Text = "Warning!";
                }
            }
            catch (Exception ec)
            {

                WriteToLog("Gamepad thread failed to connect.");
                txStatus.Text = ec.ToString();
            }
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
                    if (ButVal < (Center + (DeadZone / 2)) && ButVal > (Center - (DeadZone / 2)))
                    {
                        DataToFilter = "";
                    }
                    else
                    {
                        if (TranslateGPD)
                        {
                            ButValDec = ButVal / 361.11;
                            ButValDec = Math.Round(ButValDec, 0);
                            DataToFilter = ButLabel + ":" + ButValDec.ToString() + "~"; 
                        }
                        else
                        {
                            DataToFilter = s;
                        }
                        
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
                        if (TranslateGPD)
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
                        }
                        else
                        {
                            DataToFilter = s;
                        }
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
                                Thread.Sleep(int.Parse(txRate.ToString()));
                                int bytesSent = MainSocket.Send(GPMsg);
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

        public static void SendGPDataIndependent()
        {

            if (CurrentlyConnected && GamePadEnabled)
            {
                try
                {
                    using (StreamWriter sw3 = File.AppendText(LogFullPath))
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
                                        sw3.WriteLine(GPD);
                                    }

                                    byte[] GPMsg = Encoding.ASCII.GetBytes(GPD);
                                    Thread.Sleep(int.Parse(txRate.ToString()));
                                    //Socket.SendTo(Buffer, Length, Net.Sockets.SocketFlags.None, ReceiveEndpoint)
                                    int bytesSent = GamePadSocket.Send(GPMsg);
                                    //int bytesSent = GamePadSocket.SendTo(GPMsg,32, System.Net.Sockets.SocketFlags.None,);

                                }

                            }


                        }


                        sw3.Close();
                        GamePadSocket.Shutdown(SocketShutdown.Both);
                        GamePadSocket.Close();
                        GamePadSocket = null;
             
                    }
                }
                catch (Exception sgp)
                {

                    sgp.ToString();
                }
            }
        }

        public static void SendGPDataIndependentUDP()
        {

            if (CurrentlyConnected && GamePadEnabled)
            {
                try
                {
                    using (StreamWriter sw4 = File.AppendText(LogFullPath))
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
                                        sw4.WriteLine(GPD);
                                    }

                                    byte[] msg = Encoding.ASCII.GetBytes(GPD);
                        
                                    // This call blocks. 
                                    Thread.Sleep(int.Parse(txRate.ToString()));
                                 
                                    GamePadSocketUDP.SendTo(msg, 0, msg.Length, SocketFlags.None, GameUDPEndPoint);
                                }
                            }
                        }

              
                        sw4.Close();
                        GamePadSocketUDP.Shutdown(SocketShutdown.Both);
                        GamePadSocketUDP.Close();
                        GamePadSocketUDP = null;
                    }
                }
                catch (Exception sgp)
                {

                    sgp.ToString();
                }
            }
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
                //Environment.Exit(1);
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

        public void DisconnectGamePad()
        {
            try
            {
                cmd = "<GAMEPADKILL>";
                byte[] msg = Encoding.UTF8.GetBytes(cmd);
                data = "";
                int bytesSent = MainSocket.Send(msg);
                WriteToLog("Client says: " + cmd);
                int bytesRec = MainSocket.Receive(bytes);
                data = Encoding.UTF8.GetString(bytes, 0, bytesRec);
                WriteToLog("Server says: " + data);
                txStatus.Text = "Gamepad discconected successfully!";
 
            }
            catch (Exception ec)
            {
                txStatus.Text = ec.ToString();
                WriteToLog("Dagnabit! Something went horribly wrong when I tried to disconnect the gamepad...");
            }
            cbGameEnabled.IsChecked = false;
            WriteToLog("Gamepad disconnected.");
            GamePadEnabled = false;
   
        }

        #endregion 

        #region TCPSocketFunctions

        public void Connect()
        {


            SetConfigData();
            if (CurrentlyConnected)
            {
                DeadZone = Int32.Parse(dicConfig["deadzone"]);
            }
            else
            {
                DeadZone = Int32.Parse(dicConfig["deadzone"]);
                try
                {
                    ServerAddress = txServername.Text;
                    Port = Int32.Parse(txPort.Text);
                    IPHostEntry ipHostInfo = Dns.Resolve(ServerAddress);
                    IPAddress ipAddress = ipHostInfo.AddressList[0];
                    IPEndPoint remoteEP = new IPEndPoint(ipAddress, Port);
                    MainSocket = new Socket(AddressFamily.InterNetwork,
                    SocketType.Stream, ProtocolType.Tcp);

                    // Connect the socket to the remote endpoint. Catch any errors.

                    MainSocket.SendTimeout = 5000;
                    MainSocket.ReceiveTimeout = 5000;
                    MainSocket.Connect(remoteEP);
                    WriteToLog("Socket connected to " + MainSocket.RemoteEndPoint.ToString());
                    CurrentlyConnected = true;
                    txStatus.Text = "Connected";
                }
                catch (Exception ex)
                {
                    txStatus.Text = ex.ToString();
                    WriteToLog("Could not connect to " + txServername.Text + ":" + txPort.Text);
                    
                }
            }
        }

        public static Socket ConnectIndependent(string IP, int Port)
        {

                DeadZone = Int32.Parse(dicConfig["deadzone"]);
                try
                {
                    IPHostEntry ipHostInfo = Dns.Resolve(IP);
                    IPAddress ipAddress = ipHostInfo.AddressList[0];
                    IPEndPoint remoteEPs = new IPEndPoint(ipAddress, Port);
                    Socket privateSocket = new Socket(AddressFamily.InterNetwork,
                    SocketType.Stream, ProtocolType.Tcp);

                    // Connect the socket to the remote endpoint. Catch any errors.
                    privateSocket.SendTimeout = 5000;
                    privateSocket.ReceiveTimeout = 5000;
                    privateSocket.Connect(remoteEPs);
                   
                    CurrentlyConnected = true;
                    return privateSocket;
    
                }
                catch (Exception ex)
                {
                    string exc = ex.ToString();
                    return null;

                }


        }

        public static Socket ConnectIndependentUDP(string IP, int Port)
        {

                DeadZone = Int32.Parse(dicConfig["deadzone"]);
                try
                {


                    IPHostEntry hostEntry = Dns.Resolve(IP);

                    GameUDPEndPoint = new IPEndPoint(hostEntry.AddressList[0], Port);

                    Socket privateSocket = new Socket(GameUDPEndPoint.Address.AddressFamily,
                        SocketType.Dgram,
                        ProtocolType.Udp);

                    // Connect the socket to the remote endpoint. Catch any errors.

                    privateSocket.SendTimeout = 5000;
                    privateSocket.ReceiveTimeout = 5000;
                    privateSocket.Connect(GameUDPEndPoint);
                   
                    CurrentlyConnected = true;
                    return privateSocket;
                }
                catch (Exception ex)
                {
                    string exc = ex.ToString();
                    return null;

                }

                
        }

        public void Disconnect()
        {
            try
            {
                if (GamePadEnabled)
                {
                    DisconnectGamePad();
                }
                if (VideoEnabled)
                {
                    DisconnectVideo();
                }
                cmd = "<SERVERKILLCONNECTION>";
                byte[] msg = Encoding.ASCII.GetBytes(cmd);
                // Send the data through the socket.
                int bytesSent = MainSocket.Send(msg);
                WriteToLog("Client says: " + cmd);
                MainSocket.Shutdown(SocketShutdown.Both);
                MainSocket.Close();
                WriteToLog("Socket disconnected...");
                txStatus.Text = "Disconnected";

            }
            catch (Exception ec)
            {
                MainSocket = null;
                txStatus.Text = ec.ToString();
                WriteToLog("Socket was already disconnected...");
            }
            SetConfigData();
            CurrentlyConnected = false;
            MainSocket = null;
        }

        public void DisconnectIndependent(Socket SocketToDisconnect)
        {
            try
            {
                cmd = "<SERVERKILLCONNECTION>";
                byte[] msg = Encoding.ASCII.GetBytes(cmd);
                // Send the data through the socket.
                int bytesSent = MainSocket.Send(msg);
                WriteToLog("Client says: " + cmd);
                string SocketDeets = SocketToDisconnect.ToString();
                SocketToDisconnect.Shutdown(SocketShutdown.Both);
                SocketToDisconnect.Close();
                WriteToLog("Socket disconnected... " + SocketDeets);
                txStatus.Text = "Disconnected";

            }
            catch (Exception ec)
            {
                SocketToDisconnect = null;
                txStatus.Text = ec.ToString();
                WriteToLog("Socket was already disconnected...");
            }
            SetConfigData();
            CurrentlyConnected = false;
            SocketToDisconnect = null;
        }

        public void SendMessage()
        {
            txStatus.Text = "";
            watch.Reset();
            watch.Start();
            byte[] msg = Encoding.UTF8.GetBytes(txMessage.Text);
            data = "";


            if (CurrentlyConnected)
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
                        WriteToLog("Client says: " + txMessage.Text + " " + SendQty.ToString() + " times.");
                        for (int i = 0; i < SendQty; i++)
                        {
                            Thread.Sleep(int.Parse(txRate.ToString()));
                            int bytesSent = MainSocket.Send(msgs);
                        }

                    }
                    else
                    {
                        int bytesSent = MainSocket.Send(msg);
                        WriteToLog("Client says: " + txMessage.Text);
                    }

                    int bytesRec = MainSocket.Receive(bytes);
                    data = Encoding.UTF8.GetString(bytes, 0, bytesRec);
                    WriteToLog("Server says: " + data);
                    if (data == "...")
                    {
                        watch.Stop();
                        var elapsedMs = watch.Elapsed;
                        txStatus.Text = "Total Transaction Time: " + elapsedMs.ToString() + L + data;
                    }
                    else
                    {
                        txStatus.Text = data;

                    }

                    
                    txMessage.Text = "";
                }
                catch (Exception ex)
                {
                    WriteToLog("Uh oh... Could not communicate with the server.");
                    txStatus.Text = ex.ToString();
                }
            }
            else
            {
                WriteToLog("Server not connected!");

            }
        }

        #endregion

        #region GeneralFunctions

        public void WriteToLog(string Message)
        {
            Message = DateTime.Now.ToString("HH:mm:ss") + " " + Message;
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

        public void RunCMD(string CMD)
        {
            string strCmdText;
            strCmdText = "/C " + CMD; //copy /b Image1.jpg + Archive.rar Image2.jpg";
            System.Diagnostics.Process.Start("CMD.exe", strCmdText);
        }

        public void KillProcessByName(string ProcessName)
        {
            try
            {
                foreach (Process proc in Process.GetProcessesByName(ProcessName))
                {
                    proc.Kill();
                }
            }
            catch (Exception ex)
            {
                txStatus.Text = ex.ToString();
                WriteToLog("Oh no Mr. Bill! ...Couldnt kill the process named: " + ProcessName);
            }
         
        }

        public void ShowGamePadAdvancedControls(bool ShouldTheyBeShown)
        {
            if (ShouldTheyBeShown)
            {
                wnMain.Height = 416;
            }
            else
            {
                wnMain.Height = 330;
            }
        }

        public void PrintHelp()
        {
            WriteToLog(@"###SERVER COMMANDS###");
            WriteToLog(@"<GAMEPADINFO>  <---Tells server to open up UDP/TCP connection (If selected) to accept controller commands.");
            WriteToLog(@"Server returns: ip address and port in the following format - 192.168.1.100:4000");
            WriteToLog(@"<GAMEPADKILL>  <---Tells server to shut down the gamepad connection/thread");
            WriteToLog(@"Server returns: <GAMEPADKILL-OK>");
            WriteToLog(@"<VIDEOINFO>  <---Tells server to open up the video stream");
            WriteToLog(@"Server returns: ip addrss and port of the video stream in the following format - 192.168.1.100:4000");
            WriteToLog(@"<VIDEOKILL>  <---Tells server to close the video stream");
            WriteToLog(@"Server returns: <VIDEOKILL-OK>");
            WriteToLog(@"<SERVERKILLCONNECTION>  <---Tells server to shut down main TCP connection to the client");
            WriteToLog(@"Server returns: Goodbye!");
            WriteToLog(@"<SETIPLOCAL>  <---Sets the IP to whatever the controllers's current IP is. This is the default and you would only use this command to switch back changes you made using other IP commands.");
            WriteToLog(@"Server returns: IP address");
            WriteToLog(@"<SETIPDOMAIN> <---Sets the IP/domain name to your defualt domain loaded in your device.");
            WriteToLog(@"Server returns: IP address or domain address");
            WriteToLog(@"<SETIP=76.43.746.237> or <SETIP=www.robocop.com> <--Overrides the return IP to anything you send it.");
            WriteToLog(@"Server returns: IP address or domain address");
            txStatus.Text = "You're welcome...";
        }

        #endregion

        #region UI

        public void btnConnect_Click(object sender, RoutedEventArgs e)
        {
  
            Connect();
        }

        private void btnDisconnect_Click(object sender, RoutedEventArgs e)
        {
            Disconnect();
        }

        private void txMessage_KeyDown(object sender, KeyEventArgs e)
        
        {
            if (e.Key == System.Windows.Input.Key.Return && txMessage.Text != "")
            {
                SendMessage();

             }
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            SendMessage();
        }

        private void cbGameEnabled_Click(object sender, RoutedEventArgs e)
        {
            string GPT = cbGamepadType.Text;
            GamePadEnabled = cbGameEnabled.IsChecked.Value;
            if (GamePadEnabled)
            {

                ShowGamePadAdvancedControls(true);
                if (GPT == "Server Controlled Gamepad UDP")
                {
                    RunGamePadIndependentUDP();
                }
                else if (GPT == "Server Controlled Gamepad TCP")
                {
                    RunGamePadIndependent();
                }
                else
                {
                    RunGamePad();
                }
            }
            else
            {
                ShowGamePadAdvancedControls(false);
                DisconnectGamePad();
            }

    
        }

        private void cbTranslate_Click(object sender, RoutedEventArgs e)
        {
            TranslateGPD = cbTranslate.IsChecked.Value;
            dicConfig["translategpd"] = TranslateGPD.ToString();
        }

        private void cbOutputGPData_Click(object sender, RoutedEventArgs e)
        {
            
            cbOutputGPDataB = cbOutputGPData.IsChecked.Value;
            dicConfig["writetolog"] = cbOutputGPDataB.ToString();
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
            txStatus.Text = "";
    
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            DeadZone = Int32.Parse(Math.Round(slDeadZone.Value, 0).ToString());
            txStatus.Text = "DeadZone set to: " + (slDeadZone.Value / 32750).ToString("P");
            dicConfig["deadzone"] = Math.Round(slDeadZone.Value, 0).ToString();
        }

        private void Slider_ValueChanged_1(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

            txRate = Int32.Parse(Math.Round(slTXRate.Value, 0).ToString());
            txStatus.Text = "TX rate set to: " + Math.Round(slTXRate.Value, 0).ToString() + " miliseconds.";
            dicConfig["txrate"] = Math.Round(slTXRate.Value, 0).ToString();
        }

        private void slCenter_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

            Center = Int32.Parse(Math.Round(slCenter.Value, 0).ToString());
            txStatus.Text = "Analog center set to: " + Math.Round(slCenter.Value, 0).ToString();
            dicConfig["center"] = Math.Round(slCenter.Value, 0).ToString();
            
        }

        private void txServername_LostFocus(object sender, RoutedEventArgs e)
        {
            dicConfig["servername"] = txServername.Text;
        }

        private void txPort_TextChanged(object sender, TextChangedEventArgs e)
        {
            dicConfig["port"] = txPort.Text;
        }

        private void lbCenter_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            slCenter.Value = 30750;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SetConfigData();
        }

            #region Not Used
            private void txDeadZone_LostFocus(object sender, RoutedEventArgs e)
            {
            }
            private void txDeadZone_TextChanged(object sender, TextChangedEventArgs e)
            {
            }

            private void cbOutputGPData_Checked(object sender, RoutedEventArgs e)
            {
            }

            private void txMessage_TextChanged(object sender, TextChangedEventArgs e)
            {
            }

            private void Grid_GotFocus(object sender, RoutedEventArgs e)
            {
            }

            private void Window_GotFocus(object sender, RoutedEventArgs e)
            {
            }

            private void lstMaine_SelectionChanged(object sender, SelectionChangedEventArgs e)
            {
            }

            private void Window_MouseMove(object sender, MouseEventArgs e)
            {
            }

            private void Window_Activated(object sender, EventArgs e)
            {
            }

        #endregion

        private void txPort_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Return)
            {
                Connect();
            }
        }



        private void cbVideo_Click(object sender, RoutedEventArgs e)
        {
            VideoEnabled = cbVideo.IsChecked.Value;
            if (CurrentlyConnected)
            {
                if (VideoEnabled)
                {
                    RunVideo();
                }
            }
            else
            {
                txStatus.Text = "Warning!";
                WriteToLog("You must be connected to the main server if you would like to stream video!");
                cbVideo.IsChecked = false;

            }
            if (!VideoEnabled)
            {
                DisconnectVideo();
            }
        }

        private void cbGamepadType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            dicConfig["gamepadmode"] = cbGamepadType.SelectedIndex.ToString();
        }     
        #endregion

        private void Image_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            PrintHelp();
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




