using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;

namespace ServerV2
{
    class Program
    {
        struct clientInfo { public TcpClient client; public string name; }
        struct tcpconbuffer { public TcpClient client; public byte[] buffer; }
        static string[] channels = { "Main" ,"sasda"};
        static List<clientInfo> clients = new List<clientInfo>();
        static TcpListener listener;
        static void Main(string[] args)
        {
            //starts listening port 1234 with tcp
            listener = new TcpListener(IPAddress.Any, 1234);
            listener.Start();
            listener.BeginAcceptTcpClient(new AsyncCallback(OnConnect), null);
            Console.Read();
        }
        static private void OnConnect(IAsyncResult asyncResult)
        {
            //accepts connection
            TcpClient tcpClient = listener.EndAcceptTcpClient(asyncResult);
            //start listeningn again
            listener.BeginAcceptTcpClient(new AsyncCallback(OnConnect), null);
            //creates buffer for the connection
            tcpconbuffer tb;
            tb.client = tcpClient;
            tb.buffer = new byte[2048];
            //starts listening for data
            tcpClient.GetStream().BeginRead(tb.buffer, 0, tb.buffer.Length, OnReceive, tb);
            //sends all the channels
            foreach (string s in channels)
            {
                Data data = new Data();
                data.command = Command.Connect;
                data.from = s;
                tcpClient.GetStream().Write(Data.getBytes(data));
                //setting thread to sleep so that messages are more easily divided
                System.Threading.Thread.Sleep(100);
            }
            //sends info about other users
            foreach (clientInfo ci in clients)
            {
                Data data = new Data();
                data.command = Command.Connect;
                data.from = ci.name;
                tcpClient.GetStream().Write(Data.getBytes(data));
                //setting thread to sleep so that messages are more easily divided
                System.Threading.Thread.Sleep(100);
            }
        }
        static private void OnReceive(IAsyncResult asyncResult)
        {
            tcpconbuffer tb = (tcpconbuffer)asyncResult.AsyncState;
            bool failed = false;
            try
            {
                //tries to end the reading
                tb.client.GetStream().EndRead(asyncResult);
            }
            //incase the connection failed
            catch (System.IO.IOException)
            {
                Console.WriteLine("IOEXP");
                failed = true;
                //tries to sends disconnect order, tells others about disconnect
                foreach (clientInfo clientInfo in clients.ToList())
                {
                    if (clientInfo.client == tb.client)
                    {
                        foreach (clientInfo client in clients)
                        {
                            Data data1 = new Data();
                            data1.command = Command.Disconnect;
                            data1.from = clientInfo.name;
                            try{
                            client.client.GetStream().Write(Data.getBytes(data1));
                            client.client.GetStream().Close();
                            }
                            catch(InvalidOperationException)
                            {}
                            Console.WriteLine(clientInfo.name + " Kicked");
                        }
                        clients.Remove(clientInfo);
                    }
                }
            }
            catch (ObjectDisposedException)
            {}
            //checks if failed or client connection closed
            if (!failed && tb.client.Connected)
            {
                //translates data
                Data data = new Data(tb.buffer);
                //checks command type
                if (data.command == Command.Connect)
                {
                    //sends the same data back as confirmation
                    tb.client.GetStream().Write(Data.getBytes(data));
                    clientInfo ci;
                    ci.client = tb.client;
                    ci.name = data.from;
                    //tells other clients about the user
                    foreach(clientInfo cii in clients)
                    {
                        Data data1 = new Data();
                        data1.command = Command.Connect;
                        data1.from = cii.name;
                        ci.client.GetStream().Write(Data.getBytes(data1));
                        cii.client.GetStream().Write(Data.getBytes(data));
                    }
                    clients.Add(ci);
                    //starts listening again
                    tb.client.GetStream().BeginRead(tb.buffer, 0, tb.buffer.Length, OnReceive, tb);                    
                    Console.WriteLine(data.from + " Connected!");
                    
                }
                else if (data.command == Command.Disconnect)
                {
                    //removes the client from list
                    clientInfo ci;
                    ci.client = tb.client;
                    ci.name = data.from;
                    clients.Remove(ci);
                    //closes stream
                    ci.client.GetStream().Write(Data.getBytes(data));
                    ci.client.Close();
                    Console.WriteLine(data.from + " Disconnected!");
                    //tells others
                    foreach(clientInfo cii in clients)
                    {
                        cii.client.GetStream().Write(Data.getBytes(data));
                    }
                }
                else if (data.command == Command.Message)
                {
                    Console.WriteLine(data.from + " > " + data.to + " : " + data.message);
                    //checks if message to channel or private
                    if (Array.IndexOf(channels, data.to) != -1)
                    {
                        foreach (clientInfo client in clients)
                        {
                            if (client.name.CompareTo(data.from) != 0)
                            {
                                //sent to everyone
                                client.client.GetStream().Write(Data.getBytes(data));
                            }
                        }
                    }
                    else
                    {
                        foreach (clientInfo client in clients)
                        {
                            if (client.name.CompareTo(data.to) == 0 && client.name.CompareTo(data.from) != 0)
                            {
                                //sent only if name matches message recipient
                                client.client.GetStream().Write(Data.getBytes(data));
                            }
                        }
                    }
                    //listens for more messages
                    tb.client.GetStream().BeginRead(tb.buffer, 0, tb.buffer.Length, OnReceive, tb);
                }
            }
        }

    }
    enum Command
    {
        Connect,
        AddChannel,
        Message,
        Disconnect
    }
    class Data
    {
        public Command command;
        public string message;
        public string from;
        public string to;

        public Data()
        { }
        public Data(byte[] bytes)
        {
            command = (Command)BitConverter.ToInt32(bytes, 0);
            int from_l = BitConverter.ToInt32(bytes, 4);
            int to_l = BitConverter.ToInt32(bytes, 8);
            int message_l = BitConverter.ToInt32(bytes, 12);
            if (from_l > 0)
            {
                from = System.Text.Encoding.UTF8.GetString(bytes, 16, from_l);
                if (to_l > 0)
                {
                    to = System.Text.Encoding.UTF8.GetString(bytes, 16 + from_l, to_l);
                    if (message_l > 0)
                    {
                        message = System.Text.Encoding.UTF8.GetString(bytes, 16 + from_l + to_l, message_l);
                    }
                }
            }
            else
            { throw new Exception("No name in message"); }
        }
        static public byte[] getBytes(Data data)
        {
            List<byte> bytes = new List<byte>();
            bytes.AddRange(BitConverter.GetBytes((Int32)data.command));
            bytes.AddRange(BitConverter.GetBytes(data.from.Length));
            if (data.to != null)
            {
                bytes.AddRange(BitConverter.GetBytes(data.to.Length));
                if (data.message != null)
                {
                    bytes.AddRange(BitConverter.GetBytes(data.message.Length));
                    bytes.AddRange(System.Text.Encoding.UTF8.GetBytes(data.from));
                    bytes.AddRange(System.Text.Encoding.UTF8.GetBytes(data.to));
                    bytes.AddRange(System.Text.Encoding.UTF8.GetBytes(data.message));
                    return bytes.ToArray();
                }
                else
                {
                    throw new Exception("Malformed message");
                }
            }
            else
            {
                bytes.AddRange(BitConverter.GetBytes(0));
                bytes.AddRange(BitConverter.GetBytes(0));
                bytes.AddRange(System.Text.Encoding.UTF8.GetBytes(data.from));
            }
            return bytes.ToArray();
        }
    }
}
