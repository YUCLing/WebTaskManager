using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace WebTaskManager
{
    class ProcessInfo
    {
        public readonly string Path;
        public readonly Process Process;
        public int Processor;

        public ProcessInfo(Process proc,string path)
        {
            Process = proc;
            Path = path;
        }
    }

    class WebsocketService : WebSocketBehavior
    {
        bool verifyed = false;

        protected override void OnMessage(MessageEventArgs e)
        {
            if (!e.IsText)
                return;
            try
            {
                JObject json = JObject.Parse(e.Data);
                switch ((string)json["action"])
                {
                    case "Verify":
                        JObject verify = new JObject
                        {
                            ["message"] = "verify"
                        };
                        if (WebTaskManager.ToMD5((string)json["password"]) == Convert.ToString(WebTaskManager.GetSetting("Password")))
                        {
                            verifyed = true;
                            verify["success"] = true;
                        }
                        else
                            verify["success"] = false;
                        Send(verify.ToString());
                        break;
                    case "GetProcesses":
                        JObject processes = new JObject
                        {
                            ["message"] = "processes"
                        };
                        if (verifyed)
                        {
                            List<ProcessInfo> procList = new List<ProcessInfo>();
                            JArray list = new JArray();
                            var wmiQueryString = "SELECT ProcessId, ExecutablePath, CommandLine FROM Win32_Process";
                            using (var searcher = new ManagementObjectSearcher(wmiQueryString))
                            using (var results = searcher.Get())
                            {
                                var query = from p in Process.GetProcesses()
                                            join mo in results.Cast<ManagementObject>()
                                            on p.Id equals (int)(uint)mo["ProcessId"]
                                            select new
                                            {
                                                Process = p,
                                                Path = (string)mo["ExecutablePath"],
                                                CommandLine = (string)mo["CommandLine"],
                                            };
                                foreach (var item in query)
                                {
                                    procList.Add(new ProcessInfo(item.Process, item.Path));
                                }
                            }
                            string cond = "";
                            for (int i = 0;i<procList.Count;i++)
                            {
                                if (i != 0)
                                {
                                    cond += " Or ";
                                }
                                cond += "IDProcess=" + procList[i].Process.Id;
                            }
                            ManagementObjectSearcher cS = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_PerfFormattedData_PerfProc_Process WHERE " + cond);
                            foreach (ManagementObject obj in cS.Get())
                            {
                                int processor = Convert.ToInt32(obj["PercentProcessorTime"]) / Environment.ProcessorCount;
                                foreach (ProcessInfo pi in procList)
                                {
                                    if (pi.Process.Id == Convert.ToInt32(obj["IDProcess"]))
                                    {
                                        pi.Processor = processor;
                                    }
                                }
                            }
                            foreach (ProcessInfo proc in procList)
                            {
                                JObject jo = new JObject
                                {
                                    ["ID"] = proc.Process.Id,
                                    ["ProcName"] = proc.Process.ProcessName,
                                    ["CPU"] = proc.Processor,
                                    ["Memory"] = proc.Process.WorkingSet64,
                                    ["Title"] = proc.Process.MainWindowTitle,
                                    ["Folder"] = proc.Path,
                                    ["Responding"] = proc.Process.Responding
                                };
                                list.Add(jo);
                            }
                            processes["processes"] = list;
                            processes["success"] = true;
                        }
                        else
                            processes["success"] = false;
                        Send(processes.ToString());
                        break;
                    case "EndProcess":
                        JObject endProc = new JObject
                        {
                            ["message"] = "endprocess"
                        };
                        if (verifyed)
                        {
                            endProc["success"] = false;
                            Process[] procList = Process.GetProcesses();
                            foreach (Process proc in procList)
                            {
                                if (proc.Id == (int)json["id"])
                                {
                                    proc.Kill();
                                    endProc["success"] = true;
                                }
                            }
                        }
                        else
                            endProc["success"] = false;
                        Send(endProc.ToString());
                        break;
                    default:
                        JObject res = new JObject
                        {
                            ["success"] = false
                        };
                        Send(res.ToString());
                        break;
                }
            } catch
            {
                return;
            }
        }
    }
}
