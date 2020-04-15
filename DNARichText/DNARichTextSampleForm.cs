using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace DNARichText
{
    /// <summary>
    /// Sample implementation of DNA Rich Text
    /// </summary>
    public partial class DNARichTextSampleForm : Form
    {
        public DNARichTextSampleForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// This event-based-method will be called when Form is shown (at start of application)
        /// </summary>
        private void CustomTextSourceSample_Shown(object sender, EventArgs e)
        {
            var color = new DNACharColor(Color.Red, new[] { FontStyle.Bold});
            var kc = color.Color.ToKnownColor();
            //var c = color.Color;
            //var s = color.Styles;


            var watch = Stopwatch.StartNew();
            string filename = @"..\..\..\test_data\dna_lower.txt";
            string text = System.IO.File.ReadAllText(filename);
            label1.Text = "Filename = " + filename;
            label1.Text += "\nfile size: " + text.Length;
            Update(); Refresh(); Application.DoEvents();

            watch.Restart();
            dnaFastColoredTextBox1.Text = text;

            dnaFastColoredTextBox1.ApplyStyle("ttt", dnaFastColoredTextBox1.SyntaxHighlighter.RedStyle);
            dnaFastColoredTextBox1.ApplyStyle("aa", dnaFastColoredTextBox1.SyntaxHighlighter.BoldStyle);

            watch.Stop();
            label1.Text += "\nparse time ms: " + watch.ElapsedMilliseconds;
            Update(); Refresh(); Application.DoEvents();


            //text = (fctb.TextSource as DynamicTextSource).GetTextAsLines();

            //watch.Restart();
            //textBox1.Font = fctb.Font;
            //textBox1.Text = text;
            //watch.Stop();
            //label1.Text += "\nparse time ms: " + watch.ElapsedMilliseconds;
            //Update(); Refresh(); Application.DoEvents();

            //watch.Restart();
            //richTextBox1.Font = fctb.Font;
            //richTextBox1.Text = text;
            //watch.Stop();
            //label1.Text += "\nparse time ms: " + watch.ElapsedMilliseconds;
            //Update(); Refresh(); Application.DoEvents();
        }

    }


    




    
}
