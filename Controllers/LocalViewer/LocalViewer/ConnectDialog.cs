using System;
using System.Net;
using System.Windows.Forms;

namespace LocalViewer
{
    public partial class ConnectDialog : Form
    {
        public int Port
        {
            get { return (int)portNumericUpDown.Value; }
            set { portNumericUpDown.Value = value; }
        }

        public string Token
        {
            get { return tokenTextBox.Text; }
            set { tokenTextBox.Text = value; }
        }

        public ConnectDialog()
        {
            InitializeComponent();

            portNumericUpDown.Minimum = IPEndPoint.MinPort;
            portNumericUpDown.Maximum = IPEndPoint.MaxPort;
            Port = 8082;
        }

        private void ConnectDialog_Load(object sender, EventArgs e)
        {
            MinimumSize = Size;
        }
    }
}
