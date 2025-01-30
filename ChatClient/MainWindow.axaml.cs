using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using ChatCommon;
using NetworkFramework;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using Avalonia.Data.Converters;

namespace ChatClient;

public partial class MainWindow : Window
{
    DispatcherTimer timerUpdate;
    Client _client;
    int _myId;
    string _myName;

    Dictionary<int, ChatMember> _online = new ();
    ObservableCollection<ChatMessage> _messages = new();

    public MainWindow()
    {
        InitializeComponent();

        listBoxMembers.DisplayMemberBinding = new Binding("Name");
        listMessages.ItemsSource = _messages;
        listMessages.AutoScrollToSelectedItem = true;

        timerUpdate = new DispatcherTimer();
        timerUpdate.Interval = TimeSpan.FromMilliseconds(100);

        textEdit1.KeyDown += (s, e) =>
        {
            if (e.Key == Avalonia.Input.Key.Enter)
                Send();
        };
    }

    public void SetLoginInfo(Client client, string name) 
    {
        _client = client;
        _myName = name;
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        timerUpdate.Tick += (s, ea) => {
            while (_client.Incoming.TryPopFront(out var msg))
            {
                OnMessage(msg);
            }
        };
        timerUpdate.Start();

        Msg msgEnter = (int)ChatMsg.Client_EnterChat;
        msgEnter.PushStr(_myName);
        _client.Send(msgEnter);
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        timerUpdate?.Stop();
        _client?.Disconnect();
    }

    void OnMessage(Msg msg)
    {
        var msgType = (ChatMsg)msg.Id;
        switch (msgType)
        {
            case ChatMsg.Client_AssignId:
                _myId = msg.PopInt();
                break;

            case ChatMsg.Chat_AddMember:
                var name = msg.PopStr();
                var id = msg.PopInt();
                _online[id] = new ChatMember { Name = name, Id = id };
                UpdateMembersList();
                break;

            case ChatMsg.Chat_RemoveMember:
                var idRemoved = msg.PopInt();
                if (_online.ContainsKey(idRemoved))
                    _online.Remove(idRemoved);
                UpdateMembersList();
                break;

            case ChatMsg.Chat_SendMessage:
                var text = msg.PopStr();
                var fromName = msg.PopStr();
                var fromId = msg.PopInt();
                var from = _online.ContainsKey(fromId) ? _online[fromId].Name : fromName;
                var chatMsg = new ChatMessage() { Name = from, Text = text, MyMessage = fromId == _myId };
                _messages.Add(chatMsg);
                listMessages.SelectedItem = chatMsg;
                break;
        }
    }

    void UpdateMembersList()
    {
        listBoxMembers.ItemsSource = null;
        listBoxMembers.ItemsSource = _online.Values;
    }

    void Send()
    {
        var txt = textEdit1.Text;
        if (string.IsNullOrWhiteSpace(txt))
            return;
        if (txt.Length > 500)
            txt = txt.Substring(0, 500);
        Msg msg = (int)ChatMsg.Chat_SendMessage;
        msg.PushInt(_myId);
        msg.PushStr(_myName);
        msg.PushStr(txt);
        _client.Send(msg);
        textEdit1.Text = null;
    }
}

public class MyMessageBackgroundConverter : MarkupExtension, IMultiValueConverter
{
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return this;
    }

    public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Count == 3 && values[0] is bool b)
        {
            if (b)
            {
                return values[2];
            }
            else return values[1];
        }

        return AvaloniaProperty.UnsetValue;
    }
}