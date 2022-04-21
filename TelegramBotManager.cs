using System;
using System.Net;
using System.IO;

namespace WebServer.Module.Telegram
{
    public class TelegramBotManager
    {

        private static readonly string baseUrl = @"https://api.telegram.org/bot";
        private static readonly string tokenErrorRoom = "Input room token";
        private static readonly string chatIDErrorRoom = "Input room ID";

        private static readonly string tokenCriticalRoom = "Input room2 token";
        private static readonly string chatIDCriticalRoom = "Input room2 ID";

        private static readonly string _token = "Input token";

        static SecurityProtocolType securityDefault = ServicePointManager.SecurityProtocol;
        public static void init(string DB_IP, int DB_port)
        {

            if (DB_IP != "" && DB_port > 0 && ConnectTest(DB_IP, DB_port))
            {
                m_RedisAnalytics = new RedisClient(DB_IP, DB_port);
            }

        }

        private static bool ConnectTest(string ip, int port)
        {
            bool result = false;
            Socket socket = null;
            try
            {
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, false);
                IAsyncResult ret = socket.BeginConnect(ip, port, null, null);
                result = ret.AsyncWaitHandle.WaitOne(100, true);
            }
            catch
            {
            }
            finally
            {
                if (socket != null)
                {
                    socket.Close();
                }
            }
            return result;
        }

        public static bool CheckServerState()
        {
            if ("Input Send Log Condition Check")
                return true;

            return false;
        }

        public static void SendMessageErrorRoom(string text, string selectServer)
        {
            securityDefault = ServicePointManager.SecurityProtocol;
            
            if(CheckServerState())
            {
                text = "[Server" + selectServer + "]" + text;
                SendMessage(chatIDErrorRoom, text, tokenErrorRoom, selectServer);
            }
        }


        public static void SendMessageErrorRoom_Error(string text, string packetType, string selectServer)
        {
            securityDefault = ServicePointManager.SecurityProtocol;
            try
            {
                if (CheckServerState())
                {
                    string sendType = checkErrorType(packetType);
                    switch (sendType)
                    {
                        case "NoSend":
                            return;
                        default:
                            text = "[" + sendType + "]" + "[Server" + selectServer + "]" + "[" + packetType + "]" + text;
                            break;
                    }
                    SendMessage(chatIDErrorRoom, text, tokenErrorRoom, selectServer);
                }
            }
            catch (Exception e)
            {
                text = "[Telegram - SendMessage_Error]" + "[Server" + selectServer + "]" + "[" + packetType + "]" + text + "\n" + e.ToString();
                SendMessage(chatIDCriticalRoom, text, tokenCriticalRoom, selectServer);
            }
            
        }

        public static void SendMessage(string chatID, string text, string token, string selectServer)
        {
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                text = text.Replace('#', '@');
                text = text.Replace('&', '@');//텔레그램
                string Url = string.Format("{0}{1}/sendMessage?chat_id={2}&text={3}", baseUrl, token, chatID, text);

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url);
                //request.AutomaticDecompression = DecompressionMethods.GZip;
                request.GetResponse();
                ServicePointManager.SecurityProtocol = securityDefault;
            }
            catch (Exception e)
            {
                ServicePointManager.SecurityProtocol = securityDefault;
            }
           
        }

        public static void SendMessageServerState(string text)
        {
            securityDefault = ServicePointManager.SecurityProtocol;

            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                text = text.Replace('#', '@');
                text = text.Replace('&', '@');//텔레그램
                string Url = string.Format("{0}{1}/sendMessage?chat_id={2}&text={3}", baseUrl, _token, chatIDCriticalRoom, text);

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url);
                request.GetResponse();
                ServicePointManager.SecurityProtocol = securityDefault;
            }
            catch
            {
                ServicePointManager.SecurityProtocol = securityDefault;
            }

        }
        public static object Lock = new object();
        private static RedisClient redis;

        public static string checkErrorType(string type)
        {
            if (redis == null) return "NoDB";
            string Error = "Info";
            string key = gameserverutil.telegramError + type;
            lock (Lock)
            {
                if (redis.Exists(key) != 0)
                {
                    string getValue = ByteToString(m_RedisAnalytics.Get(key));
                    if (getValue == "")
                    {
                        redis.Set(key, ToByte(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                        redis.Expire(key, 60 * 60 * 24 * 2);
                    }
                    else
                    {
                        DateTime initTime = DateTime.Parse(getValue);

                        if (initTime.AddMinutes(1) > DateTime.Now)
                        {
                            Error = "NoSend";
                        }
                        else
                        {
                            if (initTime.AddMinutes(5) > DateTime.Now)
                                Error = "Critical";
                            else
                                Error = "Warning";
                            TimeSpan expireTime = redis.GetTimeToLive(key);
                            redis.Set(key, ToByte(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                            redis.Expire(key, (int)expireTime.TotalSeconds);
                        }

                    }
                }
                else
                {
                    Error = "Info";
                    redis.Set(key, ToByte(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                    redis.Expire(key, 60 * 60 * 24 * 2);
                }
            }
            return Error;
        }

        public static byte[] ToByte(string strText)
        {
            if (strText.Length > 0)
                return System.Text.Encoding.Unicode.GetBytes(strText);

            return null;
        }

        public static string ByteToString(byte[] byteText)
        {
            if (byteText != null)
                return System.Text.Encoding.Unicode.GetString(byteText);

            return "";
        }

    }
}