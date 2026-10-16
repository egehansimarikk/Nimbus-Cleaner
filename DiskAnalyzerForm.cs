using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinCleaner
{
    internal sealed class DiskAnalyzerForm : Form
    {
        private readonly Palette _palette;
        private readonly ComboBox _driveSelector;
        private readonly NumericUpDown _thresholdInput;
        private readonly Button _scanButton;
        private readonly Button _cancelButton;
        private readonly DataGridView _resultsGrid;
        private readonly Label _statusLabel;
        private readonly ContextMenuStrip _contextMenu;
        private BindingList<DiskFileResult> _results = new BindingList<DiskFileResult>();
        private CancellationTokenSource _scanCancellation;

        public DiskAnalyzerForm(Palette palette)
        {
            _palette = palette;
            this.Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            Text = S.G("diskAnalyzer.title");
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new System.Drawing.Size(800, 480);
            Size = new System.Drawing.Size(1050, 650);
            Font = new System.Drawing.Font("Segoe UI", 9.25f);
            AutoScaleMode = AutoScaleMode.Dpi;

            var controls = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 54,
                Padding = new Padding(12, 10, 8, 6),
                WrapContents = false,
                FlowDirection = FlowDirection.LeftToRight
            };
            controls.Controls.Add(new Label { Text = S.G("diskAnalyzer.drive"), AutoSize = true, Margin = new Padding(0, 8, 6, 0) });

            _driveSelector = new ComboBox { Width = 130, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 2, 16, 0) };
            _driveSelector.Items.AddRange(GetReadyDriveRoots().Cast<object>().ToArray());
            if (_driveSelector.Items.Count > 0) _driveSelector.SelectedIndex = 0;
            controls.Controls.Add(_driveSelector);

            controls.Controls.Add(new Label { Text = S.G("diskAnalyzer.threshold"), AutoSize = true, Margin = new Padding(0, 8, 6, 0) });
            _thresholdInput = new NumericUpDown
            {
                Width = 100,
                Minimum = 0,
                Maximum = 1000000,
                DecimalPlaces = 1,
                Increment = 10,
                Value = 100,
                Margin = new Padding(0, 2, 16, 0)
            };
            controls.Controls.Add(_thresholdInput);

            _scanButton = new Button { Text = S.G("diskAnalyzer.scan"), Width = 100, Height = 32, Margin = new Padding(0, 0, 8, 0) };
            _scanButton.Click += async (_, __) => await ScanAsync();
            controls.Controls.Add(_scanButton);

            _cancelButton = new Button { Text = S.G("diskAnalyzer.cancel"), Width = 100, Height = 32, Enabled = false, Margin = new Padding(0, 0, 8, 0) };
            _cancelButton.Click += (_, __) => _scanCancellation?.Cancel();
            controls.Controls.Add(_cancelButton);

            _resultsGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoGenerateColumns = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            _resultsGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(DiskFileResult.FileName), HeaderText = S.G("diskAnalyzer.fileName"), FillWeight = 22 });
            _resultsGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(DiskFileResult.Path), HeaderText = S.G("diskAnalyzer.path"), FillWeight = 48 });
            _resultsGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(DiskFileResult.FileSize), HeaderText = S.G("diskAnalyzer.fileSize"), FillWeight = 14 });
            _resultsGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(DiskFileResult.LastModifiedDate),
                HeaderText = S.G("diskAnalyzer.lastModified"),
                FillWeight = 20,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "g" }
            });
            _resultsGrid.DataSource = _results;

            _contextMenu = new ContextMenuStrip();
            _contextMenu.Items.Add(S.G("diskAnalyzer.openLocation"), null, (_, __) => OpenFileLocation());
            _contextMenu.Items.Add(S.G("diskAnalyzer.delete"), null, (_, __) => DeleteFile());
            _resultsGrid.ContextMenuStrip = _contextMenu;
            _resultsGrid.CellMouseDown += (_, e) =>
            {
                if (e.Button != MouseButtons.Right || e.RowIndex < 0) return;
                _resultsGrid.ClearSelection();
                _resultsGrid.Rows[e.RowIndex].Selected = true;
                _resultsGrid.CurrentCell = _resultsGrid.Rows[e.RowIndex].Cells[0];
            };

            _statusLabel = new Label { Text = S.G("diskAnalyzer.ready"), Dock = DockStyle.Bottom, Height = 30, Padding = new Padding(12, 6, 8, 4) };
            Controls.Add(_resultsGrid);
            Controls.Add(_statusLabel);
            Controls.Add(controls);
            ApplyPalette();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _scanCancellation?.Cancel();
            base.OnFormClosing(e);
        }

        private async Task ScanAsync()
        {
            if (_driveSelector.SelectedItem == null)
            {
                MessageBox.Show(this, S.G("diskAnalyzer.noDrives"), Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _scanCancellation = new CancellationTokenSource();
            _driveSelector.Enabled = false;
            _thresholdInput.Enabled = false;
            _scanButton.Enabled = false;
            _cancelButton.Enabled = true;
            _results.Clear();
            _statusLabel.Text = S.G("diskAnalyzer.scanning");

            try
            {
                var results = await DiskAnalyzer.ScanAsync((string)_driveSelector.SelectedItem, (double)_thresholdInput.Value, _scanCancellation.Token);
                if (IsDisposed) return;
                _results = new BindingList<DiskFileResult>(results);
                _resultsGrid.DataSource = _results;
                _statusLabel.Text = string.Format(S.G("diskAnalyzer.found"), results.Count);
            }
            catch (OperationCanceledException)
            {
                if (!IsDisposed) _statusLabel.Text = S.G("diskAnalyzer.cancelled");
            }
            catch (Exception ex)
            {
                if (!IsDisposed) MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _scanCancellation.Dispose();
                _scanCancellation = null;
                if (!IsDisposed)
                {
                    _driveSelector.Enabled = true;
                    _thresholdInput.Enabled = true;
                    _scanButton.Enabled = true;
                    _cancelButton.Enabled = false;
                }
            }
        }

        private void OpenFileLocation()
        {
            var selected = SelectedResult();
            if (selected == null) return;

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = "/select,\"" + selected.Path + "\"",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DeleteFile()
        {
            var selected = SelectedResult();
            if (selected == null) return;

            var answer = MessageBox.Show(this, string.Format(S.G("diskAnalyzer.deleteConfirm"), selected.FileName), Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (answer != DialogResult.Yes) return;

            try
            {
                File.Delete(selected.Path);
                _results.Remove(selected);
                _statusLabel.Text = S.G("diskAnalyzer.deleted");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private DiskFileResult SelectedResult()
        {
            return _resultsGrid.CurrentRow?.DataBoundItem as DiskFileResult;
        }

        private static string[] GetReadyDriveRoots()
        {
            var roots = new System.Collections.Generic.List<string>();
            try
            {
                foreach (var drive in DriveInfo.GetDrives())
                {
                    try
                    {
                        if (drive.DriveType != DriveType.CDRom && drive.IsReady)
                            roots.Add(drive.RootDirectory.FullName);
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
            }
            return roots.ToArray();
        }

        private void ApplyPalette()
        {
            BackColor = _palette.Bg;
            ForeColor = _palette.Fg;
            _statusLabel.BackColor = _palette.Bg;
            _statusLabel.ForeColor = _palette.Muted;
            _resultsGrid.BackgroundColor = _palette.Card;
            _resultsGrid.GridColor = _palette.Line;
            _resultsGrid.EnableHeadersVisualStyles = false;
            _resultsGrid.ColumnHeadersDefaultCellStyle.BackColor = _palette.Card;
            _resultsGrid.ColumnHeadersDefaultCellStyle.ForeColor = _palette.Fg;
            _resultsGrid.DefaultCellStyle.BackColor = _palette.Card;
            _resultsGrid.DefaultCellStyle.ForeColor = _palette.Fg;
            _resultsGrid.DefaultCellStyle.SelectionBackColor = _palette.SideSel;
            _resultsGrid.DefaultCellStyle.SelectionForeColor = _palette.Fg;

            foreach (var control in Controls.OfType<FlowLayoutPanel>().SelectMany(panel => panel.Controls.OfType<Button>()))
            {
                control.BackColor = _palette.Btn;
                control.ForeColor = _palette.Fg;
                control.FlatStyle = FlatStyle.Flat;
            }
        }
    }
}
