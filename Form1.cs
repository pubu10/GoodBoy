using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
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
            static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);

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
            }

            void PreventSleep()
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

            void OnSessionLock()
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



            notifyIcon1.BalloonTipIcon = ToolTipIcon.Info;
            notifyIcon1.BalloonTipText = "Minimized";//
            notifyIcon1.BalloonTipTitle = "Background running..";//
            notifyIcon1.ShowBalloonTip(1000);



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
            MessageBox.Show("Develop By PUBUDU", "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {

            SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS);

            MessageBox.Show("Good Boy is now turn off.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Warning);

        }
    }
}
