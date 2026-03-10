using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using Terminal.Gui;

namespace SystemInfo
{
    [SupportedOSPlatform("windows")]
    class Program
    {
        #region Windows API — Screensaver / Power

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);

        [Flags]
        public enum EXECUTION_STATE : uint
        {
            ES_AWAYMODE_REQUIRED = 0x00000040,
            ES_CONTINUOUS        = 0x80000000,
            ES_DISPLAY_REQUIRED  = 0x00000002,
            ES_SYSTEM_REQUIRED   = 0x00000001
        }

        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(SetConsoleCtrlEventHandler handler, bool add);
        private delegate bool SetConsoleCtrlEventHandler(CtrlType sig);
        private enum CtrlType
        {
            CTRL_C_EVENT       = 0,
            CTRL_BREAK_EVENT   = 1,
            CTRL_CLOSE_EVENT   = 2,
            CTRL_LOGOFF_EVENT  = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }

        #endregion

        #region Windows API — Console QuickEdit

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        const int  STD_INPUT_HANDLE      = -10;
        const uint ENABLE_QUICK_EDIT     = 0x0040;
        const uint ENABLE_EXTENDED_FLAGS = 0x0080;

        static void EnableQuickEdit()
        {
            var hIn = GetStdHandle(STD_INPUT_HANDLE);
            if (GetConsoleMode(hIn, out uint mode))
                SetConsoleMode(hIn, mode | ENABLE_QUICK_EDIT | ENABLE_EXTENDED_FLAGS);
        }

        #endregion

        #region Windows API — Memory

        [StructLayout(LayoutKind.Sequential)]
        struct MEMORYSTATUSEX
        {
            public uint  dwLength;
            public uint  dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

        #endregion

        // TUI label references updated on every refresh
        static Label lblMachine  = null!;
        static Label lblOs       = null!;
        static Label lblPlatform = null!;
        static Label lblVersion  = null!;
        static Label lblIps      = null!;
        static Label lblMemTotal = null!;
        static Label lblMemAvail = null!;
        static Label lblMemUsed  = null!;
        static Label lblTemps    = null!;

        // Color schemes
        static ColorScheme baseScheme  = null!;  // light gray on dark gray (borders, static text)
        static ColorScheme valueScheme = null!;  // white on dark gray (dynamic data)

        // Storage panel — one (left label, bar, right label) triple per drive, built at startup
        static readonly System.Collections.Generic.List<(Label lbl, StorageBar bar, Label freeLbl)> storageRows = new();
        static FrameView storageFrame = null!;

