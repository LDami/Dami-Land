using System;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;

namespace SampSharpGameMode1
{
    public class MySocketIO
    {
        IPAddress ipAdress;
        IPEndPoint remoteEP;
        public enum SocketStatus { DISCONNECTED, CONNECTED };
        Socket socket;
        NetworkStream ns;

        public MySocketIO(String ipaddress, int port)
        {
            try
            {
                ipAdress = IPAddress.Parse(ipaddress);
                remoteEP = new IPEndPoint(ipAdress, port);
            }
            catch (SocketException e)
            {
                Logger.WriteLineAndClose("MySocketIO.cs - MySocketIO.__MySocketIO:E: " + e.Message);
            }
        }

        protected internal void Connect()
        {
            Thread t = new Thread(new ThreadStart(() => {
                socket = new Socket(ipAdress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                while (!socket.Connected)
                {
                    try
                    {
                        socket.Dispose();
                        socket = new Socket(ipAdress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                        socket.Connect(remoteEP);
                        ns = new NetworkStream(socket);
                    }
                    catch (SocketException e)
                    {
                        Logger.WriteLineAndClose("MySocketIO.cs - MySocketIO.Connect:E: Can't open socket: " + e.Message);
                    }
                    Thread.Sleep(5000);
                }
                Logger.WriteLineAndClose("MySocketIO.cs - MySocketIO.Connect:I: Socket open !");
                //this.CheckState();
            }));
            t.Start();
        }
        protected internal void Reconnect()
        {
            if (socket != null)
                socket.Close();
            this.Connect();
        }

        private void CheckState()
        {
            Thread t = new Thread(new ThreadStart(() => {
                byte[] ping = ASCIIEncoding.ASCII.GetBytes("ping");
                while (socket != null)
                {
                    try
                    {
                        //socket.Send(ping, 4, SocketFlags.None);
                        if(ns != null)
                        {
                            ns.Write(ping, 0, ping.Length);
                            ns.Flush();
                            Thread.Sleep(30);
                        }
                        else
                            Logger.WriteLineAndClose("MySocketIO.cs - MySocketIO.CheckState:E: Can't send ping: NetworkStream is null !");
                    }
                    catch(SocketException e)
                    {
                        Logger.WriteLineAndClose("MySocketIO.cs - MySocketIO.CheckState:E: Can't send ping: " + e.Message);
                    }
                    catch (ObjectDisposedException e)
                    {
                        Logger.WriteLineAndClose("MySocketIO.cs - MySocketIO:Write:E: " + e.Message);
                    }
                    if (!socket.Connected)
                    {
                        Logger.WriteLineAndClose("MySocketIO.cs - MySocketIO.CheckState:E: Socket is not longer open, trying to reconnect ...");
                        this.Reconnect();
                    }
                    Thread.Sleep(1000);
                }
                Logger.WriteLineAndClose("MySocketIO.cs - MySocketIO.CheckState:W: Socket is now null");
            }));
            Logger.WriteLineAndClose("MySocketIO.cs - MySocketIO.CheckState:I: Starting watchgdog");
            t.Start();
            Logger.WriteLineAndClose("MySocketIO.cs - MySocketIO.CheckState:I: Watchgdog started");
        }

        protected internal int Close()
        {
            if (socket == null)
                return 0;
            try
            {
                Logger.WriteLineAndClose("MySocketIO.cs - MySocketIO:Close:I: Closing Socket");
                ns.Close();
                socket.Close();
                socket.Dispose();
            }
            catch(SocketException e)
            {
                Logger.WriteLineAndClose("MySocketIO.cs - MySocketIO:Close:E: " + e.Message);
            }
            return 1;
        }

        protected internal SocketStatus GetStatus()
        {
            if (socket == null)
                return SocketStatus.DISCONNECTED;
            else
            {
                if (socket.Connected)
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
            if (socket == null || ns == null)
                return -1;

            byte[] dataToSend = ASCIIEncoding.ASCII.GetBytes(data);
            try
            {
                //socket.Send(dataToSend, dataToSend.Length, SocketFlags.None);
                ns.Write(dataToSend, 0, dataToSend.Length);
                ns.Flush();
                Thread.Sleep(30);
            }
            catch (SocketException e)
            {
                Logger.WriteLineAndClose("MySocketIO.cs - MySocketIO:Write:E: " + e.Message);
                Logger.WriteAndClose("MySocketIO.cs - MySocketIO:Write:I: Reconnecting ... ");
                this.Reconnect();
                if (this.GetStatus() == MySocketIO.SocketStatus.CONNECTED)
                {
                    Logger.WriteLineAndClose("Done");
                    try
                    {
                        socket.Send(dataToSend, dataToSend.Length, SocketFlags.None);
                        Logger.WriteLineAndClose("MySocketIO.cs - MySocketIO:Write:I: Data sent !");
                    }
                    catch (SocketException ex)
                    {
                        Logger.WriteLineAndClose("MySocketIO.cs - MySocketIO:Write:E: " + ex.Message);
                        return -1;
                    }
                }
                else
                {
                    Logger.WriteLineAndClose("MySocketIO.cs - MySocketIO:Write: E: KO");
                    return -1;
                }
            }
            catch (ObjectDisposedException e)
            {
                Logger.WriteLineAndClose("MySocketIO.cs - MySocketIO:Write:E: " + e.Message);
            }
            return dataToSend.Length;
        }
        protected internal int WriteAsync(String data)
        {
            if (socket == null || ns == null)
                return -1;

            byte[] dataToSend = ASCIIEncoding.ASCII.GetBytes(data);
            try
            {
                //socket.Send(dataToSend, dataToSend.Length, SocketFlags.None);
                ns.WriteAsync(dataToSend, 0, dataToSend.Length);
            }
            catch (SocketException e)
            {
                Logger.WriteLineAndClose("MySocketIO.cs - MySocketIO:Write:E: " + e.Message);
            }
            catch (ObjectDisposedException e)
            {
                Logger.WriteLineAndClose("MySocketIO.cs - MySocketIO:Write:E: " + e.Message);
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
