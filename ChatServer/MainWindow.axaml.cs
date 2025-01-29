using Avalonia.Controls;
using ChatCommon;
using System.IO;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Avalonia;

namespace ChatServer
{
    public partial class MainWindow : Window
    {
        IPEndPoint endPoint;
        ChatServer server;
        //bool forceClose = false;

        public MainWindow()
        {
            InitializeComponent();

            //notifyIcon1.Icon = System.Drawing.SystemIcons.Application;

            IPAddress ipAddress = GetLocalIPAddress();
            endPoint = new IPEndPoint(ipAddress, 11299);

            this.Title = ipAddress.ToString();

            server = new ChatServer();
            UpdateLatestClient();

            Thread thread = new Thread(new ThreadStart(ServerMain));
            thread.IsBackground = true;
            thread.Start();

            this.Closing += MainWindow_Closing;
        }

        private void MainWindow_Closing(object sender, WindowClosingEventArgs e)
        {
            //if (forceClose)
            //    return;
            //if (e.CloseReason == WindowCloseReason.WindowClosing)
            //{
            //    this.Hide();
            //    e.Cancel = true;
            //}
        }

        public static IPAddress GetLocalIPAddress()
        {
            return IPAddress.Parse("127.0.0.1");
            //var host = Dns.GetHostEntry(Dns.GetHostName());
            //foreach (var ip in host.AddressList)
            //{
            //    if (ip.AddressFamily == AddressFamily.InterNetwork)
            //    {
            //        return ip;
            //    }
            //}
            //throw new System.Exception("No network adapters with an IPv4 address in the system!");
        }
        void UpdateLatestClient()
        {
            string latestClientFile = Path.Combine("LatestClient", FileHelper.ChatClientFile);
            byte[] latestClientHash = FileHelper.CalcHash(latestClientFile);
            byte[] latestClientBytes = new byte[0];
            if (latestClientHash.Length > 0)
            {
                using (var fs = new FileStream(latestClientFile, FileMode.Open))
                {
                    latestClientBytes = new byte[fs.Length];
                    fs.Read(latestClientBytes, 0, latestClientBytes.Length);
                }
            }
            server._latestClientHash = latestClientHash;
            server._latestClientBytes = latestClientBytes;
        }

        void ServerMain()
        {
            server.Start(endPoint);
            while (true)
            {
                server.Update(true);
            }
        }

        //void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        //{
        //    Show();
        //}

        private void Button_Close_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            //notifyIcon1.Visible = false;
            //this.forceClose = true;
            Close();
        }

        private void Button_UpdateClient_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            UpdateLatestClient();
        }

        private void Button_CopyIp_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Clipboard.SetTextAsync(endPoint.Address.ToString());
        }
    }
}