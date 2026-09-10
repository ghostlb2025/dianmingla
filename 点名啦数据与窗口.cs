using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace DianMingLa
{
    internal sealed class RosterDocument
    {
        public int SchemaVersion { get; set; }
        public List<RosterClassData> Classes { get; set; }

        public RosterDocument()
        {
            SchemaVersion = 1;
            Classes = new List<RosterClassData>();
        }
    }

    internal sealed class RosterClassData
    {
        public string Name { get; set; }
        public List<RosterStudentData> Students { get; set; }

        public RosterClassData()
        {
            Name = "";
            Students = new List<RosterStudentData>();
        }
    }

    internal sealed class RosterStudentData
    {
        public string Name { get; set; }
        public int Count { get; set; }
    }

    internal sealed class HistoryEntry
    {
        public string Time { get; set; }
        public string ClassName { get; set; }
        public string StudentName { get; set; }
        public bool Undone { get; set; }
    }

    internal static class DataFiles
    {
        public static T Load<T>(string path) where T : class
        {
            if (!File.Exists(path)) return null;
            string json = File.ReadAllText(path, Encoding.UTF8);
            return new JavaScriptSerializer().Deserialize<T>(json);
        }

        public static void Save<T>(string path, T value)
        {
            string directory = Path.GetDirectoryName(path);
            if (!String.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            string temporary = path + ".tmp";
            string backup = path + ".bak";
            string json = new JavaScriptSerializer().Serialize(value);
            File.WriteAllText(temporary, json, new UTF8Encoding(false));

            if (File.Exists(path))
            {
                try
                {
                    File.Replace(temporary, path, backup, true);
                    return;
                }
                catch
                {
                    File.Copy(path, backup, true);
                    File.Copy(temporary, path, true);
                    File.Delete(temporary);
                    return;
                }
            }
            File.Move(temporary, path);
        }
    }

    internal sealed class TextPromptDialog : Form
    {
        private readonly TextBox input;
        public string Value { get { return input.Text.Trim(); } }

        public TextPromptDialog(string title, string prompt, string initial)
        {
            Text = title;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(390, 132);
            Font = new Font("Microsoft YaHei UI", 9.5F);

            Label label = new Label();
            label.Text = prompt;
            label.Location = new Point(14, 14);
            label.Size = new Size(360, 24);

            input = new TextBox();
            input.Text = initial ?? "";
            input.Location = new Point(16, 42);
            input.Size = new Size(358, 28);

            Button ok = new QuietButton();
            ok.Text = "确定";
            ok.DialogResult = DialogResult.OK;
            ok.Location = new Point(218, 87);
            ok.Size = new Size(74, 30);

            Button cancel = new QuietButton();
            cancel.Text = "取消";
            cancel.DialogResult = DialogResult.Cancel;
            cancel.Location = new Point(300, 87);
            cancel.Size = new Size(74, 30);

            Controls.Add(label);
            Controls.Add(input);
            Controls.Add(ok);
            Controls.Add(cancel);
            AcceptButton = ok;
            CancelButton = cancel;
            Shown += delegate { input.Focus(); input.SelectAll(); };
        }
    }

    internal sealed class RosterManagerDialog : Form
    {
        private readonly ListBox classList;
        private readonly TextBox namesBox;
        private readonly Label countLabel;
        private int editingIndex;
        public List<RosterClassData> Classes { get; private set; }

        public RosterManagerDialog(IEnumerable<RosterClassData> source)
        {
            Classes = source.Select(CloneClass).ToList();
            editingIndex = -1;
            Text = "班级与名单";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(680, 440);
            Size = new Size(760, 520);
            Font = new Font("Microsoft YaHei UI", 9.5F);
            BackColor = Color.FromArgb(243, 246, 250);

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(12);
            root.ColumnCount = 2;
            root.RowCount = 2;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 225F));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));

            Panel left = new Panel();
            left.Dock = DockStyle.Fill;
            left.Padding = new Padding(0, 0, 10, 0);
            classList = new ListBox();
            classList.Dock = DockStyle.Fill;
            classList.Font = new Font("Microsoft YaHei UI", 10F);
            classList.SelectedIndexChanged += ClassList_SelectedIndexChanged;

            FlowLayoutPanel classButtons = new FlowLayoutPanel();
            classButtons.Dock = DockStyle.Bottom;
            classButtons.Height = 76;
            classButtons.Padding = new Padding(0, 6, 0, 0);
            classButtons.WrapContents = true;
            classButtons.Controls.Add(ActionButton("新建", NewClass));
            classButtons.Controls.Add(ActionButton("改名", RenameClass));
            classButtons.Controls.Add(ActionButton("复制", CopyClass));
            classButtons.Controls.Add(ActionButton("删除", DeleteClass));
            left.Controls.Add(classList);
            left.Controls.Add(classButtons);

            Panel right = new Panel();
            right.Dock = DockStyle.Fill;
            right.BackColor = Color.White;
            right.Padding = new Padding(12);

            Label help = new Label();
            help.Text = "每行一个姓名，也可直接粘贴 Excel 中的一列";
            help.Dock = DockStyle.Top;
            help.Height = 28;
            help.ForeColor = Color.FromArgb(102, 112, 133);

            countLabel = new Label();
            countLabel.Dock = DockStyle.Bottom;
            countLabel.Height = 26;
            countLabel.TextAlign = ContentAlignment.MiddleLeft;
            countLabel.ForeColor = Color.FromArgb(102, 112, 133);

            FlowLayoutPanel nameActions = new FlowLayoutPanel();
            nameActions.Dock = DockStyle.Bottom;
            nameActions.Height = 38;
            nameActions.FlowDirection = FlowDirection.LeftToRight;
            nameActions.Controls.Add(ActionButton("导入 TXT/CSV", ImportNames));
            nameActions.Controls.Add(ActionButton("清空", delegate { namesBox.Clear(); }));

            namesBox = new TextBox();
            namesBox.Multiline = true;
            namesBox.AcceptsReturn = true;
            namesBox.ScrollBars = ScrollBars.Vertical;
            namesBox.Dock = DockStyle.Fill;
            namesBox.Font = new Font("Microsoft YaHei UI", 11F);
            namesBox.TextChanged += delegate { UpdateCount(); };

            right.Controls.Add(namesBox);
            right.Controls.Add(help);
            right.Controls.Add(countLabel);
            right.Controls.Add(nameActions);

            FlowLayoutPanel footer = new FlowLayoutPanel();
            footer.Dock = DockStyle.Fill;
            footer.FlowDirection = FlowDirection.RightToLeft;
            footer.Padding = new Padding(0, 7, 0, 0);
            RoundedButton cancel = ActionButton("取消", null);
            cancel.DialogResult = DialogResult.Cancel;
            RoundedButton save = ActionButton("保存", SaveAndClose);
            save.BackColor = Color.FromArgb(91, 75, 206);
            save.ForeColor = Color.White;
            save.BorderColor = Color.Transparent;
            save.HoverBackColor = Color.FromArgb(78, 63, 194);
            save.PressedBackColor = Color.FromArgb(64, 50, 166);
            footer.Controls.Add(cancel);
            footer.Controls.Add(save);

            root.Controls.Add(left, 0, 0);
            root.Controls.Add(right, 1, 0);
            root.Controls.Add(footer, 0, 1);
            root.SetColumnSpan(footer, 2);
            Controls.Add(root);
            CancelButton = cancel;
            RefreshClassList();
            if (classList.Items.Count > 0) classList.SelectedIndex = 0;
        }

        private static RosterClassData CloneClass(RosterClassData source)
        {
            RosterClassData copy = new RosterClassData();
            copy.Name = source.Name;
            copy.Students = source.Students.Select(s => new RosterStudentData { Name = s.Name, Count = s.Count }).ToList();
            return copy;
        }

        private RoundedButton ActionButton(string text, EventHandler action)
        {
            RoundedButton button = new RoundedButton();
            button.Text = text;
            button.AutoSize = true;
            button.MinimumSize = new Size(74, 30);
            button.BackColor = Color.White;
            button.ForeColor = Color.FromArgb(31, 42, 61);
            button.BorderColor = Color.FromArgb(221, 229, 239);
            button.HoverBackColor = Color.FromArgb(231, 237, 247);
            button.PressedBackColor = Color.FromArgb(220, 230, 245);
            if (action != null) button.Click += action;
            return button;
        }

        private void RefreshClassList()
        {
            int selected = classList.SelectedIndex;
            classList.Items.Clear();
            foreach (RosterClassData item in Classes) classList.Items.Add(item.Name);
            if (classList.Items.Count > 0) classList.SelectedIndex = Math.Max(0, Math.Min(selected, classList.Items.Count - 1));
        }

        private void ClassList_SelectedIndexChanged(object sender, EventArgs e)
        {
            CommitNames(false);
            editingIndex = classList.SelectedIndex;
            namesBox.Enabled = editingIndex >= 0;
            namesBox.Text = editingIndex >= 0
                ? String.Join(Environment.NewLine, Classes[editingIndex].Students.Select(s => s.Name).ToArray())
                : "";
            UpdateCount();
        }

        private List<string> ParsedNames()
        {
            return namesBox.Lines.Select(n => n.Trim()).Where(n => n.Length > 0).ToList();
        }

        private bool CommitNames(bool showErrors)
        {
            if (editingIndex < 0 || editingIndex >= Classes.Count) return true;
            List<string> names = ParsedNames();
            List<string> duplicates = names.GroupBy(n => n).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            if (duplicates.Count > 0)
            {
                if (showErrors)
                    MessageBox.Show(this, "同一班级中存在重复姓名：\n" + String.Join("、", duplicates.ToArray()) +
                        "\n\n请先处理后再保存。", "重复姓名", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            Dictionary<string, int> oldCounts = Classes[editingIndex].Students
                .GroupBy(s => s.Name).ToDictionary(g => g.Key, g => g.First().Count);
            Classes[editingIndex].Students = names.Select(n => new RosterStudentData
            {
                Name = n,
                Count = oldCounts.ContainsKey(n) ? oldCounts[n] : 0
            }).ToList();
            return true;
        }

        private void NewClass(object sender, EventArgs e)
        {
            CommitNames(false);
            using (TextPromptDialog dialog = new TextPromptDialog("新建班级", "班级名称", ""))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK || dialog.Value.Length == 0) return;
                if (Classes.Any(c => c.Name == dialog.Value))
                {
                    MessageBox.Show(this, "这个班级已经存在。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                Classes.Add(new RosterClassData { Name = dialog.Value });
                RefreshClassList();
                classList.SelectedIndex = Classes.Count - 1;
            }
        }

        private void RenameClass(object sender, EventArgs e)
        {
            int index = classList.SelectedIndex;
            if (index < 0) return;
            using (TextPromptDialog dialog = new TextPromptDialog("班级改名", "新的班级名称", Classes[index].Name))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK || dialog.Value.Length == 0) return;
                if (Classes.Where((c, i) => i != index).Any(c => c.Name == dialog.Value))
                {
                    MessageBox.Show(this, "这个班级已经存在。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                Classes[index].Name = dialog.Value;
                RefreshClassList();
                classList.SelectedIndex = index;
            }
        }

        private void CopyClass(object sender, EventArgs e)
        {
            int index = classList.SelectedIndex;
            if (index < 0) return;
            CommitNames(false);
            string baseName = Classes[index].Name + " 副本";
            string name = baseName;
            int number = 2;
            while (Classes.Any(c => c.Name == name)) { name = baseName + number; number++; }
            RosterClassData copy = CloneClass(Classes[index]);
            copy.Name = name;
            foreach (RosterStudentData student in copy.Students) student.Count = 0;
            Classes.Add(copy);
            RefreshClassList();
            classList.SelectedIndex = Classes.Count - 1;
        }

        private void DeleteClass(object sender, EventArgs e)
        {
            int index = classList.SelectedIndex;
            if (index < 0) return;
            if (!ConfirmDialog.Confirm(this, "确定删除“" + Classes[index].Name + "”吗？\n历史记录不会被删除。",
                "删除班级", "确认删除")) return;
            Classes.RemoveAt(index);
            editingIndex = -1;
            RefreshClassList();
        }

        private void ImportNames(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Title = "导入名单";
                dialog.Filter = "名单文件|*.txt;*.csv|文本文件|*.txt|CSV 文件|*.csv";
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                string[] lines = File.ReadAllLines(dialog.FileName, Encoding.UTF8);
                List<string> names = new List<string>();
                foreach (string line in lines)
                {
                    string value = line.Trim();
                    if (Path.GetExtension(dialog.FileName).Equals(".csv", StringComparison.OrdinalIgnoreCase))
                    {
                        int comma = value.IndexOf(',');
                        if (comma >= 0) value = value.Substring(0, comma);
                        value = value.Trim().Trim('"');
                    }
                    if (value.Length > 0) names.Add(value);
                }
                namesBox.Text = String.Join(Environment.NewLine, names.ToArray());
            }
        }

        private void UpdateCount()
        {
            countLabel.Text = ParsedNames().Count + " 人";
        }

        private void SaveAndClose(object sender, EventArgs e)
        {
            if (!CommitNames(true)) return;
            if (Classes.Count == 0)
            {
                MessageBox.Show(this, "请至少保留一个班级。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (Classes.Any(c => c.Students.Count == 0))
            {
                if (MessageBox.Show(this, "有班级尚未添加姓名，仍然保存吗？", "空名单",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
            }
            DialogResult = DialogResult.OK;
            Close();
        }
    }

    internal sealed class HistorySelectionRow
    {
        public bool IsSelected { get; set; }
        public HistoryEntry Entry { get; private set; }
        public string Time { get { return Entry.Time; } }
        public string ClassName { get { return Entry.ClassName; } }
        public string StudentName { get { return Entry.StudentName; } }
        public string Status { get { return Entry.Undone ? "已撤销" : "有效"; } }

        public HistorySelectionRow(HistoryEntry entry)
        {
            Entry = entry;
        }
    }

    internal sealed class SelectionHeaderCell : DataGridViewColumnHeaderCell
    {
        public CheckState CheckState { get; set; }

        public SelectionHeaderCell()
        {
            CheckState = CheckState.Unchecked;
            ToolTipText = "全选记录";
        }

        protected override void Paint(Graphics graphics, Rectangle clipBounds, Rectangle cellBounds,
            int rowIndex, DataGridViewElementStates dataGridViewElementState, object value,
            object formattedValue, string errorText, DataGridViewCellStyle cellStyle,
            DataGridViewAdvancedBorderStyle advancedBorderStyle, DataGridViewPaintParts paintParts)
        {
            base.Paint(graphics, clipBounds, cellBounds, rowIndex, dataGridViewElementState,
                value, "", errorText, cellStyle, advancedBorderStyle, paintParts);

            System.Windows.Forms.VisualStyles.CheckBoxState state =
                System.Windows.Forms.VisualStyles.CheckBoxState.UncheckedNormal;
            if (CheckState == CheckState.Checked)
                state = System.Windows.Forms.VisualStyles.CheckBoxState.CheckedNormal;
            if (CheckState == CheckState.Indeterminate)
                state = System.Windows.Forms.VisualStyles.CheckBoxState.MixedNormal;
            Size glyph = CheckBoxRenderer.GetGlyphSize(graphics, state);
            Point location = new Point(
                cellBounds.Left + Math.Max(0, (cellBounds.Width - glyph.Width) / 2),
                cellBounds.Top + Math.Max(0, (cellBounds.Height - glyph.Height) / 2));
            CheckBoxRenderer.DrawCheckBox(graphics, location, state);
        }
    }

    internal sealed class HistoryDialog : Form
    {
        private readonly List<HistoryEntry> entries;
        private readonly List<HistorySelectionRow> rows;
        private readonly DataGridView grid;
        private readonly SelectionHeaderCell selectionHeader;
        private readonly Label selectedCountLabel;
        private readonly Label emptyLabel;
        private readonly RoundedButton deleteButton;
        private readonly RoundedButton exportButton;
        public bool WasCleared { get; private set; }
        public bool HistoryChanged { get; private set; }
        public bool UndoRequested { get; private set; }

        public HistoryDialog(IEnumerable<HistoryEntry> source)
        {
            entries = source as List<HistoryEntry> ?? source.ToList();
            rows = entries.OrderByDescending(e => e.Time)
                .Select(e => new HistorySelectionRow(e)).ToList();
            Text = "点名记录";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(720, 480);
            MinimumSize = new Size(600, 380);
            Font = new Font("Microsoft YaHei UI", 9.5F);
            BackColor = Color.FromArgb(243, 246, 250);

            grid = new DataGridView();
            grid.Dock = DockStyle.Fill;
            grid.ReadOnly = false;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;
            grid.AutoGenerateColumns = false;
            grid.RowHeadersVisible = false;
            grid.SelectionMode = DataGridViewSelectionMode.CellSelect;
            grid.MultiSelect = false;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.ColumnHeadersHeight = 34;
            grid.RowTemplate.Height = 30;
            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(244, 245, 251);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(31, 42, 61);
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            grid.DefaultCellStyle.Font = new Font("Microsoft YaHei UI", 9.5F);
            grid.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            grid.DefaultCellStyle.BackColor = Color.White;
            grid.DefaultCellStyle.ForeColor = Color.FromArgb(31, 42, 61);
            grid.DefaultCellStyle.SelectionBackColor = Color.White;
            grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(31, 42, 61);
            selectionHeader = new SelectionHeaderCell();
            DataGridViewCheckBoxColumn selectionColumn = new DataGridViewCheckBoxColumn();
            selectionColumn.HeaderCell = selectionHeader;
            selectionColumn.DataPropertyName = "IsSelected";
            selectionColumn.Width = 42;
            selectionColumn.MinimumWidth = 42;
            selectionColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            selectionColumn.Resizable = DataGridViewTriState.False;
            selectionColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
            selectionColumn.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            grid.Columns.Add(selectionColumn);
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "时间", DataPropertyName = "Time", FillWeight = 31F, ReadOnly = true });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "班级", DataPropertyName = "ClassName", FillWeight = 26F, ReadOnly = true });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "姓名", DataPropertyName = "StudentName", FillWeight = 21F, ReadOnly = true });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "状态", DataPropertyName = "Status", FillWeight = 16F, ReadOnly = true });
            grid.ColumnHeaderMouseClick += Grid_ColumnHeaderMouseClick;
            grid.CurrentCellDirtyStateChanged += Grid_CurrentCellDirtyStateChanged;
            grid.CellValueChanged += Grid_CellValueChanged;
            grid.DataSource = rows;

            Panel gridHost = new Panel();
            gridHost.Dock = DockStyle.Fill;
            gridHost.Controls.Add(grid);
            emptyLabel = new Label();
            emptyLabel.Dock = DockStyle.Fill;
            emptyLabel.Text = "暂无点名记录";
            emptyLabel.TextAlign = ContentAlignment.MiddleCenter;
            emptyLabel.Font = new Font("Microsoft YaHei UI", 11F);
            emptyLabel.ForeColor = Color.FromArgb(120, 132, 151);
            emptyLabel.BackColor = Color.FromArgb(249, 250, 253);
            emptyLabel.Visible = rows.Count == 0;
            gridHost.Controls.Add(emptyLabel);
            if (emptyLabel.Visible) emptyLabel.BringToFront();

            TableLayoutPanel footer = new TableLayoutPanel();
            footer.Dock = DockStyle.Bottom;
            footer.Height = 54;
            footer.Padding = new Padding(10, 8, 10, 8);
            footer.ColumnCount = 2;
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            selectedCountLabel = new Label();
            selectedCountLabel.Text = "已选 0 条";
            selectedCountLabel.Dock = DockStyle.Fill;
            selectedCountLabel.TextAlign = ContentAlignment.MiddleLeft;
            selectedCountLabel.ForeColor = Color.FromArgb(102, 112, 133);

            FlowLayoutPanel actions = new FlowLayoutPanel();
            actions.AutoSize = true;
            actions.Dock = DockStyle.Fill;
            actions.WrapContents = false;
            actions.FlowDirection = FlowDirection.LeftToRight;
            actions.Margin = Padding.Empty;
            RoundedButton close = MakeButton("关闭", delegate { Close(); });
            exportButton = MakeButton("导出所选", ExportCsv);
            RoundedButton undo = MakeButton("撤销上次", UndoLast);
            deleteButton = MakeButton("删除所选", DeleteSelected);
            actions.Controls.Add(deleteButton);
            actions.Controls.Add(undo);
            actions.Controls.Add(exportButton);
            actions.Controls.Add(close);
            footer.Controls.Add(selectedCountLabel, 0, 0);
            footer.Controls.Add(actions, 1, 0);

            Controls.Add(gridHost);
            Controls.Add(footer);
            UpdateSelectionState();
        }

        private RoundedButton MakeButton(string text, EventHandler click)
        {
            RoundedButton button = new RoundedButton();
            button.Text = text;
            button.AutoSize = true;
            button.MinimumSize = new Size(88, 30);
            button.BackColor = Color.White;
            button.ForeColor = Color.FromArgb(31, 42, 61);
            button.BorderColor = Color.FromArgb(221, 229, 239);
            button.HoverBackColor = Color.FromArgb(231, 237, 247);
            button.PressedBackColor = Color.FromArgb(220, 230, 245);
            button.Click += click;
            return button;
        }

        private void ExportCsv(object sender, EventArgs e)
        {
            List<HistorySelectionRow> selectedRows = rows.Where(row => row.IsSelected).ToList();
            if (selectedRows.Count == 0) return;
            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Title = "导出点名记录";
                dialog.Filter = "CSV 文件|*.csv";
                dialog.FileName = "点名记录_" + DateTime.Now.ToString("yyyyMMdd") + ".csv";
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                StringBuilder csv = new StringBuilder();
                csv.AppendLine("时间,班级,姓名,状态");
                foreach (HistoryEntry item in selectedRows.Select(row => row.Entry))
                {
                    csv.Append(Csv(item.Time)).Append(',').Append(Csv(item.ClassName)).Append(',')
                        .Append(Csv(item.StudentName)).Append(',').Append(item.Undone ? "已撤销" : "有效").AppendLine();
                }
                File.WriteAllText(dialog.FileName, csv.ToString(), new UTF8Encoding(true));
                MessageBox.Show(this, "已导出 " + selectedRows.Count + " 条记录。", "导出完成",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private static string Csv(string value)
        {
            return "\"" + (value ?? "").Replace("\"", "\"\"") + "\"";
        }

        private void DeleteSelected(object sender, EventArgs e)
        {
            List<HistorySelectionRow> selectedRows = rows.Where(row => row.IsSelected).ToList();
            if (selectedRows.Count == 0) return;
            string message = "将删除 " + selectedRows.Count + " 条历史记录，不改变本轮点名进度。\n"
                + "如需纠正最近一次点名，请使用“撤销上次”。";
            if (!ConfirmDialog.Confirm(this, message, "删除记录", "确认删除")) return;

            foreach (HistorySelectionRow row in selectedRows)
            {
                entries.Remove(row.Entry);
                rows.Remove(row);
            }
            HistoryChanged = true;
            WasCleared = entries.Count == 0;
            RefreshRows();
        }

        private void Grid_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (grid.IsCurrentCellDirty && grid.CurrentCell != null && grid.CurrentCell.ColumnIndex == 0)
                grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        private void Grid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == 0) UpdateSelectionState();
        }

        private void Grid_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex != 0 || rows.Count == 0) return;
            grid.EndEdit();
            bool selectAll = selectionHeader.CheckState != CheckState.Checked;
            foreach (HistorySelectionRow row in rows) row.IsSelected = selectAll;
            RefreshRows();
        }

        private void RefreshRows()
        {
            int firstVisibleRow = grid.FirstDisplayedScrollingRowIndex;
            grid.DataSource = null;
            grid.DataSource = rows;
            if (firstVisibleRow >= 0 && firstVisibleRow < grid.Rows.Count)
            {
                try { grid.FirstDisplayedScrollingRowIndex = firstVisibleRow; }
                catch (InvalidOperationException) { }
            }
            emptyLabel.Visible = rows.Count == 0;
            if (emptyLabel.Visible) emptyLabel.BringToFront();
            UpdateSelectionState();
        }

        private void UpdateSelectionState()
        {
            if (selectedCountLabel == null || deleteButton == null || exportButton == null || selectionHeader == null)
                return;
            int selected = rows.Count(row => row.IsSelected);
            selectedCountLabel.Text = "已选 " + selected + " 条";
            deleteButton.Enabled = selected > 0;
            exportButton.Enabled = selected > 0;
            selectionHeader.CheckState = selected == 0
                ? CheckState.Unchecked
                : selected == rows.Count ? CheckState.Checked : CheckState.Indeterminate;
            grid.InvalidateCell(selectionHeader);
        }

        private void UndoLast(object sender, EventArgs e)
        {
            if (!entries.Any(item => !item.Undone))
            {
                MessageBox.Show(this, "没有可以撤销的点名记录。", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            UndoRequested = true;
            Close();
        }
    }
}
