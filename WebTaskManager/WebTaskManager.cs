using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using WebSocketSharp;
using WebSocketSharp.Net;
using WebSocketSharp.Server;

namespace WebTaskManager
{
    public static class WebTaskManager
    {
        static string randChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstxyz123456789_";
        static JObject settings = new JObject();
        static NotifyIcon ni;

        public static HttpServer server;
        public static Stream GetResource(string fileName)
        {
            return Assembly.GetExecutingAssembly().GetManifestResourceStream(Assembly.GetExecutingAssembly().GetName().Name + ".Resources." + fileName);
        }

        public static byte[] StreamToByte(Stream stream)
        {
            byte[] byt = new byte[stream.Length];
            stream.Read(byt, 0, byt.Length);
            return byt;
        }

        public static string ToB64(string value)
        {
            if (value == null || value == "")
            {
                return "";
            }
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            return Convert.ToBase64String(bytes);
        }

        public static string FromB64(string value)
        {
            if (value == null || value == "")
            {
                return "";
            }
            byte[] bytes = Convert.FromBase64String(value);
            return Encoding.UTF8.GetString(bytes);
        }

        public static void SetSetting(string name,object val)
        {
            settings[name] = JToken.FromObject(val);
            FileStream sFile = new FileStream(Path.GetDirectoryName(Application.ExecutablePath) + @"\Settings.dat", FileMode.Create);
            StreamWriter sw = new StreamWriter(sFile);
            sw.Write(ToB64(settings.ToString()));
            sw.Close();
            sFile.Close();
        }

        public static object GetSetting(string name)
        {
            return settings[name];
        }

        public static string GeneratePassword()
        {
            string tmp = "";
            Random rand = new Random();
            for (int i = 0; i < rand.Next(8, 12); i++)
            {
                tmp += randChars[rand.Next(0, randChars.Length - 1)];
            }
            return tmp;
        }


