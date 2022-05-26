using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;

namespace modbus_server
{
    public partial class FormAbout : Form
    {
        
        public FormAbout()
        {
            InitializeComponent();

        }
        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            this.linkLabel1.LinkVisited = true;
            Process.Start(@"C:\Program Files\Google\Chrome\Application\chrome.exe", "https://www.billionwatts.com.tw");
        }
    }
}
