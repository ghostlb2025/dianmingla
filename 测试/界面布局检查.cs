using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

internal static class LayoutCheck
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("用法：界面布局检查.exe <点名啦.exe> <预览图.png>");
            return 2;
        }

        string testDataDirectory = Path.Combine(Path.GetTempPath(), "DianMingLa-layout-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testDataDirectory);
        Environment.SetEnvironmentVariable("DIANMINGLA_DATA_DIR", testDataDirectory);
        string settingsPath = Path.Combine(testDataDirectory, "settings.json");
        string rosterPath = Path.Combine(testDataDirectory, "班级名单.json");
        string historyPath = Path.Combine(testDataDirectory, "点名历史.json");
        bool settingsExisted = File.Exists(settingsPath);
        byte[] originalSettings = settingsExisted ? File.ReadAllBytes(settingsPath) : null;
        bool rosterExisted = File.Exists(rosterPath);
        byte[] originalRoster = rosterExisted ? File.ReadAllBytes(rosterPath) : null;
        string rosterBackupPath = rosterPath + ".bak";
        bool rosterBackupExisted = File.Exists(rosterBackupPath);
        byte[] originalRosterBackup = rosterBackupExisted ? File.ReadAllBytes(rosterBackupPath) : null;
        bool historyExisted = File.Exists(historyPath);
        byte[] originalHistory = historyExisted ? File.ReadAllBytes(historyPath) : null;
        string historyBackupPath = historyPath + ".bak";
        bool historyBackupExisted = File.Exists(historyBackupPath);
        byte[] originalHistoryBackup = historyBackupExisted ? File.ReadAllBytes(historyBackupPath) : null;

        try
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Assembly assembly = Assembly.LoadFrom(args[0]);
            Type formType = assembly.GetType("DianMingLa.MainForm", true);
            using (Form form = (Form)Activator.CreateInstance(formType, true))
            {
                form.TopMost = false;
            form.StartPosition = FormStartPosition.Manual;
            form.Location = new Point(-32000, -32000);
            form.ShowInTaskbar = false;
            form.Size = new Size(920, 500);
            form.Show();
            Application.DoEvents();
            form.PerformLayout();

            MethodInfo selectClass = formType.GetMethod("SelectClass", BindingFlags.Instance | BindingFlags.NonPublic);
            selectClass.Invoke(form, new object[] { "示例二班" });
            bool allPassed = true;
            FieldInfo classesField = formType.GetField("classes", BindingFlags.Instance | BindingFlags.NonPublic);
            IDictionary demoClasses = classesField.GetValue(form) as IDictionary;
            bool demoDataPassed = demoClasses != null && demoClasses.Count == 3
                && ((ICollection)demoClasses["示例一班"]).Count == 36
                && ((ICollection)demoClasses["示例二班"]).Count == 49
                && ((ICollection)demoClasses["示例三班"]).Count == 24;
            Console.WriteLine("公开示例班：3 个，人数为 36/49/24={0}", demoDataPassed);
            allPassed = allPassed && demoDataPassed;
            FieldInfo currentNameField = formType.GetField("currentNameLabel", BindingFlags.Instance | BindingFlags.NonPublic);
            Label centeredName = currentNameField.GetValue(form) as Label;
            bool centeredPassed = centeredName != null
                && Math.Abs((centeredName.Left + centeredName.Width / 2) - centeredName.Parent.ClientSize.Width / 2) <= 1;
            Console.WriteLine("顶部姓名相对整栏居中={0}", centeredPassed);
            allPassed = allPassed && centeredPassed;
            string[] windowActions = FindControls<Button>(form).Select(button => button.Text).ToArray();
            bool customTitlePassed = form.FormBorderStyle == FormBorderStyle.None
                && windowActions.Contains("\uE921") && !windowActions.Contains("\uE922") && windowActions.Contains("\uE8BB")
                && form.Icon != null && form.MinimumSize == new Size(920, 500) && form.MaximumSize == new Size(920, 500);
            Console.WriteLine("单行自定义顶栏与应用图标={0}", customTitlePassed);
            allPassed = allPassed && customTitlePassed;
            Button[] featureButtons = FindControls<Button>(form)
                .Where(button => new string[] { "名单", "记录", "音乐", "迷你模式" }.Contains(button.Text)).ToArray();
            FieldInfo topMostButtonField = formType.GetField("topMostButton", BindingFlags.Instance | BindingFlags.NonPublic);
            Button topMostButton = topMostButtonField.GetValue(form) as Button;
            bool equalButtonsPassed = featureButtons.Length == 4
                && featureButtons.Select(button => button.Width).Distinct().Count() == 1
                && featureButtons.Select(button => button.Height).Distinct().Count() == 1
                && FindControls<CheckBox>(form).Length == 0
                && topMostButton != null && new string[] { "\uE718", "\uE840" }.Contains(topMostButton.Text)
                && topMostButton.Width == 40 && topMostButton.BackColor == Color.White
                && topMostButton.Font.Size >= 11.5F;
            Console.WriteLine("四个功能按钮等宽；置顶使用放大的系统窗口图标={0}", equalButtonsPassed);
            allPassed = allPassed && equalButtonsPassed;
            Button[] allButtons = FindControls<Button>(form);
            PropertyInfo showFocusCuesProperty = typeof(Control).GetProperty(
                "ShowFocusCues", BindingFlags.Instance | BindingFlags.NonPublic);
            bool focusCuePassed = showFocusCuesProperty != null && allButtons.Length > 0
                && allButtons.All(button => !(bool)showFocusCuesProperty.GetValue(button, null));
            Console.WriteLine("主界面全部按钮不绘制焦点框={0}", focusCuePassed);
            allPassed = allPassed && focusCuePassed;

            FieldInfo resetButtonField = formType.GetField("resetButton", BindingFlags.Instance | BindingFlags.NonPublic);
            Button focusedResetButton = resetButtonField.GetValue(form) as Button;
            Button[] focusBorderButtons = featureButtons
                .Concat(new Button[] { focusedResetButton })
                .Where(button => button != null).ToArray();
            bool noRedFocusBorderPassed = focusBorderButtons.Length == 5;
            foreach (Button focusButton in focusBorderButtons)
            {
                bool acceptedFocus = focusButton.Focus();
                Application.DoEvents();
                using (Bitmap focusedPreview = new Bitmap(focusButton.Width, focusButton.Height))
                {
                    focusButton.DrawToBitmap(focusedPreview,
                        new Rectangle(Point.Empty, focusButton.Size));
                    bool redEdge = HasRedEdgePixels(focusedPreview);
                    bool blueEdge = HasBlueEdgePixels(focusedPreview);
                    Console.WriteLine("焦点检查“{0}”：接受焦点={1}，红边={2}，蓝边={3}",
                        focusButton.Text, acceptedFocus || focusButton.Focused, redEdge, blueEdge);
                    noRedFocusBorderPassed = noRedFocusBorderPassed
                        && !focusButton.Focused
                        && !redEdge && !blueEdge;
                }
            }
            Console.WriteLine("名单、记录、音乐、迷你模式、重置点击后不保留焦点且边缘无红蓝框={0}", noRedFocusBorderPassed);
            allPassed = allPassed && noRedFocusBorderPassed;

            FieldInfo drawButtonField = formType.GetField("drawButton", BindingFlags.Instance | BindingFlags.NonPublic);
            Button mainDrawButton = drawButtonField.GetValue(form) as Button;
            bool drawButtonPassed = mainDrawButton != null && mainDrawButton.Parent != null
                && mainDrawButton.Bottom <= mainDrawButton.Parent.ClientSize.Height - 2
                && mainDrawButton.Padding.Bottom >= 2
                && mainDrawButton.TextAlign == ContentAlignment.MiddleCenter;
            Console.WriteLine("开始点名按钮文字完整且四周留有空间={0}", drawButtonPassed);
            allPassed = allPassed && drawButtonPassed;

            FieldInfo clockPositionField = formType.GetField("clockLabel", BindingFlags.Instance | BindingFlags.NonPublic);
            Label clockPositionLabel = clockPositionField.GetValue(form) as Label;
            Rectangle clockBounds = clockPositionLabel == null ? Rectangle.Empty : BoundsRelativeTo(clockPositionLabel, form);
            bool clockPositionPassed = clockPositionLabel != null && clockPositionLabel.Font.Bold
                && clockPositionLabel.TextAlign == ContentAlignment.MiddleRight
                && form.ClientSize.Width - clockBounds.Right <= 6;
            Console.WriteLine("日期时间靠右且使用稍粗字体={0}", clockPositionPassed);
            allPassed = allPassed && clockPositionPassed;

            bool palettePassed = form.BackColor == Color.FromArgb(243, 246, 250)
                && centeredName.Parent.BackColor == Color.FromArgb(248, 247, 255)
                && mainDrawButton.BackColor == Color.FromArgb(91, 75, 206)
                && focusedResetButton.ForeColor == Color.FromArgb(102, 112, 133)
                && focusedResetButton.BackColor == Color.FromArgb(247, 249, 252);
            Console.WriteLine("新版雾白、浅紫、蓝紫主色和中性重置配色已生效={0}", palettePassed);
            allPassed = allPassed && palettePassed;

            Type confirmDialogType = assembly.GetType("DianMingLa.ConfirmDialog", true);
            using (Form confirmDialog = (Form)Activator.CreateInstance(confirmDialogType,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null,
                new object[] { "重置", "确认清空本轮记录吗？", "确认清空" }, null))
            {
                Button cancelConfirm = FindControls<Button>(confirmDialog).FirstOrDefault(button => button.Text == "取消");
                Button dangerConfirm = FindControls<Button>(confirmDialog).FirstOrDefault(button => button.Text == "确认清空");
                bool confirmDialogPassed = confirmDialog.FormBorderStyle == FormBorderStyle.None
                    && cancelConfirm != null && dangerConfirm != null
                    && confirmDialog.AcceptButton == cancelConfirm && confirmDialog.CancelButton == cancelConfirm
                    && dangerConfirm.BackColor == Color.FromArgb(196, 61, 61)
                    && dangerConfirm.ForeColor == Color.White;
                Console.WriteLine("危险操作使用统一确认窗且默认选择取消={0}", confirmDialogPassed);
                allPassed = allPassed && confirmDialogPassed;
            }
            Size[] testSizes = new Size[] { new Size(920, 500) };

            foreach (Size testSize in testSizes)
            {
                form.Size = testSize;
                ForceLayout(form);
                Application.DoEvents();

                FlowLayoutPanel roster = FindControl<FlowLayoutPanel>(form);
                if (roster == null)
                {
                    Console.Error.WriteLine("失败：未找到学生名单区域。");
                    return 1;
                }

                Control[] cards = roster.Controls.Cast<Control>().ToArray();
                int maxRight = cards.Length == 0 ? 0 : cards.Max(card => card.Right + card.Margin.Right);
                int maxBottom = cards.Length == 0 ? 0 : cards.Max(card => card.Bottom + card.Margin.Bottom);
                int rowCount = cards.Select(card => card.Top).Distinct().Count();
                bool allLaidOut = cards.All(card => card.Width > 0 && card.Height > 0 && card.Left >= 0 && card.Top >= 0);
                bool fitsWidth = maxRight <= roster.ClientSize.Width;
                bool fitsHeight = maxBottom <= roster.ClientSize.Height;
                bool noScrollbars = !roster.AutoScroll && !roster.HorizontalScroll.Visible && !roster.VerticalScroll.Visible;
                bool largeEnough = cards.All(card => card.Font.Size >= 12F);
                bool passed = cards.Length == 49 && allLaidOut && noScrollbars && fitsWidth && fitsHeight && largeEnough;
                allPassed = allPassed && passed;

                Console.WriteLine("窗口 {0} × {1}：名单区 {2} × {3}，49 人排成 {4} 行，占用 {5} × {6}，无需滚动={7}",
                    testSize.Width, testSize.Height, roster.ClientSize.Width, roster.ClientSize.Height,
                    rowCount, maxRight, maxBottom, passed);

                if (testSize == testSizes[0])
                {
                    foreach (Button button in FindControls<Button>(form))
                    {
                        Rectangle bounds = BoundsRelativeTo(button, form);
                        Console.WriteLine("按钮“{0}”：x={1}, y={2}, w={3}, h={4}",
                            button.Text, bounds.X, bounds.Y, bounds.Width, bounds.Height);
                    }
                    using (Bitmap preview = new Bitmap(form.Width, form.Height))
                    {
                        form.DrawToBitmap(preview, new Rectangle(Point.Empty, form.Size));
                        preview.Save(args[1], ImageFormat.Png);
                    }
                }
            }

            FieldInfo musicField = formType.GetField("musicFiles", BindingFlags.Instance | BindingFlags.NonPublic);
            ICollection musicList = musicField == null ? null : musicField.GetValue(form) as ICollection;
            string[] musicNames = musicList == null ? new string[0]
                : musicList.Cast<object>().Select(item => item.ToString()).ToArray();
            bool musicPassed = musicNames.Length == 3
                && musicNames.Contains("欢乐节奏-Take the Ride.mp3")
                && musicNames.Contains("紧张冲刺-Rush.mp3")
                && musicNames.Contains("轻快活泼-Spring Chicken.mp3");
            Console.WriteLine("公开版默认音乐：{0} 项，均为 CC0={1}", musicNames.Length, musicPassed);
            allPassed = allPassed && musicPassed;

            string[] embeddedMusic = assembly.GetManifestResourceNames()
                .Where(name => name.StartsWith("DianMingLa.Music.", StringComparison.Ordinal)).ToArray();
            bool embeddedMusicPassed = embeddedMusic.Length == 3
                && embeddedMusic.All(name =>
                {
                    using (Stream stream = assembly.GetManifestResourceStream(name))
                        return stream != null && stream.Length > 0;
                });
            Console.WriteLine("EXE 内嵌 CC0 音乐：{0} 首，资源完整={1}", embeddedMusic.Length, embeddedMusicPassed);
            allPassed = allPassed && embeddedMusicPassed;

            FieldInfo clockField = formType.GetField("clockLabel", BindingFlags.Instance | BindingFlags.NonPublic);
            Label clock = clockField.GetValue(form) as Label;
            bool clockPassed = clock != null && clock.Text.Split(new string[] { Environment.NewLine }, StringSplitOptions.None).Length == 2;
            Console.WriteLine("时间与日期分两行={0}", clockPassed);
            allPassed = allPassed && clockPassed;

            MethodInfo rebuildMusic = formType.GetMethod("RebuildMusicMenu", BindingFlags.Instance | BindingFlags.NonPublic);
            rebuildMusic.Invoke(form, null);
            FieldInfo musicMenuField = formType.GetField("musicMenu", BindingFlags.Instance | BindingFlags.NonPublic);
            ContextMenuStrip musicMenu = musicMenuField.GetValue(form) as ContextMenuStrip;
            bool musicMenuPassed = musicMenu != null && musicMenu.Items.Count >= 6;
            ComboBox classSelector = FindControl<ComboBox>(form);
            bool classSelectorPassed = classSelector != null && classSelector.Items.Count == 3
                && !classSelector.TabStop && !classSelector.Focused;
            Console.WriteLine("菜单式音乐={0}；班级下拉选择且启动无蓝色焦点={1}", musicMenuPassed, classSelectorPassed);
            allPassed = allPassed && musicMenuPassed && classSelectorPassed;

            FieldInfo settingsField = formType.GetField("settings", BindingFlags.Instance | BindingFlags.NonPublic);
            object settings = settingsField.GetValue(form);
            settings.GetType().GetProperty("MusicEnabled").SetValue(settings, false, null);
            MethodInfo startDraw = formType.GetMethod("StartDraw", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo finishDraw = formType.GetMethod("FinishDraw", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo finalTarget = formType.GetField("finalTarget", BindingFlags.Instance | BindingFlags.NonPublic);
            startDraw.Invoke(form, null);
            string firstTarget = (string)finalTarget.GetValue(form);
            finishDraw.Invoke(form, null);
            startDraw.Invoke(form, null);
            string secondTarget = (string)finalTarget.GetValue(form);
            finishDraw.Invoke(form, null);
            Label progress = FindControls<Label>(form).FirstOrDefault(item => item.Text == "2 / 49");
            bool drawPassed = !String.IsNullOrEmpty(firstTarget) && !String.IsNullOrEmpty(secondTarget)
                && firstTarget != secondTarget && progress != null;
            Console.WriteLine("连续点名：{0}、{1}；进度={2}；无重复={3}",
                firstTarget, secondTarget, progress == null ? "未找到" : progress.Text, drawPassed);
            allPassed = allPassed && drawPassed;

            bool dataSaved = File.Exists(rosterPath) && File.Exists(historyPath)
                && File.ReadAllText(historyPath).Contains(firstTarget)
                && File.ReadAllText(historyPath).Contains(secondTarget);
            MethodInfo undoLast = formType.GetMethod("UndoLastDraw", BindingFlags.Instance | BindingFlags.NonPublic);
            undoLast.Invoke(form, null);
            Label undoneProgress = FindControls<Label>(form).FirstOrDefault(item => item.Text == "1 / 49");
            bool undoPassed = undoneProgress != null && File.ReadAllText(historyPath).Contains("\"Undone\":true");
            Console.WriteLine("名单与历史自动保存={0}；撤销上次={1}", dataSaved, undoPassed);
            allPassed = allPassed && dataSaved && undoPassed;

            MethodInfo snapshotMethod = formType.GetMethod("SnapshotClasses", BindingFlags.Instance | BindingFlags.NonPublic);
            object classSnapshot = snapshotMethod.Invoke(form, null);
            Type rosterDialogType = assembly.GetType("DianMingLa.RosterManagerDialog", true);
            using (Form rosterDialog = (Form)Activator.CreateInstance(rosterDialogType, new object[] { classSnapshot }))
            {
                bool rosterDialogPassed = rosterDialog.Controls.Count > 0 && FindControl<ListBox>(rosterDialog) != null
                    && FindControl<TextBox>(rosterDialog) != null;
                Console.WriteLine("名单管理窗口可用={0}", rosterDialogPassed);
                allPassed = allPassed && rosterDialogPassed;
            }

            FieldInfo historyEntriesField = formType.GetField("historyEntries", BindingFlags.Instance | BindingFlags.NonPublic);
            object historySnapshot = historyEntriesField.GetValue(form);
            Type historyDialogType = assembly.GetType("DianMingLa.HistoryDialog", true);
            using (Form historyDialog = (Form)Activator.CreateInstance(historyDialogType, new object[] { historySnapshot }))
            {
                bool historyDialogPassed = FindControl<DataGridView>(historyDialog) != null;
                Console.WriteLine("历史查看与导出窗口可用={0}", historyDialogPassed);
                allPassed = allPassed && historyDialogPassed;
            }

            Button minimizeButton = FindControls<Button>(form).First(button => button.Text == "\uE921");
            minimizeButton.PerformClick();
            Application.DoEvents();
            bool minimizePassed = form.Visible && form.WindowState == FormWindowState.Minimized;
            form.WindowState = FormWindowState.Normal;

            Button closeButton = FindControls<Button>(form).First(button => button.Text == "\uE8BB");
            closeButton.PerformClick();
            Application.DoEvents();
            FieldInfo trayIconField = formType.GetField("trayIcon", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo trayMenuField = formType.GetField("trayMenu", BindingFlags.Instance | BindingFlags.NonPublic);
            NotifyIcon trayIcon = trayIconField.GetValue(form) as NotifyIcon;
            ContextMenuStrip trayMenu = trayMenuField.GetValue(form) as ContextMenuStrip;
            bool closeToTrayPassed = !form.Visible && trayIcon != null && trayIcon.Visible
                && trayMenu != null && trayMenu.Items.Cast<ToolStripItem>().Any(item => item.Text == "打开点名啦")
                && trayMenu.Items.Cast<ToolStripItem>().Any(item => item.Text == "退出");

            MethodInfo restoreFromTray = formType.GetMethod("RestoreFromTray", BindingFlags.Instance | BindingFlags.NonPublic);
            restoreFromTray.Invoke(form, null);
            Application.DoEvents();
            bool restorePassed = form.Visible && !trayIcon.Visible;
            Console.WriteLine("最小化进入任务栏={0}；叉号进入托盘={1}；托盘恢复={2}",
                minimizePassed, closeToTrayPassed, restorePassed);
            allPassed = allPassed && minimizePassed && closeToTrayPassed && restorePassed;

            closeButton.PerformClick();
            Application.DoEvents();
            ToolStripItem exitTrayItem = trayMenu.Items.Cast<ToolStripItem>().First(item => item.Text == "退出");
            exitTrayItem.PerformClick();
            Application.DoEvents();
            bool trayExitPassed = form.IsDisposed;
            Console.WriteLine("只有托盘“退出”真正结束程序={0}", trayExitPassed);
            allPassed = allPassed && trayExitPassed;

                return allPassed ? 0 : 1;
            }
        }
        finally
        {
            if (settingsExisted)
                File.WriteAllBytes(settingsPath, originalSettings);
            else if (File.Exists(settingsPath))
                File.Delete(settingsPath);
            if (rosterExisted)
                File.WriteAllBytes(rosterPath, originalRoster);
            else if (File.Exists(rosterPath))
                File.Delete(rosterPath);
            if (historyExisted)
                File.WriteAllBytes(historyPath, originalHistory);
            else if (File.Exists(historyPath))
                File.Delete(historyPath);
            if (rosterBackupExisted)
                File.WriteAllBytes(rosterBackupPath, originalRosterBackup);
            else if (File.Exists(rosterBackupPath))
                File.Delete(rosterBackupPath);
            if (historyBackupExisted)
                File.WriteAllBytes(historyBackupPath, originalHistoryBackup);
            else if (File.Exists(historyBackupPath))
                File.Delete(historyBackupPath);
            if (File.Exists(rosterPath + ".tmp")) File.Delete(rosterPath + ".tmp");
            if (File.Exists(historyPath + ".tmp")) File.Delete(historyPath + ".tmp");
            Environment.SetEnvironmentVariable("DIANMINGLA_DATA_DIR", null);
            if (Directory.Exists(testDataDirectory)) Directory.Delete(testDataDirectory, true);
        }
    }

    private static void ForceLayout(Control control)
    {
        control.PerformLayout();
        foreach (Control child in control.Controls) ForceLayout(child);
    }

    private static T FindControl<T>(Control root) where T : Control
    {
        if (root is T) return (T)root;
        foreach (Control child in root.Controls)
        {
            T found = FindControl<T>(child);
            if (found != null) return found;
        }
        return null;
    }

    private static T[] FindControls<T>(Control root) where T : Control
    {
        return root.Controls.Cast<Control>()
            .SelectMany(child => new T[] { child as T }.Where(item => item != null).Concat(FindControls<T>(child)))
            .ToArray();
    }

    private static Rectangle BoundsRelativeTo(Control control, Control ancestor)
    {
        Point point = control.Location;
        Control parent = control.Parent;
        while (parent != null && parent != ancestor)
        {
            point.Offset(parent.Location);
            parent = parent.Parent;
        }
        return new Rectangle(point, control.Size);
    }

    private static bool HasRedEdgePixels(Bitmap bitmap)
    {
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                bool edge = x <= 2 || y <= 2 || x >= bitmap.Width - 3 || y >= bitmap.Height - 3;
                if (!edge) continue;
                Color pixel = bitmap.GetPixel(x, y);
                if (pixel.R >= 150 && pixel.R > pixel.G * 1.4 && pixel.R > pixel.B * 1.4)
                    return true;
            }
        }
        return false;
    }

    private static bool HasBlueEdgePixels(Bitmap bitmap)
    {
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                bool edge = x <= 2 || y <= 2 || x >= bitmap.Width - 3 || y >= bitmap.Height - 3;
                if (!edge) continue;
                Color pixel = bitmap.GetPixel(x, y);
                if (pixel.B >= 140 && pixel.B > pixel.R * 1.2 && pixel.B > pixel.G * 1.05)
                    return true;
            }
        }
        return false;
    }
}
