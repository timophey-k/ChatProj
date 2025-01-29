using ChatCommon;
using NetworkFramework;
using System.Collections.Generic;
using System.Linq;

namespace ChatServer {
    public class ChatServer : Server {
        Dictionary<int, ChatMember> _members = new Dictionary<int, ChatMember>();
        TSQueue<int> _lostIds = new TSQueue<int>();
        List<ChatMessage> _history = new List<ChatMessage>();

        public byte[] _latestClientHash = new byte[0];
        public byte[] _latestClientBytes = new byte[0];

        public override void OnClientValidated(Connection client) {
            var msg = (int)ChatMsg.Client_Accepted;
            client.Send(msg);
        }

        protected override void OnClientDisconnect(Connection client) {
            _lostIds.PushBack(client.ID);
        }

        protected override void OnMessage(Msg msg) {
            while(!_lostIds.IsEmpty()) {
                int lostId = _lostIds.PopFront();
                if(_members.ContainsKey(lostId)) {
                    _members.Remove(lostId);
                    Msg msgRem = (int)ChatMsg.Chat_RemoveMember;
                    msgRem.PushInt(lostId);
                    MessageAllClients(msgRem);
                }
            }

            var client = msg.Remote;
            switch((ChatMsg)msg.Id) {
                case ChatMsg.Update_VersionInfo:
                    var clientHashBytes = msg.PopBytes();
                    bool updateAvailable = _latestClientHash.Length > 0 &&
                        !Enumerable.SequenceEqual(_latestClientHash, clientHashBytes);
                    Msg msgVerInfo = (int)ChatMsg.Update_VersionInfo;
                    if(updateAvailable) {
                        msgVerInfo.PushBytes(_latestClientHash);
                        msgVerInfo.PushInt(1);
                    }
                    else {
                        msgVerInfo.PushInt(0);
                    }
                    client.Send(msgVerInfo);
                    break;

                case ChatMsg.Update_Load:
                    Msg msgFileData = (int)ChatMsg.Update_Load;
                    msgFileData.PushBytes(_latestClientBytes);
                    msgFileData.PushStr(FileHelper.ChatClientFile);
                    client.Send(msgFileData);
                    break;

                case ChatMsg.Client_EnterChat:
                    var newMember = new ChatMember();
                    newMember.Name = msg.PopStr();
                    newMember.Id = client.ID;
                    _members[newMember.Id] = newMember;

                    Msg msgAssign = (int)ChatMsg.Client_AssignId;
                    msgAssign.PushInt(newMember.Id);
                    MessageClient(client, msgAssign);

                    Msg msgAddMember = (int)ChatMsg.Chat_AddMember;
                    msgAddMember.PushInt(newMember.Id);
                    msgAddMember.PushStr(newMember.Name);
                    MessageAllClients(msgAddMember);

                    foreach(var id in _members.Keys) {
                        var otherMember = _members[id];
                        Msg msgAddOtherMember = (int)ChatMsg.Chat_AddMember;
                        msgAddOtherMember.PushInt(otherMember.Id);
                        msgAddOtherMember.PushStr(otherMember.Name);
                        MessageClient(client, msgAddOtherMember);
                    }
                    foreach(var historyMsg in _history) {
                        Msg msgHistory = (int)ChatMsg.Chat_SendMessage;
                        msgHistory.PushInt(historyMsg.Id);
                        msgHistory.PushStr(historyMsg.Name);
                        msgHistory.PushStr(historyMsg.Text);
                        MessageClient(client, msgHistory);
                    }

                    break;
                case ChatMsg.Chat_AddMember:
                    break;
                case ChatMsg.Chat_RemoveMember:
                    break;
                case ChatMsg.Chat_SendMessage:
                    MessageAllClients(msg);
                    var m = msg.Clone();
                    var text = m.PopStr();
                    var name = m.PopStr();
                    var senderId = m.PopInt();
                    _history.Add(new ChatMessage() { Id = senderId, Name = name, Text = text });
                    if(_history.Count > 100)
                        _history.RemoveAt(0);
                    break;
                default:
                    break;
            }
        }
    }

}
