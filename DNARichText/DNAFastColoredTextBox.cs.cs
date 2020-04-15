using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Data;
using System.Drawing.Design;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using FastColoredTextBoxNS;

namespace DNARichText
{
    public partial class DNAFastColoredTextBox : FastColoredTextBox
    {
        public DNAFastColoredTextBox()
        {
            InitializeComponent();
        }

        public void ApplyStyle(string subString, Style style)
        {
            DynamicTextSource dts = (TextSource as DynamicTextSource);
            if (dts != null)
            {
                dts.StyledText.ApplyStyle(subString, style);
                dts.Load(dts.StyledText);
            }
        }

        #region Private Methods

        /// <summary>
        /// Method called from popup menu 'Copy'
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Copy();
        }

        /// <summary>
        /// Load text into control FCTB
        /// </summary>
        /// <param name="text">any text</param>
        private void LoadText(string text)
        {
            // create styled text and apply coloring
            var styledText = new StyledText(text);

            // create our custom TextSource
            var ts = new DynamicTextSource(this);
            // load string into component
            ts.Load(styledText);
            // assign TextSource to the component
            TextSource = ts;
        }


        #endregion

        #region Override Properties

        /// <summary>
        /// Text of control
        /// </summary>
        [Browsable(true)]
        [Localizable(true)]
        [Editor(
            "System.ComponentModel.Design.MultilineStringEditor, System.Design, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"
            , typeof(UITypeEditor))]
        [SettingsBindable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Description("Text of the control.")]
        [Bindable(true)]
        public override string Text
        {
            get
            {
                var source = (TextSource as DynamicTextSource);
                if (source == null)
                {
                    return "";
                }
                return source.StyledText.Text;
            }

            set { LoadText(value); }
        }

        #endregion

        #region Override Methods

        /// <summary>
        /// Copy text to buffer and remove line breaks
        /// </summary>
        /// <param name="data"></param>
        protected override void OnCreateClipboardData(DataObject data)
        {
            string selectedText = Selection.Text
                .Replace("\r\n", "")
                .Replace("\n", "");
            data.SetData(DataFormats.UnicodeText, true, selectedText);
        }


        #endregion

        #region ScrollBar (custom implemetation)

        private void vScrollBar_Scroll(object sender, ScrollEventArgs e)
        {
            this.OnScroll(e, e.Type != ScrollEventType.ThumbTrack && e.Type != ScrollEventType.ThumbPosition);
        }

        private void DNAFastColoredTextBox_ScrollbarsUpdated(object sender, EventArgs e)
        {
            AdjustScrollbar(vScrollBar, VerticalScroll.Maximum, VerticalScroll.Value, ClientSize.Height);
        }


        /// <summary>
        /// This method for System.Windows.Forms.ScrollBar and inherited classes
        /// </summary>
        private void AdjustScrollbar(ScrollBar scrollBar, int max, int value, int clientSize)
        {
            scrollBar.LargeChange = clientSize / 3;
            scrollBar.SmallChange = clientSize / 11;
            scrollBar.Maximum = max + scrollBar.LargeChange;
            scrollBar.Visible = max > 0;
            scrollBar.Value = Math.Min(scrollBar.Maximum, value);
        }

        #endregion

        #region Speed Optimization for sizing control

        /// <summary>
        /// Update LineInfos items to have same length as 'lines'
        /// </summary>
        /// <param name="newcount">count of 'lines' - same should be a length of LineInfos</param>
        public void UpdateLineInfos(int newcount)
        {
            // here is a speed optimization - if we have already more items in 'LineInfos' than in 'lines' - do nothing
            if (LineInfos.Count >= newcount)
            {
                // we dont need to remove items from this list - data is same for all items except field 'startY'
                //  but even this field will be same for same indexes, so we can leave already calculated items as is
                //Debug.WriteLine("UpdateLineInfos  skiped   newcount =  " + newcount);
                return;
            }

            int charHeight = CharHeight;
            LineInfos.Capacity = newcount;
            int paddingTop = Paddings.Top;
            // add items to LineInfos to enshure length is same as 'lines' has
            for (int i = LineInfos.Count; i < newcount; i++)
            {
                int startY = i*charHeight + paddingTop;
                LineInfos.Add(new LineInfo(startY));                
            }
        }

        /// <summary>
        /// Helper method for 'RecalcMy'
        /// </summary>
        private void RecalcMaxLineLengthMy()
        {
            int count = TextSource.Count;
            UpdateLineInfos(count);
            TextHeight = TextSource.Count * CharHeight;
        }

        /// <summary>
        /// Same functionality as base method 'Recalc' with that difference that is a way faster
        /// It is opimized specially for DNA text.
        /// </summary>
        public void RecalcMy()
        {
            var watch = Stopwatch.StartNew();
            RecalcMaxLineLengthMy();
            UpdateScrollbars();
            watch.Stop(); Console.WriteLine("RecalcMy: " + watch.ElapsedMilliseconds);
        }

        #endregion
    }
}
