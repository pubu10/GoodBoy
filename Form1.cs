using Microsoft.Win32;
using System;
using System.Deployment.Application;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace GoodBoy
{
    public partial class Form1 : Form
    {
        public delegate void Notify();  // delegate

        [DllImport("wtsapi32.dll")]
        private static extern bool WTSRegisterSessionNotification(IntPtr hWnd, int dwFlags);

        [DllImport("wtsapi32.dll")]
        private static extern bool WTSUnRegisterSessionNotification(IntPtr hWnd);

        [FlagsAttribute]
        public enum EXECUTION_STATE : uint
        {
            ES_AWAYMODE_REQUIRED = 0x00000040,
            ES_CONTINUOUS = 0x80000000,
            ES_DISPLAY_REQUIRED = 0x00000002,
            ES_SYSTEM_REQUIRED = 0x00000001
            // Legacy flag, should not be used.
            // ES_USER_PRESENT = 0x00000004
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);

        private const int SessionChangeMessage = 0x02B1;
        private const int SessionLockParam = 0x7;
        private const int SessionUnlockParam = 0x8;
        private const int NotifyForThisSession = 0; // This session only

        public Form1()
        {
            SystemEvents.PowerModeChanged += OnPowerChange;

            WTSRegisterSessionNotification(this.Handle, NotifyForThisSession);
            PreventSleep();
            Console.WriteLine("Runnig:");
            InitializeComponent();

            InitializeInactivityCheckTimer();
            lastMouseActivity = DateTime.Now;

            // Set the global mouse hook
            _mouseProc = HookCallback;
            _hookID = SetHook(_mouseProc);
        }

        private void PreventSleep()
        {
            // Prevent Idle-to-Sleep (monitor not affected) (see note above)
            SetThreadExecutionState(EXECUTION_STATE.ES_DISPLAY_REQUIRED | EXECUTION_STATE.ES_CONTINUOUS);
        }

        protected override void WndProc(ref Message m)
        {
            // check for session change notifications
            if (m.Msg == SessionChangeMessage)
            {
                if (m.WParam.ToInt32() == SessionLockParam)
                    OnSessionLock(); // Do something when locked
                else if (m.WParam.ToInt32() == SessionUnlockParam)
                    OnSessionUnlock(); // Do something when unlocked
            }

            if (m.Msg == 0x0112) // WM_SYSCOMMAND
            {
                if (m.WParam == new IntPtr(0xF020)) // SC_MINIMIZE
                {
                    notifyIcon1.BalloonTipIcon = ToolTipIcon.Info;
                    notifyIcon1.BalloonTipText = "Good Boy";//
                    notifyIcon1.BalloonTipTitle = "Background running..";//
                    notifyIcon1.ShowBalloonTip(1000);

                    this.WindowState = FormWindowState.Minimized;
                    this.ShowInTaskbar = false;
                }
                m.Result = new IntPtr(0);
            }

            base.WndProc(ref m);
            return;
        }

        private void OnSessionUnlock()
        {
            Console.WriteLine("Runnig:OnSessionUnlock");

            //notifyIcon1.BalloonTipIcon = ToolTipIcon.Info;
            //notifyIcon1.BalloonTipText = "Good Boy";//
            //notifyIcon1.BalloonTipTitle = "U Are Back";//
            //notifyIcon1.ShowBalloonTip(1000);
        }

        private void OnSessionLock()
        {
            Console.WriteLine("Runnig:OnSessionLock");

            //notifyIcon1.BalloonTipIcon = ToolTipIcon.Info;
            //notifyIcon1.BalloonTipText = "Good Boy";//
            //notifyIcon1.BalloonTipTitle = "BYE..";//
            //notifyIcon1.ShowBalloonTip(1000);
        }

        private void OnPowerChange(object s, PowerModeChangedEventArgs e)
        {
            switch (e.Mode)
            {
                case PowerModes.Resume:
                    Console.WriteLine("Runnig:Resume");
                    break;

                case PowerModes.Suspend:
                    Console.WriteLine("Runnig:Suspend");

                    notifyIcon1.BalloonTipIcon = ToolTipIcon.Info;
                    notifyIcon1.BalloonTipText = "Good Boy";//
                    notifyIcon1.BalloonTipTitle = "C U Tomorrow";//
                    notifyIcon1.ShowBalloonTip(1000);

                    break;

                case PowerModes.StatusChange:
                    Console.WriteLine("Runnig:StatusChanged");
                    break;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Cursor = new Cursor(Cursor.Current.Handle);
            Cursor.Position = new Point(Cursor.Position.X - 20, Cursor.Position.Y - 20);
            Cursor.Position = new Point(Cursor.Position.X - 20, Cursor.Position.Y - 20);
            Cursor.Position = new Point(Cursor.Position.X - 20, Cursor.Position.Y - 20);

            InstallUpdateSyncWithInfo(true);
        }

        private void InstallUpdateSyncWithInfo(bool status)
        {
            try
            {
                // Display a message that the app MUST reboot. Display the minimum required version.
                UpdateCheckInfo info = null;

                if (ApplicationDeployment.IsNetworkDeployed)
                {
                    ApplicationDeployment ad = ApplicationDeployment.CurrentDeployment;

                    try
                    {
                        info = ad.CheckForDetailedUpdate();
                    }
                    catch (DeploymentDownloadException dde)
                    {
                        MessageBox.Show("The new version of the application cannot be downloaded at this time. \n\nPlease check your network connection, or try again later. Error: " + dde.Message);
                        return;
                    }
                    catch (InvalidDeploymentException ide)
                    {
                        MessageBox.Show("Cannot check for a new version of the application. The ClickOnce deployment is corrupt. Please redeploy the application and try again. Error: " + ide.Message);
                        return;
                    }
                    catch (InvalidOperationException ioe)
                    {
                        MessageBox.Show("This application cannot be updated. It is likely not a ClickOnce application. Error: " + ioe.Message);
                        return;
                    }

                    if (info.UpdateAvailable)
                    {
                        Boolean doUpdate = true;

                        if (!info.IsUpdateRequired)
                        {
                            DialogResult dr = MessageBox.Show("An update is available for GoodBoy. Would you like to update the application now? \n\n(Version " + info.AvailableVersion + " ) " + info.UpdateSizeBytes / 1080 + " Mb", "Update Available", MessageBoxButtons.OKCancel);
                            if (!(DialogResult.OK == dr))
                            {
                                doUpdate = false;
                            }
                        }
                        else
                        {
                            // Display a message that the app MUST reboot. Display the minimum required version.
                            MessageBox.Show("This GoodBoy application has detected a mandatory update from your current " +
                                "version to version " + info.MinimumRequiredVersion.ToString() + " (" + info.UpdateSizeBytes / 1080 + " Mb)" +
                                ". \n\nThe application will now install the update and restart.",
                                "Update Available", MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                        }

                        if (doUpdate)
                        {
                            try
                            {
                                ad.Update();
                                MessageBox.Show("The application has been upgraded, and will now restart.");
                                Application.Restart();
                            }
                            catch (DeploymentDownloadException dde)
                            {
                                MessageBox.Show("Cannot install the latest version of the application. \n\nPlease check your network connection, or try again later. Error: " + dde);
                                return;
                            }
                        }
                    }
                    else
                    {
                        if (status)
                            MessageBox.Show("CurrentVersion: " + ad.CurrentVersion + "\n\n Time Of LastUpdate Check: " + ad.TimeOfLastUpdateCheck.ToString("yyyy-MM-ddd HH:mm:ss"));
                    }
                }
                else
                {
                    MessageBox.Show("In GoodBoy, the online update feature is disabled.", "Not published correctly.", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "opps:(", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
                this.WindowState = FormWindowState.Normal;

            this.Activate();
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
                this.WindowState = FormWindowState.Normal;

            this.Activate();
        }

        private void mSGToolStripMenuItem_Click(object sender, EventArgs e)
        {
            notifyIcon1.BalloonTipIcon = ToolTipIcon.Info;
            notifyIcon1.BalloonTipText = "Minimized";//
            notifyIcon1.BalloonTipTitle = "Background running..";//
            notifyIcon1.ShowBalloonTip(1000);
        }

        private void reStartToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Restart();
            Environment.Exit(0);
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            RegistryKey reg = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

            if (checkBox1.Checked)
            {
                reg.SetValue("Good Boy", Application.ExecutablePath.ToString());
                MessageBox.Show("Message", "ok", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                reg.DeleteValue("Good Boy", true);
                MessageBox.Show("Deleted", "Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void label1_Click(object sender, EventArgs e)
        {
        }

        private void button2_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Develop By P U B U DU", "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS);

            UnhookWindowsHookEx(_hookID);
            activityTimer?.Stop();
            inactivityCheckTimer?.Stop();

            MessageBox.Show("Good Boy is now turn off.", "GoodBoy", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            InstallUpdateSyncWithInfo(false);
        }

        private Timer activityTimer;
        private Timer inactivityCheckTimer;
        private DateTime lastMouseActivity;

        // Delegate and hook ID for global mouse hook
        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        private LowLevelMouseProc _mouseProc;
        private IntPtr _hookID = IntPtr.Zero;

        private void InitializeInactivityCheckTimer()
        {
            inactivityCheckTimer = new Timer();
            inactivityCheckTimer.Interval = 1000; // Check every second
            inactivityCheckTimer.Tick += InactivityCheckTimer_Tick;
            inactivityCheckTimer.Start();
        }

        private void InactivityCheckTimer_Tick(object sender, EventArgs e)
        {
            if ((DateTime.Now - lastMouseActivity).TotalMinutes >= 1)
            {
                if (activityTimer == null || !activityTimer.Enabled)
                {
                    InitializeActivityTimer();
                }
            }
            else
            {
                if (activityTimer != null && activityTimer.Enabled)
                {
                    activityTimer.Stop();
                }
            }
        }

        private void InitializeActivityTimer()
        {
            activityTimer = new Timer();
            activityTimer.Interval = 10000;
            activityTimer.Tick += ActivityTimer_Tick;
            activityTimer.Start();
        }

        private void ActivityTimer_Tick(object sender, EventArgs e)
        {
            SimulateMouseActivity();
            RefreshTeams();
        }

        private void SimulateMouseActivity()
        {
            Cursor.Position = new Point(Cursor.Position.X + 1, Cursor.Position.Y + 1);
            Cursor.Position = new Point(Cursor.Position.X - 1, Cursor.Position.Y - 1);
        }

        private void RefreshTeams()
        {
            var processes = Process.GetProcessesByName("ms-teams");

            if (processes.Length == 0)
            {
                try
                {
                    Process.Start("ms-teams");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to start Teams: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                processes = Process.GetProcessesByName("ms-teams");
            }

            if (processes.Length > 0)
            {
                var teamsWindow = processes[0].MainWindowHandle;
                if (teamsWindow != IntPtr.Zero)
                {
                    SetForegroundWindow(teamsWindow);
                    SendKeys.SendWait("{F5}");
                }
                else
                {
                    Process.Start("ms-teams");
                }
            }
        }

        private IntPtr SetHook(LowLevelMouseProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_MOUSE_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                lastMouseActivity = DateTime.Now;
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private const int WH_MOUSE_LL = 14;

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            UnhookWindowsHookEx(_hookID);
            activityTimer?.Stop();
            inactivityCheckTimer?.Stop();
            base.OnFormClosing(e);
        }

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private void button3_Click(object sender, EventArgs e)
        {
            adminStart adminStart = new adminStart();
            adminStart.Show();
        }

        private void checkBoxTeams_CheckedChanged(object sender, EventArgs e)
        {
            if (!checkBoxTeams.Checked)
            {
                activityTimer?.Stop();
                inactivityCheckTimer?.Stop();
            }
            else
            {
                InitializeInactivityCheckTimer();
            }
        }
    }
}