using Microsoft.Win32;
using System;
using System.Collections;
using System.ComponentModel;
using System.Configuration.Install;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.XPath;

namespace net.leksi
{
    public class SrvWrap: ServiceBase
    {
        static void Main(string[] args)
        {
            if (Environment.UserInteractive)
            {
                SrvWrap prog = new SrvWrap();
                if (args.Length == 0)
                {
                    if(!File.Exists("srvwrap.exe.config"))
                    {
                        Console.WriteLine("Usage:\nsrvwrap.exe <congif_file>");
                        Console.WriteLine("or");
                        Console.WriteLine("there should be srvwrap.exe.config at current directory.");
                        return;
                    }
                    prog.config_path = "./srvwrap.exe.config";
                } else
                {
                    prog.config_path = args[0];
                }
                prog.ServiceMain();
            }
            else
            {
                ServiceBase.Run(new ServiceBase[] { new SrvWrap() });
            }
        }

        protected override void OnStart(string[] args)
        {
            base.OnStart(args);
            new Thread(new ThreadStart(ServiceMain)).Start();
        }

        protected override void OnStop()
        {
            if(proc != null && !proc.HasExited)
            {
                proc.CancelOutputRead();
                proc.CancelErrorRead();
                proc.Kill();
            }
            base.OnStop();
        }

        string config_path = null;
        Process proc = null;