        public static string ToMD5(string input)
        {
            if (input == null)
            {
                return null;
            }
            MD5 md5Hash = MD5.Create();
            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));
            StringBuilder sBuilder = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }
            return sBuilder.ToString();
        }

        public static void HttpReq(object sender, HttpRequestEventArgs e)
        {
            e.Response.AddHeader("Server", "Web Task Manager");
            e.Response.ContentEncoding = Encoding.UTF8;
            try
            {
                string path = e.Request.RawUrl;
                switch (path)
                {
                    case "/":
                    case "/index.html":
                        e.Response.StatusCode = 200;
                        e.Response.ContentType = "text/html";
                        e.Response.WriteContent(StreamToByte(GetResource("index.html")));
                        break;
                    case "/mdui.min.js":
                        e.Response.StatusCode = 200;
                        e.Response.ContentType = "application/javascript";
                        e.Response.WriteContent(StreamToByte(GetResource("mdui.min.js")));
                        break;
                    case "/mdui.min.css":
                        e.Response.StatusCode = 200;
                        e.Response.ContentType = "text/css";
                        e.Response.WriteContent(StreamToByte(GetResource("mdui.min.css")));
                        break;
                    case "/vue.min.js":
                        e.Response.StatusCode = 200;
                        e.Response.ContentType = "application/javascript";
                        e.Response.WriteContent(StreamToByte(GetResource("vue.min.js")));
                        break;
                    case "/app.js":
                        e.Response.StatusCode = 200;
                        e.Response.ContentType = "application/javascript";
                        e.Response.WriteContent(StreamToByte(GetResource("app.js")));
                        break;
                    case "/favicon.ico":
                        e.Response.StatusCode = 200;
                        e.Response.ContentType = "image/x-icon";
                        e.Response.WriteContent(StreamToByte(GetResource("icon.ico")));
                        break;
                    case "/action/login":
                        if (e.Request.QueryString["Password"] != null && e.Request.QueryString["Password"] == Convert.ToString(GetSetting("Password")))
                        {
                            e.Response.StatusCode = 200;
                            e.Response.SetCookie(new Cookie("Password", e.Request.QueryString["Password"]));
                            e.Response.WriteContent(Encoding.UTF8.GetBytes(new JObject
                            {
                                ["success"] = true
                            }.ToString()));
                        }
                        else
                        {
                            e.Response.StatusCode = 403;
                            e.Response.WriteContent(Encoding.UTF8.GetBytes(new JObject
                            {
                                ["success"] = false
                            }.ToString()));
                        }
                        break;
                    case "/action/getprocesses":
                        JObject processes = new JObject();
                        if (e.Request.Cookies["Password"].Value == Convert.ToString(GetSetting("Password")))
                        {
                            Process[] procList = Process.GetProcesses();
                            JArray list = new JArray();
                            foreach (Process proc in procList)
                            {
                                JObject jo = new JObject
                                {
                                    ["ID"] = proc.Id,
                                    ["ProcName"] = proc.ProcessName,
                                    ["Memory"] = proc.WorkingSet64,
                                    ["Title"] = proc.MainWindowTitle
                                };
                                list.Add(jo);
                            }
                            processes["processes"] = list;
                            processes["success"] = true;
                        }
                        else
                            processes["success"] = false;
                        e.Response.StatusCode = 200;
                        e.Response.ContentEncoding = Encoding.UTF8;
                        e.Response.WriteContent(Encoding.UTF8.GetBytes(processes.ToString()));
                        break;
                    case "/action/end":
                        JObject endProc = new JObject();
                        if (e.Request.Cookies["Password"].Value == Convert.ToString(GetSetting("Password")))
                        {
                            endProc["success"] = false;
                            Process[] procList = Process.GetProcesses();
                            foreach (Process proc in procList)
                            {
                                if (proc.Id == Convert.ToInt32(e.Request.QueryString["id"]))
                                {
                                    proc.Kill();
                                    endProc["success"] = true;
                                }
                            }
                        }
                        else
                            endProc["success"] = false;
                        e.Response.StatusCode = 200;
                        e.Response.WriteContent(Encoding.UTF8.GetBytes(endProc.ToString()));
                        break;
                    default:
                        if (path.StartsWith("/fonts/"))
                        {
                            path = path.Replace("/fonts/", "").Replace("roboto/", "").Replace("icons/", "");
                            Stream font = GetResource("Fonts." + path);
                            if (font == null)
                            {
                                e.Response.StatusCode = 404;
                                e.Response.ContentType = "text/html";
                                e.Response.ContentEncoding = Encoding.UTF8;
                                e.Response.WriteContent(Encoding.UTF8.GetBytes("<html><head><title>Not Found - Web Task Manager</title></head><body><h1>Web Task Manager无法找到相应的资源<!-- Because you are viewing Google --></h1></body></html>"));
                            }
                            else
                            {
                                e.Response.StatusCode = 200;
                                e.Response.WriteContent(StreamToByte(font));
                            }
                        }
                        break;
                }
            } catch(Exception ex)
            {
                e.Response.StatusCode = 500;
                e.Response.WriteContent(Encoding.UTF8.GetBytes("<html><head><title>Error - Web Task Manager</head><body><h1>Web Task Manager遇到了些问题</h1><pre>" + ex.ToString().Replace("\n", "<br/>") + "</pre></body></html>"));
            }
        }

        static void Main()
        {
            if (!File.Exists(Path.GetDirectoryName(Application.ExecutablePath) + @"\Settings.dat"))
            {
                string pw = GeneratePassword();
                settings["Port"] = 5556;
                settings["Password"] = ToMD5(pw);
                FileStream sFile = new FileStream(Path.GetDirectoryName(Application.ExecutablePath) + @"\Settings.dat", FileMode.Create);
                StreamWriter sw = new StreamWriter(sFile);
                sw.Write(ToB64(settings.ToString()));
                sw.Close();
                sFile.Close();
                MessageBox.Show("登录密码：" + pw + "\n默认端口：5556\n\n注意：密码保存后只能修改不能查看", "Web Task Manager");
            }
            FileStream settingFile = new FileStream(Path.GetDirectoryName(Application.ExecutablePath) + @"\Settings.dat", FileMode.OpenOrCreate);
            StreamReader sr = new StreamReader(settingFile);
            try
            {
                settings = JObject.Parse(FromB64(sr.ReadToEnd()));
            } catch
            {
                string pw = GeneratePassword();
                settings["Port"] = 5556;
                settings["Password"] = ToMD5(pw);
                FileStream sFile = new FileStream(Path.GetDirectoryName(Application.ExecutablePath) + @"\Settings.dat", FileMode.Create);
                StreamWriter sw = new StreamWriter(sFile);
                sw.Write(ToB64(settings.ToString()));
                sw.Close();
                sFile.Close();
                MessageBox.Show("登录密码：" + pw + "\n默认端口：5556\n\n注意：密码保存后只能修改不能查看", "Web Task Manager");
            }
            sr.Close();
            settingFile.Close();
            ni = new NotifyIcon
            {
                Icon = new Icon(GetResource("icon.ico")),
                Text = "Web Task Manager",
                Visible = true,
                ContextMenu = new ContextMenu(new MenuItem[]
                {
                    new MenuItem("修改端口",new EventHandler((object sender,EventArgs e) =>
                    {
                        new ChangePort().ShowDialog();
                    })),
                    new MenuItem("修改密码",new EventHandler((object sender,EventArgs e) =>
                    {
                        new ChangePassword().ShowDialog();
                    })),
                    new MenuItem("退出程序",new EventHandler((object sender,EventArgs e) =>
                    {
                        ni.Visible = false;
                        ni.Dispose();
                        Application.Exit();
                    }))
                })
            };
            server = new HttpServer(Convert.ToInt32(settings["Port"]));
            server.AddWebSocketService<WebsocketService>("/websocket");
            server.OnGet += HttpReq;
            server.Start();
            Application.Run();
        }
    }
}
