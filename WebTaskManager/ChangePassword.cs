using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace WebTaskManager
{
    public partial class ChangePassword : Form
    {
        public ChangePassword()
        {
            InitializeComponent();
        }

        private void Random_Click(object sender, EventArgs e)
        {
            password.Text = WebTaskManager.GeneratePassword();
        }

        private void Save_Click(object sender, EventArgs e)
        {
            WebTaskManager.SetSetting("Password", WebTaskManager.ToMD5(password.Text));
            Close();
        }
    }
}
