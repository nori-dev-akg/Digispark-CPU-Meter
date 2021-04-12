using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using HidSharp;
using HidSharp.Experimental;
using HidSharp.Reports;
using HidSharp.Reports.Encodings;
using HidSharp.Utility;


namespace CPUMeter
{
    public partial class FormMain : Form
    {
        public const string NICName = "Realtek PCIe GBE Family Controller";

        // counters
        PerformanceCounter cpu;
        PerformanceCounter mem;
        PerformanceCounter net;
        PerformanceCounter hd0;
        PerformanceCounter hd1;

        // Total Memory Size(MB)
        ulong totalMemory = new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory / 1024 / 1024;

        // HID Digispark
        DeviceList deviceList;
        HidDevice dev;
        HidStream hidStream;
        bool isOpen = false;
        System.Timers.Timer timerTick;

        public FormMain()
        {
            InitializeComponent();

            // タスクバーに表示しないようにします。
            ShowInTaskbar = false;

            // メニュー項目を作成します。
            var menuItem = new ToolStripMenuItem();
            menuItem.Text = "&Exit";
            menuItem.Click += new EventHandler(Exit_Click);

            // メニューを作成します。
            var menu = new ContextMenuStrip();
            menu.Items.Add(menuItem);

            // アイコンを作成します。
            // アイコンファイル(32x32の24bit Bitmap)が別途必要になります。
            var icon = new NotifyIcon();
            icon.Icon = Properties.Resources.Icon1;
            icon.Visible = true;
            icon.Text = "TaskTray Application";
            icon.ContextMenuStrip = menu;


            Init();
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
        }

        /**
         * @brief Exitメニューが選択されたときに呼び出されます。
         */
        private void Exit_Click(object sender, EventArgs e)
        {
            // アプリケーションを終了します。
            Application.Exit();
        }

        private void Init()
        {
            cpu = pc("Processor", "% Processor Time", "_Total");
            mem = pc("Memory", "Available MBytes", "");
            net = pc("Network Interface", "Bytes Total/sec", NICName);
            hd0 = pc("PhysicalDisk", "% Idle Time", "0 C:");
            hd1 = pc("PhysicalDisk", "% Idle Time", "1 D:");

            timerTick = new System.Timers.Timer(1000);
            timerTick.Elapsed += TimerTick_Elapsed;
            timerTick.AutoReset = true;

            if (initHID())
            {
                timerTick.Enabled = true;
            }
        }

        System.Diagnostics.PerformanceCounter pc(string categoryName, string counterName, string instanceName)
        {
            string machineName = ".";

            if (!System.Diagnostics.PerformanceCounterCategory.Exists(categoryName, machineName))
            {
                Console.WriteLine("cannot found category! :" + categoryName);
                return null;
            }

            if (!System.Diagnostics.PerformanceCounterCategory.CounterExists(counterName, categoryName, machineName))
            {
                Console.WriteLine("cannot found category counter! :" + counterName);
                return null;
            }

            System.Diagnostics.PerformanceCounter pc = new System.Diagnostics.PerformanceCounter(categoryName, counterName, instanceName, machineName);

            return pc;
        }

        private void TimerTick_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            // 1sec Tick
            ulong usedMem = (ulong)(totalMemory - mem.NextValue());
            byte usedMemPer = (byte)(100 * usedMem / totalMemory); // %
            byte cpuPer = (byte)cpu.NextValue();   // %
            byte hddPer = (byte)(100 - hd0.NextValue()); // %
            short netKB = (short)(net.NextValue() / 1024); // KB

            Console.WriteLine("CPU:{0}% MEM:{1}% NET:{2}KB HD:{3}", cpuPer, usedMemPer, netKB, hddPer);


            if (isOpen)
            {
                //using (hidStream)
                //{
                byte[] data = new byte[5 + 1];

                data[0] = 0;    // ReportID
                data[1] = cpuPer;
                data[2] = usedMemPer;
                data[3] = hddPer;
                if(netKB>=1000) { netKB = (short)(netKB / 1000 * (-1)); }
                data[4] = BitConverter.GetBytes(netKB)[0];
                data[5] = BitConverter.GetBytes(netKB)[1];

                hidStream.SetFeature(data); // send Digispark
                //}
            }
        }

        private bool initHID()
        {
            HidSharpDiagnostics.EnableTracing = true;
            HidSharpDiagnostics.PerformStrictChecks = true;

            if(deviceList != null)
            {
                deviceList.Changed -= DeviceList_Changed;
            }
            deviceList = DeviceList.Local;
            deviceList.Changed += DeviceList_Changed;

            var devs = deviceList.GetHidDevices(0x16c0, 0x05dc);
            if (devs.Count() == 0) {
                isOpen = false;
                return false;
            }
            if (isOpen) { return true; }

            dev = devs.FirstOrDefault();
            Console.WriteLine(dev.ToString() + " @ " + dev.DevicePath);

            Console.WriteLine(string.Format("Max Lengths: Input {0}, Output {1}, Feature {2}",
                dev.GetMaxInputReportLength(),
                dev.GetMaxOutputReportLength(),
                dev.GetMaxFeatureReportLength()));

            var rawReportDescriptor = dev.GetRawReportDescriptor();
            Console.WriteLine("Report Descriptor:");
            Console.WriteLine("  {0} ({1} bytes)", string.Join(" ", rawReportDescriptor.Select(d => d.ToString("X2"))), rawReportDescriptor.Length);

            int indent = 0;
            foreach (var element in EncodedItem.DecodeItems(rawReportDescriptor, 0, rawReportDescriptor.Length))
            {
                if (element.ItemType == ItemType.Main && element.TagForMain == MainItemTag.EndCollection) { indent -= 2; }

                Console.WriteLine("  {0}{1}", new string(' ', indent), element);

                if (element.ItemType == ItemType.Main && element.TagForMain == MainItemTag.Collection) { indent += 2; }
            }

            var reportDescriptor = dev.GetReportDescriptor();

            DeviceItem deviceItem = reportDescriptor.DeviceItems.FirstOrDefault();

            foreach (var usage in deviceItem.Usages.GetAllValues())
            {
                Console.WriteLine(string.Format("Usage: {0:X4} {1}", usage, (Usage)usage));
            }
            foreach (var report in deviceItem.Reports)
            {
                Console.WriteLine(string.Format("{0}: ReportID={1}, Length={2}, Items={3}",
                                    report.ReportType, report.ReportID, report.Length, report.DataItems.Count));
                foreach (var dataItem in report.DataItems)
                {
                    Console.WriteLine(string.Format("  {0} Elements x {1} Bits, Units: {2}, Expected Usage Type: {3}, Flags: {4}, Usages: {5}",
                        dataItem.ElementCount, dataItem.ElementBits, dataItem.Unit.System, dataItem.ExpectedUsageType, dataItem.Flags,
                        string.Join(", ", dataItem.Usages.GetAllValues().Select(usage => usage.ToString("X4") + " " + ((Usage)usage).ToString()))));
                }
            }

            isOpen = dev.TryOpen(out hidStream);

            return isOpen;
        }

        private void DeviceList_Changed(object sender, DeviceListChangedEventArgs e)
        {
            if (initHID()) {
                timerTick.Enabled = true;
            }
            else
            {
                timerTick.Enabled = false;
            }
            Console.WriteLine("DeviceList_Changed: {0}", isOpen);
        }
    }
}
