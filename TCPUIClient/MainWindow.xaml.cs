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

using OpenQA.Selenium.IE;
using OpenQA.Selenium.Chrome;
using System.Runtime.InteropServices;
using OpenQA.Selenium.Remote;
using NAudio.Wave;




namespace TCPUIClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 



    public partial class MainWindow : Window
    {
        #region Variables



        public static Socket MainSocket;
        public static Socket GamePadSocket;
        public static Socket GamePadSocketUDP;
        public static Socket VideoSocket;
        public static Socket AudioFromMCSocket;
        public static Socket AudioToMCSocket;

        public static Stopwatch watch = Stopwatch.StartNew();
        public static IPEndPoint GameUDPEndPoint;
        public static WaveIn waveIn;
        public enum Direction { UP, DOWN, LEFT, RIGHT };

        public static List<double> LevelList = new List<double>();
        public static Dictionary<string, string> dicConfig = new Dictionary<string, string>();

        public delegate void ThreadLoggerCallback(string message);
        public delegate void ThreadStatusCallback(string message);
        public delegate void ThreadRecievedCallback(string message);
        public delegate void UpdateLogCallback(string message);
        public delegate void UpdateLevelCallback(double Level);

        public static byte[] bytes = new byte[1024];

        public static char c1 = (char)10;

        public static float sample32;

        public static double Amp = 1000;
        public static double Refresh = 0;
        public static double LastLev = 0;
        public static double MAX = 0;
        public static double MIN = 0;
        public static double myLevel;
        public static double LastLevelSent = 0;
        public static double tempLevel = 0;
        public static double CenterAdjust = 0;

        public static bool SendAudioDataOverUDP = false;
        public static bool CurrentlyConnected = false;
        public static bool GamePadEnabled = false;
        public static bool EnableLogFile = false;
        public static bool TranslateGPD = false;
        public static bool VideoEnabled = false;
        public static bool VideoControlOn = false;
        public static bool GamePadConnected = false;
        public static bool KeepAliveEnabled = false;
        public static bool LogGamepadEnabled = false;
        public static bool RecieveUDP = false;
        public static bool Printing;
        public static bool RunCurrent;
        public static bool Axis1Inv;
        public static bool Axis2Inv;
        public static bool Axis3Inv;
        public static bool Axis4Inv;
        public static bool Axis5Inv;
        public static bool Axis6Inv;



        public static int CycleCounter = 0;
        public static int AudioDevice = 0;


        public static string L = c1.ToString();
        public static string data = "";
        public static string GPID = "";
        public static string LogPath = @"C:\Logs\";
        public static string VideoBat = @"C:\Logs\vid.bat";
        public static string ConfigPath = @"C:\Logs\config.txt";
        public static string LogName = ("TCPClientLog-" + DateTime.Now.ToString("D") + ".txt").Replace(@"/", ".").Replace(":", ".");
        public static string LogFullPath = LogPath + LogName;
        public static string cmd;
        public static string VideoMode;
        public static string UDPMessage = "";
        public static string ServerAddress;
        public static string MyMessage;
        public static string AudioControl;
        public static string LastGPMessage;

        public static Int32 DeadZone = 0;
        public static Int32 txRate = 0;
        public static Int32 Center = 0;
        public static Int32 KARate = 0;
        public static Int32 SampleRate = 0;
        public static Int32 Port;
        public static Int32 Axis1Min = 0;
        public static Int32 Axis1Mid = 90;
        public static Int32 Axis1Max = 180;
        public static Int32 Axis2Min = 0;
        public static Int32 Axis2Mid = 90;
        public static Int32 Axis2Max = 180;
        public static Int32 Axis3Min = 0;
        public static Int32 Axis3Mid = 90;
        public static Int32 Axis3Max = 180;
        public static Int32 Axis4Min = 0;
        public static Int32 Axis4Mid = 90;
        public static Int32 Axis4Max = 180;
        public static Int32 Axis5Min = 0;
        public static Int32 Axis5Mid = 90;
        public static Int32 Axis5Max = 180;
        public static Int32 Axis6Min = 0;
        public static Int32 Axis6Mid = 90;
        public static Int32 Axis6Max = 180;

        public static Int64 LastTransmissionTime = 0;

        #endregion

        public MainWindow()
        {
            InitializeComponent();
        }

        #region Audio

        private void GetAudioDevices(bool printResults)
        {
            List<String> labels = new List<String>();

            int waveInDevices = WaveIn.DeviceCount;
            for (int waveInDevice = 0; waveInDevice < waveInDevices; waveInDevice++)
            {
                WaveInCapabilities deviceInfo = WaveIn.GetCapabilities(waveInDevice);
                labels.Add(waveInDevice.ToString() + "-" + deviceInfo.ProductName.ToString() + "... ");
                if (printResults)
                {
                    WriteToLog("Device: " + waveInDevice.ToString() + " " +
                        deviceInfo.ProductName.ToString() + "... " +
                            deviceInfo.Channels.ToString() + " channels");
                }

            }

            cbAudioSource.ItemsSource = labels;
            cbAudioSource.Items.Refresh();
        }

        private void UpdateLevel(double Level)
        {
            prgLevel.Value = Level;
        }

        public void StartListening()
        {
            Printing = true;
            record();
            Thread test = new Thread(ProcessSample);
            test.Start();
        }

        public void StopListening()
        {
            waveIn.StopRecording();
            prgLevel.Value = 0;
        }

        public void record()
        {

            //string[] words = Device.Split('');
            int ADnum = cbAudioSource.SelectedIndex;
            waveIn = new WaveIn();

            waveIn.DeviceNumber = ADnum;
            waveIn.DataAvailable += waveIn_DataAvailable;
            int sampleRate = 8000; // 8 kHz
            int channels = 1; // mono
            waveIn.WaveFormat = new WaveFormat(sampleRate, channels);
            try
            {
                waveIn.StartRecording();
            }
            catch (Exception w)
            {
                w.ToString();
            }

        }

        private void waveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            for (int index = 0; index < e.BytesRecorded; index += 2)
            {
                short sample = (short)((e.Buffer[index + 1] << 8) |
                                        e.Buffer[index + 0]);
                sample32 = sample / 32768f;
                var num = Math.Round((System.Convert.ToDouble(sample32) * Amp), 0);
                LevelList.Add(num);
                try
                {
                    MAX = LevelList.Max();
                    MIN = LevelList.Min();
                }
                catch (Exception p)
                {
                    p.ToString();
                }

            }
        }

        public void ProcessSample()
        {
            while (Printing)
            {
                if (LevelList.Count > SampleRate)
                {
                    try
                    {
                        // 
                        double CurLev = MAX - MIN;
                        if (CurLev < LastLev + Refresh && CurLev > LastLev - Refresh)
                        {
                            prgLevel.Dispatcher.Invoke(new UpdateLevelCallback(this.UpdateLevel), new object[] { LastLev });
                            SendAudioData(LastLev);
                        }
                        else
                        {
                            prgLevel.Dispatcher.Invoke(new UpdateLevelCallback(this.UpdateLevel), new object[] { CurLev });
                            SendAudioData(CurLev);
                            LastLev = CurLev;
                        }

                    }
                    catch (Exception e)
                    {
                        e.ToString();
                    }
                    LevelList.Clear();

                }
            }
        }

        public void SendAudioData(double Level)
        {

            if (SendAudioDataOverUDP && GamePadConnected)
            {

                if (Level != LastLevelSent)
                {
                    tempLevel = Math.Round(Level * 1.8, 0);
                    if (tempLevel > 180)
                    {
                        tempLevel = 180;
                    }
                    if (tempLevel < 0)
                    {
                        tempLevel = 0;
                    }

                    AudioControl = "Audio:" + tempLevel.ToString() + "~"; //RotationZ:84~
                    byte[] msg = Encoding.ASCII.GetBytes(AudioControl);
                    GamePadSocketUDP.SendTo(msg, 0, msg.Length, SocketFlags.None, GameUDPEndPoint);
                    LastTransmissionTime = GetEPOCHTimeInMilliSeconds();
                    LastLevelSent = tempLevel;
                }
                LastLevelSent = Level;
            }


        }

        #endregion

        #region ConfigData

        public void GetConfigData()
        {
            //Set up config file with defualt values if it doesnt exist
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
                dicConfig["gamepadaddress"] = "192.168.1.148:8888";
                dicConfig["videomode"] = "0";
                dicConfig["foscampassword"] = "0";
                dicConfig["videocontrol"] = "true";
                dicConfig["videoaddress"] = "www.thomasworkshop.com:88";
                dicConfig["keepaliveenabled"] = "false";
                dicConfig["loggamepadenabled"] = "false";
                dicConfig["karate"] = "250";
                dicConfig["recieveudp"] = "false";
                dicConfig["sendaudiodataoverupd"] = "true";
                dicConfig["amplitude"] = "100";
                dicConfig["samplerate"] = "0";
                dicConfig["filter"] = "0";
                dicConfig["audiodevice"] = "0";

                dicConfig["Axis1Inv"] = "false";
                dicConfig["Axis2Inv"] = "false";
                dicConfig["Axis3Inv"] = "false";
                dicConfig["Axis4Inv"] = "false";
                dicConfig["Axis5Inv"] = "false";
                dicConfig["Axis6Inv"] = "false";

                dicConfig["Axis1Min"] = "0";
                dicConfig["Axis1Mid"] = "90";
                dicConfig["Axis1Max"] = "180";

                dicConfig["Axis2Min"] = "0";
                dicConfig["Axis2Mid"] = "90";
                dicConfig["Axis2Max"] = "180";

                dicConfig["Axis3Min"] = "0";
                dicConfig["Axis3Mid"] = "90";
                dicConfig["Axis3Max"] = "180";

                dicConfig["Axis4Min"] = "0";
                dicConfig["Axis4Mid"] = "90";
                dicConfig["Axis4Max"] = "180";

                dicConfig["Axis5Min"] = "0";
                dicConfig["Axis5Mid"] = "90";
                dicConfig["Axis5Max"] = "180";

                dicConfig["Axis6Min"] = "0";
                dicConfig["Axis6Mid"] = "90";
                dicConfig["Axis6Max"] = "180";

                SetConfigData();

            }


            //Pull all values from the config file. 
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

            GetAudioDevices(false);

            try
            {
                if (dicConfig["sendaudiodataoverupd"].ToUpper() == "TRUE")
                {
                    SendAudioDataOverUDP = true;
                    cbSendAudioDataOverUDP.IsChecked = true;
                }
                else
                {
                    SendAudioDataOverUDP = false;
                    cbSendAudioDataOverUDP.IsChecked = false;
                }

                if (dicConfig["recieveudp"].ToUpper() == "TRUE")
                {
                    RecieveUDP = true;
                    cbRecieveUDP.IsChecked = true;
                }
                else
                {
                    RecieveUDP = false;
                    cbRecieveUDP.IsChecked = false;
                }

                if (dicConfig["loggamepadenabled"].ToUpper() == "TRUE")
                {
                    LogGamepadEnabled = true;
                    cbLogGamepad.IsChecked = true;
                }
                else
                {
                    LogGamepadEnabled = false;
                    cbLogGamepad.IsChecked = false;
                }

                if (dicConfig["writetolog"].ToUpper() == "TRUE")
                {
                    EnableLogFile = true;
                    cbEnableLogFile.IsChecked = true;
                }
                else
                {
                    EnableLogFile = false;
                    cbEnableLogFile.IsChecked = false;
                }

                if (dicConfig["keepaliveenabled"].ToUpper() == "TRUE")
                {

                    KeepAliveEnabled = true;
                    cbKeepAlive.IsChecked = true;
                }
                else
                {
                    KeepAliveEnabled = false;
                    cbKeepAlive.IsChecked = false;
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

                if (dicConfig["videocontrol"].ToUpper() == "TRUE")
                {
                    VideoControlOn = true;
                    cbVideoControl.IsChecked = true;
                }
                else
                {
                    VideoControlOn = false;
                    cbVideoControl.IsChecked = false;
                }

                if (dicConfig["Axis1Inv"].ToUpper() == "TRUE")
                {
                    Axis1Inv = true;
                    cbAxis1Inv.IsChecked = true;
                }
                else
                {
                    Axis1Inv = false;
                    cbAxis1Inv.IsChecked = false;
                }

                if (dicConfig["Axis2Inv"].ToUpper() == "TRUE")
                {
                    Axis2Inv = true;
                    cbAxis2Inv.IsChecked = true;
                }
                else
                {
                    Axis2Inv = false;
                    cbAxis2Inv.IsChecked = false;
                }

                if (dicConfig["Axis3Inv"].ToUpper() == "TRUE")
                {
                    Axis3Inv = true;
                    cbAxis3Inv.IsChecked = true;
                }
                else
                {
                    Axis3Inv = false;
                    cbAxis3Inv.IsChecked = false;
                }

                if (dicConfig["Axis4Inv"].ToUpper() == "TRUE")
                {
                    Axis4Inv = true;
                    cbAxis4Inv.IsChecked = true;
                }
                else
                {
                    Axis4Inv = false;
                    cbAxis4Inv.IsChecked = false;
                }

                if (dicConfig["Axis5Inv"].ToUpper() == "TRUE")
                {
                    Axis5Inv = true;
                    cbAxis5Inv.IsChecked = true;
                }
                else
                {
                    Axis5Inv = false;
                    cbAxis5Inv.IsChecked = false;
                }

                if (dicConfig["Axis6Inv"].ToUpper() == "TRUE")
                {
                    Axis6Inv = true;
                    cbAxis6Inv.IsChecked = true;
                }
                else
                {
                    Axis6Inv = false;
                    cbAxis6Inv.IsChecked = false;
                }

              


                //Set UI
                txServername.Text = dicConfig["servername"];
                txPort.Text = dicConfig["port"];

                slDeadZone.Value = double.Parse(dicConfig["deadzone"]);
                slTXRate.Value = double.Parse(dicConfig["txrate"]);
                slCenter.Value = double.Parse(dicConfig["center"]);
                slKeepAliveRate.Value = double.Parse(dicConfig["karate"]);
                slSampleRate.Value = double.Parse(dicConfig["samplerate"]);
                slAmp.Value = double.Parse(dicConfig["amplitude"]);
                slFilter.Value = double.Parse(dicConfig["filter"]);


                slAxis1Min.Value = double.Parse(dicConfig["Axis1Min"]);
                slAxis1Mid.Value = double.Parse(dicConfig["Axis1Mid"]);
                slAxis1Max.Value = double.Parse(dicConfig["Axis1Max"]);

                slAxis2Min.Value = double.Parse(dicConfig["Axis2Min"]);
                slAxis2Mid.Value = double.Parse(dicConfig["Axis2Mid"]);
                slAxis2Max.Value = double.Parse(dicConfig["Axis2Max"]);

                slAxis3Min.Value = double.Parse(dicConfig["Axis3Min"]);
                slAxis3Mid.Value = double.Parse(dicConfig["Axis3Mid"]);
                slAxis3Max.Value = double.Parse(dicConfig["Axis3Max"]);

                slAxis4Min.Value = double.Parse(dicConfig["Axis4Min"]);
                slAxis4Mid.Value = double.Parse(dicConfig["Axis4Mid"]);
                slAxis4Max.Value = double.Parse(dicConfig["Axis4Max"]);

                slAxis5Min.Value = double.Parse(dicConfig["Axis5Min"]);
                slAxis5Mid.Value = double.Parse(dicConfig["Axis5Mid"]);
                slAxis5Max.Value = double.Parse(dicConfig["Axis5Max"]);

                slAxis6Min.Value = double.Parse(dicConfig["Axis6Min"]);
                slAxis6Mid.Value = double.Parse(dicConfig["Axis6Mid"]);
                slAxis6Max.Value = double.Parse(dicConfig["Axis6Max"]);



                cbGamepadType.SelectedIndex = int.Parse(dicConfig["gamepadmode"]);
                cbVideoType.SelectedIndex = int.Parse(dicConfig["videomode"]);
                cbAudioSource.SelectedIndex = int.Parse(dicConfig["audiodevice"]);

                VideoMode = cbVideoType.Text;
                ShowGamePadAdvancedControls(false);


                //Set Variables
                DeadZone = Int32.Parse(dicConfig["deadzone"]);
                txRate = Int32.Parse(dicConfig["txrate"]);
                Center = Int32.Parse(dicConfig["center"]);
                KARate = Int32.Parse(dicConfig["karate"]);

            }
            catch (Exception)
            {

                if (File.Exists(ConfigPath))
                {
                    File.Delete(ConfigPath);
                }

                GetConfigData();
                txStatus.Text = "Warning!";
                WriteToLog("Config file has been rebuilt!");

            }

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

        public void PrintConfig()
        {
            foreach (var pair in dicConfig)
            {
                WriteToLog(pair.Key + "=" + pair.Value);
            }
        }

        private void Grid_Initialized(object sender, EventArgs e)
        {
            GetConfigData();
            LoadConfigToUI();
            txStatus.Text = "Welcome!";
        }

        #endregion

        #region VideoFunctions

        public void RunVideo()
        {
            if (cbVideoType.Text == "Foscam" || cbVideoType.Text == "Client Controlled Foscam")
            {
                FoscamLogin();
            }
            else if (cbVideoType.Text == "GStreamer")
            {
                RunVideoGS();
            }
        }

        public void DisconnectVideo()
        {
            try
            {
                if (cbVideoType.Text != "Client Controlled Foscam")
                {
                    cmd = "<VIDEOKILL>";
                    byte[] msg = Encoding.UTF8.GetBytes(cmd);
                    data = "";
                    int bytesSent = MainSocket.Send(msg);
                    WriteToLog("Client says: " + cmd);
                    int bytesRec = MainSocket.Receive(bytes);
                    data = Encoding.UTF8.GetString(bytes, 0, bytesRec);
                    txRawResponse.Text = data;
                    WriteToLog("Server says: " + data);
                }

                if (cbVideoType.Text == "Foscam" || cbVideoType.Text == "Client Controlled Foscam")
                {
                    DisconnectVideoFC();
                }
                else if (cbVideoType.Text == "GStreamer")
                {
                    DisconnectVideoGS();
                }
            }
            catch (Exception e)
            {


            }

        }

        public string[] GetVideoInfo()
        {
            if (cbVideoType.Text == "Client Controlled Foscam")
            {
                data = dicConfig["videoaddress"];
            }
            else
            {
                cmd = "<VIDEOINFO>"; //vid cmd
                byte[] msg = Encoding.UTF8.GetBytes(cmd);
                data = "";
                int bytesSent = MainSocket.Send(msg);
                WriteToLog("Client says: " + cmd);
                int bytesRec = MainSocket.Receive(bytes);
                data = Encoding.UTF8.GetString(bytes, 0, bytesRec);
                txRawResponse.Text = data;
                WriteToLog("Server says: " + data);

            }
            return data.Split(':');
        }

        #region GStreamer

        public void RunVideoGS()
        {
            if (CurrentlyConnected)
            {
                try
                {
                    string[] AddressParts = GetVideoInfo();
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

        public void DisconnectVideoGS()
        {
            try
            {
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

        #region Foscam

        public void FoscamLogin()
        {

            string[] AddressParts = GetVideoInfo();
            WriteToLog("Attempting to log into Foscam viewer at address: " + AddressParts[0] + ":" + AddressParts[1]);
            ChromeOptions options = new ChromeOptions();
            options.AddArgument("--always-authorize-plugins=true");
            options.AddArgument("--test-type");
            WebAutomationToolkit.Web.WebDriver = new ChromeDriver(options);


            WebAutomationToolkit.Web.NavigateToURL(@"http://" + AddressParts[0] + ":" + AddressParts[1]);
            WebAutomationToolkit.Utilities.Wait(2, 500);
            WebAutomationToolkit.Web.Sync.SyncByID("username", 10);
            WebAutomationToolkit.Web.Edit.SetTextByCSSPath("#passwd", dicConfig["foscampassword"]);
            WebAutomationToolkit.Web.Button.ClickByCSSPath("#login_ok");
            WebAutomationToolkit.Web.Sync.SyncByCSSPath("#LiveMenu", 10);
            WebAutomationToolkit.Web.Button.ClickByCSSPath("#LiveMenu");
            WebAutomationToolkit.Utilities.Wait(5);
            WriteToLog("Webcam viewer has been launched successfully...");
        }

        public static void MoveCamera(Direction direction)
        {
            switch (direction)
            {
                case Direction.UP:
                    WebAutomationToolkit.Web.Button.ClickByID(@"live_yt1_ptzMoveUp");
                    //WriteToLog("Camera move: UP");
                    break;
                case Direction.DOWN:
                    WebAutomationToolkit.Web.Button.ClickByID(@"live_yt1_ptzMoveDown");
                    //WriteToLog("Camera move: DOWN");
                    break;
                case Direction.LEFT:
                    WebAutomationToolkit.Web.Button.ClickByID(@"live_yt5_ptzMoveLeft");
                    //WriteToLog("Camera move: LEFT");
                    break;
                case Direction.RIGHT:
                    WebAutomationToolkit.Web.Button.ClickByID(@"live_yt5_ptzMoveRight");
                    //WriteToLog("Camera move: RIGHT");
                    break;
                default:
                    break;
            }
            WebAutomationToolkit.Utilities.Wait(0, 500);
        }

        public static void ControlFCam(string cmd)
        {
            if (cmd.IndexOf("Buttons12:ON") >= 0)
            {
                MoveCamera(Direction.UP);
            }
            if (cmd.IndexOf("Buttons13:ON") >= 0)
            {
                MoveCamera(Direction.DOWN);
            }
            if (cmd.IndexOf("Buttons14:ON") >= 0)
            {
                MoveCamera(Direction.LEFT);
            }
            if (cmd.IndexOf("Buttons15:ON") >= 0)
            {
                MoveCamera(Direction.RIGHT);
            }



        }

        public void DisconnectVideoFC()
        {
            try
            {
                WebAutomationToolkit.Web.CloseBrowser();
                WebAutomationToolkit.Web.WebDriver.Quit();
                txStatus.Text = "Success!";
                WriteToLog("The Foscam browser has been closed.");
            }
            catch (Exception)
            {

                txStatus.Text = "Warning!";
                WriteToLog("YOU DID THIS! Didn't you? ...The client attempted to close the broser, but couldn't. Had you closed it already?");
            }

            //KillProcessByName("chromedriver.exe *32");
        }

        #endregion

        #endregion

        #region GamePadFunctions

        public void LogFromThread(string Message)
        {
            txMain.Dispatcher.Invoke(
            new ThreadLoggerCallback(this.ThreadLogger),
            new object[] { Message });
        }

        public void StatusFromThread(string Message)
        {
            txStatus.Dispatcher.Invoke(
            new ThreadStatusCallback(this.ThreadStatus),
            new object[] { Message });
        }

        public void RecievedFromThread(string Message)
        {

            txRawResponse.Dispatcher.Invoke(
            new ThreadRecievedCallback(this.ThreadRecieved),
            new object[] { Message });
        }

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
                    Thread GPThread = new Thread(SendGPData);
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
                txRawResponse.Text = data;
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
                        Thread GPThread = new Thread(SendGPDataIndependent);
                        GPThread.Start();
                        Thread.Sleep(1000);
                        if (GamePadConnected)
                        {
                            WriteToLog(GPID);
                            WriteToLog("Gamepad connected to: " + data);
                            txStatus.Text = "Gamepad Connected!";
                        }
                        else
                        {
                            cbGameEnabled.IsChecked = false;
                            WriteToLog("Please connect a gamepad!");
                            txStatus.Text = "Warning!";
                            GamePadEnabled = false;
                        }

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
            if (cbGamepadType.Text == "Server Controlled Gamepad UDP")
            {
                #region If server controlled
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
                            txRawResponse.Text = data;
                            WriteToLog("Server says: " + data);
                            string[] AddressParts = data.Split(':');
                            GamePadSocketUDP = ConnectIndependentUDP(AddressParts[0], int.Parse(AddressParts[1]));

                            //Thread GPThread = new Thread(new ThreadStart(MainWindow.SendGPData));
                            Thread GPThread = new Thread(SendGPDataIndependentUDP);
                            GPThread.Start();
                            Thread.Sleep(1000);

                            if (GamePadConnected)
                            {
                                WriteToLog(GPID);
                                WriteToLog("Gamepad connected to: " + data);
                                txStatus.Text = "Gamepad Connected!";
                            }
                            else
                            {
                                cbGameEnabled.IsChecked = false;
                                WriteToLog("Please connect a gamepad!");
                                txStatus.Text = "Warning!";
                                GamePadEnabled = false;

                            }

                        }
                        else
                        {

                            txStatus.Text = "Gamepad Disconnected!";
                        }
                    }
                    else
                    {
                        cbGameEnabled.IsChecked = false;
                        WriteToLog("Please connect to the server before connecting a gamepad or slect client controlled gamepad.");
                        txStatus.Text = "Warning!";
                    }
                }
                catch (Exception ec)
                {

                    WriteToLog("Gamepad thread failed to connect.");
                    txStatus.Text = ec.ToString();
                }
                #endregion
            }
            else if (cbGamepadType.Text == "Client Controlled Gamepad UDP")
            {
                WriteToLog("Attempting to stream gamepad data to " + dicConfig["gamepadaddress"] + " via UDP.");
                string[] AddressParts = dicConfig["gamepadaddress"].Split(':');
                GamePadSocketUDP = ConnectIndependentUDP(AddressParts[0], int.Parse(AddressParts[1]));

                Thread GPThread = new Thread(new ThreadStart(SendGPDataIndependentUDP));
                //Thread GPThread = new Thread(MainWindow.SendGPDataIndependentUDP);
                GPThread.Start();
                Thread.Sleep(1000);
                if (GamePadConnected)
                {
                    WriteToLog(GPID);
                    WriteToLog("Gamepad connected to: " + dicConfig["gamepadaddress"]);
                    txStatus.Text = "Gamepad Connected!";
                }
                else
                {
                    cbGameEnabled.IsChecked = false;
                    WriteToLog("Please connect a gamepad!");
                    txStatus.Text = "Warning!";
                    GamePadEnabled = false;
                }


            }

        }

        public string GamePadDataFilter(string DataToFilter)
        {
            bool CenterStick = false;
            string s = DataToFilter;
            string[] cmds = s.Split(' ');
            string ButLabel = cmds[1].Replace(",", "");
            int ButVal = int.Parse(cmds[3]);
            DataToFilter = ButLabel + ":" + ButVal;
            string IO = "";
            double ButValDec;

            #region Preliminary Filter: Deadzone and basic conversions

            switch (ButLabel)
            {
                case "Sliders0":
                case "Sliders1":
                case "RotationZ":
                case "RotationY":
                case "RotationX":
                case "X":
                case "Y":
                case "Z":
                    if (ButVal < (Center + (DeadZone / 2)) && ButVal > (Center - (DeadZone / 2)))
                    {
                        CenterStick = true;
                    }

                    if (TranslateGPD)
                    {
                        ButValDec = ButVal / 361.11;
                        ButValDec = Math.Round(ButValDec, 0);
                        if (CenterStick)
                        {
                            ButValDec = 90;
                        }
                        if (ButValDec > 180)
                        {
                            ButValDec = 180;
                        }
                        if (ButValDec < 0)
                        {
                            ButValDec = 0;
                        }

                        #region Advanced Filter: Min, Mid, and Max

                        //Axis 1: X, Left right(180) left(0)
                        //Axis 2: Y, Left up(0) down(180)
                        //Axis 3: Rotation X, Right right(180) left(0) //Legacy Z
                        //Axis 4: Rotation Z or Rotation Y, Left up(0) down(180)
                        //Axis 5: Z, Left out(0) in(180) //Legacy Slider 0
                        //Axis 6: Slider 1, Right out(0) in(180)
                       
                        switch (ButLabel)
                        {

                            case "X":
                                CenterAdjust = Axis1Mid - 90;
                                ButValDec = ButValDec + CenterAdjust;

                                if (ButValDec < Axis1Min)
                                {
                                    ButValDec = Axis1Min;
                                }

                                if (ButValDec > Axis1Max)
                                {
                                    ButValDec = Axis1Max;
                                }

                                if (Axis1Inv)
                                {
                                    ButValDec = 180 - ButValDec;
                                }

                                break;
                            case "Y":
                                CenterAdjust = Axis2Mid - 90;
                                ButValDec = ButValDec + CenterAdjust;

                                if (ButValDec < Axis2Min)
                                {
                                    ButValDec = Axis2Min;
                                }

                                if (ButValDec > Axis2Max)
                                {
                                    ButValDec = Axis2Max;
                                }


                                if (Axis2Inv)
                                {
                                    ButValDec = 180 - ButValDec;
                                }
                                break;
                           case "RotationX":
                                CenterAdjust = Axis3Mid - 90;
                                ButValDec = ButValDec + CenterAdjust;

                                if (ButValDec < Axis3Min)
                                {
                                    ButValDec = Axis3Min;
                                }

                                if (ButValDec > Axis3Max)
                                {
                                    ButValDec = Axis3Max;
                                }


                                if (Axis3Inv)
                                {
                                    ButValDec = 180 - ButValDec;
                                }
                                break;
                            case "RotationZ": //legacy
                            case "RotationY":
                                CenterAdjust = Axis4Mid - 90;
                                ButValDec = ButValDec + CenterAdjust;

                                if (ButValDec < Axis4Min)
                                {
                                    ButValDec = Axis4Min;
                                }

                                if (ButValDec > Axis4Max)
                                {
                                    ButValDec = Axis4Max;
                                }

                                if (Axis4Inv)
                                {
                                    ButValDec = 180 - ButValDec;
                                }
                                break;
                            case "Z":
                                CenterAdjust = Axis5Mid - 90;
                                ButValDec = ButValDec + CenterAdjust;

                                if (ButValDec < Axis5Min)
                                {
                                    ButValDec = Axis5Min;
                                }

                                if (ButValDec > Axis5Max)
                                {
                                    ButValDec = Axis5Max;
                                }
                                

                                if (Axis5Inv)
                                {
                                    ButValDec = 180 - ButValDec;
                                }
                                break;

                            case "Sliders1":
                                CenterAdjust = Axis6Mid - 90;
                                ButValDec = ButValDec + CenterAdjust;

                                if (ButValDec < Axis6Min)
                                {
                                    ButValDec = Axis6Min;
                                }

                                if (ButValDec > Axis6Max)
                                {
                                    ButValDec = Axis6Max;
                                }


                                if (Axis6Inv)
                                {
                                    ButValDec = 180 - ButValDec;
                                }
                                break;

                        }

                        #endregion

                        DataToFilter = ButLabel + ":" + ButValDec.ToString() + "~";
                    }
                    else
                    {
                        DataToFilter = s;
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

            #endregion


     


            return DataToFilter;
        }

        public void SendGPData()
        {

            if (CurrentlyConnected && GamePadEnabled)
            {
                try
                {
                    using (StreamWriter sw = File.AppendText(LogFullPath))
                    {
                        string GPD = "";
                        var MyJS = FindGamepad();
                        while (GamePadEnabled)
                        {
                            MyJS.Poll();
                            var datas = MyJS.GetBufferedData();
                            foreach (var state in datas)
                            {
                                GPD = GamePadDataFilter(state.ToString());
                                if (GPD != "")
                                {
                                    if (EnableLogFile)
                                    {

                                        if (LogGamepadEnabled)
                                        {
                                            LogFromThread(GPD);

                                            sw.WriteLine(GPD);
                                        }
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

        public void SendGPDataIndependent()
        {

            if (CurrentlyConnected && GamePadEnabled)
            {
                try
                {
                    using (StreamWriter sw3 = File.AppendText(LogFullPath))
                    {
                        string GPD = "";
                        var MyJS = FindGamepad();
                        while (GamePadEnabled)
                        {
                            MyJS.Poll();
                            var datas = MyJS.GetBufferedData();
                            foreach (var state in datas)
                            {
                                GPD = GamePadDataFilter(state.ToString());
                                if (GPD != "")
                                {
                                    if (EnableLogFile)
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

        public void SendGPDataIndependentUDP()
        {

            if (CurrentlyConnected && GamePadEnabled)
            {
                try
                {
                    using (StreamWriter sw4 = File.AppendText(LogFullPath))
                    {
                        string GPD = "";
                        var MyJS = FindGamepad();
                        while (GamePadEnabled)
                        {
                            MyJS.Poll();
                            var datas = MyJS.GetBufferedData();


                            if (datas.Count() < 1 && UDPMessage != "")
                            {
                                byte[] msg = Encoding.ASCII.GetBytes(UDPMessage);
                                GamePadSocketUDP.SendTo(msg, 0, msg.Length, SocketFlags.None, GameUDPEndPoint);
                                LastTransmissionTime = GetEPOCHTimeInMilliSeconds();
                                UDPMessage = "";
                            }

                            //Keep alive if no gamepad data is available and keep alive is selected
                            if (KeepAliveEnabled && datas.Count() < 1)
                            {
                                Int64 CurrentTime = GetEPOCHTimeInMilliSeconds();
                                Int64 TimeBetweenTransmissions = CurrentTime - LastTransmissionTime;

                                if (TimeBetweenTransmissions > KARate)
                                {

                                    byte[] msg = Encoding.ASCII.GetBytes("KA:" + CurrentTime.ToString());
                                    GamePadSocketUDP.SendTo(msg, 0, msg.Length, SocketFlags.None, GameUDPEndPoint);
                                    LastTransmissionTime = GetEPOCHTimeInMilliSeconds();

                                    if (LogGamepadEnabled)
                                    {

                                        LogFromThread("KA:" + CurrentTime.ToString());

                                    }
                                }
                            }


                            foreach (var state in datas)
                            {

                                //add a message from the client if requested via the UDPMessage= command
                                GPD = GamePadDataFilter(state.ToString());

                                if (GPD != "")
                                {



                                    //This splits off the d pad for use with the foscam video feed
                                    if (VideoControlOn && VideoEnabled && GamePadEnabled && CurrentlyConnected && VideoMode.IndexOf("Foscam") >= 0)
                                    {
                                        if (GPD.IndexOf("Buttons12:ON") >= 0 || GPD.IndexOf("Buttons13:ON") >= 0 || GPD.IndexOf("Buttons14:ON") >= 0 || GPD.IndexOf("Buttons15:ON") >= 0)
                                        {
                                            ControlFCam(GPD);
                                        }
                                    }

                                    // This pauses to accomodate TX rate
                                    Thread.Sleep(int.Parse(txRate.ToString()));
                                    if (LastGPMessage != GPD)
                                    {
                                        if (LogGamepadEnabled)
                                        {
                                            LogFromThread(GPD);
                                            sw4.WriteLine(GPD);
                                        }

                                        byte[] msg = Encoding.ASCII.GetBytes(GPD);
                                        GamePadSocketUDP.SendTo(msg, 0, msg.Length, SocketFlags.None, GameUDPEndPoint);
                                        LastTransmissionTime = GetEPOCHTimeInMilliSeconds();
                                    }
                                    LastGPMessage = GPD;

                                    if (RecieveUDP)
                                    {

                                    }



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

        public static Joystick FindGamepad()
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
            GamePadConnected = true;
            if (joystickGuid == Guid.Empty)
            {
                MainWindow.GPID = "No Gamepad found.";
                GamePadConnected = false;
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
            if (cbGamepadType.Text == "Server Controlled Gamepad UDP")
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
                    txRawResponse.Text = data;
                    WriteToLog("Server says: " + data);
                    txStatus.Text = "Gamepad discconected successfully!";

                }
                catch (Exception ec)
                {
                    txStatus.Text = ec.ToString();
                    WriteToLog("Dagnabit! Something went horribly wrong when I tried to disconnect the gamepad...");
                }
            }

            txStatus.Text = "Success!";
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
            if (txMessage.Text != "")
            {
                txStatus.Text = "";
                watch.Reset();
                watch.Start();
                byte[] msg = Encoding.UTF8.GetBytes(txMessage.Text);
                data = "";

                ClientCommandHandler(txMessage.Text);

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
                        txRawResponse.Text = data;
                        WriteToLog("Server says: " + data);
                        if (data == "...")
                        {
                            watch.Stop();
                            var elapsedMs = watch.Elapsed;
                            txStatus.Text = "Total Transaction Time: " + elapsedMs.ToString();
                        }
                        else
                        {
                            txStatus.Text = data;

                        }


                        txMessage.Text = "";
                    }
                    catch (Exception ex)
                    {
                        WriteToLog("Could not communicate with the TCP server.");
                        txStatus.Text = ex.ToString();
                    }
                }
                else
                {
                    WriteToLog("Server not connected!");

                }
            }
            else
            {
                WriteToLog("No data entered...");

            }
        }

        #endregion

        #region GeneralFunctions

        public Int64 GetEPOCHTimeInMilliSeconds()
        {
            TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
            return Convert.ToInt64(t.TotalMilliseconds);
        }

        public void WriteToLog(string Message)
        {
            Message = DateTime.Now.ToString("HH:mm:ss") + " " + Message;
            txMain.AppendText(Message + L);
            txMain.ScrollToEnd();
            if (EnableLogFile && !GamePadEnabled)
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

        private void ThreadLogger(string Message)
        {
            Message = DateTime.Now.ToString("HH:mm:ss") + " " + Message;
            txMain.AppendText(Message + L);
            txMain.ScrollToEnd();
        }

        private void ThreadStatus(string Message)
        {
            txStatus.Text = Message;
        }

        private void ThreadRecieved(string Message)
        {
            txRawResponse.Text = Message;
        }

        public void RunCMD(string CMD)
        {
            string strCmdText;
            strCmdText = "/C " + CMD; //copy /b Image1.jpg + Archive.rar Image2.jpgTEST";
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
                //ignoring due to new UI layout
            }
        }

        public void PrintHelp()
        {
            WriteToLog(@"###Client COMMANDS###");
            WriteToLog(@"TXRATE=# <---Sets the TX rate to a precise number. Number must not be a decimal.");
            WriteToLog(@"GAMEPADADDRESS=???.???.???.???:???? <---The address and port of the UDP server that the gamepad data will be sent to.");
            WriteToLog(@"VIDEOADDRESS=???.???.???.???:???? <---The address and port of the Foscam video server that will be launched via web automation");
            WriteToLog(@"CONFIG <---Prints the config data");
            WriteToLog(@"HELP <---Prints the help text");

            WriteToLog(L);

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

        public void RunScript(string FileLocation)
        {
            int SendQty = 1;
            if (FileLocation.IndexOf("*") > -1)
            {
                string[] qty = FileLocation.Split('*');
                SendQty = int.Parse(qty[1]);
                FileLocation = qty[0];
            }

            if (FileLocation.ToUpper() == "DEFAULT" || FileLocation.ToUpper() == "D")
            {
                FileLocation = @"C:\Temp\script.txt";
            }

            LogFromThread("Running Script Located at: " + FileLocation);
            System.Collections.Generic.IEnumerable<String> lines = File.ReadLines(FileLocation);
            int count = 0;
            for (int i = 0; i < SendQty; i++)
            {
                foreach (var item in lines)
                {
                    if (item.IndexOf("//") != 0 && item != "")
                    {
                        count++;
                        string[] InstructionSet = item.Split(',');

                        LogFromThread(InstructionSet[0]);
                        if (GamePadConnected)
                        {
                            byte[] msg = Encoding.ASCII.GetBytes(InstructionSet[0]);
                            GamePadSocketUDP.SendTo(msg, 0, msg.Length, SocketFlags.None, GameUDPEndPoint);
                            LastTransmissionTime = GetEPOCHTimeInMilliSeconds();


                        }
                        Thread.Sleep(int.Parse(InstructionSet[1]));
                    }
                }
                LogFromThread("Script finished running " + count + " commands successfully!");
            }
        }

        public void ClientCommandHandler(string command)
        {
            if (command.ToUpper().IndexOf("GAMEPADADDRESS=") > -1)
            {
                string[] gpParts = command.Split('=');
                dicConfig["gamepadaddress"] = gpParts[1];
                WriteToLog("Gamepad address is now set to: " + gpParts[1]);
                txMessage.Text = "";
            }

            if (command.ToUpper().IndexOf("RUN=") > -1)
            {
                string[] gpParts = command.Split('=');
                Thread myNewThread = new Thread(() => RunScript(gpParts[1]));
                myNewThread.Start();

            }

            if (command.ToUpper().IndexOf("UDPMESSAGE=") > -1)
            {
                string[] gpParts = command.Split('=');
                UDPMessage = gpParts[1];
                WriteToLog("UDP massage: " + gpParts[1] + " will be sent... ");
                txMessage.Text = "";
            }

            if (command.ToUpper().IndexOf("VIDEOADDRESS=") > -1)
            {
                string[] gpParts = command.Split('=');
                dicConfig["videoaddress"] = gpParts[1];
                WriteToLog("Video address is now set to: " + gpParts[1]);
                txMessage.Text = "";
            }

            if (command.ToUpper().IndexOf("FOSCAMPASSWORD=") > -1)
            {
                string[] gpParts = command.Split('=');
                dicConfig["foscampassword"] = gpParts[1];
                WriteToLog("Foscam Password is now set");
                txMessage.Text = "";
            }

            if (command.ToUpper() == "HELP")
            {
                PrintHelp();
                txMessage.Text = "";
            }

            if (command.ToUpper() == "CONFIG")
            {
                PrintConfig();
                txMessage.Text = "";
            }

            if (command.ToUpper().IndexOf("TXRATE=") > -1)
            {
                string[] gpParts = command.Split('=');
                if (int.Parse(gpParts[1]) <= slTXRate.Maximum && int.Parse(gpParts[1]) >= slTXRate.Minimum)
                {
                    dicConfig["txrate"] = gpParts[1];
                    slTXRate.Value = int.Parse(gpParts[1]);
                    WriteToLog("TX Rate is now set to: " + gpParts[1] + " miliseconds");
                    txMessage.Text = "";
                }
                else
                {
                    WriteToLog("The value you entered is not valid. Please enter a whole number between " + slTXRate.Minimum.ToString() + " and " + slTXRate.Maximum.ToString());
                }


            }


            if (command.ToUpper().IndexOf("KARATE=") > -1)
            {
                string[] gpParts = command.Split('=');

                if (int.Parse(gpParts[1]) <= slKeepAliveRate.Maximum && int.Parse(gpParts[1]) >= slKeepAliveRate.Minimum)
                {
                    dicConfig["karate"] = gpParts[1];
                    slKeepAliveRate.Value = int.Parse(gpParts[1]);
                    WriteToLog("Keep Alive Rate is now set to: " + gpParts[1] + " miliseconds");
                    txMessage.Text = "";
                }
                else
                {
                    WriteToLog("The value you entered is not valid. Please enter a whole number between " + slKeepAliveRate.Minimum.ToString() + " and " + slKeepAliveRate.Maximum.ToString());
                }


            }
        }

        #endregion

        #region UI

        #region Trim/advanced

        #region Reset Advance

        private void Label_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            slAxis1Max.Value = 180;
            slAxis1Mid.Value = 90;
            slAxis1Min.Value = 0;
        }

        private void Label_MouseLeftButtonUp_1(object sender, MouseButtonEventArgs e)
        {
            slAxis2Max.Value = 180;
            slAxis2Mid.Value = 90;
            slAxis2Min.Value = 0;
        }

        private void Label_MouseLeftButtonUp_2(object sender, MouseButtonEventArgs e)
        {
            slAxis3Max.Value = 180;
            slAxis3Mid.Value = 90;
            slAxis3Min.Value = 0;
        }

        private void Label_MouseLeftButtonUp_3(object sender, MouseButtonEventArgs e)
        {
            slAxis4Max.Value = 180;
            slAxis4Mid.Value = 90;
            slAxis4Min.Value = 0;
        }

        private void Label_MouseLeftButtonUp_4(object sender, MouseButtonEventArgs e)
        {
            slAxis5Max.Value = 180;
            slAxis5Mid.Value = 90;
            slAxis5Min.Value = 0;
        }

        private void Label_MouseLeftButtonUp_5(object sender, MouseButtonEventArgs e)
        {
            slAxis6Max.Value = 180;
            slAxis6Mid.Value = 90;
            slAxis6Min.Value = 0;
        }

        #endregion

        private void slAxis1Min_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Axis1Min = Int32.Parse(Math.Round(slAxis1Min.Value, 0).ToString());
            txStatus.Text = "Axis 1 Min set to: " + Math.Round(slAxis1Min.Value, 0).ToString();
            dicConfig["Axis1Min"] = Math.Round(slAxis1Min.Value, 0).ToString();
        }

        private void slAxis2Min_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Axis2Min = Int32.Parse(Math.Round(slAxis2Min.Value, 0).ToString());
            txStatus.Text = "Axis 2 Min set to: " + Math.Round(slAxis2Min.Value, 0).ToString();
            dicConfig["Axis2Min"] = Math.Round(slAxis2Min.Value, 0).ToString();
        }

        private void slAxis3Min_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Axis3Min = Int32.Parse(Math.Round(slAxis3Min.Value, 0).ToString());
            txStatus.Text = "Axis 3 Min set to: " + Math.Round(slAxis3Min.Value, 0).ToString();
            dicConfig["Axis3Min"] = Math.Round(slAxis3Min.Value, 0).ToString();
        }

        private void slAxis4Min_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Axis4Min = Int32.Parse(Math.Round(slAxis4Min.Value, 0).ToString());
            txStatus.Text = "Axis 4 Min set to: " + Math.Round(slAxis4Min.Value, 0).ToString();
            dicConfig["Axis4Min"] = Math.Round(slAxis4Min.Value, 0).ToString();
        }

        private void slAxis5Min_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Axis5Min = Int32.Parse(Math.Round(slAxis5Min.Value, 0).ToString());
            txStatus.Text = "Axis 5 Min set to: " + Math.Round(slAxis5Min.Value, 0).ToString();
            dicConfig["Axis5Min"] = Math.Round(slAxis5Min.Value, 0).ToString();

        }

        private void slAxis6Min_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Axis6Min = Int32.Parse(Math.Round(slAxis6Min.Value, 0).ToString());
            txStatus.Text = "Axis 6 Min set to: " + Math.Round(slAxis6Min.Value, 0).ToString();
            dicConfig["Axis6Min"] = Math.Round(slAxis6Min.Value, 0).ToString();
        }

        private void slAxis1Mid_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Axis1Mid = Int32.Parse(Math.Round(slAxis1Mid.Value, 0).ToString());
            txStatus.Text = "Axis 1 Mid set to: " + Math.Round(slAxis1Mid.Value, 0).ToString();
            dicConfig["Axis1Mid"] = Math.Round(slAxis1Mid.Value, 0).ToString();

            try
            {
                slAxis1Min.Maximum = slAxis1Mid.Value - 1;
                slAxis1Max.Minimum = slAxis1Mid.Value + 1;
            }
            catch (Exception e1)
            {
            }

        }

        private void slAxis2Mid_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Axis2Mid = Int32.Parse(Math.Round(slAxis2Mid.Value, 0).ToString());
            txStatus.Text = "Axis 2 Mid set to: " + Math.Round(slAxis2Mid.Value, 0).ToString();
            dicConfig["Axis2Mid"] = Math.Round(slAxis2Mid.Value, 0).ToString();

            try
            {
                slAxis2Min.Maximum = slAxis2Mid.Value - 1;
                slAxis2Max.Minimum = slAxis2Mid.Value + 1;
            }
            catch (Exception e1)
            {
            }
        }

        private void slAxis3Mid_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Axis3Mid = Int32.Parse(Math.Round(slAxis3Mid.Value, 0).ToString());
            txStatus.Text = "Axis 3 Mid set to: " + Math.Round(slAxis3Mid.Value, 0).ToString();
            dicConfig["Axis3Mid"] = Math.Round(slAxis3Mid.Value, 0).ToString();
            try
            {
                slAxis3Min.Maximum = slAxis3Mid.Value - 1;
                slAxis3Max.Minimum = slAxis3Mid.Value + 1;
            }
            catch (Exception e1)
            {
            }
        }

        private void slAxis4Mid_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Axis4Mid = Int32.Parse(Math.Round(slAxis4Mid.Value, 0).ToString());
            txStatus.Text = "Axis 4 Mid set to: " + Math.Round(slAxis4Mid.Value, 0).ToString();
            dicConfig["Axis4Mid"] = Math.Round(slAxis4Mid.Value, 0).ToString();
            try
            {
                slAxis4Min.Maximum = slAxis4Mid.Value - 1;
                slAxis4Max.Minimum = slAxis4Mid.Value + 1;
            }
            catch (Exception e1)
            {
            }
        }

        private void slAxis5Mid_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Axis5Mid = Int32.Parse(Math.Round(slAxis5Mid.Value, 0).ToString());
            txStatus.Text = "Axis 5 Mid set to: " + Math.Round(slAxis5Mid.Value, 0).ToString();
            dicConfig["Axis5Mid"] = Math.Round(slAxis5Mid.Value, 0).ToString();
            try
            {
                slAxis5Min.Maximum = slAxis5Mid.Value - 1;
                slAxis5Max.Minimum = slAxis5Mid.Value + 1;
            }
            catch (Exception e1)
            {
            }
        }

        private void slAxis6Mid_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Axis6Mid = Int32.Parse(Math.Round(slAxis6Mid.Value, 0).ToString());
            txStatus.Text = "Axis 6 Mid set to: " + Math.Round(slAxis6Mid.Value, 0).ToString();
            dicConfig["Axis6Mid"] = Math.Round(slAxis6Mid.Value, 0).ToString();
            try
            {
                slAxis6Min.Maximum = slAxis6Mid.Value - 1;
                slAxis6Max.Minimum = slAxis6Mid.Value + 1;
            }
            catch (Exception e1)
            {
            }
        }

        private void slAxis1Max_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Axis1Max = Int32.Parse(Math.Round(slAxis1Max.Value, 0).ToString());
            txStatus.Text = "Axis 1 Max set to: " + Math.Round(slAxis1Max.Value, 0).ToString();
            dicConfig["Axis1Max"] = Math.Round(slAxis1Max.Value, 0).ToString();
        }

        private void slAxis2Max_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Axis2Max = Int32.Parse(Math.Round(slAxis2Max.Value, 0).ToString());
            txStatus.Text = "Axis 2 Max set to: " + Math.Round(slAxis2Max.Value, 0).ToString();
            dicConfig["Axis2Max"] = Math.Round(slAxis2Max.Value, 0).ToString();
        }

        private void slAxis3Max_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Axis3Max = Int32.Parse(Math.Round(slAxis3Max.Value, 0).ToString());
            txStatus.Text = "Axis 3 Max set to: " + Math.Round(slAxis3Max.Value, 0).ToString();
            dicConfig["Axis3Max"] = Math.Round(slAxis3Max.Value, 0).ToString();
        }

        private void slAxis4Max_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Axis4Max = Int32.Parse(Math.Round(slAxis4Max.Value, 0).ToString());
            txStatus.Text = "Axis 4 Max set to: " + Math.Round(slAxis4Max.Value, 0).ToString();
            dicConfig["Axis4Max"] = Math.Round(slAxis4Max.Value, 0).ToString();
        }

        private void slAxis5Max_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Axis5Max = Int32.Parse(Math.Round(slAxis5Max.Value, 0).ToString());
            txStatus.Text = "Axis 5 Max set to: " + Math.Round(slAxis5Max.Value, 0).ToString();
            dicConfig["Axis5Max"] = Math.Round(slAxis5Max.Value, 0).ToString();
        }

        private void slAxis6Max_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Axis6Max = Int32.Parse(Math.Round(slAxis6Max.Value, 0).ToString());
            txStatus.Text = "Axis 6 Max set to: " + Math.Round(slAxis6Max.Value, 0).ToString();
            dicConfig["Axis6Max"] = Math.Round(slAxis6Max.Value, 0).ToString();
        }
        #endregion

        #region Not Used

        private void cbGamepadType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
        }

        private void cbVideoType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void txDeadZone_LostFocus(object sender, RoutedEventArgs e)
        {
        }
        private void txDeadZone_TextChanged(object sender, TextChangedEventArgs e)
        {
        }

        private void cbEnableLogFile_Checked(object sender, RoutedEventArgs e)
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

        #region Basic

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
                if (GPT.IndexOf("Gamepad UDP") > -1)
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

        private void cbEnableLogFile_Click(object sender, RoutedEventArgs e)
        {

            EnableLogFile = cbEnableLogFile.IsChecked.Value;
            dicConfig["writetolog"] = EnableLogFile.ToString();
            if (EnableLogFile)
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
            DisconnectVideo();
            DisconnectGamePad();
            SetConfigData();
            Application.Current.Shutdown();
        }

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
            if (CurrentlyConnected || cbVideoType.Text == "Client Controlled Foscam")
            {
                if (VideoEnabled)
                {
                    RunVideo();
                }
            }
            else
            {
                txStatus.Text = "Warning!";
                WriteToLog("You must be connected to the main server if you would like to stream video with the selected mode: " + cbVideoType.Text);
                cbVideo.IsChecked = false;

            }
            if (!VideoEnabled)
            {
                DisconnectVideo();
            }
        }

        private void Image_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            PrintHelp();
        }

        private void cbVideoControl_Click(object sender, RoutedEventArgs e)
        {
            VideoControlOn = cbVideoControl.IsChecked.Value;
            dicConfig["videocontrol"] = cbVideoControl.IsChecked.Value.ToString();
        }

        private void cbGamepadType_DropDownClosed(object sender, EventArgs e)
        {
            if (cbGamepadType.Text == "Client Controlled Gamepad UDP")
            {
                WriteToLog("Current assigned UDP gamepad address is: " + dicConfig["gamepadaddress"] + L + "To change this use command: GAMEPADADDRESS=Whateveraddressyouwant.com:888");
            }

            dicConfig["gamepadmode"] = cbGamepadType.SelectedIndex.ToString();
        }

        private void cbVideoType_DropDownClosed(object sender, EventArgs e)
        {
            if (cbVideoType.Text == "Client Controlled Foscam")
            {
                WriteToLog("Current assigned Foscam address is: " + dicConfig["videoaddress"] + L + "To change this use command: VIDEOADDRESS=Whateveraddressyouwant.com:88");
            }
            dicConfig["videomode"] = cbVideoType.SelectedIndex.ToString();
            VideoMode = cbVideoType.Text;
        }

        private void slKeepAliveRate_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            KARate = Int32.Parse(Math.Round(slKeepAliveRate.Value, 0).ToString());
            txStatus.Text = "Keep alive rate set to: " + Math.Round(slKeepAliveRate.Value, 0).ToString() + " milliseconds.";
            dicConfig["karate"] = Math.Round(slKeepAliveRate.Value, 0).ToString();
        }

        private void cbKeepAlive_Click(object sender, RoutedEventArgs e)
        {
            KeepAliveEnabled = cbKeepAlive.IsChecked.Value;
            dicConfig["keepaliveenabled"] = cbKeepAlive.IsChecked.Value.ToString();
        }

        private void cbLogGamepad_Click(object sender, RoutedEventArgs e)
        {
            LogGamepadEnabled = cbLogGamepad.IsChecked.Value;
            dicConfig["loggamepadenabled"] = cbLogGamepad.IsChecked.Value.ToString();
        }

        private void cbRecieveUDP_Click(object sender, RoutedEventArgs e)
        {
            RecieveUDP = cbRecieveUDP.IsChecked.Value;
            dicConfig["recieveudp"] = cbRecieveUDP.IsChecked.Value.ToString();
        }

        private void btnFindAudio_Click(object sender, RoutedEventArgs e)
        {
            GetAudioDevices(true);
        }

        private void btnListen_Click(object sender, RoutedEventArgs e)
        {
            StartListening();
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            StopListening();
        }

        private void slAmp_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Amp = slAmp.Value;
            txStatus.Text = "Amplitude set to: " + Math.Round(slAmp.Value, 0).ToString();
            dicConfig["amplitude"] = Math.Round(slAmp.Value, 0).ToString();

        }

        private void slSampleRate_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SampleRate = Convert.ToInt32(slSampleRate.Value);
            txStatus.Text = "Sample rate set to: " + Math.Round(slSampleRate.Value, 0).ToString();
            dicConfig["samplerate"] = Math.Round(slSampleRate.Value, 0).ToString();
        }

        private void slFilter_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Refresh = slFilter.Value;
            txStatus.Text = "Filter set to: " + Math.Round(slFilter.Value, 0).ToString();
            dicConfig["filter"] = Math.Round(slFilter.Value, 0).ToString();
        }

        private void cbAudioSource_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            dicConfig["audiodevice"] = cbAudioSource.SelectedIndex.ToString();
            AudioDevice = cbAudioSource.SelectedIndex;
        }

        private void cbSendAudioDataOverUDP_Click(object sender, RoutedEventArgs e)
        {
            SendAudioDataOverUDP = cbSendAudioDataOverUDP.IsChecked.Value;
            dicConfig["sendaudiodataoverupd"] = cbSendAudioDataOverUDP.IsChecked.Value.ToString();
        }

        #endregion

        private void cbAxis1Inv_Checked(object sender, RoutedEventArgs e)
        {
            Axis1Inv = cbAxis1Inv.IsChecked.Value;
            dicConfig["Axis1Inv"] = cbAxis1Inv.IsChecked.Value.ToString();
            txStatus.Text = "Axis 1 Inverted: " + Axis1Inv.ToString();
        }

  
        private void cbAxis2Inv_Checked(object sender, RoutedEventArgs e)
        {
            Axis2Inv = cbAxis2Inv.IsChecked.Value;
            dicConfig["Axis2Inv"] = cbAxis2Inv.IsChecked.Value.ToString();
            txStatus.Text = "Axis 2 Inverted: " + Axis2Inv.ToString();
        }

        private void cbAxis3Inv_Checked(object sender, RoutedEventArgs e)
        {
            Axis3Inv = cbAxis3Inv.IsChecked.Value;
            dicConfig["Axis3Inv"] = cbAxis3Inv.IsChecked.Value.ToString();
            txStatus.Text = "Axis 3 Inverted: " + Axis3Inv.ToString();
        }

        private void cbAxis4Inv_Checked(object sender, RoutedEventArgs e)
        {
            Axis4Inv = cbAxis4Inv.IsChecked.Value;
            dicConfig["Axis4Inv"] = cbAxis4Inv.IsChecked.Value.ToString();
            txStatus.Text = "Axis 4 Inverted: " + Axis4Inv.ToString();
        }

        private void cbAxis5Inv_Checked(object sender, RoutedEventArgs e)
        {
            Axis5Inv = cbAxis5Inv.IsChecked.Value;
            dicConfig["Axis5Inv"] = cbAxis5Inv.IsChecked.Value.ToString();
            txStatus.Text = "Axis 5 Inverted: " + Axis5Inv.ToString();
        }

        private void cbAxis6Inv_Checked(object sender, RoutedEventArgs e)
        {
           Axis6Inv = cbAxis6Inv.IsChecked.Value;
            dicConfig["Axis6Inv"] = cbAxis6Inv.IsChecked.Value.ToString();
            txStatus.Text = "Axis 6 Inverted: " + Axis6Inv.ToString();
        }


        #endregion

        private void cbAxis1Inv_Click(object sender, RoutedEventArgs e)
        {


        }

    }

}
    




