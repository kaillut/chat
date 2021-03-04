using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace client_v2
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //creates TCP client with the information given
            Program.client = new System.Net.Sockets.TcpClient(textBox1.Text, int.Parse(textBox2.Text));
            Data data = new Data();
            data.command = Command.Connect;
            data.from = textBox3.Text;
            Program.name = textBox3.Text;
            new FMain().Show();
            Program.client.GetStream().BeginRead(Program.buffer, 0, Program.buffer.Length, Program.Receive, null);
            //waits for the read thread to start
            System.Threading.Thread.Sleep(100);
            //sends connect message
            Program.client.GetStream().Write(Data.getBytes(data),0,Data.getBytes(data).Length);
            this.Close();
            

        }
    }
}
