using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using ChatCommon;
using NetworkFramework;
using System;
using System.IO;
using System.Linq;
using System.Net;

namespace ChatLauncher
{
    public partial class MainWindow : Window
    {
        public bool AllowLaunch;
        public string MyName = "";
        public Client Client;

        byte[] _currentHash = Array.Empty<byte>();
        byte[] _targetHash = Array.Empty<byte>();

        DispatcherTimer timerUpdate;
        DispatcherTimer timerConnection;

        public MainWindow()
        {
            InitializeComponent();

            timerUpdate = new DispatcherTimer();
            timerConnection = new DispatcherTimer();

            btnEnter.Click += (s, e) =>
            {
                var endPoint = new IPEndPoint(IPAddress.Parse(tbServerAddress.Text), Defaults.Port);
                if (Client.Connect(endPoint))
                {
                    PrintStatus("Connecting...");
                    timerUpdate.Start();
                    MyName = tbUserName.Text;
                    if (string.IsNullOrWhiteSpace(MyName))
                        MyName = Environment.UserName;
                    SetUIEnabled(false);
                }
                else
                {
                    PrintStatus("Failed to connect");
                }
                timerConnection.Start();
            };
        }

        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);

            Client = new Client();

            PrintStatus("");

            tbServerAddress.Text = "127.0.0.1";
            tbUserName.Text = Environment.UserName;

            _currentHash = FileHelper.CalcHash(FileHelper.ChatClientFile);

            timerUpdate.Interval = TimeSpan.FromMicroseconds(100);
            timerUpdate.Tick += (s, ea) => {
                while (!Client.Incoming.IsEmpty())
                {
                    OnMessage(Client.Incoming.PopFront());
                }
            };

            timerConnection.Interval = TimeSpan.FromMilliseconds(5000);
            timerConnection.Tick += (s, ea) => {
                timerConnection.Stop();
                if (!Client.IsConnected)
                {
                    timerUpdate.Stop();
                    Client.Disconnect();
                    PrintStatus("Failed to connect by timeout. Server is unreachable.");
                    SetUIEnabled(true);
                }
            };
        }

        void PrintStatus(string text)
        {
            labelStatus.Text = text;
        }

        void SetUIEnabled(bool enabled)
        {
            tbUserName.IsEnabled = enabled;
            tbServerAddress.IsEnabled = enabled;
            btnEnter.IsEnabled = enabled;
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            timerConnection.Stop();
            timerUpdate.Stop();
            if (!AllowLaunch)
                Client?.Disconnect();
        }

        void OnMessage(Msg msg)
        {
            var msgType = (ChatMsg)msg.Id;
            switch (msgType)
            {
                case ChatMsg.Client_Accepted:
                    Msg msgCheckUpdates = (int)ChatMsg.Update_VersionInfo;
                    msgCheckUpdates.PushBytes(_currentHash);
                    Client.Send(msgCheckUpdates);
                    break;

                case ChatMsg.Update_VersionInfo:
                    int updateAvailable = msg.PopInt();
                    if (updateAvailable > 0)
                    {
                        _targetHash = msg.PopBytes();
                        Client.Send((int)ChatMsg.Update_Load);
                    }
                    else
                    {
                        AllowLaunch = true;
                        Close();
                    }
                    break;

                case ChatMsg.Update_Load:
                    var filename = msg.PopStr();
                    var data = msg.PopBytes();
                    File.WriteAllBytes(filename, data);
                    if (_targetHash.Length > 0 &&
                        Enumerable.SequenceEqual(_targetHash, FileHelper.CalcHash(filename)))
                    {
                        AllowLaunch = true;
                        Close();
                    }
                    else
                    {
                        Client.Disconnect();
                        Close();
                    }
                    break;
            }
        }
    }
}