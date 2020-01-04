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
    public partial class ChangePort : Form
    {

        public ChangePort()
        {
            InitializeComponent();
            port.Value = Convert.ToDecimal(WebTaskManager.GetSetting("Port"));
        }

        private void Random_Click(object sender, EventArgs e)
        {
            port.Value = new Random().Next(0, 65535);
        }

        private void Save_Click(object sender, EventArgs e)
        {
            WebTaskManager.SetSetting("Port", port.Value);
            if (WebTaskManager.server.IsListening)
                WebTaskManager.server.Stop();
            WebTaskManager.server = new WebSocketSharp.Server.HttpServer(Convert.ToInt32(WebTaskManager.GetSetting("Port")));
            WebTaskManager.server.AddWebSocketService<WebsocketService>("/websocket");
            WebTaskManager.server.OnGet += WebTaskManager.HttpReq;
            WebTaskManager.server.Start();
            Close();
        }
    }
}
