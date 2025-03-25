using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Timers;

namespace MonitorControlTimer
{
    public partial class Form1 : Form
    {
        [DllImport("dxva2.dll", SetLastError = true)]
        private static extern bool GetPhysicalMonitorsFromHMONITOR(IntPtr hMonitor, uint dwPhysicalMonitorArraySize, [Out] PHYSICAL_MONITOR[] pPhysicalMonitorArray);

        [DllImport("dxva2.dll", SetLastError = true)]
        private static extern bool SetVCPFeature(IntPtr hMonitor, byte bVCPCode, uint dwNewValue);

        [DllImport("dxva2.dll", SetLastError = true)]
        private static extern bool DestroyPhysicalMonitors(uint dwPhysicalMonitorArraySize, [In] PHYSICAL_MONITOR[] pPhysicalMonitorArray);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        private const int MONITOR_DEFAULTTOPRIMARY = 0x00000001;
        private const byte VCP_CODE_POWER_MODE = 0xD6;
        private const uint POWER_MODE_OFF = 0x04;

        private bool monitorsInitialized = false;
        private System.Timers.Timer shutdownTimer;
        private int m_remainingSeconds;
        private PHYSICAL_MONITOR[] physicalMonitors;

        public Form1()
        {
            InitializeComponent();

            comboBoxTime.Items.Add("Minutes");
            comboBoxTime.Items.Add("Seconds");
            comboBoxTime.SelectedItem = "Minutes";

            shutdownTimer = new System.Timers.Timer(1000);
            shutdownTimer.Elapsed += ShutdownTimer_Elapsed;
            shutdownTimer.AutoReset = true; // Run repeatedly until stopped

            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);

            // Initialize physical monitors
            InitializePhysicalMonitors();
        }

        private void InitializePhysicalMonitors()
        {
            try
            {
                IntPtr hMonitor = MonitorFromWindow(this.Handle, MONITOR_DEFAULTTOPRIMARY);
                physicalMonitors = new PHYSICAL_MONITOR[1];

                monitorsInitialized = GetPhysicalMonitorsFromHMONITOR(hMonitor, 1, physicalMonitors);

                if (!monitorsInitialized)
                {
                    MessageBox.Show("Monitor control not available. Timer will run but won't turn off monitor.");
                }
            }
            catch (Exception ex)
            {
                monitorsInitialized = false;
                MessageBox.Show($"Monitor init failed: {ex.Message}");
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            int timeValue = (int)numTime.Value;
            string timeUnit = comboBoxTime.SelectedItem.ToString();

            if (timeUnit == "Minutes")
            {
                m_remainingSeconds = timeValue * 60;
            }
            else
            {
                m_remainingSeconds = timeValue;
            }

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
            if (physicalMonitors != null && physicalMonitors.Length > 0)
            {
                foreach (var monitor in physicalMonitors)
                {
                    if (!SetVCPFeature(monitor.hPhysicalMonitor, VCP_CODE_POWER_MODE, POWER_MODE_OFF))
                    {
                        MessageBox.Show("Failed to turn off the monitor.");
                    }
                }
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);

            if (physicalMonitors != null && physicalMonitors.Length > 0)
            {
                DestroyPhysicalMonitors((uint)physicalMonitors.Length, physicalMonitors);
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct PHYSICAL_MONITOR
        {
            public IntPtr hPhysicalMonitor;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szPhysicalMonitorDescription;
        }
    }
}