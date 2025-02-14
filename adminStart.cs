using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Principal;
using System.Windows.Forms;
using Formatting = Newtonsoft.Json.Formatting;

namespace GoodBoy
{
    public partial class adminStart : Form
    {
        public string logFilePath { get; set; } = Directory.GetCurrentDirectory() + "\\log.json";

        public adminStart()
        {
            InitializeComponent();

            try
            {
                bool isadmin = false;
                string user = string.Empty;

                BindUI();
                IsAdministrator(out isadmin, out user);
                this.Text = $"Open as {user}";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public void BindUI()
        {
            try
            {
                var data = Read();
                if (data != null)
                {
                    txtAdminUserName.Text = data.UserName;
                    txtAdminPassword.Text = data.Password;

                    if (data.Histories != null && data.Histories.Count > 0)
                    {
                        comboBox1.DisplayMember = "NickName";
                        comboBox1.ValueMember = "Id";
                        comboBox1.Items.Clear();
                        foreach (var history in data.Histories)
                        {
                            comboBox1.Items.Add(history);
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void btnOpenApp_Click(object sender, EventArgs e)
        {
            try
            {
                OpenApp(txtAdminOpen.Text);
                string NicName = ShowSaveConfirmationPopup();
                var data = Read();
                int max = 0;

                if (data != null && data.Histories != null && data.Histories.Count > 0)
                {
                    max = data.Histories.Max(x => x.Id);
                    data.Histories.Add(new History { Id = max + 1, NickName = NicName, });
                }
                else
                {
                    data.Histories = new List<History>();
                    data.Histories.Add(new History { Id = max + 1, NickName = NicName, Path = txtAdminOpen.Text });
                }

                SaveHistory(data);
                MessageBox.Show("Saved.");

                BindUI();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnSaveCredentials_Click(object sender, EventArgs e)
        {
            try
            {
                SaveCredentials();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public void SaveCredentials()
        {
            try
            {
                var history = Read();

                var logEntry = new LogEntry
                {
                    UserName = txtAdminUserName.Text,
                    Password = txtAdminPassword.Text,
                    Histories = history.Histories
                };

                string jsonLog = JsonConvert.SerializeObject(logEntry, Formatting.Indented);

                File.WriteAllText(logFilePath, jsonLog + Environment.NewLine);
                MessageBox.Show("written successfully.");
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void SaveHistory(LogEntry history)
        {
            string logFilePath = "log.json";

            var logEntry = new LogEntry
            {
                UserName = history.UserName,
                Password = history.Password,
                Histories = history.Histories
            };

            string jsonLog = JsonConvert.SerializeObject(logEntry, Formatting.Indented);

            File.WriteAllText(logFilePath, jsonLog + Environment.NewLine);
        }

        public LogEntry Read()
        {
            try
            {
                if (File.Exists(logFilePath))
                {
                    string logContents = File.ReadAllText(logFilePath);
                    return JsonConvert.DeserializeObject<LogEntry>(logContents);
                }
                else return new LogEntry();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void OpenApp(string exePath)
        {
            try
            {
                string userName = txtAdminUserName.Text;
                string domain = "."; // Use "." for local machine
                string password = txtAdminPassword.Text;

                try
                {
                    SecureString securePassword = new SecureString();
                    foreach (char c in password)
                    {
                        securePassword.AppendChar(c);
                    }

                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = exePath,
                        UseShellExecute = false,
                        UserName = userName,
                        Domain = domain,
                        Password = securePassword,
                        LoadUserProfile = true
                    };

                    Process process = Process.Start(psi);
                    Console.WriteLine($"Launched {exePath} as {userName}");
                }
                catch (Exception)
                {
                    throw;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        private string ShowSaveConfirmationPopup()
        {
            try
            {
                DialogResult result = MessageBox.Show(
                    "Do you want to enter a nickname?",
                    "Confirmation",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    string nickname = ShowInputDialog("Enter your nickname:", "Input Required");
                    if (!string.IsNullOrEmpty(nickname))
                    {
                        return nickname;
                    }
                    else
                    {
                        return string.Empty;
                    }
                }
                else
                {
                    return string.Empty;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        private string ShowInputDialog(string prompt, string title)
        {
            // Create a new form for input
            Form inputForm = new Form()
            {
                Width = 300,
                Height = 180,
                Text = title,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterScreen
            };

            Label label = new Label() { Left = 20, Top = 20, Text = prompt, AutoSize = true };
            System.Windows.Forms.TextBox textBox = new System.Windows.Forms.TextBox() { Left = 20, Top = 50, Width = 240 };
            System.Windows.Forms.Button confirmation = new System.Windows.Forms.Button() { Text = "OK", Left = 80, Width = 100, Top = 90, DialogResult = DialogResult.OK };

            inputForm.Controls.Add(label);
            inputForm.Controls.Add(textBox);
            inputForm.Controls.Add(confirmation);
            inputForm.AcceptButton = confirmation;

            // Show dialog and return input
            return inputForm.ShowDialog() == DialogResult.OK ? textBox.Text : string.Empty;
        }

        private static void IsAdministrator(out bool retuen, out string Name)
        {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                retuen = principal.IsInRole(WindowsBuiltInRole.Administrator);
                Name = identity.Name;
                MessageBox.Show($" {identity.Name} Account is {(retuen ? "Admin" : "not a Admin")} ", "", MessageBoxButtons.OK, retuen ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            }
        }

        private void btnOpenwithGb_Click(object sender, EventArgs e)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = txtAdminOpen.Text,
                    UseShellExecute = false,
                    LoadUserProfile = true
                };

                Process process = Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            bool isadmin = false;
            string user = string.Empty;
            IsAdministrator(out isadmin, out user);
            if (!isadmin)
            {
                try
                {
                    OpenApp(Application.ExecutablePath);
                    Application.Exit();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to restart as administrator: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
                MessageBox.Show($"GoodBoy is administrator", "Infor", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                txtAdminOpen.Text = ((History)comboBox1.SelectedItem).Path;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            txtAdminOpen.Text = string.Empty;
        }
    }

    public class LogEntry
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public List<History> Histories { get; set; }
    }

    public class History
    {
        public int Id { get; set; }
        public string Path { get; set; }
        public string NickName { get; set; }
    }
}