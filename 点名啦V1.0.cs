using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Globalization;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;

[assembly: AssemblyTitle("点名啦")]
[assembly: AssemblyProduct("点名啦")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

namespace DianMingLa
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                try
                {
                    File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error.log"),
                        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine + ex,
                        new UTF8Encoding(false));
                }
                catch { }
                MessageBox.Show("点名啦启动失败：\n\n" + ex.Message + "\n\n详细信息已写入 error.log。",
                    "启动失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    internal sealed class AppSettings
    {
        public bool TopMostEnabled { get; set; }
        public bool MusicEnabled { get; set; }
        public string SelectedMusic { get; set; }
        public int Volume { get; set; }
        public string LastClass { get; set; }

        public AppSettings()
        {
            TopMostEnabled = true;
            MusicEnabled = true;
            SelectedMusic = "";
            Volume = 42;
            LastClass = "示例一班";
        }
    }

    internal sealed class StudentInfo
    {
        public string Name { get; private set; }
        public int Count { get; set; }

        public StudentInfo(string name)
        {
            Name = name;
            Count = 0;
        }
    }

    internal enum StudentCardState
    {
        Waiting,
        Rolling,
        Selected,
        Drawn
    }

    internal sealed class StudentCard : Control
    {
        private StudentCardState visualState;
        private int count;

        public string StudentName { get; private set; }

        public StudentCard(string studentName)
        {
            StudentName = studentName;
            visualState = StudentCardState.Waiting;
            count = 0;
            DoubleBuffered = true;
            Size = new Size(92, 44);
            Margin = new Padding(3);
            Font = new Font("Microsoft YaHei UI", 12F, FontStyle.Bold);
            Cursor = Cursors.Default;
            AccessibleName = studentName;
            AccessibleRole = AccessibleRole.StaticText;
        }

        public void SetDisplay(StudentCardState state, int drawCount)
        {
            visualState = state;
            count = drawCount;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            Color fill = Color.White;
            Color border = Color.FromArgb(221, 229, 239);
            Color nameColor = Color.FromArgb(31, 42, 61);
            Color statusColor = Color.FromArgb(102, 112, 133);

            if (visualState == StudentCardState.Rolling)
            {
                fill = Color.FromArgb(255, 244, 214);
                border = Color.FromArgb(168, 100, 8);
                nameColor = Color.FromArgb(168, 100, 8);
                statusColor = Color.FromArgb(168, 100, 8);
            }
            else if (visualState == StudentCardState.Selected)
            {
                fill = Color.FromArgb(232, 247, 240);
                border = Color.FromArgb(18, 128, 92);
                nameColor = Color.FromArgb(18, 128, 92);
                statusColor = Color.FromArgb(18, 128, 92);
            }
            else if (visualState == StudentCardState.Drawn)
            {
                fill = Color.FromArgb(241, 244, 248);
                border = Color.FromArgb(228, 231, 236);
                nameColor = Color.FromArgb(152, 162, 179);
                statusColor = Color.FromArgb(152, 162, 179);
            }

            Rectangle rect = new Rectangle(1, 1, Width - 3, Height - 3);
            using (GraphicsPath path = RoundedRect(rect, 8))
            using (SolidBrush brush = new SolidBrush(fill))
            using (Pen pen = new Pen(border, visualState == StudentCardState.Rolling || visualState == StudentCardState.Selected ? 2F : 1F))
            {
                e.Graphics.FillPath(brush, path);
                e.Graphics.DrawPath(pen, path);
            }

            Rectangle nameRect = new Rectangle(5, 3, Width - 10, Height - 7);
            TextRenderer.DrawText(e.Graphics, StudentName, Font, nameRect, nameColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        private static GraphicsPath RoundedRect(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    internal class QuietButton : Button
    {
        public QuietButton()
        {
            TabStop = false;
        }

        protected override bool ShowFocusCues
        {
            get { return false; }
        }
    }

    internal sealed class RoundedButton : Control, IButtonControl
    {
        private bool mouseOver;
        private bool mouseDown;
        private DialogResult dialogResult;

        public int CornerRadius { get; set; }
        public ContentAlignment TextAlign { get; set; }
        public Color BorderColor { get; set; }
        public Color HoverBackColor { get; set; }
        public Color HoverForeColor { get; set; }
        public Color PressedBackColor { get; set; }
        public Color PressedForeColor { get; set; }
        public DialogResult DialogResult
        {
            get { return dialogResult; }
            set { dialogResult = value; }
        }

        public RoundedButton()
        {
            CornerRadius = 10;
            BorderColor = Color.Transparent;
            HoverBackColor = Color.Empty;
            HoverForeColor = Color.Empty;
            PressedBackColor = Color.Empty;
            PressedForeColor = Color.Empty;
            Cursor = Cursors.Hand;
            Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold);
            TextAlign = ContentAlignment.MiddleCenter;
            TabStop = false;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                ControlStyles.StandardClick, true);
            SetStyle(ControlStyles.Selectable, false);
        }

        protected override bool ShowFocusCues
        {
            get { return false; }
        }

        public void NotifyDefault(bool value)
        {
            Invalidate();
        }

        public void PerformClick()
        {
            if (Enabled && Visible) OnClick(EventArgs.Empty);
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            if (dialogResult != DialogResult.None)
            {
                Form owner = FindForm();
                if (owner != null) owner.DialogResult = dialogResult;
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            Color background = Parent == null ? SystemColors.Control : Parent.BackColor;
            using (SolidBrush brush = new SolidBrush(background))
                e.Graphics.FillRectangle(brush, ClientRectangle);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            mouseOver = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            mouseOver = false;
            mouseDown = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) mouseDown = true;
            Invalidate();
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            mouseDown = false;
            Invalidate();
            base.OnMouseUp(e);
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            Invalidate();
            base.OnEnabledChanged(e);
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            Form owner = FindForm();
            if (owner != null && !owner.IsDisposed) owner.ActiveControl = null;
            Invalidate();
        }

        protected override void OnLostFocus(EventArgs e)
        {
            Invalidate();
            base.OnLostFocus(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle bounds = new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1));
            Color fill = BackColor;
            Color text = ForeColor;

            if (!Enabled)
            {
                fill = Blend(BackColor, Color.White, 55);
                text = Color.FromArgb(145, 156, 174);
            }
            else if (mouseDown)
            {
                fill = PressedBackColor.IsEmpty ? Blend(BackColor, Color.Black, 9) : PressedBackColor;
                if (!PressedForeColor.IsEmpty) text = PressedForeColor;
            }
            else if (mouseOver)
            {
                fill = HoverBackColor.IsEmpty
                    ? Blend(BackColor, BackColor.GetBrightness() > 0.92F
                        ? Color.FromArgb(210, 220, 236) : Color.White, 7)
                    : HoverBackColor;
                if (!HoverForeColor.IsEmpty) text = HoverForeColor;
            }

            using (GraphicsPath path = RoundedRect(bounds, CornerRadius))
            using (SolidBrush brush = new SolidBrush(fill))
            {
                e.Graphics.FillPath(brush, path);
                if (BorderColor.A > 0)
                {
                    using (Pen pen = new Pen(BorderColor, 1F)) e.Graphics.DrawPath(pen, path);
                }
            }

            Rectangle textBounds = new Rectangle(
                Padding.Left,
                Padding.Top,
                Math.Max(1, ClientSize.Width - Padding.Horizontal),
                Math.Max(1, ClientSize.Height - Padding.Vertical));
            TextRenderer.DrawText(e.Graphics, Text, Font, textBounds, text,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis |
                TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
        }

        private static Color Blend(Color source, Color target, int targetPercent)
        {
            int sourcePercent = 100 - targetPercent;
            return Color.FromArgb(source.A,
                (source.R * sourcePercent + target.R * targetPercent) / 100,
                (source.G * sourcePercent + target.G * targetPercent) / 100,
                (source.B * sourcePercent + target.B * targetPercent) / 100);
        }

        private static GraphicsPath RoundedRect(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int d = Math.Max(2, radius * 2);
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    internal sealed class ConfirmDialog : Form
    {
        public ConfirmDialog(string title, string message, string confirmText)
        {
            Text = title;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            Size = new Size(420, 184);
            MinimumSize = Size;
            MaximumSize = Size;
            Padding = new Padding(1);
            BackColor = Color.FromArgb(221, 229, 239);
            Font = new Font("Microsoft YaHei UI", 9.5F);

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.BackColor = Color.White;
            root.ColumnCount = 1;
            root.RowCount = 3;
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58F));

            Panel header = new Panel();
            header.Dock = DockStyle.Fill;
            header.BackColor = Color.White;

            Label titleLabel = new Label();
            titleLabel.Text = title;
            titleLabel.Dock = DockStyle.Fill;
            titleLabel.Padding = new Padding(18, 0, 0, 0);
            titleLabel.TextAlign = ContentAlignment.MiddleLeft;
            titleLabel.Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold);
            titleLabel.ForeColor = Color.FromArgb(31, 42, 61);

            QuietButton close = new QuietButton();
            close.Text = "\uE8BB";
            close.Dock = DockStyle.Right;
            close.Width = 44;
            close.FlatStyle = FlatStyle.Flat;
            close.FlatAppearance.BorderSize = 0;
            close.FlatAppearance.MouseOverBackColor = Color.FromArgb(196, 43, 28);
            close.FlatAppearance.MouseDownBackColor = Color.FromArgb(168, 47, 47);
            close.BackColor = Color.White;
            close.ForeColor = Color.FromArgb(31, 42, 61);
            close.Font = new Font("Segoe MDL2 Assets", 9.5F);
            close.TabStop = false;
            close.DialogResult = DialogResult.Cancel;
            close.MouseEnter += delegate { close.ForeColor = Color.White; };
            close.MouseLeave += delegate { close.ForeColor = Color.FromArgb(31, 42, 61); };

            header.Controls.Add(titleLabel);
            header.Controls.Add(close);

            Label messageLabel = new Label();
            messageLabel.Text = message;
            messageLabel.Dock = DockStyle.Fill;
            messageLabel.Padding = new Padding(22, 8, 22, 8);
            messageLabel.TextAlign = ContentAlignment.MiddleLeft;
            messageLabel.ForeColor = Color.FromArgb(52, 64, 84);

            FlowLayoutPanel footer = new FlowLayoutPanel();
            footer.Dock = DockStyle.Fill;
            footer.FlowDirection = FlowDirection.RightToLeft;
            footer.WrapContents = false;
            footer.Padding = new Padding(12, 10, 12, 8);
            footer.BackColor = Color.FromArgb(247, 249, 252);

            RoundedButton cancel = new RoundedButton();
            cancel.Text = "取消";
            cancel.Size = new Size(88, 38);
            cancel.Margin = new Padding(8, 0, 0, 0);
            cancel.BackColor = Color.White;
            cancel.ForeColor = Color.FromArgb(102, 112, 133);
            cancel.BorderColor = Color.FromArgb(221, 229, 239);
            cancel.HoverBackColor = Color.FromArgb(231, 237, 247);
            cancel.PressedBackColor = Color.FromArgb(220, 230, 245);
            cancel.DialogResult = DialogResult.Cancel;

            RoundedButton confirm = new RoundedButton();
            confirm.Text = confirmText;
            confirm.Size = new Size(96, 38);
            confirm.Margin = Padding.Empty;
            confirm.BackColor = Color.FromArgb(196, 61, 61);
            confirm.ForeColor = Color.White;
            confirm.HoverBackColor = Color.FromArgb(168, 47, 47);
            confirm.PressedBackColor = Color.FromArgb(143, 37, 37);
            confirm.DialogResult = DialogResult.Yes;

            footer.Controls.Add(cancel);
            footer.Controls.Add(confirm);
            root.Controls.Add(header, 0, 0);
            root.Controls.Add(messageLabel, 0, 1);
            root.Controls.Add(footer, 0, 2);
            Controls.Add(root);
            AcceptButton = cancel;
            CancelButton = cancel;
        }

        public static bool Confirm(IWin32Window owner, string message, string title, string confirmText)
        {
            using (ConfirmDialog dialog = new ConfirmDialog(title, message, confirmText))
            {
                return dialog.ShowDialog(owner) == DialogResult.Yes;
            }
        }
    }

    internal sealed class MainForm : Form
    {
        private const int WmNcLButtonDown = 0x00A1;
        private const int HtCaption = 2;

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam);

        private static readonly Color BackgroundColor = Color.FromArgb(243, 246, 250);
        private static readonly Color SurfaceColor = Color.White;
        private static readonly Color SecondarySurfaceColor = Color.FromArgb(244, 245, 251);
        private static readonly Color ResultSurfaceColor = Color.FromArgb(248, 247, 255);
        private static readonly Color BorderColor = Color.FromArgb(221, 229, 239);
        private static readonly Color PrimaryColor = Color.FromArgb(91, 75, 206);
        private static readonly Color PrimaryHover = Color.FromArgb(78, 63, 194);
        private static readonly Color PrimaryDark = Color.FromArgb(64, 50, 166);
        private static readonly Color TextColor = Color.FromArgb(31, 42, 61);
        private static readonly Color MutedColor = Color.FromArgb(102, 112, 133);
        private static readonly Color SidebarColor = Color.FromArgb(247, 249, 252);
        private static readonly Color DangerColor = Color.FromArgb(196, 61, 61);
        private static readonly Color DangerSurfaceColor = Color.FromArgb(253, 236, 236);

        private readonly Dictionary<string, List<StudentInfo>> classes;
        private readonly Dictionary<string, RoundedButton> classButtons;
        private readonly Dictionary<string, StudentCard> studentCards;
        private readonly Random random;
        private readonly Stopwatch drawWatch;
        private readonly Timer animationTimer;
        private readonly Timer clockTimer;
        private readonly Timer audioTimer;
        private readonly System.Windows.Media.MediaPlayer mediaPlayer;

        private TableLayoutPanel mainLayout;
        private FlowLayoutPanel studentFlow;
        private ComboBox classSelector;
        private Label clockLabel;
        private Label currentNameLabel;
        private Label resultHintLabel;
        private Label progressLabel;
        private Label classTitleLabel;
        private Label musicStatusLabel;
        private RoundedButton drawButton;
        private RoundedButton resetButton;
        private CheckBox topMostCheck;
        private Button topMostButton;
        private CheckBox musicCheck;
        private ComboBox musicCombo;
        private TrackBar volumeTrack;
        private RoundedButton previewButton;
        private RoundedButton miniModeButton;
        private RoundedButton musicMenuButton;
        private ContextMenuStrip musicMenu;
        private ContextMenuStrip trayMenu;
        private NotifyIcon trayIcon;
        private ToolTip toolTip;
        private readonly List<string> musicFiles;
        private readonly List<HistoryEntry> historyEntries;

        private Panel miniPanel;
        private Label miniNameLabel;
        private Label miniStatusLabel;
        private RoundedButton miniDrawButton;

        private string currentClass;
        private string highlightedName;
        private string selectedName;
        private string finalTarget;
        private bool drawing;
        private bool slowingDown;
        private bool previewing;
        private long slowdownStartedAt;
        private List<StudentInfo> animationOrder;
        private int animationIndex;
        private Rectangle normalBounds;
        private bool miniDragging;
        private Point miniDragOrigin;
        private Point formDragOrigin;
        private double audioVolume;
        private bool audioFadingOut;
        private bool initializing;
        private bool exitRequested;

        private readonly string baseDirectory;
        private readonly string musicDirectory;
        private readonly string settingsPath;
        private readonly string rosterPath;
        private readonly string historyPath;
        private AppSettings settings;

        public MainForm()
        {
            string testDataDirectory = Environment.GetEnvironmentVariable("DIANMINGLA_DATA_DIR");
            baseDirectory = String.IsNullOrWhiteSpace(testDataDirectory)
                ? AppDomain.CurrentDomain.BaseDirectory
                : testDataDirectory;
            musicDirectory = Path.Combine(baseDirectory, "music");
            settingsPath = Path.Combine(baseDirectory, "settings.json");
            rosterPath = Path.Combine(baseDirectory, "班级名单.json");
            historyPath = Path.Combine(baseDirectory, "点名历史.json");
            classes = LoadClassData(rosterPath);
            classButtons = new Dictionary<string, RoundedButton>();
            studentCards = new Dictionary<string, StudentCard>();
            musicFiles = new List<string>();
            historyEntries = LoadHistory(historyPath);
            random = new Random();
            drawWatch = new Stopwatch();
            animationOrder = new List<StudentInfo>();
            settings = LoadSettings();
            initializing = true;

            mediaPlayer = new System.Windows.Media.MediaPlayer();
            mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;

            animationTimer = new Timer();
            animationTimer.Interval = 100;
            animationTimer.Tick += AnimationTimer_Tick;

            clockTimer = new Timer();
            clockTimer.Interval = 1000;
            clockTimer.Tick += ClockTimer_Tick;

            audioTimer = new Timer();
            audioTimer.Interval = 50;
            audioTimer.Tick += AudioTimer_Tick;

            InitializeWindow();
            toolTip = new ToolTip();
            InitializeInterface();
            InitializeTrayIcon();
            topMostCheck.Checked = settings.TopMostEnabled;
            EnsureMusicFolderAndMigrateLegacyMusic();
            PopulateMusicList();
            RefreshClassSelector();

            string firstClass = classes.ContainsKey(settings.LastClass) ? settings.LastClass : classes.Keys.FirstOrDefault();
            if (!String.IsNullOrEmpty(firstClass)) SelectClass(firstClass);
            initializing = false;
            TopMost = topMostCheck.Checked;
            SaveSettings();
            UpdateClock();
            clockTimer.Start();
        }

        private void InitializeWindow()
        {
            Text = "点名啦 1.0";
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(920, 500);
            MinimumSize = new Size(920, 500);
            MaximumSize = new Size(920, 500);
            FormBorderStyle = FormBorderStyle.None;
            Padding = new Padding(1);
            BackColor = BackgroundColor;
            Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Regular);
            AutoScaleMode = AutoScaleMode.Dpi;
            KeyPreview = true;
            KeyDown += MainForm_KeyDown;
            FormClosing += MainForm_FormClosing;
            Shown += delegate { ActiveControl = null; };
        }

        private void InitializeInterface()
        {
            mainLayout = new TableLayoutPanel();
            mainLayout.Dock = DockStyle.Fill;
            mainLayout.BackColor = BackgroundColor;
            mainLayout.ColumnCount = 1;
            mainLayout.RowCount = 2;
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            mainLayout.Controls.Add(BuildSidebar(), 0, 0);
            mainLayout.Controls.Add(BuildContent(), 0, 1);
            Controls.Add(mainLayout);

            BuildMiniPanel();
        }

        private Control BuildSidebar()
        {
            TableLayoutPanel toolbar = new TableLayoutPanel();
            toolbar.Dock = DockStyle.Fill;
            toolbar.BackColor = SurfaceColor;
            toolbar.Padding = new Padding(10, 6, 0, 6);
            toolbar.ColumnCount = 9;
            toolbar.RowCount = 1;
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 230F));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 82F));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 82F));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 82F));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 82F));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40F));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40F));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 44F));
            toolbar.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            classSelector = new ComboBox();
            classSelector.DropDownStyle = ComboBoxStyle.DropDownList;
            classSelector.Dock = DockStyle.Fill;
            classSelector.Margin = new Padding(0, 1, 8, 1);
            classSelector.Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold);
            classSelector.FlatStyle = FlatStyle.Flat;
            classSelector.BackColor = Color.FromArgb(247, 249, 252);
            classSelector.DrawMode = DrawMode.OwnerDrawFixed;
            classSelector.ItemHeight = 25;
            classSelector.TabStop = false;
            classSelector.DrawItem += delegate(object sender, DrawItemEventArgs e)
            {
                if (e.Index < 0) return;
                bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
                using (SolidBrush background = new SolidBrush(selected ? Color.FromArgb(239, 237, 255) : classSelector.BackColor))
                    e.Graphics.FillRectangle(background, e.Bounds);
                TextRenderer.DrawText(e.Graphics, classSelector.Items[e.Index].ToString(), classSelector.Font,
                    new Rectangle(e.Bounds.X + 8, e.Bounds.Y, e.Bounds.Width - 12, e.Bounds.Height), TextColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            };
            classSelector.SelectedIndexChanged += delegate
            {
                if (initializing || drawing || classSelector.SelectedItem == null) return;
                SelectClass(classSelector.SelectedItem.ToString());
            };
            toolbar.Controls.Add(classSelector, 0, 0);

            Panel dragSurface = new Panel();
            dragSurface.Dock = DockStyle.Fill;
            dragSurface.MouseDown += TitleBar_MouseDown;
            toolbar.Controls.Add(dragSurface, 1, 0);

            RoundedButton rosterButton = MakeButton("名单", SecondarySurfaceColor, TextColor, 64, 34);
            rosterButton.Dock = DockStyle.Fill;
            rosterButton.Margin = new Padding(3, 0, 3, 0);
            rosterButton.Click += ShowRosterManager;
            toolbar.Controls.Add(rosterButton, 2, 0);

            RoundedButton historyButton = MakeButton("记录", SecondarySurfaceColor, TextColor, 64, 34);
            historyButton.Dock = DockStyle.Fill;
            historyButton.Margin = new Padding(3, 0, 3, 0);
            historyButton.Click += ShowHistory;
            toolbar.Controls.Add(historyButton, 3, 0);

            musicMenuButton = MakeButton("音乐", SecondarySurfaceColor, TextColor, 74, 34);
            musicMenuButton.Dock = DockStyle.Fill;
            musicMenuButton.Margin = new Padding(3, 0, 3, 0);
            musicMenuButton.Click += delegate
            {
                RebuildMusicMenu();
                musicMenu.Show(musicMenuButton, new Point(0, musicMenuButton.Height));
            };
            toolbar.Controls.Add(musicMenuButton, 4, 0);

            musicMenu = new ContextMenuStrip();
            musicMenu.Font = new Font("Microsoft YaHei UI", 9.5F);

            miniModeButton = MakeButton("迷你模式", SecondarySurfaceColor, TextColor, 80, 34);
            miniModeButton.Dock = DockStyle.Fill;
            miniModeButton.Margin = new Padding(3, 0, 3, 0);
            miniModeButton.Click += delegate { EnterMiniMode(); };
            toolbar.Controls.Add(miniModeButton, 5, 0);

            topMostCheck = new CheckBox();
            topMostCheck.CheckedChanged += TopMostCheck_CheckedChanged;

            topMostButton = MakeWindowButton("\uE718", Color.White, TextColor);
            topMostButton.Font = new Font("Segoe MDL2 Assets", 11.5F, FontStyle.Regular);
            topMostButton.ForeColor = MutedColor;
            topMostButton.AccessibleName = "窗口置顶";
            toolTip.SetToolTip(topMostButton, "窗口置顶");
            topMostButton.Click += delegate { topMostCheck.Checked = !topMostCheck.Checked; };
            toolbar.Controls.Add(topMostButton, 6, 0);

            Button minimize = MakeWindowButton("\uE921", Color.White, TextColor);
            minimize.Click += delegate { WindowState = FormWindowState.Minimized; };
            toolbar.Controls.Add(minimize, 7, 0);

            Button close = MakeWindowButton("\uE8BB", Color.White, TextColor);
            close.FlatAppearance.MouseOverBackColor = Color.FromArgb(196, 43, 28);
            close.FlatAppearance.MouseDownBackColor = Color.FromArgb(168, 47, 47);
            close.MouseEnter += delegate { close.ForeColor = Color.White; };
            close.MouseLeave += delegate { close.ForeColor = TextColor; };
            close.Click += delegate { Close(); };
            toolbar.Controls.Add(close, 8, 0);
            return toolbar;
        }

        private Control BuildLegacySidebar()
        {
            TableLayoutPanel toolbar = new TableLayoutPanel();
            toolbar.Dock = DockStyle.Fill;
            toolbar.BackColor = SidebarColor;
            toolbar.Padding = new Padding(10, 6, 10, 6);
            toolbar.ColumnCount = 2;
            toolbar.RowCount = 1;
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 56F));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44F));
            toolbar.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            TableLayoutPanel classArea = new TableLayoutPanel();
            classArea.Dock = DockStyle.Fill;
            classArea.BackColor = Color.White;
            classArea.Margin = new Padding(0, 0, 5, 0);
            classArea.Padding = new Padding(8, 3, 8, 5);
            classArea.ColumnCount = 1;
            classArea.RowCount = 2;
            classArea.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            classArea.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            TableLayoutPanel classHeader = new TableLayoutPanel();
            classHeader.Dock = DockStyle.Fill;
            classHeader.Margin = Padding.Empty;
            classHeader.ColumnCount = 3;
            classHeader.RowCount = 1;
            classHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            classHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 82F));
            classHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 82F));

            Label classSection = SectionLabel("选择班级");
            classSection.Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Bold);
            classSection.Dock = DockStyle.Fill;
            classHeader.Controls.Add(classSection, 0, 0);

            miniModeButton = MakeButton("迷你模式", Color.FromArgb(232, 238, 249), TextColor, 76, 22);
            miniModeButton.Dock = DockStyle.Fill;
            miniModeButton.Margin = new Padding(2, 0, 4, 2);
            miniModeButton.Font = new Font("Microsoft YaHei UI", 8.5F, FontStyle.Bold);
            miniModeButton.Click += delegate { EnterMiniMode(); };
            classHeader.Controls.Add(miniModeButton, 1, 0);

            topMostCheck = new CheckBox();
            topMostCheck.Text = "窗口置顶";
            topMostCheck.AutoSize = false;
            topMostCheck.Dock = DockStyle.Fill;
            topMostCheck.Margin = new Padding(3, 0, 0, 0);
            topMostCheck.Font = new Font("Microsoft YaHei UI", 8.5F);
            topMostCheck.ForeColor = TextColor;
            topMostCheck.TextAlign = ContentAlignment.MiddleLeft;
            topMostCheck.CheckedChanged += TopMostCheck_CheckedChanged;
            classHeader.Controls.Add(topMostCheck, 2, 0);
            classArea.Controls.Add(classHeader, 0, 0);

            TableLayoutPanel classRow = new TableLayoutPanel();
            classRow.Dock = DockStyle.Fill;
            classRow.Margin = Padding.Empty;
            classRow.ColumnCount = 5;
            classRow.RowCount = 1;
            for (int i = 0; i < 5; i++) classRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            classRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            int classColumn = 0;
            foreach (string className in new string[] { "示例一班", "示例二班", "示例三班" })
            {
                RoundedButton button = MakeButton(className, Color.FromArgb(246, 248, 252), TextColor, 96, 32);
                button.Dock = DockStyle.Fill;
                button.Margin = new Padding(2, 0, 2, 0);
                button.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
                string captured = className;
                button.Click += delegate { SelectClass(captured); };
                classButtons[className] = button;
                classRow.Controls.Add(button, classColumn, 0);
                classColumn++;
            }
            classArea.Controls.Add(classRow, 0, 1);

            TableLayoutPanel musicArea = new TableLayoutPanel();
            musicArea.Dock = DockStyle.Fill;
            musicArea.BackColor = Color.White;
            musicArea.Margin = new Padding(5, 0, 0, 0);
            musicArea.Padding = new Padding(8, 3, 8, 5);
            musicArea.ColumnCount = 1;
            musicArea.RowCount = 2;
            musicArea.RowStyles.Add(new RowStyle(SizeType.Absolute, 22F));
            musicArea.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            TableLayoutPanel musicHeader = new TableLayoutPanel();
            musicHeader.Dock = DockStyle.Fill;
            musicHeader.Margin = Padding.Empty;
            musicHeader.ColumnCount = 2;
            musicHeader.RowCount = 1;
            musicHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 74F));
            musicHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            Label musicSection = SectionLabel("背景音乐");
            musicSection.Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Bold);
            musicSection.Dock = DockStyle.Fill;
            musicHeader.Controls.Add(musicSection, 0, 0);

            musicStatusLabel = new Label();
            musicStatusLabel.Text = "可添加 MP3、WAV 或 WMA";
            musicStatusLabel.Font = new Font("Microsoft YaHei UI", 8F);
            musicStatusLabel.ForeColor = MutedColor;
            musicStatusLabel.TextAlign = ContentAlignment.MiddleRight;
            musicStatusLabel.AutoEllipsis = true;
            musicStatusLabel.Dock = DockStyle.Fill;
            musicHeader.Controls.Add(musicStatusLabel, 1, 0);
            musicArea.Controls.Add(musicHeader, 0, 0);

            TableLayoutPanel musicRow = new TableLayoutPanel();
            musicRow.Dock = DockStyle.Fill;
            musicRow.Margin = Padding.Empty;
            musicRow.ColumnCount = 6;
            musicRow.RowCount = 1;
            musicRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 84F));
            musicRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            musicRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 54F));
            musicRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 54F));
            musicRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 32F));
            musicRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 82F));
            musicRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            musicCheck = new CheckBox();
            musicCheck.Text = "点名音乐";
            musicCheck.AutoSize = false;
            musicCheck.Dock = DockStyle.Fill;
            musicCheck.Margin = new Padding(1, 0, 2, 0);
            musicCheck.Font = new Font("Microsoft YaHei UI", 8.5F);
            musicCheck.ForeColor = TextColor;
            musicCheck.CheckedChanged += MusicCheck_CheckedChanged;
            musicRow.Controls.Add(musicCheck, 0, 0);

            musicCombo = new ComboBox();
            musicCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            musicCombo.Font = new Font("Microsoft YaHei UI", 8.5F);
            musicCombo.DropDownWidth = 260;
            musicCombo.Dock = DockStyle.Fill;
            musicCombo.Margin = new Padding(2, 2, 2, 1);
            musicCombo.SelectedIndexChanged += delegate
            {
                if (previewing) StopMusicImmediately();
                settings.SelectedMusic = musicCombo.SelectedItem == null ? "" : musicCombo.SelectedItem.ToString();
                SaveSettings();
            };
            musicRow.Controls.Add(musicCombo, 1, 0);

            RoundedButton importButton = MakeButton("添加", Color.FromArgb(238, 244, 255), PrimaryColor, 50, 32);
            importButton.Dock = DockStyle.Fill;
            importButton.Margin = new Padding(2, 0, 2, 0);
            importButton.Font = new Font("Microsoft YaHei UI", 8.5F, FontStyle.Bold);
            importButton.Click += ImportButton_Click;
            musicRow.Controls.Add(importButton, 2, 0);

            previewButton = MakeButton("试听", Color.FromArgb(246, 248, 252), TextColor, 50, 32);
            previewButton.Dock = DockStyle.Fill;
            previewButton.Margin = new Padding(2, 0, 2, 0);
            previewButton.Font = new Font("Microsoft YaHei UI", 8.5F, FontStyle.Bold);
            previewButton.Click += PreviewButton_Click;
            musicRow.Controls.Add(previewButton, 3, 0);

            Label volumeLabel = new Label();
            volumeLabel.Text = "音量";
            volumeLabel.Dock = DockStyle.Fill;
            volumeLabel.TextAlign = ContentAlignment.MiddleCenter;
            volumeLabel.Font = new Font("Microsoft YaHei UI", 8F);
            volumeLabel.ForeColor = MutedColor;
            musicRow.Controls.Add(volumeLabel, 4, 0);

            volumeTrack = new TrackBar();
            volumeTrack.Minimum = 0;
            volumeTrack.Maximum = 100;
            volumeTrack.TickFrequency = 10;
            volumeTrack.TickStyle = TickStyle.None;
            volumeTrack.AutoSize = false;
            volumeTrack.Dock = DockStyle.Fill;
            volumeTrack.Margin = new Padding(0, 4, 0, 2);
            volumeTrack.ValueChanged += VolumeTrack_ValueChanged;
            musicRow.Controls.Add(volumeTrack, 5, 0);
            musicArea.Controls.Add(musicRow, 0, 1);

            toolbar.Controls.Add(classArea, 0, 0);
            toolbar.Controls.Add(musicArea, 1, 0);
            return toolbar;
        }

        private Control BuildContent()
        {
            TableLayoutPanel content = new TableLayoutPanel();
            content.Dock = DockStyle.Fill;
            content.BackColor = BackgroundColor;
            content.Padding = new Padding(10, 6, 2, 8);
            content.RowCount = 3;
            content.ColumnCount = 1;
            content.RowStyles.Add(new RowStyle(SizeType.Absolute, 62F));
            content.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            content.RowStyles.Add(new RowStyle(SizeType.Absolute, 54F));

            content.Controls.Add(BuildResultCard(), 0, 0);
            content.Controls.Add(BuildRosterCard(), 0, 1);
            content.Controls.Add(BuildActionBar(), 0, 2);
            return content;
        }

        private Control BuildResultCard()
        {
            Panel card = new Panel();
            card.Dock = DockStyle.Fill;
            card.BackColor = ResultSurfaceColor;
            card.Margin = new Padding(0, 0, 0, 7);
            card.Padding = new Padding(10, 3, 10, 3);

            resultHintLabel = new Label();
            resultHintLabel.Text = "准备开始点名";
            resultHintLabel.Font = new Font("Microsoft YaHei UI", 9F);
            resultHintLabel.ForeColor = MutedColor;
            resultHintLabel.TextAlign = ContentAlignment.MiddleLeft;
            resultHintLabel.Dock = DockStyle.Fill;

            currentNameLabel = new Label();
            currentNameLabel.Text = "请选择班级";
            currentNameLabel.Font = new Font("Microsoft YaHei UI", 23F, FontStyle.Bold);
            currentNameLabel.ForeColor = PrimaryColor;
            currentNameLabel.TextAlign = ContentAlignment.MiddleCenter;
            currentNameLabel.Dock = DockStyle.Fill;

            progressLabel = new Label();
            progressLabel.Text = "0 / 0";
            progressLabel.Font = new Font("Microsoft YaHei UI", 8.5F);
            progressLabel.ForeColor = MutedColor;
            progressLabel.TextAlign = ContentAlignment.MiddleRight;
            progressLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom;
            progressLabel.Size = new Size(140, 49);
            progressLabel.Location = new Point(card.ClientSize.Width - 150, 3);

            card.Controls.Add(currentNameLabel);
            card.Controls.Add(progressLabel);
            progressLabel.BringToFront();
            card.Resize += delegate
            {
                progressLabel.Left = card.ClientSize.Width - progressLabel.Width - 10;
                progressLabel.Height = Math.Max(20, card.ClientSize.Height - 6);
            };
            return card;
        }

        private Control BuildRosterCard()
        {
            Panel roster = new Panel();
            roster.Dock = DockStyle.Fill;
            roster.BackColor = SurfaceColor;
            roster.Padding = new Padding(8, 5, 8, 7);
            roster.Margin = new Padding(0);

            classTitleLabel = new Label();
            classTitleLabel.Text = "学生名单";
            classTitleLabel.Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Bold);
            classTitleLabel.ForeColor = TextColor;
            classTitleLabel.Dock = DockStyle.Top;
            classTitleLabel.Height = 24;

            studentFlow = new FlowLayoutPanel();
            studentFlow.Dock = DockStyle.Fill;
            studentFlow.AutoScroll = false;
            studentFlow.WrapContents = true;
            studentFlow.FlowDirection = FlowDirection.LeftToRight;
            studentFlow.BackColor = SurfaceColor;
            studentFlow.Padding = new Padding(0);
            studentFlow.Resize += delegate { ResizeStudentCards(); };

            roster.Controls.Add(studentFlow);
            return roster;
        }

        private Control BuildActionBar()
        {
            Panel bar = new Panel();
            bar.Dock = DockStyle.Fill;
            bar.BackColor = BackgroundColor;
            bar.Margin = Padding.Empty;
            bar.Padding = Padding.Empty;

            drawButton = MakeButton("开始点名", PrimaryColor, Color.White, 176, 44);
            drawButton.Font = new Font("Microsoft YaHei UI", 13F, FontStyle.Bold);
            drawButton.Padding = new Padding(0, 0, 0, 2);
            drawButton.HoverBackColor = PrimaryHover;
            drawButton.PressedBackColor = PrimaryDark;
            drawButton.Location = new Point(0, 5);
            drawButton.Click += DrawButton_Click;

            resetButton = MakeButton("重置", Color.FromArgb(247, 249, 252), MutedColor, 88, 38);
            resetButton.BorderColor = BorderColor;
            resetButton.HoverBackColor = DangerSurfaceColor;
            resetButton.HoverForeColor = DangerColor;
            resetButton.PressedBackColor = Color.FromArgb(248, 218, 218);
            resetButton.PressedForeColor = Color.FromArgb(168, 47, 47);
            resetButton.Location = new Point(188, 8);
            resetButton.Click += ResetButton_Click;

            clockLabel = new Label();
            clockLabel.Font = new Font("Microsoft YaHei UI", 8.5F, FontStyle.Bold);
            clockLabel.ForeColor = TextColor;
            clockLabel.TextAlign = ContentAlignment.MiddleRight;
            clockLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            clockLabel.Size = new Size(142, 44);
            clockLabel.Location = new Point(bar.Width - 142, 8);

            bar.Controls.Add(drawButton);
            bar.Controls.Add(resetButton);
            bar.Controls.Add(clockLabel);
            bar.Resize += delegate
            {
                clockLabel.Left = bar.ClientSize.Width - clockLabel.Width;
                clockLabel.Top = Math.Max(0, (bar.ClientSize.Height - clockLabel.Height) / 2 + 3);
            };
            return bar;
        }

        private void BuildMiniPanel()
        {
            miniPanel = new Panel();
            miniPanel.Dock = DockStyle.Fill;
            miniPanel.BackColor = Color.FromArgb(23, 34, 52);
            miniPanel.Visible = false;
            miniPanel.MouseDown += MiniDrag_MouseDown;
            miniPanel.MouseMove += MiniDrag_MouseMove;
            miniPanel.MouseUp += MiniDrag_MouseUp;

            miniStatusLabel = new Label();
            miniStatusLabel.Text = "点名啦 · 示例一班";
            miniStatusLabel.ForeColor = Color.FromArgb(177, 192, 217);
            miniStatusLabel.Font = new Font("Microsoft YaHei UI", 8.5F);
            miniStatusLabel.Location = new Point(16, 7);
            miniStatusLabel.Size = new Size(220, 22);
            miniStatusLabel.MouseDown += MiniDrag_MouseDown;
            miniStatusLabel.MouseMove += MiniDrag_MouseMove;
            miniStatusLabel.MouseUp += MiniDrag_MouseUp;

            miniNameLabel = new Label();
            miniNameLabel.Text = "准备点名";
            miniNameLabel.ForeColor = Color.White;
            miniNameLabel.Font = new Font("Microsoft YaHei UI", 20F, FontStyle.Bold);
            miniNameLabel.TextAlign = ContentAlignment.MiddleCenter;
            miniNameLabel.Location = new Point(16, 32);
            miniNameLabel.Size = new Size(248, 56);
            miniNameLabel.Anchor = AnchorStyles.Left | AnchorStyles.Top;

            miniDrawButton = MakeButton("开始点名", PrimaryColor, Color.White, 152, 42);
            miniDrawButton.Padding = new Padding(0, 0, 0, 1);
            miniDrawButton.Location = new Point(16, 96);
            miniDrawButton.Click += DrawButton_Click;

            RoundedButton expand = MakeButton("展开", Color.FromArgb(54, 69, 92), Color.White, 86, 42);
            expand.Location = new Point(178, 96);
            expand.Click += delegate { ExitMiniMode(); };

            RoundedButton close = MakeButton("×", miniPanel.BackColor, Color.White, 32, 28);
            close.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            close.Location = new Point(244, 3);
            close.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            close.Click += delegate { Close(); };

            miniPanel.Controls.Add(miniStatusLabel);
            miniPanel.Controls.Add(miniNameLabel);
            miniPanel.Controls.Add(miniDrawButton);
            miniPanel.Controls.Add(expand);
            miniPanel.Controls.Add(close);
            Controls.Add(miniPanel);
        }

        private static Label SectionLabel(string text)
        {
            Label label = new Label();
            label.Text = text;
            label.Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold);
            label.ForeColor = TextColor;
            label.AutoSize = true;
            return label;
        }

        private static RoundedButton MakeButton(string text, Color back, Color fore, int width, int height)
        {
            RoundedButton button = new RoundedButton();
            button.Text = text;
            button.BackColor = back;
            button.ForeColor = fore;
            button.Size = new Size(width, height);
            if (back.ToArgb() == PrimaryColor.ToArgb())
            {
                button.HoverBackColor = PrimaryHover;
                button.PressedBackColor = PrimaryDark;
            }
            else if (back.ToArgb() == SecondarySurfaceColor.ToArgb())
            {
                button.HoverBackColor = Color.FromArgb(235, 233, 248);
                button.PressedBackColor = Color.FromArgb(221, 216, 244);
            }
            return button;
        }

        private static Button MakeWindowButton(string text, Color back, Color fore)
        {
            Button button = new QuietButton();
            button.Text = text;
            button.Dock = DockStyle.Fill;
            button.Margin = Padding.Empty;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(241, 245, 251);
            button.FlatAppearance.MouseDownBackColor = Color.FromArgb(220, 230, 245);
            button.BackColor = back;
            button.ForeColor = fore;
            button.Font = new Font("Segoe MDL2 Assets", 9.5F, FontStyle.Regular);
            button.TabStop = false;
            return button;
        }

        private void InitializeTrayIcon()
        {
            trayMenu = new ContextMenuStrip();
            trayMenu.Font = new Font("Microsoft YaHei UI", 9.5F);

            ToolStripMenuItem openItem = new ToolStripMenuItem("打开点名啦");
            openItem.Font = new Font(trayMenu.Font, FontStyle.Bold);
            openItem.Click += delegate { RestoreFromTray(); };
            trayMenu.Items.Add(openItem);
            trayMenu.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem exitItem = new ToolStripMenuItem("退出");
            exitItem.Click += delegate
            {
                exitRequested = true;
                trayIcon.Visible = false;
                Close();
            };
            trayMenu.Items.Add(exitItem);

            trayIcon = new NotifyIcon();
            trayIcon.Icon = (Icon)(Icon ?? SystemIcons.Application).Clone();
            trayIcon.Text = "点名啦 1.0";
            trayIcon.ContextMenuStrip = trayMenu;
            trayIcon.Visible = false;
            trayIcon.DoubleClick += delegate { RestoreFromTray(); };
        }

        private void HideToTray()
        {
            if (drawing)
            {
                animationTimer.Stop();
                drawWatch.Stop();
                drawing = false;
                slowingDown = false;
                highlightedName = null;
                finalTarget = null;
                drawButton.Text = "开始点名";
                miniDrawButton.Text = "开始点名";
                drawButton.Enabled = true;
                miniDrawButton.Enabled = true;
                resetButton.Enabled = true;
                if (classSelector != null) classSelector.Enabled = true;
                foreach (RoundedButton button in classButtons.Values) button.Enabled = true;
                resultHintLabel.Text = "准备开始点名";
                currentNameLabel.Text = "准备点名";
                currentNameLabel.ForeColor = PrimaryColor;
                miniNameLabel.Text = "准备点名";
                miniStatusLabel.Text = "点名啦 · " + currentClass;
                RefreshCards();
            }

            StopMusicImmediately();
            trayIcon.Visible = true;
            Hide();
        }

        private void RestoreFromTray()
        {
            Show();
            if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal;
            trayIcon.Visible = false;
            Activate();
            BringToFront();
        }

        private void TitleBar_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || WindowState == FormWindowState.Maximized) return;
            ReleaseCapture();
            SendMessage(Handle, WmNcLButtonDown, new IntPtr(HtCaption), IntPtr.Zero);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (Pen border = new Pen(Color.FromArgb(185, 198, 218)))
                e.Graphics.DrawRectangle(border, 0, 0, Width - 1, Height - 1);
        }

        private static Dictionary<string, List<StudentInfo>> LoadClassData(string path)
        {
            try
            {
                RosterDocument document = DataFiles.Load<RosterDocument>(path);
                if (document != null && document.Classes != null && document.Classes.Count > 0)
                {
                    Dictionary<string, List<StudentInfo>> loaded = new Dictionary<string, List<StudentInfo>>();
                    foreach (RosterClassData rosterClass in document.Classes)
                    {
                        if (String.IsNullOrWhiteSpace(rosterClass.Name) || loaded.ContainsKey(rosterClass.Name)) continue;
                        List<StudentInfo> students = new List<StudentInfo>();
                        if (rosterClass.Students != null)
                        {
                            foreach (RosterStudentData item in rosterClass.Students.Where(s => s != null && !String.IsNullOrWhiteSpace(s.Name)))
                            {
                                StudentInfo student = new StudentInfo(item.Name.Trim());
                                student.Count = Math.Max(0, item.Count);
                                students.Add(student);
                            }
                        }
                        loaded[rosterClass.Name.Trim()] = students;
                    }
                    if (loaded.Count > 0) return loaded;
                }
            }
            catch
            {
                try
                {
                    RosterDocument backup = DataFiles.Load<RosterDocument>(path + ".bak");
                    if (backup != null && backup.Classes != null && backup.Classes.Count > 0)
                    {
                        Dictionary<string, List<StudentInfo>> recovered = new Dictionary<string, List<StudentInfo>>();
                        foreach (RosterClassData rosterClass in backup.Classes)
                            recovered[rosterClass.Name] = rosterClass.Students.Select(s =>
                            {
                                StudentInfo student = new StudentInfo(s.Name);
                                student.Count = Math.Max(0, s.Count);
                                return student;
                            }).ToList();
                        return recovered;
                    }
                }
                catch { }
            }
            return BuildClassData();
        }

        private void SaveClassData()
        {
            try
            {
                RosterDocument document = new RosterDocument();
                foreach (KeyValuePair<string, List<StudentInfo>> pair in classes)
                {
                    RosterClassData rosterClass = new RosterClassData();
                    rosterClass.Name = pair.Key;
                    rosterClass.Students = pair.Value.Select(s => new RosterStudentData { Name = s.Name, Count = s.Count }).ToList();
                    document.Classes.Add(rosterClass);
                }
                DataFiles.Save(rosterPath, document);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "班级名单保存失败。\n\n" + ex.Message, "保存失败",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private List<RosterClassData> SnapshotClasses()
        {
            return classes.Select(pair => new RosterClassData
            {
                Name = pair.Key,
                Students = pair.Value.Select(s => new RosterStudentData { Name = s.Name, Count = s.Count }).ToList()
            }).ToList();
        }

        private void RefreshClassSelector()
        {
            if (classSelector == null) return;
            string selected = currentClass ?? settings.LastClass;
            classSelector.BeginUpdate();
            classSelector.Items.Clear();
            foreach (string name in classes.Keys) classSelector.Items.Add(name);
            classSelector.EndUpdate();
            if (!String.IsNullOrEmpty(selected) && classSelector.Items.Contains(selected))
                classSelector.SelectedItem = selected;
            else if (classSelector.Items.Count > 0)
                classSelector.SelectedIndex = 0;
        }

        private void ShowRosterManager(object sender, EventArgs e)
        {
            if (drawing) return;
            using (RosterManagerDialog dialog = new RosterManagerDialog(SnapshotClasses()))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                string previous = currentClass;
                Dictionary<string, List<StudentInfo>> updated = new Dictionary<string, List<StudentInfo>>();
                foreach (RosterClassData rosterClass in dialog.Classes)
                {
                    updated[rosterClass.Name] = rosterClass.Students.Select(item =>
                    {
                        StudentInfo student = new StudentInfo(item.Name);
                        student.Count = Math.Max(0, item.Count);
                        return student;
                    }).ToList();
                }
                classes.Clear();
                foreach (KeyValuePair<string, List<StudentInfo>> pair in updated) classes[pair.Key] = pair.Value;
                SaveClassData();
                RefreshClassSelector();
                string next = classes.ContainsKey(previous) ? previous : classes.Keys.FirstOrDefault();
                if (!String.IsNullOrEmpty(next)) SelectClass(next);
            }
        }

        private static List<HistoryEntry> LoadHistory(string path)
        {
            try
            {
                List<HistoryEntry> loaded = DataFiles.Load<List<HistoryEntry>>(path);
                return loaded ?? new List<HistoryEntry>();
            }
            catch
            {
                try
                {
                    List<HistoryEntry> backup = DataFiles.Load<List<HistoryEntry>>(path + ".bak");
                    if (backup != null) return backup;
                }
                catch { }
                return new List<HistoryEntry>();
            }
        }

        private void SaveHistory()
        {
            try { DataFiles.Save(historyPath, historyEntries); }
            catch (Exception ex)
            {
                MessageBox.Show(this, "点名结果已经显示，但历史记录保存失败。\n\n" + ex.Message,
                    "记录保存失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ShowHistory(object sender, EventArgs e)
        {
            if (drawing) return;
            using (HistoryDialog dialog = new HistoryDialog(historyEntries))
            {
                dialog.ShowDialog(this);
                if (dialog.WasCleared)
                {
                    historyEntries.Clear();
                    SaveHistory();
                }
                else if (dialog.UndoRequested)
                {
                    UndoLastDraw();
                }
            }
        }

        private void UndoLastDraw()
        {
            HistoryEntry last = historyEntries.LastOrDefault(item => !item.Undone);
            if (last == null) return;
            last.Undone = true;
            if (classes.ContainsKey(last.ClassName))
            {
                StudentInfo student = classes[last.ClassName].LastOrDefault(item => item.Name == last.StudentName && item.Count > 0);
                if (student != null) student.Count--;
            }
            selectedName = null;
            highlightedName = null;
            if (classes.ContainsKey(currentClass))
            {
                currentNameLabel.Text = "已撤销";
                currentNameLabel.ForeColor = PrimaryColor;
                miniNameLabel.Text = "已撤销";
                RefreshCards();
                UpdateProgress();
            }
            SaveClassData();
            SaveHistory();
        }

        private void SelectClass(string className)
        {
            if (drawing || !classes.ContainsKey(className)) return;
            currentClass = className;
            selectedName = null;
            highlightedName = null;
            settings.LastClass = className;

            if (classSelector != null && !Object.Equals(classSelector.SelectedItem, className))
                classSelector.SelectedItem = className;

            foreach (KeyValuePair<string, RoundedButton> pair in classButtons)
            {
                bool selected = pair.Key == className;
                pair.Value.BackColor = selected ? PrimaryColor : Color.White;
                pair.Value.ForeColor = selected ? Color.White : TextColor;
            }

            studentFlow.SuspendLayout();
            studentFlow.Controls.Clear();
            studentCards.Clear();
            foreach (StudentInfo student in classes[className])
            {
                StudentCard card = new StudentCard(student.Name);
                card.SetDisplay(student.Count > 0 ? StudentCardState.Drawn : StudentCardState.Waiting, student.Count);
                studentCards[student.Name] = card;
                studentFlow.Controls.Add(card);
            }
            studentFlow.ResumeLayout();
            if (IsHandleCreated)
                BeginInvoke(new Action(ResizeStudentCards));
            else
                ResizeStudentCards();

            classTitleLabel.Text = className + " · 全员名单";
            resultHintLabel.Text = "准备开始点名";
            currentNameLabel.Text = "准备点名";
            currentNameLabel.ForeColor = PrimaryColor;
            miniNameLabel.Text = "准备点名";
            miniStatusLabel.Text = "点名啦 · " + className;
            UpdateProgress();
            SaveClassData();
            SaveSettings();
        }

        private void DrawButton_Click(object sender, EventArgs e)
        {
            if (drawing)
            {
                BeginSlowdown();
            }
            else
            {
                StartDraw();
            }
        }

        private void StartDraw()
        {
            if (String.IsNullOrEmpty(currentClass))
            {
                MessageBox.Show(this, "请先选择一个班级。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            List<StudentInfo> available = classes[currentClass].Where(s => s.Count == 0).ToList();
            if (available.Count == 0)
            {
                MessageBox.Show(this, "当前班级的学生已经全部点名完毕。\n如需重新开始，请点击“重置”。",
                    "本轮已完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            drawing = true;
            slowingDown = false;
            selectedName = null;
            highlightedName = null;
            finalTarget = available[random.Next(available.Count)].Name;
            animationOrder = Shuffle(available);
            animationIndex = 0;
            drawWatch.Restart();
            animationTimer.Interval = 130;
            animationTimer.Start();

            drawButton.Text = "停止";
            miniDrawButton.Text = "停止";
            resetButton.Enabled = false;
            if (classSelector != null) classSelector.Enabled = false;
            foreach (RoundedButton button in classButtons.Values) button.Enabled = false;
            resultHintLabel.Text = "正在随机点名……";
            currentNameLabel.ForeColor = Color.FromArgb(168, 100, 8);
            miniStatusLabel.Text = currentClass + " · 正在随机点名……";

            StartMusic(false);
            ShowNextCandidate();
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            long elapsed = drawWatch.ElapsedMilliseconds;
            if (!slowingDown && elapsed >= 3500)
            {
                BeginSlowdown();
            }

            if (slowingDown)
            {
                long slowElapsed = elapsed - slowdownStartedAt;
                if (slowElapsed >= 1500)
                {
                    FinishDraw();
                    return;
                }
                double t = Math.Max(0.0, Math.Min(1.0, slowElapsed / 1500.0));
                animationTimer.Interval = 80 + (int)(270.0 * t * t);
            }
            else
            {
                double t = Math.Max(0.0, Math.Min(1.0, elapsed / 800.0));
                animationTimer.Interval = 130 - (int)(55.0 * t);
            }

            ShowNextCandidate();
        }

        private void ShowNextCandidate()
        {
            if (animationOrder.Count == 0) return;
            if (animationIndex >= animationOrder.Count)
            {
                animationOrder = Shuffle(animationOrder);
                animationIndex = 0;
            }

            string next = animationOrder[animationIndex].Name;
            animationIndex++;
            if (animationOrder.Count > 1 && next == highlightedName)
            {
                if (animationIndex >= animationOrder.Count)
                {
                    animationOrder = Shuffle(animationOrder);
                    animationIndex = 0;
                }
                next = animationOrder[animationIndex].Name;
                animationIndex++;
            }

            highlightedName = next;
            currentNameLabel.Text = next;
            miniNameLabel.Text = next;
            RefreshCards();
        }

        private void BeginSlowdown()
        {
            if (!drawing || slowingDown) return;
            slowingDown = true;
            slowdownStartedAt = drawWatch.ElapsedMilliseconds;
            drawButton.Text = "即将揭晓…";
            miniDrawButton.Text = "即将揭晓…";
            drawButton.Enabled = false;
            miniDrawButton.Enabled = false;
            resultHintLabel.Text = "即将揭晓，请注意——";
        }

        private void FinishDraw()
        {
            animationTimer.Stop();
            drawWatch.Stop();
            drawing = false;
            slowingDown = false;
            highlightedName = finalTarget;
            selectedName = finalTarget;

            StudentInfo selected = classes[currentClass].First(s => s.Name == finalTarget);
            selected.Count += 1;
            historyEntries.Add(new HistoryEntry
            {
                Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                ClassName = currentClass,
                StudentName = finalTarget,
                Undone = false
            });

            currentNameLabel.Text = finalTarget;
            currentNameLabel.ForeColor = Color.FromArgb(18, 128, 92);
            resultHintLabel.Text = "本次点到";
            miniNameLabel.Text = finalTarget;
            miniStatusLabel.Text = currentClass + " · 本次点到";

            drawButton.Text = "继续点名";
            miniDrawButton.Text = "继续点名";
            drawButton.Enabled = true;
            miniDrawButton.Enabled = true;
            resetButton.Enabled = true;
            if (classSelector != null) classSelector.Enabled = true;
            foreach (RoundedButton button in classButtons.Values) button.Enabled = true;
            RefreshCards();
            UpdateProgress();
            SaveClassData();
            SaveHistory();
            FadeOutMusic();
        }

        private void RefreshCards()
        {
            if (String.IsNullOrEmpty(currentClass)) return;
            foreach (StudentInfo student in classes[currentClass])
            {
                StudentCardState state = StudentCardState.Waiting;
                if (student.Count > 0) state = StudentCardState.Drawn;
                if (drawing && student.Name == highlightedName) state = StudentCardState.Rolling;
                if (!drawing && student.Name == selectedName) state = StudentCardState.Selected;
                studentCards[student.Name].SetDisplay(state, student.Count);
            }
        }

        private void UpdateProgress()
        {
            if (String.IsNullOrEmpty(currentClass)) return;
            int drawn = classes[currentClass].Count(s => s.Count > 0);
            int total = classes[currentClass].Count;
            progressLabel.Text = drawn + " / " + total;
        }

        private void ResizeStudentCards()
        {
            if (studentCards.Count == 0 || studentFlow.ClientSize.Width < 100 || studentFlow.ClientSize.Height < 80) return;

            int count = studentCards.Count;
            int usableWidth = Math.Max(100, studentFlow.ClientSize.Width - 4);
            int usableHeight = Math.Max(80, studentFlow.ClientSize.Height - 4);
            int bestColumns = Math.Min(10, count);
            double bestScale = 0.0;

            for (int columns = 8; columns <= Math.Min(14, count); columns++)
            {
                int rows = (int)Math.Ceiling(count / (double)columns);
                double cardWidth = usableWidth / (double)columns - 6.0;
                double cardHeight = usableHeight / (double)rows - 6.0;
                if (cardWidth < 68.0 || cardHeight < 31.0) continue;
                double scale = Math.Min(cardWidth / 92.0, cardHeight / 44.0);
                if (scale > bestScale)
                {
                    bestScale = scale;
                    bestColumns = columns;
                }
            }

            int bestRows = (int)Math.Ceiling(count / (double)bestColumns);
            int width = Math.Max(68, Math.Min(112, usableWidth / bestColumns - 6));
            int height = Math.Max(36, Math.Min(54, usableHeight / bestRows - 6));
            foreach (StudentCard card in studentCards.Values)
            {
                card.Size = new Size(width, height);
                card.Margin = new Padding(3);
            }
        }

        private void ResetButton_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(currentClass) || drawing) return;
            if (!ConfirmDialog.Confirm(this,
                "确定要清空“" + currentClass + "”本轮的点名记录吗？",
                "重置", "确认清空")) return;

            foreach (StudentInfo student in classes[currentClass]) student.Count = 0;
            selectedName = null;
            highlightedName = null;
            currentNameLabel.Text = "准备点名";
            currentNameLabel.ForeColor = PrimaryColor;
            resultHintLabel.Text = "准备开始点名";
            miniNameLabel.Text = "准备点名";
            miniStatusLabel.Text = "点名啦 · " + currentClass;
            RefreshCards();
            UpdateProgress();
            SaveClassData();
        }

        private List<StudentInfo> Shuffle(List<StudentInfo> source)
        {
            List<StudentInfo> result = new List<StudentInfo>(source);
            for (int i = result.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                StudentInfo temp = result[i];
                result[i] = result[j];
                result[j] = temp;
            }
            return result;
        }

        private void TopMostCheck_CheckedChanged(object sender, EventArgs e)
        {
            if (!miniPanel.Visible) TopMost = topMostCheck.Checked;
            if (topMostButton != null)
            {
                topMostButton.Text = topMostCheck.Checked ? "\uE840" : "\uE718";
                topMostButton.BackColor = SurfaceColor;
                topMostButton.ForeColor = topMostCheck.Checked
                    ? PrimaryColor
                    : MutedColor;
                toolTip.SetToolTip(topMostButton, topMostCheck.Checked ? "取消窗口置顶" : "窗口置顶");
            }
            settings.TopMostEnabled = topMostCheck.Checked;
            SaveSettings();
        }

        private void MusicCheck_CheckedChanged(object sender, EventArgs e)
        {
            settings.MusicEnabled = musicCheck.Checked;
            if (!musicCheck.Checked) FadeOutMusic();
            SaveSettings();
        }

        private void VolumeTrack_ValueChanged(object sender, EventArgs e)
        {
            settings.Volume = volumeTrack.Value;
            if (!audioFadingOut && (previewing || drawing))
            {
                audioVolume = volumeTrack.Value / 100.0;
                mediaPlayer.Volume = audioVolume;
            }
            SaveSettings();
        }

        private void ImportButton_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Title = "添加背景音乐";
                dialog.Filter = "支持的音乐|*.mp3;*.wav;*.wma|MP3 音乐|*.mp3|WAV 音乐|*.wav|WMA 音乐|*.wma";
                dialog.Multiselect = true;
                if (dialog.ShowDialog(this) != DialogResult.OK) return;

                string lastImported = "";
                foreach (string source in dialog.FileNames)
                {
                    try
                    {
                        string fileName = Path.GetFileName(source);
                        string target = Path.Combine(musicDirectory, fileName);
                        if (String.Equals(Path.GetFullPath(source), Path.GetFullPath(target), StringComparison.OrdinalIgnoreCase))
                        {
                            lastImported = fileName;
                            continue;
                        }

                        string stem = Path.GetFileNameWithoutExtension(fileName);
                        string ext = Path.GetExtension(fileName);
                        int suffix = 2;
                        while (File.Exists(target))
                        {
                            target = Path.Combine(musicDirectory, stem + " (" + suffix + ")" + ext);
                            suffix++;
                        }
                        File.Copy(source, target, false);
                        lastImported = Path.GetFileName(target);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, "无法添加音乐：\n" + source + "\n\n" + ex.Message,
                            "添加失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }

                PopulateMusicList();
                if (!String.IsNullOrEmpty(lastImported))
                {
                    settings.SelectedMusic = lastImported;
                    SaveSettings();
                }
            }
        }

        private void PreviewButton_Click(object sender, EventArgs e)
        {
            if (previewing)
            {
                StopMusicImmediately();
                return;
            }
            StartMusic(true);
        }

        private void RebuildMusicMenu()
        {
            musicMenu.Items.Clear();

            ToolStripMenuItem enabled = new ToolStripMenuItem("开启音乐");
            enabled.Checked = settings.MusicEnabled;
            enabled.CheckOnClick = true;
            enabled.Click += delegate
            {
                settings.MusicEnabled = enabled.Checked;
                if (!settings.MusicEnabled) FadeOutMusic();
                SaveSettings();
            };
            musicMenu.Items.Add(enabled);

            ToolStripMenuItem tracks = new ToolStripMenuItem("选择曲目");
            if (musicFiles.Count == 0)
            {
                ToolStripMenuItem empty = new ToolStripMenuItem("尚未添加音乐");
                empty.Enabled = false;
                tracks.DropDownItems.Add(empty);
            }
            else
            {
                foreach (string file in musicFiles)
                {
                    string captured = file;
                    ToolStripMenuItem item = new ToolStripMenuItem(Path.GetFileNameWithoutExtension(file));
                    item.Checked = String.Equals(settings.SelectedMusic, file, StringComparison.OrdinalIgnoreCase);
                    item.Click += delegate
                    {
                        if (previewing) StopMusicImmediately();
                        settings.SelectedMusic = captured;
                        SaveSettings();
                    };
                    tracks.DropDownItems.Add(item);
                }
            }
            musicMenu.Items.Add(tracks);

            ToolStripMenuItem volume = new ToolStripMenuItem("音量");
            foreach (int value in new int[] { 0, 25, 50, 75, 100 })
            {
                int captured = value;
                ToolStripMenuItem item = new ToolStripMenuItem(value + "%");
                item.Checked = settings.Volume == value;
                item.Click += delegate
                {
                    settings.Volume = captured;
                    if (!audioFadingOut && (previewing || drawing))
                    {
                        audioVolume = captured / 100.0;
                        mediaPlayer.Volume = audioVolume;
                    }
                    SaveSettings();
                };
                volume.DropDownItems.Add(item);
            }
            musicMenu.Items.Add(volume);
            musicMenu.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem preview = new ToolStripMenuItem(previewing ? "停止试听" : "试听当前曲目");
            preview.Click += PreviewButton_Click;
            musicMenu.Items.Add(preview);
            ToolStripMenuItem import = new ToolStripMenuItem("添加音乐…");
            import.Click += ImportButton_Click;
            musicMenu.Items.Add(import);
        }

        private void StartMusic(bool isPreview)
        {
            if (!isPreview && !settings.MusicEnabled) return;
            string path = SelectedMusicPath();
            if (String.IsNullOrEmpty(path) || !File.Exists(path))
            {
                if (isPreview)
                    MessageBox.Show(this, "请先在“音乐”菜单中添加并选择一首音乐。", "没有可用音乐",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                mediaPlayer.Stop();
                mediaPlayer.Close();
                mediaPlayer.Open(new Uri(path, UriKind.Absolute));
                audioVolume = 0.0;
                audioFadingOut = false;
                mediaPlayer.Volume = 0.0;
                mediaPlayer.Play();
                previewing = isPreview;
                audioTimer.Start();
            }
            catch (Exception ex)
            {
                if (isPreview)
                {
                    MessageBox.Show(this, "无法播放这首音乐。\n\n" + ex.Message,
                        "播放失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void FadeOutMusic()
        {
            if (audioVolume <= 0.001 && !previewing)
            {
                try { mediaPlayer.Stop(); } catch { }
                return;
            }
            audioFadingOut = true;
            audioTimer.Start();
        }

        private void StopMusicImmediately()
        {
            audioTimer.Stop();
            try
            {
                mediaPlayer.Stop();
                mediaPlayer.Close();
            }
            catch { }
            audioVolume = 0.0;
            audioFadingOut = false;
            previewing = false;
            if (previewButton != null) previewButton.Text = "试听";
        }

        private void AudioTimer_Tick(object sender, EventArgs e)
        {
            double target = Math.Max(0, Math.Min(100, settings.Volume)) / 100.0;
            if (audioFadingOut)
            {
                audioVolume = Math.Max(0.0, audioVolume - Math.Max(0.02, target / 10.0));
                mediaPlayer.Volume = audioVolume;
                if (audioVolume <= 0.001) StopMusicImmediately();
            }
            else
            {
                audioVolume = Math.Min(target, audioVolume + Math.Max(0.02, target / 10.0));
                mediaPlayer.Volume = audioVolume;
                if (audioVolume >= target - 0.001) audioTimer.Stop();
            }
        }

        private void MediaPlayer_MediaEnded(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(delegate { StopMusicImmediately(); }));
            }
            else
            {
                StopMusicImmediately();
            }
        }

        private string SelectedMusicPath()
        {
            if (String.IsNullOrEmpty(settings.SelectedMusic)) return "";
            return Path.Combine(musicDirectory, settings.SelectedMusic);
        }

        private void EnsureMusicFolderAndMigrateLegacyMusic()
        {
            try
            {
                Directory.CreateDirectory(musicDirectory);
                string oldDefault = Path.Combine(musicDirectory, "默认背景音乐.mp3");
                string migrated = Path.Combine(musicDirectory, "好运来.mp3");
                if (File.Exists(oldDefault) && !File.Exists(migrated))
                    File.Move(oldDefault, migrated);
                if (String.Equals(settings.SelectedMusic, "默认背景音乐.mp3", StringComparison.OrdinalIgnoreCase))
                    settings.SelectedMusic = "好运来.mp3";

                ExtractBundledMusic();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "音乐目录初始化失败，点名功能仍可正常使用。\n\n" + ex.Message,
                    "音乐提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ExtractBundledMusic()
        {
            Dictionary<string, string> bundled = new Dictionary<string, string>();
            bundled["欢乐节奏-Take the Ride.mp3"] = "DianMingLa.Music.TakeTheRide.mp3";
            bundled["紧张冲刺-Rush.mp3"] = "DianMingLa.Music.Rush.mp3";
            bundled["轻快活泼-Spring Chicken.mp3"] = "DianMingLa.Music.SpringChicken.mp3";

            Assembly assembly = Assembly.GetExecutingAssembly();
            foreach (KeyValuePair<string, string> item in bundled)
            {
                string target = Path.Combine(musicDirectory, item.Key);
                if (File.Exists(target)) continue;

                using (Stream input = assembly.GetManifestResourceStream(item.Value))
                {
                    if (input == null) continue;
                    string temporary = target + ".tmp";
                    using (FileStream output = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
                        input.CopyTo(output);
                    if (File.Exists(target)) File.Delete(temporary);
                    else File.Move(temporary, target);
                }
            }
        }

        private static bool IsSupportedMusic(string path)
        {
            string ext = Path.GetExtension(path).ToLowerInvariant();
            return ext == ".mp3" || ext == ".wav" || ext == ".wma";
        }

        private void PopulateMusicList()
        {
            string preferred = settings.SelectedMusic ?? "";
            musicFiles.Clear();
            try
            {
                string[] files = Directory.GetFiles(musicDirectory, "*.*")
                    .Where(IsSupportedMusic)
                    .OrderBy(Path.GetFileName)
                    .ToArray();
                foreach (string file in files) musicFiles.Add(Path.GetFileName(file));
            }
            catch { }

            if (musicFiles.Count > 0)
            {
                string selected = musicFiles.FirstOrDefault(f => String.Equals(f, preferred, StringComparison.OrdinalIgnoreCase));
                settings.SelectedMusic = selected ?? musicFiles[0];
            }
            else
            {
                settings.SelectedMusic = "";
            }
            SaveSettings();
        }

        private AppSettings LoadSettings()
        {
            try
            {
                if (!File.Exists(settingsPath)) return new AppSettings();
                string json = File.ReadAllText(settingsPath, Encoding.UTF8);
                AppSettings loaded = new JavaScriptSerializer().Deserialize<AppSettings>(json);
                return loaded ?? new AppSettings();
            }
            catch
            {
                return new AppSettings();
            }
        }

        private void SaveSettings()
        {
            if (initializing) return;
            try
            {
                if (topMostCheck != null) settings.TopMostEnabled = topMostCheck.Checked;
                settings.LastClass = currentClass ?? settings.LastClass;
                string json = new JavaScriptSerializer().Serialize(settings);
                File.WriteAllText(settingsPath, json, new UTF8Encoding(false));
            }
            catch { }
        }

        private void EnterMiniMode()
        {
            if (miniPanel.Visible) return;
            normalBounds = Bounds;
            mainLayout.Visible = false;
            miniPanel.Visible = true;
            miniPanel.BringToFront();
            FormBorderStyle = FormBorderStyle.None;
            MinimumSize = new Size(280, 150);
            MaximumSize = new Size(280, 150);
            Size = new Size(280, 150);
            TopMost = true;

            Screen screen = Screen.FromControl(this);
            int x = Math.Min(Math.Max(screen.WorkingArea.Left, Left), screen.WorkingArea.Right - Width);
            int y = Math.Min(Math.Max(screen.WorkingArea.Top, Top), screen.WorkingArea.Bottom - Height);
            Location = new Point(x, y);
        }

        private void ExitMiniMode()
        {
            if (!miniPanel.Visible) return;
            miniPanel.Visible = false;
            mainLayout.Visible = true;
            FormBorderStyle = FormBorderStyle.None;
            MaximumSize = Size.Empty;
            MinimumSize = Size.Empty;
            Bounds = new Rectangle(normalBounds.Location, new Size(920, 500));
            MinimumSize = new Size(920, 500);
            MaximumSize = new Size(920, 500);
            TopMost = topMostCheck.Checked;
        }

        private void MiniDrag_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            miniDragging = true;
            miniDragOrigin = Cursor.Position;
            formDragOrigin = Location;
        }

        private void MiniDrag_MouseMove(object sender, MouseEventArgs e)
        {
            if (!miniDragging) return;
            Point now = Cursor.Position;
            Location = new Point(formDragOrigin.X + now.X - miniDragOrigin.X, formDragOrigin.Y + now.Y - miniDragOrigin.Y);
        }

        private void MiniDrag_MouseUp(object sender, MouseEventArgs e)
        {
            miniDragging = false;
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.Z && !drawing)
            {
                UndoLastDraw();
                e.SuppressKeyPress = true;
                return;
            }
            if (e.Control && e.KeyCode == Keys.T)
            {
                topMostCheck.Checked = !topMostCheck.Checked;
                e.SuppressKeyPress = true;
                return;
            }
            if (e.Control && e.KeyCode == Keys.M)
            {
                if (miniPanel.Visible) ExitMiniMode(); else EnterMiniMode();
                e.SuppressKeyPress = true;
                return;
            }
            if (e.KeyCode == Keys.Space && !(ActiveControl is ComboBox) && !(ActiveControl is TrackBar))
            {
                if (drawing) BeginSlowdown(); else StartDraw();
                e.SuppressKeyPress = true;
            }
        }

        private void ClockTimer_Tick(object sender, EventArgs e)
        {
            UpdateClock();
        }

        private void UpdateClock()
        {
            DateTime now = DateTime.Now;
            CultureInfo chinese = CultureInfo.GetCultureInfo("zh-CN");
            clockLabel.Text = now.ToString("H:mm:ss") + Environment.NewLine
                + now.ToString("yyyy/M/d/ddd", chinese);
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!exitRequested && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                HideToTray();
                return;
            }

            SaveSettings();
            animationTimer.Stop();
            clockTimer.Stop();
            StopMusicImmediately();
            if (trayIcon != null)
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
            }
            if (trayMenu != null) trayMenu.Dispose();
        }

        private static Dictionary<string, List<StudentInfo>> BuildClassData()
        {
            Dictionary<string, List<StudentInfo>> result = new Dictionary<string, List<StudentInfo>>();
            result["示例一班"] = MakeDemoStudents("甲", 36);
            result["示例二班"] = MakeDemoStudents("乙", 49);
            result["示例三班"] = MakeDemoStudents("丙", 24);
            return result;
        }

        private static List<StudentInfo> MakeDemoStudents(string prefix, int count)
        {
            return Enumerable.Range(1, count)
                .Select(index => new StudentInfo(prefix + "同学" + index.ToString("00")))
                .ToList();
        }

        private static List<StudentInfo> MakeStudents(string[] names)
        {
            return names.Select(name => new StudentInfo(name)).ToList();
        }
    }
}
