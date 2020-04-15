namespace DNARichText
{
    partial class DNARichTextSampleForm
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.label1 = new System.Windows.Forms.Label();
            this.dnaFastColoredTextBox1 = new DNARichText.DNAFastColoredTextBox();
            ((System.ComponentModel.ISupportInitialize)(this.dnaFastColoredTextBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.Dock = System.Windows.Forms.DockStyle.Top;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.label1.Location = new System.Drawing.Point(0, 0);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(983, 113);
            this.label1.TabIndex = 2;
            // 
            // dnaFastColoredTextBox1
            // 
            this.dnaFastColoredTextBox1.AcceptsReturn = false;
            this.dnaFastColoredTextBox1.AcceptsTab = false;
            this.dnaFastColoredTextBox1.AllowDrop = false;
            this.dnaFastColoredTextBox1.AllowMacroRecording = false;
            this.dnaFastColoredTextBox1.AutoCompleteBracketsList = new char[] {
        '(',
        ')',
        '{',
        '}',
        '[',
        ']',
        '\"',
        '\"',
        '\'',
        '\''};
            this.dnaFastColoredTextBox1.AutoIndent = false;
            this.dnaFastColoredTextBox1.AutoIndentChars = false;
            this.dnaFastColoredTextBox1.AutoScrollMinSize = new System.Drawing.Size(10, 29);
            this.dnaFastColoredTextBox1.BackBrush = null;
            this.dnaFastColoredTextBox1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.dnaFastColoredTextBox1.CaretVisible = false;
            this.dnaFastColoredTextBox1.CausesValidation = false;
            this.dnaFastColoredTextBox1.CharHeight = 23;
            this.dnaFastColoredTextBox1.CharWidth = 11;
            this.dnaFastColoredTextBox1.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.dnaFastColoredTextBox1.DisabledColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))));
            this.dnaFastColoredTextBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dnaFastColoredTextBox1.Font = new System.Drawing.Font("Consolas", 10F);
            this.dnaFastColoredTextBox1.HighlightFoldingIndicator = false;
            this.dnaFastColoredTextBox1.IsReplaceMode = false;
            this.dnaFastColoredTextBox1.Location = new System.Drawing.Point(0, 113);
            this.dnaFastColoredTextBox1.Name = "dnaFastColoredTextBox1";
            this.dnaFastColoredTextBox1.Paddings = new System.Windows.Forms.Padding(5, 3, 3, 3);
            this.dnaFastColoredTextBox1.ReadOnly = true;
            this.dnaFastColoredTextBox1.SelectionColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(255)))));
            this.dnaFastColoredTextBox1.ServiceColors = null;
            this.dnaFastColoredTextBox1.ShowLineNumbers = false;
            this.dnaFastColoredTextBox1.ShowScrollBars = false;
            this.dnaFastColoredTextBox1.Size = new System.Drawing.Size(983, 515);
            this.dnaFastColoredTextBox1.TabIndex = 6;
            this.dnaFastColoredTextBox1.Zoom = 100;
            // 
            // DNARichTextSampleForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(983, 628);
            this.Controls.Add(this.dnaFastColoredTextBox1);
            this.Controls.Add(this.label1);
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.Name = "DNARichTextSampleForm";
            this.Text = "CustomTextSourceSample";
            this.Shown += new System.EventHandler(this.CustomTextSourceSample_Shown);
            ((System.ComponentModel.ISupportInitialize)(this.dnaFastColoredTextBox1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private DNAFastColoredTextBox dnaFastColoredTextBox1;
    }
}