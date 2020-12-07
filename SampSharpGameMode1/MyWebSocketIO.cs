using System;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;

namespace SampSharpGameMode1
{
    public class MyWebSocketIO
    {
        IPAddress ipAdress;
        IPEndPoint remoteEP;
        public enum SocketStatus { DISCONNECTED, CONNECTED };
        Socket socket;
        ClientWebSocket webSocket;

        System.Threading.CancellationTokenSource cts;
        Uri uri;
        public MyWebSocketIO(String ipaddress, int port)
        {
            cts = new CancellationTokenSource();
            uri = new Uri("ws://" + ipaddress + ":" + port);
        }

        protected internal void Connect()
        {
            Thread t = new Thread(new ThreadStart(async () => {
                webSocket = new ClientWebSocket();
                while (webSocket.State != WebSocketState.Open)
                {
                    webSocket = new ClientWebSocket();
                    try
                    {
                        await webSocket.ConnectAsync(uri, cts.Token);
                    }
                    catch (WebSocketException e)
                    {
                        Console.WriteLine("MySocketIO.cs - MySocketIO.Connect:E: Can't open socket: " + e.Message);
                        webSocket.Abort();
                    }
                    if (webSocket.State == WebSocketState.Open)
                    {
                        Console.WriteLine("MySocketIO.cs - MySocketIO.Connect:I: Socket open");
                        this.CheckState();
                    }
                    else
                        Console.WriteLine("MySocketIO.cs - MySocketIO.Connect:E: Socket not open");
                    Thread.Sleep(5000);
                }
            }));
            t.Start();
        }

        protected internal async void Reconnect()
        {
            await webSocket.CloseAsync(WebSocketCloseStatus.Empty, "", cts.Token);
            this.Connect();
        }

        private void CheckState()
        {
            Thread t = new Thread(new ThreadStart(async () => {
                byte[] dataToSend = ASCIIEncoding.ASCII.GetBytes("ping");
                var sendBuffer = new ArraySegment<byte>(dataToSend);
                string dataToReceive;
                var receiveBuffer = new ArraySegment<byte>();
                while (webSocket != null)
                {
                    await webSocket.SendAsync(sendBuffer, WebSocketMessageType.Text, true, cts.Token);
                    await webSocket.ReceiveAsync(receiveBuffer, cts.Token);
                    dataToReceive = ASCIIEncoding.ASCII.GetString(receiveBuffer);
                    if (dataToReceive.Equals("pong") && webSocket.State != WebSocketState.Open)
                        Console.WriteLine("MySocketIO.cs - MySocketIO.Connect:E: Socket is not longer open");
                    Thread.Sleep(1000);
                }
                Console.WriteLine("MySocketIO.cs - MySocketIO.Connect:W: Socket is now null");
            }));
            Console.WriteLine("MySocketIO.cs - MySocketIO.Connect:I: Starting watchgdog");
            t.Start();
            Console.WriteLine("MySocketIO.cs - MySocketIO.Connect:I: Watchgdog started");
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
            catch (SocketException e)
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
                if (this.GetStatus() == MyWebSocketIO.SocketStatus.CONNECTED)
                {
                    Console.WriteLine("Done");
                    try
                    {
                        webSocket.SendAsync(sendBuffer, WebSocketMessageType.Text, true, cts.Token);
                        Console.WriteLine("MySocketIO.cs - MySocketIO:Write:I: Data sent !");
                    }
                    catch (SocketException ex)
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
