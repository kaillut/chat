using System;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace client_v2
{
    
    static class Program
    {
        public static TcpClient client;
        public static string name;
        public static volatile bool stop = false;
        public static byte[] buffer = new byte[2048];
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            new Form1().Show();
            Application.Run();
        }
        public static void Send(Data data)
        {
            //translates the data to bytes and sends it
            client.GetStream().Write(Data.getBytes(data),0,Data.getBytes(data).Length);
        }
        public static void Receive(IAsyncResult asyncResult)
        {
            int received_size = client.GetStream().EndRead(asyncResult);
            byte[] Bdata = new byte[2048];
            //copies the buffer
            Buffer.BlockCopy(buffer, 0, Bdata, 0, received_size);
            Data data = new Data(Bdata);
            HandleData(data);
            //checks if stop given
            if (!stop)
            {
                client.GetStream().BeginRead(buffer, 0, buffer.Length, Receive, null);
            }
        }
        public static void HandleData(Data data)
        {
            //opens new tab if connecting
            if (data.command == Command.Connect)
            {
                FMain.GetMain.Invoke(new Action(() =>
                {
                    TabPage tb = new TabPage(data.from);
                    TextBox textBox = new TextBox();
                    tb.Controls.Add(textBox);
                    textBox.Size = tb.Size;
                    textBox.Enabled = false;
                    textBox.Multiline = true;
                    textBox.Dock = DockStyle.Fill;
                    FMain.GetMain.Controls.OfType<TabControl>().FirstOrDefault().TabPages.Add(tb);
                    tb.Show();
                }));
            }
            //closes tab if someone else disconnects
            else if (data.command == Command.Disconnect && !data.from.Equals(Program.name))
            {
                FMain.GetMain.Invoke(new Action(() =>
                {
                    foreach (TabPage tb in FMain.GetMain.Controls.OfType<TabControl>().FirstOrDefault().TabPages)
                    {
                        if (tb.Text == data.from)
                        {
                            FMain.GetMain.Controls.OfType<TabControl>().FirstOrDefault().TabPages.Remove(tb);
                        }
                    }

                }));

            }
            //shows message
            else if (data.command == Command.Message)
            {
                FMain.GetMain.Invoke(new Action(() =>
                {
                    foreach (TabPage tb in FMain.GetMain.Controls.OfType<TabControl>().FirstOrDefault().TabPages)
                    {
                        if (tb.Text == data.to)
                        {
                            tb.Controls.OfType<TextBox>().FirstOrDefault().AppendText(data.from + " > " + data.message + Environment.NewLine);
                        }
                    }

                }));
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
        public static byte[] getBytes(Data data)
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
