/*
 File          : MainForm.cs
 Project       : SENG3020 - Term Project
 Programmer    : Valentyn Novosydliuk
 File Version  : 11/28/2025
 Description   : Defines the main Windows Form for the FDMS Ground Terminal including
                 dashboard, history, invalid packet and settings views.
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using FDMS.GroundTerminal.Models;
using FDMS.GroundTerminal.Services;
using FDMS.GroundTerminal.Data;

namespace FDMS.GroundTerminal
{
    public class MainForm : Form
    {
        // ======= THEME COLORS (pastel / aviation style) =======
        private readonly Color AppBackground = Color.FromArgb(234, 242, 248); // light blue-gray
        private readonly Color CardBackground = Color.White;                   // white cards
        private readonly Color HeaderBackground = Color.FromArgb(215, 230, 240); // soft blue header
        private readonly Color AccentColor = Color.FromArgb(88, 148, 200);  // blue primary
        private readonly Color AccentColorGreen = Color.FromArgb(92, 160, 140);  // green secondary
        private readonly Color SoftBorder = Color.FromArgb(200, 210, 220); // subtle borders
        private readonly Color TextDark = Color.FromArgb(40, 55, 70);    // dark gray
        private readonly Color TextLight = Color.FromArgb(110, 130, 150); // lighter gray

        // Services
        private readonly IDatabaseService _databaseService;
        private bool _isRealTimeEnabled;

        // Common
        private TabControl tabMain;

        // Dashboard tab
        private TabPage tabDashboard;
        private ComboBox cboDashboardTail;
        private Label lblDashboardRealTime;
        private Button btnStartRealTime;
        private Button btnStopRealTime;
        private Label lblAccelXValue;
        private Label lblAccelYValue;
        private Label lblAccelZValue;
        private Label lblWeightValue;
        private Label lblAltitudeValue;
        private Label lblPitchValue;
        private Label lblBankValue;
        private DataGridView dgvDashboardLog;
        private Timer timerRealTime;

        // Binding list for the Event Log grid
        private BindingList<TelemetryRecord> _dashboardLog = new BindingList<TelemetryRecord>();

        // History tab
        private TabPage tabHistory;
        private TextBox txtHistoryTail;
        private DateTimePicker dtpHistoryFrom;
        private DateTimePicker dtpHistoryTo;
        private Button btnHistorySearch;
        private DataGridView dgvHistoryResults;

        // Invalid tab
        private TabPage tabInvalid;
        private TextBox txtInvalidTail;
        private DateTimePicker dtpInvalidFrom;
        private DateTimePicker dtpInvalidTo;
        private Button btnInvalidSearch;
        private DataGridView dgvInvalidResults;

        // Settings tab
        private TabPage tabSettings;
        private Label lblDbStatusValue;
        private Label lblServerName;
        private Label lblDatabaseName;

        /*
         Function     : MainForm
         Description  : Constructor that initializes the database service and builds the
                        main layout for the FDMS Ground Terminal user interface.
         Parameters   : none
         Return Values: void
        */
        public MainForm()
        {
            string connectionString = Data.DataBaseConfiguration.GetConnectionString();
            _databaseService = new SqlServerDatabaseService(connectionString); // REAL DB

            BuildLayout();
            InitializeGui();
        }

        // ======================= LAYOUT =======================

        /*
         Function     : BuildLayout
         Description  : Configures the main form properties and creates the tab control
                        with dashboard, history, invalid packets and settings tabs.
         Parameters   : none
         Return Values: void
        */
        private void BuildLayout()
        {
            // Form properties
            Text = "FDMS Ground Terminal";
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(1000, 600);
            BackColor = AppBackground;

            // TabControl
            tabMain = new TabControl
            {
                Dock = DockStyle.Fill,
                Padding = new Point(10, 6)
            };
            Controls.Add(tabMain);

            // Tabs
            tabDashboard = new TabPage("Dashboard");
            tabHistory = new TabPage("History");
            tabInvalid = new TabPage("Invalid Packets");
            tabSettings = new TabPage("Settings");

            tabDashboard.BackColor = AppBackground;
            tabHistory.BackColor = AppBackground;
            tabInvalid.BackColor = AppBackground;
            tabSettings.BackColor = AppBackground;

            tabMain.TabPages.Add(tabDashboard);
            tabMain.TabPages.Add(tabHistory);
            tabMain.TabPages.Add(tabInvalid);
            tabMain.TabPages.Add(tabSettings);

            BuildDashboardTab();
            BuildHistoryTab();
            BuildInvalidTab();
            BuildSettingsTab();
        }

        /*
         Function     : BuildDashboardTab
         Description  : Builds the dashboard tab including live telemetry controls,
                        current value labels and the event log grid.
         Parameters   : none
         Return Values: void
        */
        private void BuildDashboardTab()
        {
            // Outer panel with padding (background)
            var outer = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                BackColor = AppBackground
            };
            tabDashboard.Controls.Add(outer);

            // Card panel
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = CardBackground,
                BorderStyle = BorderStyle.FixedSingle
            };
            outer.Controls.Add(card);

            // Top panel for controls (acts as header area)
            var topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = HeaderBackground
            };

            var lblHeaderTitle = new Label
            {
                Text = "FDMS Ground Terminal - Live Telemetry",
                AutoSize = true,
                Location = new Point(10, 10),
                ForeColor = TextDark,
                Font = new Font("Segoe UI", 11, FontStyle.Bold)
            };

            var lblTail = new Label
            {
                Text = "Tail #:",
                AutoSize = true,
                Location = new Point(10, 40),
                ForeColor = TextDark,
                Font = new Font("Segoe UI", 9)
            };
            cboDashboardTail = new ComboBox
            {
                Location = new Point(60, 36),
                Width = 120,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            btnStartRealTime = new Button
            {
                Text = "Start Real-Time",
                Location = new Point(200, 32)
            };
            StyleButton(btnStartRealTime, true);
            btnStartRealTime.Click += btnStartRealTime_Click;

            btnStopRealTime = new Button
            {
                Text = "Stop Real-Time",
                Location = new Point(360, 32)
            };
            StyleButton(btnStopRealTime, false);
            btnStopRealTime.Click += btnStopRealTime_Click;

            lblDashboardRealTime = new Label
            {
                Text = "Real-Time: OFF",
                AutoSize = true,
                ForeColor = Color.DarkRed,
                Location = new Point(540, 38),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };

            topPanel.Controls.Add(lblHeaderTitle);
            topPanel.Controls.Add(lblTail);
            topPanel.Controls.Add(cboDashboardTail);
            topPanel.Controls.Add(btnStartRealTime);
            topPanel.Controls.Add(btnStopRealTime);
            topPanel.Controls.Add(lblDashboardRealTime);

            // Middle panel for telemetry values
            var midPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 150,
                BackColor = CardBackground
            };

            int leftColX = 20;
            int rightColX = 260;
            int startY = 20;
            int rowHeight = 22;

            // G-Force
            var lblGForceTitle = new Label
            {
                Text = "G-Force Parameters",
                AutoSize = true,
                Location = new Point(leftColX, startY),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = TextDark
            };
            midPanel.Controls.Add(lblGForceTitle);

            int y = startY + 25;

            midPanel.Controls.Add(MakeLabel("Accel X:", leftColX, y));
            lblAccelXValue = MakeValueLabel(leftColX + 80, y);
            midPanel.Controls.Add(lblAccelXValue);
            y += rowHeight;

            midPanel.Controls.Add(MakeLabel("Accel Y:", leftColX, y));
            lblAccelYValue = MakeValueLabel(leftColX + 80, y);
            midPanel.Controls.Add(lblAccelYValue);
            y += rowHeight;

            midPanel.Controls.Add(MakeLabel("Accel Z:", leftColX, y));
            lblAccelZValue = MakeValueLabel(leftColX + 80, y);
            midPanel.Controls.Add(lblAccelZValue);
            y += rowHeight;

            midPanel.Controls.Add(MakeLabel("Weight:", leftColX, y));
            lblWeightValue = MakeValueLabel(leftColX + 80, y);
            midPanel.Controls.Add(lblWeightValue);

            // Attitude
            var lblAttTitle = new Label
            {
                Text = "Attitude Parameters",
                AutoSize = true,
                Location = new Point(rightColX, startY),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = TextDark
            };
            midPanel.Controls.Add(lblAttTitle);

            y = startY + 25;

            midPanel.Controls.Add(MakeLabel("Altitude:", rightColX, y));
            lblAltitudeValue = MakeValueLabel(rightColX + 80, y);
            midPanel.Controls.Add(lblAltitudeValue);
            y += rowHeight;

            midPanel.Controls.Add(MakeLabel("Pitch:", rightColX, y));
            lblPitchValue = MakeValueLabel(rightColX + 80, y);
            midPanel.Controls.Add(lblPitchValue);
            y += rowHeight;

            midPanel.Controls.Add(MakeLabel("Bank:", rightColX, y));
            lblBankValue = MakeValueLabel(rightColX + 80, y);
            midPanel.Controls.Add(lblBankValue);

            // Separate small panel just for the log label
            var logLabelPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 30,
                BackColor = CardBackground
            };

            var lblLogTitle = new Label
            {
                Text = "Event Log (Last 50 Packets)",
                AutoSize = true,
                Location = new Point(20, 7),
                ForeColor = TextDark,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            logLabelPanel.Controls.Add(lblLogTitle);

            // Data grid for log
            dgvDashboardLog = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            // Add to card in docking order (top → top → top → fill)
            card.Controls.Add(dgvDashboardLog);
            card.Controls.Add(logLabelPanel);
            card.Controls.Add(midPanel);
            card.Controls.Add(topPanel);

            // Timer for real-time updates
            timerRealTime = new Timer
            {
                Interval = 1000
            };
            timerRealTime.Tick += timerRealTime_Tick;
        }

        /*
         Function     : BuildHistoryTab
         Description  : Builds the history tab and configures controls used to search
                        and display historical telemetry records.
         Parameters   : none
         Return Values: void
        */
        private void BuildHistoryTab()
        {
            var outer = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                BackColor = AppBackground
            };
            tabHistory.Controls.Add(outer);

            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = CardBackground,
                BorderStyle = BorderStyle.FixedSingle
            };
            outer.Controls.Add(card);

            // -------- Header panel --------
            var topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = HeaderBackground
            };

            var lblTitle = new Label
            {
                Text = "Historical Data Search",
                AutoSize = true,
                Location = new Point(10, 10),
                ForeColor = TextDark,
                Font = new Font("Segoe UI", 11, FontStyle.Bold)
            };

            var lblRealtimeInfo = new Label
            {
                Text = "Real-Time Disabled",
                AutoSize = true,
                Location = new Point(10, 40),
                ForeColor = Color.DarkRed,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };

            var lblTail = new Label
            {
                Text = "Tail #:",
                AutoSize = true,
                Location = new Point(200, 40),
                ForeColor = TextDark,
                Font = new Font("Segoe UI", 9)
            };
            txtHistoryTail = new TextBox
            {
                Location = new Point(250, 37),
                Width = 100
            };

            var lblFrom = new Label
            {
                Text = "From:",
                AutoSize = true,
                Location = new Point(370, 40),
                ForeColor = TextDark,
                Font = new Font("Segoe UI", 9)
            };
            dtpHistoryFrom = new DateTimePicker
            {
                Location = new Point(420, 37),
                Width = 160,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "yyyy-MM-dd HH:mm"
            };

            var lblTo = new Label
            {
                Text = "To:",
                AutoSize = true,
                Location = new Point(590, 40),
                ForeColor = TextDark,
                Font = new Font("Segoe UI", 9)
            };
            dtpHistoryTo = new DateTimePicker
            {
                Location = new Point(620, 37),
                Width = 160,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "yyyy-MM-dd HH:mm"
            };

            btnHistorySearch = new Button
            {
                Text = "Search",
                Location = new Point(800, 35)
            };
            StyleButton(btnHistorySearch, true);
            btnHistorySearch.Click += btnHistorySearch_Click;

            topPanel.Controls.Add(lblTitle);
            topPanel.Controls.Add(lblRealtimeInfo);
            topPanel.Controls.Add(lblTail);
            topPanel.Controls.Add(txtHistoryTail);
            topPanel.Controls.Add(lblFrom);
            topPanel.Controls.Add(dtpHistoryFrom);
            topPanel.Controls.Add(lblTo);
            topPanel.Controls.Add(dtpHistoryTo);
            topPanel.Controls.Add(btnHistorySearch);

            // -------- Results label panel (separate strip) --------
            var resultsLabelPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 30,
                BackColor = CardBackground
            };

            var lblResults = new Label
            {
                Text = "Results Table",
                AutoSize = true,
                Location = new Point(30, 7),
                ForeColor = TextDark,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            resultsLabelPanel.Controls.Add(lblResults);

            // -------- Data grid at the bottom (fills remaining) --------
            dgvHistoryResults = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            // Add to card in correct docking order
            card.Controls.Add(dgvHistoryResults);   // fill
            card.Controls.Add(resultsLabelPanel);   // top
            card.Controls.Add(topPanel);           // top
        }

        /*
         Function     : BuildInvalidTab
         Description  : Builds the invalid packets tab including filter controls and
                        the grid that lists invalid packet records.
         Parameters   : none
         Return Values: void
        */
        private void BuildInvalidTab()
        {
            var outer = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                BackColor = AppBackground
            };
            tabInvalid.Controls.Add(outer);

            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = CardBackground,
                BorderStyle = BorderStyle.FixedSingle
            };
            outer.Controls.Add(card);

            var topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 90,
                BackColor = HeaderBackground
            };

            var lblTitle = new Label
            {
                Text = "Invalid Packet Log",
                AutoSize = true,
                Location = new Point(10, 10),
                ForeColor = TextDark,
                Font = new Font("Segoe UI", 11, FontStyle.Bold)
            };

            var lblDbStatus = new Label
            {
                Text = "Database Status: Connected",
                AutoSize = true,
                Location = new Point(10, 40),
                ForeColor = Color.DarkGreen,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };

            var lblTail = new Label
            {
                Text = "Tail #:",
                AutoSize = true,
                Location = new Point(250, 40),
                ForeColor = TextDark,
                Font = new Font("Segoe UI", 9)
            };
            txtInvalidTail = new TextBox
            {
                Location = new Point(300, 37),
                Width = 100
            };

            var lblFrom = new Label
            {
                Text = "From:",
                AutoSize = true,
                Location = new Point(420, 40),
                ForeColor = TextDark,
                Font = new Font("Segoe UI", 9)
            };
            dtpInvalidFrom = new DateTimePicker
            {
                Location = new Point(470, 37),
                Width = 160,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "yyyy-MM-dd HH:mm"
            };

            var lblTo = new Label
            {
                Text = "To:",
                AutoSize = true,
                Location = new Point(640, 40),
                ForeColor = TextDark,
                Font = new Font("Segoe UI", 9)
            };
            dtpInvalidTo = new DateTimePicker
            {
                Location = new Point(670, 37),
                Width = 160,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "yyyy-MM-dd HH:mm"
            };

            btnInvalidSearch = new Button
            {
                Text = "Filter",
                Location = new Point(840, 35)
            };
            StyleButton(btnInvalidSearch, true);
            btnInvalidSearch.Click += btnInvalidSearch_Click;

            topPanel.Controls.Add(lblTitle);
            topPanel.Controls.Add(lblDbStatus);
            topPanel.Controls.Add(lblTail);
            topPanel.Controls.Add(txtInvalidTail);
            topPanel.Controls.Add(lblFrom);
            topPanel.Controls.Add(dtpInvalidFrom);
            topPanel.Controls.Add(lblTo);
            topPanel.Controls.Add(dtpInvalidTo);
            topPanel.Controls.Add(btnInvalidSearch);

            var lblLog = new Label
            {
                Text = "Invalid Packet Log",
                AutoSize = true,
                Location = new Point(30, 110),
                ForeColor = TextDark,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };

            dgvInvalidResults = new DataGridView
            {
                Dock = DockStyle.Bottom,
                Height = 380,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            card.Controls.Add(dgvInvalidResults);
            card.Controls.Add(lblLog);
            card.Controls.Add(topPanel);
        }

        /*
         Function     : BuildSettingsTab
         Description  : Builds the settings tab and displays database connection status,
                        server name and database name.
         Parameters   : none
         Return Values: void
        */
        private void BuildSettingsTab()
        {
            var outer = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                BackColor = AppBackground
            };
            tabSettings.Controls.Add(outer);

            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = CardBackground,
                BorderStyle = BorderStyle.FixedSingle
            };
            outer.Controls.Add(card);

            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = HeaderBackground
            };

            var lblTitle = new Label
            {
                Text = "Settings",
                AutoSize = true,
                Location = new Point(10, 18),
                ForeColor = TextDark,
                Font = new Font("Segoe UI", 11, FontStyle.Bold)
            };

            header.Controls.Add(lblTitle);
            card.Controls.Add(header);

            var lblDbStatusLabel = new Label
            {
                Text = "Database Status:",
                AutoSize = true,
                Location = new Point(30, 80),
                ForeColor = TextDark,
                Font = new Font("Segoe UI", 9)
            };
            lblDbStatusValue = new Label
            {
                Text = "Unknown",
                AutoSize = true,
                Location = new Point(150, 80),
                ForeColor = Color.DarkRed,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };

            lblServerName = new Label
            {
                Text = "Server:",
                AutoSize = true,
                Location = new Point(30, 110),
                ForeColor = TextDark,
                Font = new Font("Segoe UI", 9)
            };

            lblDatabaseName = new Label
            {
                Text = "Database:",
                AutoSize = true,
                Location = new Point(30, 140),
                ForeColor = TextDark,
                Font = new Font("Segoe UI", 9)
            };

            card.Controls.Add(lblDbStatusLabel);
            card.Controls.Add(lblDbStatusValue);
            card.Controls.Add(lblServerName);
            card.Controls.Add(lblDatabaseName);
        }

        // ======================= HELPERS =======================

        /*
         Function     : MakeLabel
         Description  : Creates a standard label used for field captions with
                        consistent font and color settings.
         Parameters   : text  - caption text
                        x     - horizontal position
                        y     - vertical position
         Return Values: Label
        */
        private Label MakeLabel(string text, int x, int y)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Location = new Point(x, y),
                ForeColor = TextDark,
                Font = new Font("Segoe UI", 9)
            };
        }

        /*
         Function     : MakeValueLabel
         Description  : Creates a label used to display telemetry values and applies
                        a lighter foreground color.
         Parameters   : x  - horizontal position
                        y  - vertical position
         Return Values: Label
        */
        private Label MakeValueLabel(int x, int y)
        {
            return new Label
            {
                Text = "--",
                AutoSize = true,
                Location = new Point(x, y),
                ForeColor = TextLight,
                Font = new Font("Segoe UI", 9, FontStyle.Regular)
            };
        }

        /*
         Function     : StyleButton
         Description  : Applies common visual styling to buttons so that primary and
                        secondary actions are clearly distinguished.
         Parameters   : b          - button to style
                        isPrimary  - true for primary style false for secondary style
         Return Values: void
        */
        private void StyleButton(Button b, bool isPrimary = true)
        {
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.BackColor = isPrimary ? AccentColor : AccentColorGreen;
            b.ForeColor = Color.White;
            b.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            b.Height = 30;
            b.Width = 130;
        }

        /*
         Function     : StyleGrid
         Description  : Applies a consistent visual style to data grids including
                        colors for headers rows and selection.
         Parameters   : g  - data grid to style
         Return Values: void
        */
        private void StyleGrid(DataGridView g)
        {
            g.BackgroundColor = CardBackground;
            g.BorderStyle = BorderStyle.None;
            g.EnableHeadersVisualStyles = false;

            g.ColumnHeadersDefaultCellStyle.BackColor = HeaderBackground;
            g.ColumnHeadersDefaultCellStyle.ForeColor = TextDark;
            g.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);

            g.DefaultCellStyle.BackColor = Color.White;
            g.DefaultCellStyle.ForeColor = TextDark;
            g.DefaultCellStyle.SelectionBackColor = AccentColor;
            g.DefaultCellStyle.SelectionForeColor = Color.White;

            g.RowHeadersVisible = false;
            g.GridColor = SoftBorder;
        }

        // ======================= LOGIC =======================

        /*
         Function     : InitializeGui
         Description  : Initializes control values for all tabs including tail lists,
                        date ranges data bindings and grid styling and updates status.
         Parameters   : none
         Return Values: void
        */
        private void InitializeGui()
        {
            IList<string> tails = _databaseService.GetTailNumbers();
            cboDashboardTail.DataSource = tails;

            DateTime today = DateTime.Today;
            dtpHistoryFrom.Value = today.AddHours(-1);
            dtpHistoryTo.Value = today;
            dtpInvalidFrom.Value = today.AddHours(-1);
            dtpInvalidTo.Value = today;

            // Bind grids
            dgvDashboardLog.AutoGenerateColumns = true;
            dgvDashboardLog.DataSource = _dashboardLog;

            dgvHistoryResults.AutoGenerateColumns = true;
            dgvInvalidResults.AutoGenerateColumns = true;

            // Apply grid styling
            StyleGrid(dgvDashboardLog);
            StyleGrid(dgvHistoryResults);
            StyleGrid(dgvInvalidResults);

            UpdateDatabaseStatus();
            UpdateRealTimeLabel();

            //TestDatabaseConnection();
        }

        /*
         Function     : TestDatabaseConnection
         Description  : Tests the database connection and shows a message box with
                        server database and status details.
         Parameters   : none
         Return Values: void
        */
        private void TestDatabaseConnection()
        {
            var status = _databaseService.TestConnection();

            MessageBox.Show(
                $"Database Connection Test:\n\n" +
                $"Server: {status.ServerName}\n" +
                $"Database: {status.DatabaseName}\n" +
                $"Status: {(status.IsConnected ? "CONNECTED" : "FAILED")}\n" +
                $"Message: {status.StatusMessage}",
                "Database Test",
                MessageBoxButtons.OK,
                status.IsConnected ? MessageBoxIcon.Information : MessageBoxIcon.Error
            );
        }

        /*
         Function     : UpdateDatabaseStatus
         Description  : Retrieves the current database connection status from the
                        service and updates the settings tab labels.
         Parameters   : none
         Return Values: void
        */
        private void UpdateDatabaseStatus()
        {
            var status = _databaseService.TestConnection();

            lblDbStatusValue.Text = status.IsConnected ? "Connected" : "Disconnected";
            lblDbStatusValue.ForeColor = status.IsConnected ? Color.DarkGreen : Color.DarkRed;

            lblServerName.Text = "Server: " + status.ServerName;
            lblDatabaseName.Text = "Database: " + status.DatabaseName;
        }

        /*
         Function     : UpdateRealTimeLabel
         Description  : Updates the dashboard real time label text and color based on
                        the internal real time flag.
         Parameters   : none
         Return Values: void
        */
        private void UpdateRealTimeLabel()
        {
            lblDashboardRealTime.Text = _isRealTimeEnabled ? "Real-Time: ON" : "Real-Time: OFF";
            lblDashboardRealTime.ForeColor = _isRealTimeEnabled ? Color.DarkGreen : Color.DarkRed;
        }

        /*
         Function     : btnStartRealTime_Click
         Description  : Event handler for the Start Real Time button which enables
                        real time mode and starts the timer.
         Parameters   : sender  - event source
                        e       - event data
         Return Values: void
        */
        private void btnStartRealTime_Click(object sender, EventArgs e)
        {
            _isRealTimeEnabled = true;
            UpdateRealTimeLabel();
            timerRealTime.Start();
        }

        /*
         Function     : btnStopRealTime_Click
         Description  : Event handler for the Stop Real Time button which disables
                        real time mode and stops the timer.
         Parameters   : sender  - event source
                        e       - event data
         Return Values: void
        */
        private void btnStopRealTime_Click(object sender, EventArgs e)
        {
            _isRealTimeEnabled = false;
            UpdateRealTimeLabel();
            timerRealTime.Stop();
        }

        /*
         Function     : timerRealTime_Tick
         Description  : Timer tick handler that retrieves the latest telemetry record
                        for the selected tail and updates the dashboard values and log.
         Parameters   : sender  - event source
                        e       - event data
         Return Values: void
        */
        private void timerRealTime_Tick(object sender, EventArgs e)
        {
            if (!_isRealTimeEnabled) return;

            string tailNumber = cboDashboardTail.SelectedItem as string;
            if (string.IsNullOrWhiteSpace(tailNumber)) return;

            TelemetryRecord record = _databaseService.GetLatestTelemetry(tailNumber);

            lblAccelXValue.Text = record.AccelX.ToString("F3");
            lblAccelYValue.Text = record.AccelY.ToString("F3");
            lblAccelZValue.Text = record.AccelZ.ToString("F3");
            lblWeightValue.Text = record.Weight.ToString("F1");

            lblAltitudeValue.Text = record.Altitude.ToString("F1");
            lblPitchValue.Text = record.Pitch.ToString("F2");
            lblBankValue.Text = record.Bank.ToString("F2");

            // Add to bound list → grid updates automatically
            _dashboardLog.Add(record);
        }

        /*
         Function     : btnHistorySearch_Click
         Description  : Event handler for the history search button which validates
                        input and loads telemetry records for the given range.
         Parameters   : sender  - event source
                        e       - event data
         Return Values: void
        */
        private void btnHistorySearch_Click(object sender, EventArgs e)
        {
            string tailNumber = txtHistoryTail.Text.Trim();
            if (string.IsNullOrEmpty(tailNumber))
            {
                MessageBox.Show("Please enter a tail number.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DateTime from = dtpHistoryFrom.Value;
            DateTime to = dtpHistoryTo.Value;

            IList<TelemetryRecord> records =
                _databaseService.SearchTelemetry(tailNumber, from, to);

            dgvHistoryResults.DataSource = records;
        }

        /*
         Function     : btnInvalidSearch_Click
         Description  : Event handler for the invalid packet filter button which
                        validates input and loads matching invalid packets.
         Parameters   : sender  - event source
                        e       - event data
         Return Values: void
        */
        private void btnInvalidSearch_Click(object sender, EventArgs e)
        {
            string tailNumber = txtInvalidTail.Text.Trim();
            if (string.IsNullOrEmpty(tailNumber))
            {
                MessageBox.Show("Please enter a tail number.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DateTime from = dtpInvalidFrom.Value;
            DateTime to = dtpInvalidTo.Value;

            IList<InvalidPacket> packets =
                 _databaseService.SearchInvalidPackets(tailNumber, from, to);

            dgvInvalidResults.DataSource = packets;
        }

        /*
         Function     : InitializeComponent
         Description  : Minimal designer initialization used by the Windows Forms
                        designer when the form is first created.
         Parameters   : none
         Return Values: void
        */
        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // MainForm
            // 
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.Name = "MainForm";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.ResumeLayout(false);

        }

        /*
         Function     : MainForm_Load
         Description  : Event handler for the form load event. Currently no logic is
                        required on load.
         Parameters   : sender  - event source
                        e       - event data
         Return Values: void
        */
        private void MainForm_Load(object sender, EventArgs e)
        {

        }
    }
}