        static void Main(string[] args)
        {
            SetConsoleCtrlHandler(Handler, true);
            StopScreensaver();

            Application.Init();
            EnableQuickEdit();   // restore console text-select so user can copy with mouse
            var top = Application.Top;

            baseScheme = new ColorScheme
            {
                Normal    = Application.Driver.MakeAttribute(Color.DarkGray, Color.Black),
                Focus     = Application.Driver.MakeAttribute(Color.DarkGray, Color.Black),
                HotNormal = Application.Driver.MakeAttribute(Color.DarkGray, Color.Black),
                HotFocus  = Application.Driver.MakeAttribute(Color.Gray,     Color.Black),
                Disabled  = Application.Driver.MakeAttribute(Color.DarkGray, Color.Black),
            };
            valueScheme = new ColorScheme
            {
                Normal    = Application.Driver.MakeAttribute(Color.Gray,     Color.Black),
                Focus     = Application.Driver.MakeAttribute(Color.Gray,     Color.Black),
                HotNormal = Application.Driver.MakeAttribute(Color.Gray,     Color.Black),
                HotFocus  = Application.Driver.MakeAttribute(Color.White,    Color.Black),
                Disabled  = Application.Driver.MakeAttribute(Color.DarkGray, Color.Black),
            };
            var winScheme = new ColorScheme
            {
                Normal    = Application.Driver.MakeAttribute(Color.White,    Color.Black),
                Focus     = Application.Driver.MakeAttribute(Color.White,    Color.Black),
                HotNormal = Application.Driver.MakeAttribute(Color.White,    Color.Black),
                HotFocus  = Application.Driver.MakeAttribute(Color.White,    Color.Black),
                Disabled  = Application.Driver.MakeAttribute(Color.Gray,     Color.Black),
            };

            var win = new Window("SystemInfo")
            {
                X = 0, Y = 0,
                Width = Dim.Fill(), Height = Dim.Fill(),
                ColorScheme = winScheme
            };
            top.Add(win);

            // ── System Info panel (top-left) ──────────────────────────────────
            var sysFrame = new FrameView("System")
            {
                X = 0, Y = 0,
                Width = Dim.Percent(50), Height = 7,
                ColorScheme = baseScheme
            };
            // Static prefix labels (dark gray, inherited from baseScheme)
            sysFrame.Add(new Label("Machine : ") { X = 1, Y = 0 });
            sysFrame.Add(new Label("OS      : ") { X = 1, Y = 1 });
            sysFrame.Add(new Label("Platform: ") { X = 1, Y = 2 });
            sysFrame.Add(new Label("Version : ") { X = 1, Y = 3 });
            // Value labels (light gray)
            lblMachine  = new Label("") { X = 11, Y = 0, ColorScheme = valueScheme };
            lblOs       = new Label("") { X = 11, Y = 1, ColorScheme = valueScheme };
            lblPlatform = new Label("") { X = 11, Y = 2, ColorScheme = valueScheme };
            lblVersion  = new Label("") { X = 11, Y = 3, ColorScheme = valueScheme };
            sysFrame.Add(lblMachine, lblOs, lblPlatform, lblVersion);
            win.Add(sysFrame);

            // ── Network panel (top-right) ─────────────────────────────────────
            var netFrame = new FrameView("Network")
            {
                X = Pos.Right(sysFrame), Y = 0,
                Width = Dim.Fill(), Height = 7,
                ColorScheme = baseScheme
            };
            lblIps = new Label("") { X = 1, Y = 0, ColorScheme = valueScheme };
            netFrame.Add(lblIps);
            win.Add(netFrame);

            // ── Temperature panel (middle-left) ──────────────────────────────
            var tempFrame = new FrameView("Temperature")
            {
                X = 0, Y = Pos.Bottom(sysFrame),
                Width = Dim.Percent(50), Height = 8,
                ColorScheme = baseScheme
            };
            lblTemps = new Label("") { X = 1, Y = 0, ColorScheme = valueScheme };
            tempFrame.Add(lblTemps);
            win.Add(tempFrame);

            // ── Memory panel (middle-right) ───────────────────────────────────
            var memFrame = new FrameView("Memory")
            {
                X = Pos.Right(tempFrame), Y = Pos.Bottom(netFrame),
                Width = Dim.Fill(), Height = 8,
                ColorScheme = baseScheme
            };
            // Static prefix labels (dark gray, inherited)
            memFrame.Add(new Label("Total    : ") { X = 1, Y = 0 });
            memFrame.Add(new Label("Available: ") { X = 1, Y = 1 });
            memFrame.Add(new Label("Used     : ") { X = 1, Y = 2 });
            // Value labels (light gray)
            lblMemTotal = new Label("") { X = 12, Y = 0, ColorScheme = valueScheme };
            lblMemAvail = new Label("") { X = 12, Y = 1, ColorScheme = valueScheme };
            lblMemUsed  = new Label("") { X = 12, Y = 2, ColorScheme = valueScheme };
            memFrame.Add(lblMemTotal, lblMemAvail, lblMemUsed);
            win.Add(memFrame);

            // ── Storage panel (bottom, full width) ────────────────────────────
            // Layout per row:
            //   [left label: name+total+used]  [bar: stretches]  [right label: free GB]
            const int leftWidth = 38;  // "C:\  476.94 GB  234.44 GB used"
            const int freeWidth = 17;  // " 234.44 GB free"
            const int barGap    = 3;   // spaces between bar right edge and free label
            storageFrame = new FrameView("Storage")
            {
                X = 0, Y = Pos.Bottom(tempFrame),
                Width = Dim.Fill(), Height = Dim.Fill() - 1,
                ColorScheme = baseScheme
            };
            var drives = StorageDrive.GetDrives();
            for (int i = 0; i < drives.Count; i++)
            {
                var lbl = new Label("") { X = 1, Y = i * 2, ColorScheme = valueScheme };
                var bar = new StorageBar
                {
                    X        = leftWidth + 2,
                    Y        = i * 2,
                    Width    = Dim.Fill(freeWidth + barGap),
                    Height   = 1,
                    Fraction = drives[i].UsedFraction
                };
                var freeLbl = new Label("") { X = Pos.AnchorEnd(freeWidth), Y = i * 2, ColorScheme = valueScheme };
                storageRows.Add((lbl, bar, freeLbl));
                storageFrame.Add(lbl, bar, freeLbl);
            }
            win.Add(storageFrame);

            // ── Quit hint ─────────────────────────────────────────────────────
            win.Add(new Label("Press Q to quit") { X = Pos.AnchorEnd(16), Y = Pos.AnchorEnd(1) });

            // ── Initial data load + 500 ms refresh timer ──────────────────────
            RefreshData();
            Application.MainLoop.AddTimeout(TimeSpan.FromMilliseconds(500), _ =>
            {
                RefreshData();
                return true; // keep the timer running
            });

            // ── Q / q quits ───────────────────────────────────────────────────
            top.KeyPress += (e) =>
            {
                var k = e.KeyEvent.Key;
                if (k == Key.Q || k == (Key.Q | Key.ShiftMask))
                    Shutdown();
            };

            Application.Run();
            Application.Shutdown();
        }

