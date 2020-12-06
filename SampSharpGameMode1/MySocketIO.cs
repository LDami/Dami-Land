using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;

namespace SampSharpGameMode1
{
    public class MySocketIO
    {
        IPAddress ipAdress;
        IPEndPoint remoteEP;
        public enum SocketStatus { DISCONNECTED, CONNECTED };
        Socket socket;
        ClientWebSocket webSocket;

        System.Threading.CancellationTokenSource cts;
        Uri uri;
        public MySocketIO(String ipaddress, int port)
        {
            try
            {
                cts = new System.Threading.CancellationTokenSource();
                uri = new Uri("ws://" + ipaddress + ":" + port);
                webSocket = new ClientWebSocket();
                webSocket.ConnectAsync(uri, cts.Token);
            }
            catch (Exception e)
            {
                Console.WriteLine("MySocketIO.cs - MySocketIO.__MySocketIO:E: " + e);
            }
        }

        protected internal void Reconnect()
        {
            webSocket.CloseAsync(WebSocketCloseStatus.Empty, "", cts.Token);
            webSocket = new ClientWebSocket();
            webSocket.ConnectAsync(uri, cts.Token);
        }

        protected internal int Close()
        {
            if (webSocket == null)
                return 0;
            try
            {
                webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", cts.Token);
                webSocket.Dispose();
            }
            catch(SocketException e)
            {
                Console.WriteLine("MySocketIO.cs - MySocketIO:Close:E: " + e);
            }
            return 1;
        }

        protected internal SocketStatus GetStatus()
        {
            if (webSocket == null)
                return SocketStatus.DISCONNECTED;
            else
            {
                if (webSocket.State == WebSocketState.Open)
                    return SocketStatus.CONNECTED;
                else
                    return SocketStatus.DISCONNECTED;
            }
        }

        protected internal string GetIPAddress()
        {
            return remoteEP.Address.ToString();
        }

        protected internal int Write(String data)
        {
            if (webSocket == null)
                return -1;

            byte[] dataToSend = ASCIIEncoding.ASCII.GetBytes(data);
            var sendBuffer = new ArraySegment<byte>(dataToSend);
            try
            {
                webSocket.SendAsync(sendBuffer, WebSocketMessageType.Text, true, cts.Token);
            }
            catch (SocketException e)
            {
                Console.WriteLine("MySocketIO.cs - MySocketIO:Write:E: " + e);
                Console.WriteLine("MySocketIO.cs - MySocketIO:Write:I: Reconnecting ... ");
                this.Reconnect();
                if (this.GetStatus() == MySocketIO.SocketStatus.CONNECTED)
                {
                    Console.WriteLine("Done");
                    try
                    {
                        webSocket.SendAsync(sendBuffer, WebSocketMessageType.Text, true, cts.Token);
                        Console.WriteLine("MySocketIO.cs - MySocketIO:Write:I: Data sent !");
                    }
                    catch(SocketException ex)
                    {
                        Console.WriteLine("MySocketIO.cs - MySocketIO:Write:E: " + ex);
                        return -1;
                    }
                }
                else
                {
                    Console.WriteLine("KO");
                    return -1;
                }
            }
            return dataToSend.Length;
        }

        protected internal String Read()
        {
            if (socket == null)
                return "";

            byte[] buffer = new byte[socket.ReceiveBufferSize];
            int bytesRead = 0;
            try
            {
                bytesRead = socket.Receive(buffer);
            }
            catch (Exception e)
            {
                Console.WriteLine("MySocketIO.cs - MySocketIO.Read:E: " + e);
            }
            return Encoding.ASCII.GetString(buffer, 0, bytesRead);
        }
    }
}
