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

            Button ok = new Button();
            ok.Text = "确定";
            ok.DialogResult = DialogResult.OK;
            ok.Location = new Point(218, 87);
            ok.Size = new Size(74, 30);

            Button cancel = new Button();
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
            BackColor = Color.FromArgb(244, 247, 252);

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
            help.ForeColor = Color.FromArgb(95, 108, 130);

            countLabel = new Label();
            countLabel.Dock = DockStyle.Bottom;
            countLabel.Height = 26;
            countLabel.TextAlign = ContentAlignment.MiddleLeft;
            countLabel.ForeColor = Color.FromArgb(95, 108, 130);

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
            Button cancel = ActionButton("取消", null);
            cancel.DialogResult = DialogResult.Cancel;
            Button save = ActionButton("保存", SaveAndClose);
            save.BackColor = Color.FromArgb(43, 103, 246);
            save.ForeColor = Color.White;
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

        private Button ActionButton(string text, EventHandler action)
        {
            Button button = new Button();
            button.Text = text;
            button.AutoSize = true;
            button.MinimumSize = new Size(74, 30);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = Color.FromArgb(215, 224, 238);
            button.BackColor = Color.White;
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
            if (MessageBox.Show(this, "确定删除“" + Classes[index].Name + "”吗？\n历史记录不会被删除。",
                "删除班级", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
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

    internal sealed class HistoryDialog : Form
    {
        private readonly List<HistoryEntry> entries;
        private readonly DataGridView grid;
        public bool WasCleared { get; private set; }
        public bool UndoRequested { get; private set; }

        public HistoryDialog(IEnumerable<HistoryEntry> source)
        {
            entries = source.ToList();
            Text = "点名记录";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(720, 480);
            MinimumSize = new Size(600, 380);
            Font = new Font("Microsoft YaHei UI", 9.5F);

            grid = new DataGridView();
            grid.Dock = DockStyle.Fill;
            grid.ReadOnly = true;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AutoGenerateColumns = false;
            grid.RowHeadersVisible = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "时间", DataPropertyName = "Time", FillWeight = 34F });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "班级", DataPropertyName = "ClassName", FillWeight = 28F });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "姓名", DataPropertyName = "StudentName", FillWeight = 22F });
            grid.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "已撤销", DataPropertyName = "Undone", FillWeight = 16F });
            grid.DataSource = entries.OrderByDescending(e => e.Time).ToList();

            FlowLayoutPanel footer = new FlowLayoutPanel();
            footer.Dock = DockStyle.Bottom;
            footer.Height = 48;
            footer.Padding = new Padding(8);
            footer.FlowDirection = FlowDirection.RightToLeft;
            Button close = MakeButton("关闭", delegate { Close(); });
            Button export = MakeButton("导出 CSV", ExportCsv);
            Button undo = MakeButton("撤销上次", UndoLast);
            Button clear = MakeButton("清空记录", ClearHistory);
            footer.Controls.Add(close);
            footer.Controls.Add(export);
            footer.Controls.Add(undo);
            footer.Controls.Add(clear);

            Controls.Add(grid);
            Controls.Add(footer);
        }

        private Button MakeButton(string text, EventHandler click)
        {
            Button button = new Button();
            button.Text = text;
            button.AutoSize = true;
            button.MinimumSize = new Size(88, 30);
            button.Click += click;
            return button;
        }

        private void ExportCsv(object sender, EventArgs e)
        {
            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Title = "导出点名记录";
                dialog.Filter = "CSV 文件|*.csv";
                dialog.FileName = "点名记录_" + DateTime.Now.ToString("yyyyMMdd") + ".csv";
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                StringBuilder csv = new StringBuilder();
                csv.AppendLine("时间,班级,姓名,状态");
                foreach (HistoryEntry item in entries)
                {
                    csv.Append(Csv(item.Time)).Append(',').Append(Csv(item.ClassName)).Append(',')
                        .Append(Csv(item.StudentName)).Append(',').Append(item.Undone ? "已撤销" : "有效").AppendLine();
                }
                File.WriteAllText(dialog.FileName, csv.ToString(), new UTF8Encoding(true));
                MessageBox.Show(this, "已导出 " + entries.Count + " 条记录。", "导出完成",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private static string Csv(string value)
        {
            return "\"" + (value ?? "").Replace("\"", "\"\"") + "\"";
        }

        private void ClearHistory(object sender, EventArgs e)
        {
            if (MessageBox.Show(this, "确定清空全部点名历史吗？此操作不能撤销。", "清空记录",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
            entries.Clear();
            grid.DataSource = new List<HistoryEntry>();
            WasCleared = true;
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
