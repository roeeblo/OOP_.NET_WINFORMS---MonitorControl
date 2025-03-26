using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Timers;

namespace MonitorControlTimer
{
    public partial class Form1 : Form
    {
        [DllImport("user32.dll")]
        private static extern void SendMessage(int hWnd, int hMsg, int wParam, int lParam);

        private const int WM_SYSCOMMAND = 0x0112;
        private const int SC_MONITORPOWER = 0xF170;
        private const int MONITOR_OFF = 2;

        private System.Timers.Timer shutdownTimer;
        private int m_remainingSeconds;

        public Form1()
        {
            InitializeComponent();

            comboBoxTime.Items.Add("Minutes");
            comboBoxTime.Items.Add("Seconds");
            comboBoxTime.SelectedItem = "Minutes";

            shutdownTimer = new System.Timers.Timer(1000);
            shutdownTimer.Elapsed += ShutdownTimer_Elapsed;
            shutdownTimer.AutoReset = true;

            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            int timeValue = (int)numTime.Value;
            string timeUnit = comboBoxTime.SelectedItem.ToString();

            m_remainingSeconds = timeUnit == "Minutes" ? timeValue * 60 : timeValue;

            if (m_remainingSeconds > 0)
            {
                shutdownTimer.Start();
                UpdateRemainingTimeText();

                btnStart.Enabled = false;
                btnCancel.Enabled = true;
                numTime.Enabled = false;
                comboBoxTime.Enabled = false;
            }
            else
            {
                MessageBox.Show("Please enter a valid time.");
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            shutdownTimer.Stop();
            btnStart.Enabled = true;
            btnCancel.Enabled = false;
            numTime.Enabled = true;
            comboBoxTime.Enabled = true;
            txtRemaining.Text = "";
        }

        private void ShutdownTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            m_remainingSeconds--;

            if (m_remainingSeconds <= 0)
            {
                shutdownTimer.Stop();
                Invoke(new Action(() =>
                {
                    TurnOffMonitor();
                    btnStart.Enabled = true;
                    btnCancel.Enabled = false;
                    numTime.Enabled = true;
                    comboBoxTime.Enabled = true;
                    txtRemaining.Text = "Monitor turned off!";
                }));
            }
            else
            {
                Invoke(new Action(UpdateRemainingTimeText));
            }
        }

        private void UpdateRemainingTimeText()
        {
            txtRemaining.Text = $"Turning Off in {m_remainingSeconds} seconds...";
        }

        private void TurnOffMonitor()
        {
            SendMessage(this.Handle.ToInt32(), WM_SYSCOMMAND, SC_MONITORPOWER, MONITOR_OFF);
        }
    }
}