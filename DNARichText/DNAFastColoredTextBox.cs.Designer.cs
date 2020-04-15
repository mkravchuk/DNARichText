using System.Windows.Forms;

namespace DNARichText
{
    partial class DNAFastColoredTextBox
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.copyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.vScrollBar = new System.Windows.Forms.VScrollBar();
            this.contextMenuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            this.SuspendLayout();
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.copyToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(189, 34);
            // 
            // copyToolStripMenuItem
            // 
            this.copyToolStripMenuItem.Name = "copyToolStripMenuItem";
            this.copyToolStripMenuItem.ShortcutKeyDisplayString = "";
            this.copyToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.C)));
            this.copyToolStripMenuItem.Size = new System.Drawing.Size(188, 30);
            this.copyToolStripMenuItem.Text = "Copy";
            this.copyToolStripMenuItem.ToolTipText = "Copy text to clipboard in plain and HTML formats (size over 1 Mb will be copied o" +
    "nly in plain format)";
            this.copyToolStripMenuItem.Click += new System.EventHandler(this.copyToolStripMenuItem_Click);
            // 
            // vScrollBar
            // 
            this.vScrollBar.Cursor = System.Windows.Forms.Cursors.Default;
            this.vScrollBar.Dock = System.Windows.Forms.DockStyle.Right;
            this.vScrollBar.Location = new System.Drawing.Point(682, 0);
            this.vScrollBar.Name = "vScrollBar";
            this.vScrollBar.Size = new System.Drawing.Size(12, 531);
            this.vScrollBar.TabIndex = 1;
            this.vScrollBar.Scroll += new System.Windows.Forms.ScrollEventHandler(this.vScrollBar_Scroll);
            // 
            // DNAFastColoredTextBox
            // 
            this.AcceptsReturn = false;
            this.AcceptsTab = false;
            this.AllowDrop = false;
            this.AllowMacroRecording = false;
            this.AutoIndent = false;
            this.AutoIndentChars = false;
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScrollMinSize = new System.Drawing.Size(10, 28);
            this.CaretVisible = false;
            this.CausesValidation = false;
            this.ContextMenuStrip = this.contextMenuStrip1;
            this.Controls.Add(this.vScrollBar);
            this.HighlightFoldingIndicator = false;
            this.Name = "DNAFastColoredTextBox";
            this.Paddings = new System.Windows.Forms.Padding(5, 3, 3, 3);
            this.ReadOnly = true;
            this.ShowLineNumbers = false;
            this.ShowScrollBars = false;
            this.Size = new System.Drawing.Size(694, 531);
            this.ScrollbarsUpdated += new System.EventHandler(this.DNAFastColoredTextBox_ScrollbarsUpdated);
            this.contextMenuStrip1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem copyToolStripMenuItem;
        private System.Windows.Forms.VScrollBar vScrollBar;

    }
}