        static void RefreshData()
        {
            // System info
            lblMachine.Text  = Environment.MachineName;
            lblOs.Text       = RuntimeInformation.OSDescription;
            lblPlatform.Text = Environment.OSVersion.Platform.ToString();
            lblVersion.Text  = Environment.OSVersion.Version.ToString();

            // Network
            var ips = MachineIPAddress.IPaddresses;
            var ipSb = new StringBuilder();
            foreach (var ip in ips)
                ipSb.AppendLine($"IP: {ip.Address}");
            lblIps.Text = ipSb.ToString().TrimEnd();

            // Memory (Windows GlobalMemoryStatusEx)
            var mem = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
            if (GlobalMemoryStatusEx(ref mem))
            {
                const double Gb = 1024.0 * 1024 * 1024;
                double total = mem.ullTotalPhys / Gb;
                double avail = mem.ullAvailPhys / Gb;
                double used  = 100.0 * (total - avail) / total;
                lblMemTotal.Text = $"{total,7:0.00} GB";
                lblMemAvail.Text = $"{avail,7:0.00} GB";
                lblMemUsed.Text  = $"{used,7:0.00} %";
            }

            // Temperatures
            var temps = Temperature.Temperatures;
            var tSb = new StringBuilder();
            foreach (var t in temps)
            {
                if (t.CurrentValue > 0)
                    tSb.AppendLine($"{t.InstanceName}: {t.CurrentValue:0.0} °C");
            }
            if (tSb.Length == 0)
                tSb.Append("(run as admin to read temps)");
            lblTemps.Text = tSb.ToString().TrimEnd();

            // Storage
            var drives = StorageDrive.GetDrives();
            for (int i = 0; i < storageRows.Count && i < drives.Count; i++)
            {
                var d = drives[i];
                storageRows[i].lbl.Text     = $"{d.Name,-4} {d.TotalGb,7:0.00} GB  {(d.TotalGb - d.FreeGb),7:0.00} GB used";
                storageRows[i].bar.Fraction  = d.UsedFraction;
                storageRows[i].freeLbl.Text  = $"{d.FreeGb,7:0.00} GB free";
            }

            Application.Refresh();
        }

        static void StopScreensaver() =>
            SetThreadExecutionState(
                EXECUTION_STATE.ES_DISPLAY_REQUIRED |
                EXECUTION_STATE.ES_SYSTEM_REQUIRED  |
                EXECUTION_STATE.ES_CONTINUOUS);

        static void AllowScreensaver() =>
            SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS);

        static void Shutdown()
        {
            AllowScreensaver();
            Application.RequestStop();
        }

        static bool Handler(CtrlType signal)
        {
            Shutdown();
            return false;
        }
    }
}
