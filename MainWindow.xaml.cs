using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace bayserver
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static string commands_File = @"c:\temp\commands.txt";
        const int PORT_NO = 5000;
        const int MAX_CMD = 10;
        List<String>[] Cmds = new List<string>[MAX_CMD];


        //        const string SERVER_IP = "192.168.1.8"; // "10.0.0.138";
        static TcpListener listener;
        Task task;
        TcpClient client;
        NetworkStream nwStream;
        bool bCloseThread;
        bool bGot_AddRecvMsgs;

        public MainWindow()
        {
            InitializeComponent();
        }



        void SetButtonsNames()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (File.Exists(commands_File))
                {
                    StreamReader reader = new StreamReader(commands_File);
                    String currentLine;
                    int K = -1;
                    while (( currentLine = reader.ReadLine()) != null && K < MAX_CMD )
                    {
                        currentLine = currentLine.Trim();
                        if (currentLine.Length == 0)
                            continue;
                        if ( currentLine[0] == '@' )
                        {
                            String S = currentLine.Substring(1).Trim();
                            K++;
                            Cmds[K] = new List<string>();
                            switch (K)
                            {
                                case 0: Cmd0.Content = S; break;
                                case 1: Cmd1.Content = S; break;
                                case 2: Cmd2.Content = S; break;
                                case 3: Cmd3.Content = S; break;
                                case 4: Cmd4.Content = S; break;
                                case 5: Cmd5.Content = S; break;
                                case 6: Cmd6.Content = S; break;
                                case 7: Cmd7.Content = S; break;
                                case 8: Cmd8.Content = S; break;
                                case 9: Cmd9.Content = S; break;
                            }
                        }
                        else if ( K >= 0 )
                        {
                            currentLine = currentLine.ToUpper();
                            Cmds[K].Add(currentLine);
                        }
                        //Actual parsing left up to the reader; String.Split is your friend.
                    }
                }
            }), DispatcherPriority.Background);


        }

        void AddRecvMsgs(String S)
        {
            bGot_AddRecvMsgs = true;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (RecvMsgs.Text.Length == 0)
                    RecvMsgs.Text = S;
                else
                    RecvMsgs.Text += "\n" + S;
                RecvMsgs.Focus();
                RecvMsgs.CaretIndex = RecvMsgs.Text.Length;
            }), DispatcherPriority.Background);
        }

        void ClientManagmentThread()
        {
            task = null;
            bCloseThread = false;
            SetButtonsNames();
            //---incoming client connected---
            while (true)
            {
                client = listener.AcceptTcpClient();
                if (task != null)
                {
                    bCloseThread = true;
                    task.Wait();
                }
                task = Task.Run(() => HoistThread(client));
            }
        }

        void HoistThread(TcpClient client)
        {
            int bytesRead;
            //            bool IsYouConnectionAlright;
            // report that client was connected
            AddRecvMsgs("Hoist Connected");
            Dispatcher.BeginInvoke(new Action(() => { SendButton.IsEnabled = true; }), DispatcherPriority.Background);
            //---get the incoming data through a network stream---
            nwStream = client.GetStream();
            byte[] buffer = new byte[client.ReceiveBufferSize];

            while (bCloseThread == false)
            {
                //---read incoming stream---
                if (nwStream.DataAvailable)
                {
                    bytesRead = nwStream.Read(buffer, 0, client.ReceiveBufferSize);
                    if (bytesRead <= 0)
                        break;
                    //---convert the data received into a string---
                    string S = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    do
                    {
                        if (S.Contains(".") == false)
                        {
                            AddRecvMsgs(S);
                            break;
                        }
                        int K = S.IndexOf('.');
                        if (K >= S.Length - 2)
                        {  // the * is at the end ... just print it as is
                            AddRecvMsgs(S);
                            break;
                        }
                        // split off the beginning, print it, and continue
                        String S2 = S.Remove(K+1);
                        AddRecvMsgs(S2);
                        S = S.Substring(K + 1);
                    } while (S.Length > 2);
                }
                Thread.Sleep(10);
            }
            bCloseThread = false;
//            AddRecvMsgs("Hoist Disconnected");
            client.Close();
            client = null;
            nwStream = null;
        }

        void SendS(String S) { AddRecvMsgs(">" + S); if (nwStream != null) nwStream.Write(Encoding.ASCII.GetBytes(S), 0, S.Length); }
        String MBSelected()
        {
            if (MB0.IsChecked == true) return "0";
            if (MB1.IsChecked == true) return "1";
            if (MB2.IsChecked == true) return "2";
            if (MB3.IsChecked == true) return "3";
            if (MB4.IsChecked == true) return "4";
            if (MB5.IsChecked == true) return "5";
            if (MB6.IsChecked == true) return "6";
            if (MB7.IsChecked == true) return "7";
            if (MB0123.IsChecked == true) return "8";
            if (MB23.IsChecked == true) return "9";
            return "a";
        }

        String PDSelected()
        {
            if (PD1.IsChecked == true) return "1";
            if (PD2.IsChecked == true) return "2";
            if (PD3.IsChecked == true) return "3";
            if (PD4.IsChecked == true) return "4";
            return "9";
        }


        String SteeringSelected()
        {
//            if (Steering0.IsChecked == true) return "0";
            return "1";
        }

        private void Send_Click(object sender, RoutedEventArgs e)
        {
            String S = SendTextBox.Text;
            if (nwStream != null)
            {
                AddRecvMsgs(">" + S);
                nwStream.Write(Encoding.ASCII.GetBytes(S), 0, S.Length);
            }
        }
        private void Fwd1_Click(object sender, RoutedEventArgs e) { SendS("m" + MBSelected() + "fg79500"); }
        private void Rev1_Click(object sender, RoutedEventArgs e) { SendS("m" + MBSelected() + "rg79500"); }
        private void Rampup2_Click(object sender, RoutedEventArgs e) { SendS("r9f22000"); }
        private void Step_Grip_Click(object sender, RoutedEventArgs e) { SendS("m630t3700"); }
        private void Step_Release_Click(object sender, RoutedEventArgs e) { SendS("m631t3700"); }
        private void Step0_Shorter_Click(object sender, RoutedEventArgs e) { SendS("m" + MBSelected() + "00t1000"); }
        private void Step0_Longer_Click(object sender, RoutedEventArgs e) { SendS("m" + MBSelected() + "01t1000"); }
        private void Step0_ModeHome_Click(object sender, RoutedEventArgs e) { SendS("m" + MBSelected() + "0m38"); }
        private void Step0_ModeQEI_Click(object sender, RoutedEventArgs e) { SendS("m" + MBSelected() + "0mff"); }
        private void J810_Click(object sender, RoutedEventArgs e) { SendS("b" + MBSelected() + "00"); }
        private void J811_Click(object sender, RoutedEventArgs e) { SendS("b" + MBSelected() + "01"); }
        private void J820_Click(object sender, RoutedEventArgs e) { SendS("b" + MBSelected() + "10"); }
        private void J821_Click(object sender, RoutedEventArgs e) { SendS("b" + MBSelected() + "11"); }
        private void Break_Click(object sender, RoutedEventArgs e) { SendS("b" + MBSelected() + "20"); }
        private void OffBreak_Click(object sender, RoutedEventArgs e) { SendS("b" + MBSelected() + "21"); }




        private void Info0_Click(object sender, RoutedEventArgs e) { SendS("I" + MBSelected() + "0"); }
        private void Info1_Click(object sender, RoutedEventArgs e) { SendS("I" + MBSelected() + "1"); }
        private void Home_Click(object sender, RoutedEventArgs e) { SendS("S"+ SteeringSelected()+"C"); }
        private void W0_Click(object sender, RoutedEventArgs e) { SendS("S" + SteeringSelected() + "0"); }
        private void W20_Click(object sender, RoutedEventArgs e) { SendS("S" + SteeringSelected() + "2000"); }
        private void W40_Click(object sender, RoutedEventArgs e) { SendS("S" + SteeringSelected() + "4000"); }
        private void W70_Click(object sender, RoutedEventArgs e) { SendS("S" + SteeringSelected() + "7000"); }
        private void Wm20_Click(object sender, RoutedEventArgs e) { SendS("S" + SteeringSelected() + "-2000"); }
        private void Wm40_Click(object sender, RoutedEventArgs e) { SendS("S" + SteeringSelected() + "-4000"); }
        private void Wm70_Click(object sender, RoutedEventArgs e) { SendS("S" + SteeringSelected() + "-7000"); }
        private void P1_Click(object sender, RoutedEventArgs e) { SendS("P" + PDSelected() + "1"); }
        private void P0_Click(object sender, RoutedEventArgs e) { SendS("P" + PDSelected() + "0"); }
        private void PM_Click(object sender, RoutedEventArgs e) { SendS("P" + PDSelected() + "M" + PD4Val.Text); }

        private void Out0_Click(object sender, RoutedEventArgs e) { SendS("P" + PDSelected() + "P460FE1"); }
        private void Out1_Click(object sender, RoutedEventArgs e) { SendS("P" + PDSelected() + "P460FE65537"); }
        private void In_Click(object sender, RoutedEventArgs e) { SendS("P" + PDSelected() + "G60FD"); }



        private void Speed_Click(object sender, RoutedEventArgs e) { SendS("P" + PDSelected() + "P46081" + PD4Val.Text); }
        private void IRDist_Click(object sender, RoutedEventArgs e) { SendS("D"); }
        private void J10_Click(object sender, RoutedEventArgs e) { SendS("J10"); }
        private void J11_Click(object sender, RoutedEventArgs e) { SendS("J11"); }
        private void J20_Click(object sender, RoutedEventArgs e) { SendS("J20"); }
        private void J21_Click(object sender, RoutedEventArgs e) { SendS("J21"); }
        private void J30_Click(object sender, RoutedEventArgs e) { SendS("J30"); }
        private void J31_Click(object sender, RoutedEventArgs e) { SendS("J31"); }
        private void J40_Click(object sender, RoutedEventArgs e) { SendS("J40"); }
        private void J41_Click(object sender, RoutedEventArgs e) { SendS("J41"); }

        private void LOn_Click(object sender, RoutedEventArgs e) { SendS("P21"); }
        private void LUnlock_Click(object sender, RoutedEventArgs e) { SendS("P2P460FE196609"); }
        private void LUp_Click(object sender, RoutedEventArgs e) { SendS("P2V-" + LSpeed.Text); }
        private void LDown_Click(object sender, RoutedEventArgs e) { SendS("P2V" + LSpeed.Text); }
        private void LReset_Click(object sender, RoutedEventArgs e) { SendS("P22"); }
        private void LStop_Click(object sender, RoutedEventArgs e) { SendS("P2P260407"); }
        private void LLock_Click(object sender, RoutedEventArgs e) { SendS("P2P460FE1"); }
        private void LOff_Click(object sender, RoutedEventArgs e) { SendS("P20"); }

        private void ROn_Click(object sender, RoutedEventArgs e) { SendS("P11"); }
        private void RUnlock_Click(object sender, RoutedEventArgs e) { SendS("P1P460FE65537"); }
        private void RCW_Click(object sender, RoutedEventArgs e) { int K = Int32.Parse(RDegree.Text); K = K * 57; SendS("P1M" + K.ToString()); }
        private void RCCW_Click(object sender, RoutedEventArgs e) { int K = Int32.Parse(RDegree.Text); K = -K * 57; SendS("P1M" + K.ToString()); }
        private void RLock_Click(object sender, RoutedEventArgs e) { SendS("P1P460FE1"); }
        private void ROff_Click(object sender, RoutedEventArgs e) { SendS("P10"); }

        private void EOn_Click(object sender, RoutedEventArgs e) { SendS("P41"); }
        private void ESpeed_Click(object sender, RoutedEventArgs e) { SendS("P4P46081" + ESpeed.Text); }
        private void EUnlock_Click(object sender, RoutedEventArgs e) { SendS("P4P460FE65537"); }
        private void EOut_Click(object sender, RoutedEventArgs e) { int K = Int32.Parse(ECM.Text); K = -K * 100; SendS("P4M" + K.ToString()); }
        private void EIn_Click(object sender, RoutedEventArgs e) { int K = Int32.Parse(ECM.Text); K = K * 100; SendS("P4M" + K.ToString()); }
        private void ELock_Click(object sender, RoutedEventArgs e) { SendS("P4P460FE1"); }
        private void EOff_Click(object sender, RoutedEventArgs e) { SendS("P40"); }

        private void MFwTime_Click(object sender, RoutedEventArgs e) { int K = Int32.Parse(MTime.Text); int K2 = Int32.Parse(MPwr.Text);  SendS("r9tf" + ((K2<10)? "0" : "") + K2.ToString() + K.ToString()); }
        private void MRvTime_Click(object sender, RoutedEventArgs e) { int K = Int32.Parse(MTime.Text); int K2 = Int32.Parse(MPwr.Text); SendS("r9tr" + ((K2 < 10) ? "0" : "") + K2.ToString() + K.ToString()); }
        private void MFwLoc_Click(object sender, RoutedEventArgs e) { int K = Int32.Parse(MLoc.Text); int K2 = Int32.Parse(MPwr.Text); SendS("r9lf" + ((K2 < 10) ? "0" : "") + K2.ToString() + MFlag.Text + K.ToString()); }
        private void MRvLoc_Click(object sender, RoutedEventArgs e) { int K = Int32.Parse(MLoc.Text); int K2 = Int32.Parse(MPwr.Text); SendS("r9lr" + ((K2 < 10) ? "0" : "") + K2.ToString() + MFlag.Text + K.ToString()); }
        private void MStop_Click(object sender, RoutedEventArgs e) { int K = Int32.Parse(MTime.Text); SendS("r9tf000"); }
        private void RSpeed_Click(object sender, RoutedEventArgs e) { SendS("P1P46081" + RSpeed.Text); }
        private void Enc_Click(object sender, RoutedEventArgs e) { SendS("E" + MBSelected()); }
        private void E_Click(object sender, RoutedEventArgs e) { SendS("E"); }
        private void Dev1_Click(object sender, RoutedEventArgs e) { SendS("DEV1"); }
        private void Dev2_Click(object sender, RoutedEventArgs e) { SendS("DEV2"); }
        private void FlagResync_Click(object sender, RoutedEventArgs e) { SendS("FLAGSYNC"); }
        


        private void Zero_Click(object sender, RoutedEventArgs e) { SendS("Z" + MBSelected()); }
        
        private void Stop_Click(object sender, RoutedEventArgs e) { SendS("m" + MBSelected() + "fb72000"); }

        private void X_Encoder_Click(object sender, RoutedEventArgs e) { int K = Int32.Parse(XLoc.Text); SendS("m50c" + K.ToString()); }
        private void X_Zero_Click(object sender, RoutedEventArgs e) { SendS("Z5"); }

        private void LGoLocation_Click(object sender, RoutedEventArgs e)
        {
            int K = Int32.Parse(LSpeed.Text);
            String ST = K.ToString();
            if (K < 10) ST = "00" + ST;
            else if (K < 100) ST = "0" + ST;
            SendS("P2A" + ST + LLoc.Text);
        }
        private void LGetLocation_Click(object sender, RoutedEventArgs e) { SendS("P2G6064"); }
            


        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            RecvMsgs.Text = "";
            RecvMsgs.Focus();
            RecvMsgs.CaretIndex = RecvMsgs.Text.Length;
        }

        void CmdClick( int K )
        {
            var Cs = Cmds[K];
            if (Cs == null)
                return;
            foreach (var C in Cs)
            {
                string S = C.ToString();
                if (S.StartsWith("DELAY"))
                {
                    Thread.Sleep(Int32.Parse(S.Substring(5).Trim()));
                }
                else
                if (S.StartsWith("WAIT"))
                {
                    int Timeout = 0;
                    while (bGot_AddRecvMsgs == false && Timeout < 100)
                    {
                        Thread.Sleep(100);
                        Timeout++;
                    }
                }
                else
                {
                    SendS(S);
                    // AddRecvMsgs("*" + S);
                    bGot_AddRecvMsgs = false;
                }
            }
        }
        private void Cmd0_Click(object sender, RoutedEventArgs e) { Task.Run(() => CmdClick(0)); }
        private void Cmd1_Click(object sender, RoutedEventArgs e) { CmdClick(1); }
        private void Cmd2_Click(object sender, RoutedEventArgs e) { CmdClick(2); }
        private void Cmd3_Click(object sender, RoutedEventArgs e) { CmdClick(3); }
        private void Cmd4_Click(object sender, RoutedEventArgs e) { CmdClick(4); }
        private void Cmd5_Click(object sender, RoutedEventArgs e) { CmdClick(5); }
        private void Cmd6_Click(object sender, RoutedEventArgs e) { CmdClick(6); }
        private void Cmd7_Click(object sender, RoutedEventArgs e) { CmdClick(7); }
        private void Cmd8_Click(object sender, RoutedEventArgs e) { CmdClick(8); }
        private void Cmd9_Click(object sender, RoutedEventArgs e) { CmdClick(9); }

        private void StartServer_Click(object sender, RoutedEventArgs e)
        {
            StartServerButton.IsEnabled = false;
            client = null;
            nwStream = null;
            string hostName = System.Net.Dns.GetHostName();
            IPAddress localIP = null;
            NetworkInterface[] allNICs = NetworkInterface.GetAllNetworkInterfaces();
            foreach (var nic in allNICs)
            {
                var ipProp = nic.GetIPProperties();
                var gwAddresses = ipProp.GatewayAddresses;
                if (gwAddresses.Count > 0 &&
                    gwAddresses.FirstOrDefault(g => g.Address.AddressFamily == AddressFamily.InterNetwork) != null)
                {
                    localIP = ipProp.UnicastAddresses.First(d => d.Address.AddressFamily == AddressFamily.InterNetwork).Address;
                }
            }
            //---listen at the specified IP and port no.---
            IPAddress localAdd = localIP; //  IPAddress.Parse(SERVER_IP);
            listener = new TcpListener(localAdd, PORT_NO + ((Client1.IsChecked==true)? 0 : 2));
            Console.WriteLine("Listening...");
            listener.Start();
            String S = "IP: " + listener.LocalEndpoint.ToString();
            StatusLabel.Content = S;
            task = Task.Run(() => ClientManagmentThread());
        }
    }
}
