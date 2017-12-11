using System;
using System.Windows.Forms;

namespace PaintingWithFriendsClient
{
    public partial class ConnectForm : Form
    {
        public ConnectForm()
        {
            InitializeComponent();
        }

        private void connectButton_Click(object sender, EventArgs e)
        {
            if (connectButton.Text == "Connect")
            {
                int port;
                Int32.TryParse(portBox.Text, out port);
                Program.Connect(ipBox.Text, port);
                connectButton.Text = "Disconnect";
            }
            else
            {
                Program.Disconnect();
                connectButton.Text = "Connect";
            }
        }
    }
}
