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
    public partial class FMain : Form
    {
        public static FMain GetMain;
        public FMain()
        {
            GetMain = this;
            InitializeComponent();
            this.FormClosed += new FormClosedEventHandler(closing);
            this.Text = Program.name;
            this.Name = Program.name;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //forms the datablock and sends it
            Data data = new Data();
            data.command = Command.Message;
            data.to = tabControl1.SelectedTab.Text;
            //sets the message into the chat
            tabControl1.SelectedTab.Controls.OfType<TextBox>().FirstOrDefault().AppendText("<<"+textBox1.Text+Environment.NewLine);
            data.message = textBox1.Text;
            data.from = Program.name;
            //empties the messagebox
            textBox1.Text = "";
            //sends the data
            Program.Send(data);
        }
        private void closing(object sender, FormClosedEventArgs e)
        {
            //sends disconnect message if window closed
            Data data = new Data();
            data.command = Command.Disconnect;
            data.from = Program.name;
            Program.stop = true;
            Program.client.GetStream().Write(Data.getBytes(data),0,Data.getBytes(data).Length);
            Application.Exit();
        }
    }
}
