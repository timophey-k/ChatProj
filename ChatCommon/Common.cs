
using System.IO;
using System.Security.Cryptography;

namespace ChatCommon {
    public class Defaults {
        public const int Port = 11299;
    }

    public enum ChatMsg {
        Update_VersionInfo,
        Update_Load,

        Client_Accepted,
        Client_AssignId,
        Client_EnterChat,

        Chat_AddMember,
        Chat_RemoveMember,
        Chat_SendMessage
    }

    public class ChatMessage
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Text { get; set; }
        public bool MyMessage { get; set; }
    }

    public class ChatMember
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }


    public class FileHelper {
        public const string ChatClientFile = "ChatClient.dll";

        public static byte[] CalcHash(string file) {
            if(File.Exists(file)) {
                using(var md5 = MD5.Create()) {
                    using(var stream = File.OpenRead(file)) {
                        return md5.ComputeHash(stream);
                    }
                }
            }
            return new byte[0];
        }
    }
}