        private void ServiceMain()
        {
            string serviceName = null;
            if(!Environment.UserInteractive)
            {
                int my_pid = Process.GetCurrentProcess().Id;
                foreach (ServiceController sc in ServiceController.GetServices())
                {
                    ManagementObject wmi = new ManagementObject("Win32_Service.Name='" + sc.ServiceName + "'");
                    wmi.Get();
                    int id = Convert.ToInt32(wmi["ProcessId"]);
                    if (id == my_pid)
                    {
                        serviceName = sc.ServiceName;
                        break;
                    }
                }
            }
            try
            {
                if (!Environment.UserInteractive)
                {
                    if (serviceName == null)
                    {
                        throw new Exception("Finding Service Name is failed.");
                    }
                    RegistryKey rk = OpenSubKey("software/leksi.net/SrvWrap/" + serviceName);
                    if (rk == null || (config_path = (string)rk.GetValue("config")) == null)
                    {
                        throw new Exception("Configuration file is not found.");
                    }
                }
                Stream config = File.OpenRead(config_path);
                XPathDocument doc = new XPathDocument(config);
                config.Close();
                XPathNavigator navRuntime = doc.CreateNavigator().SelectSingleNode("/configuration/runtime");
                XPathNodeIterator ni = navRuntime.Select("env");
                while (ni.MoveNext())
                {
                    Environment.SetEnvironmentVariable(ni.Current.GetAttribute("name", ""), ni.Current.GetAttribute("value", ""));
                }
                if (!Environment.UserInteractive)
                {
                    this.EventLog.Source = serviceName;
                    this.EventLog.Log = string.Empty;
                }
                if (navRuntime.SelectSingleNode("stdout.log") != null)
                {
                    if(!Directory.Exists(Path.GetDirectoryName(navRuntime.SelectSingleNode("stdout.log").Value)))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(navRuntime.SelectSingleNode("stdout.log").Value));
                    }
                    Console.SetOut(new OutToLog(Console.Out, navRuntime.SelectSingleNode("stdout.log").Value));
                }
                else if (!Environment.UserInteractive)
                {
                    Console.SetOut(new OutToLog(Console.Out, this.EventLog, EventLogEntryType.Information));
                }
                if (navRuntime.SelectSingleNode("error.log") != null)
                {
                    if (!Directory.Exists(Path.GetDirectoryName(navRuntime.SelectSingleNode("error.log").Value)))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(navRuntime.SelectSingleNode("error.log").Value));
                    }
                    Console.SetError(new OutToLog(Console.Out, navRuntime.SelectSingleNode("error.log").Value));
                }
                else if (!Environment.UserInteractive)
                {
                    Console.SetError(new OutToLog(Console.Out, this.EventLog, EventLogEntryType.Error));
                }
                Directory.SetCurrentDirectory(Path.GetDirectoryName(config_path));
                if(navRuntime.SelectSingleNode("executable") == null)
                {
                    Console.WriteLine("Warning: executable path is not found at configuration file. Stopping.");
                } else
                {
                    string executable = Regex.Replace(navRuntime.SelectSingleNode("executable").Value, "%([^%]+)%", eval);
                    proc = new Process();
                    proc.StartInfo.FileName = executable;
                    if(navRuntime.SelectSingleNode("args") != null) {
                        proc.StartInfo.Arguments = Regex.Replace(navRuntime.SelectSingleNode("args").Value, "%([^%]+)%", eval);
                    }
                    proc.StartInfo.WorkingDirectory = Directory.GetCurrentDirectory();
                    proc.StartInfo.CreateNoWindow = true;
                    proc.StartInfo.UseShellExecute = false;
                    proc.StartInfo.RedirectStandardOutput = true;
                    proc.StartInfo.RedirectStandardError = true;
                    proc.OutputDataReceived += Proc_OutputDataReceived;
                    proc.ErrorDataReceived += Proc_ErrorDataReceived;
                    proc.Start();
                    proc.BeginOutputReadLine();
                    proc.BeginErrorReadLine();
                    proc.WaitForExit();

                }
                if (!Environment.UserInteractive)
                {
                    this.Stop();
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private void Proc_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if(!string.IsNullOrEmpty(e.Data))
            {
                Console.Error.WriteLine(e.Data);
            }
        }

        private void Proc_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                Console.Out.WriteLine(e.Data);
            }
        }

        private string eval(Match match)
        {
            string val = Environment.GetEnvironmentVariable(match.Groups[1].Captures[0].Value);
            if (val != null)
            {
                return val;
            }
            return match.Groups[1].Captures[0].Value;
        }

        static internal RegistryKey OpenSubKey(string path)
        {
            string[] parts = path.Split(new[] { '/' });
            RegistryKey prev = null;
            foreach (string part in parts)
            {
                RegistryKey rk = (prev == null ? Registry.LocalMachine : prev).OpenSubKey(part, true);
                if (rk == null)
                {
                    rk = (prev == null ? Registry.LocalMachine : prev).CreateSubKey(part, RegistryKeyPermissionCheck.ReadWriteSubTree);
                }
                if (rk == null)
                {
                    return null;
                }
                prev = rk;
            }
            return prev;
        }
    }

    [RunInstaller(true)]
    public class SrvWrapInstaller : Installer
    {
        string config_path;
        string serviceName;

        protected override void OnAfterInstall(IDictionary savedState)
        {
            base.OnAfterInstall(savedState);
            RegistryKey rk = SrvWrap.OpenSubKey("software/leksi.net/SrvWrap/" + serviceName);
            if (rk == null)
            {
                throw new Exception("Registry access failed.");
            }
            rk.SetValue("config", config_path);
        }

        protected override void OnAfterUninstall(IDictionary savedState)
        {
            base.OnAfterUninstall(savedState);
            RegistryKey rk = SrvWrap.OpenSubKey("software/leksi.net/SrvWrap");
            if (rk != null)
            {
                rk.DeleteSubKeyTree(serviceName);
                if(rk.SubKeyCount == 0)
                {
                    rk = SrvWrap.OpenSubKey("software/leksi.net");
                    rk.DeleteSubKeyTree("SrvWrap");
                    if (rk.SubKeyCount == 0)
                    {
                        rk = SrvWrap.OpenSubKey("software");
                        rk.DeleteSubKeyTree("leksi.net");
                    }
                }
            }
        }

        public SrvWrapInstaller()
        {
            config_path = Path.Combine(Directory.GetCurrentDirectory(), Path.GetFileName(Assembly.GetExecutingAssembly().CodeBase) + ".config");
            try
            {
                Stream config = File.OpenRead(config_path);
                XPathDocument doc = new XPathDocument(config);
                config.Close();
                XPathNavigator nav = doc.CreateNavigator().SelectSingleNode("/configuration/service");

                ServiceProcessInstaller process = new ServiceProcessInstaller();
                ServiceInstaller serviceAdmin = new ServiceInstaller();

                if (nav.SelectSingleNode("account") != null)
                {
                    ServiceAccount sa;
                    if (!Enum.TryParse<ServiceAccount>(nav.SelectSingleNode("account").Value, out sa)) {
                        throw new Exception("Unexpected value for <account/> tag: " + nav.SelectSingleNode("account").Value + "\n   expecting: User | LocalService | LocalSystem | NetworkService");
                    }
                    process.Account = sa;
                }
                if (nav.SelectSingleNode("username") != null)
                {
                    process.Username = nav.SelectSingleNode("username").Value;
                }
                if (nav.SelectSingleNode("password") != null)
                {
                    process.Password = nav.SelectSingleNode("password").Value;
                }


                if (nav.SelectSingleNode("starttype") != null)
                {
                    ServiceStartMode ssm;
                    if(!Enum.TryParse<ServiceStartMode>(nav.SelectSingleNode("starttype").Value, out ssm))
                    {
                        throw new Exception("Unexpected value for <starttype/> tag: " + nav.SelectSingleNode("starttype").Value + "\n   expecting: Automatic | Manual | Disabled");
                    }
                    serviceAdmin.StartType = ssm;
                }
                if (nav.SelectSingleNode("name") == null || string.IsNullOrEmpty(nav.SelectSingleNode("name").Value))
                {
                    throw new Exception("Service Name not found. <name/> tag is mandatory and cannot be empty.");
                }
                serviceName = serviceAdmin.ServiceName = nav.SelectSingleNode("name").Value;
                if (nav.SelectSingleNode("display-name") != null)
                {
                    serviceAdmin.DisplayName = nav.SelectSingleNode("display-name").Value;
                }
                if (nav.SelectSingleNode("description") != null)
                {
                    serviceAdmin.Description = nav.SelectSingleNode("description").Value;
                }

                Installers.Add(process);
                Installers.Add(serviceAdmin);

            } 
            catch(FileNotFoundException ex)
            {
                Console.WriteLine("\n\n\n----\nConfiguration file not found: " + config_path + "\n----\n\n\n" + ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine("\n\n\n----\n" + ex.Message + "\n----\n\n\n" + ex);
            }
        }
    }


}
