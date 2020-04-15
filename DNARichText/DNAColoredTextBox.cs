using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Design;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using FastColoredTextBoxNS;
using Microsoft.Win32;
using Char = System.Char;
using Timer = System.Windows.Forms.Timer;

namespace DNARichText
{
    public partial class DNAColoredTextBox : UserControl, ISupportInitialize
    {
        private const int WM_IME_SETCONTEXT = 0x0281;
        private const int WM_HSCROLL = 0x114;
        private const int WM_VSCROLL = 0x115;
        private const int SB_ENDSCROLL = 0x8;

        
        private readonly Range selection;
        public int TextHeight;
        private Brush backBrush;
        private int charHeight;
        private int lineInterval;
        private TextSource lines;
        private int maxLineLength;
        protected bool needRecalc;
        private Color paddingBackColor;
        private bool scrollBars;
        private Color selectionColor;
        private int updating;
        private Range updatingRange;
        private Range visibleRange;

        bool findCharMode;
        private Keys lastModifiers;
        private bool handledChar;
 
        /// <summary>
        /// Constructor
        /// </summary>
        public DNAColoredTextBox()
        {
            //drawing optimization
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.ResizeRedraw, true);
            //append monospace font
            //Font = new Font("Consolas", 9.75f, FontStyle.Regular, GraphicsUnit.Point);
            Font = new Font(FontFamily.GenericMonospace, 9.75f);
            //create one line
            InitTextSource(CreateTextSource());
            selection = new Range(this) {Start = new Place(0, 0)};
            //default settings
            BackColor = Color.White;
            SelectionColor = Color.Blue;
            needRecalc = true;
            scrollBars = true;
            Paddings = new Padding(0, 0, 0, 0);
            textAreaBorder = TextAreaBorderType.None;
            textAreaBorderColor = Color.Black;
            base.AutoScroll = true;
        }

        # region TextArea visual properties
        Color textAreaBorderColor;

        /// <summary>
        /// Color of border of text area
        /// </summary>
        [DefaultValue(typeof(Color), "Black")]
        [Description("Color of border of text area")]
        public Color TextAreaBorderColor
        {
            get { return textAreaBorderColor; }
            set
            {
                textAreaBorderColor = value;
                Invalidate();
            }
        }
        
        TextAreaBorderType textAreaBorder;
        /// <summary>
        /// Type of border of text area
        /// </summary>
        [DefaultValue(typeof(TextAreaBorderType), "None")]
        [Description("Type of border of text area")]
        public TextAreaBorderType TextAreaBorder
        {
            get { return textAreaBorder; }
            set
            {
                textAreaBorder = value;
                Invalidate();
            }
        }

        /// <summary>
        /// Fore color (default style color)
        /// </summary>
        public override Color ForeColor
        {
            get { return base.ForeColor; }
            set
            {
                base.ForeColor = value;
                Invalidate();
            }
        }

        /// <summary>
        /// Padings of text area
        /// </summary>
        [Browsable(true)]
        [Description("Paddings of text area.")]
        public Padding Paddings { get; set; }

        /// <summary>
        /// Background color.
        /// It is used if BackBrush is null.
        /// </summary>
        [DefaultValue(typeof(Color), "White")]
        [Description("Background color.")]
        public override Color BackColor
        {
            get { return base.BackColor; }
            set { base.BackColor = value; }
        }

        /// <summary>
        /// Background brush.
        /// If Null then BackColor is used.
        /// </summary>
        [Browsable(false)]
        public Brush BackBrush
        {
            get { return backBrush; }
            set
            {
                backBrush = value;
                Invalidate();
            }
        }

        /// <summary>
        /// Left distance to text beginning
        /// </summary>
        [DefaultValue(0)]
        [Description("Left distance to text beginning.")]
        public int LeftIndent { get; set; }

        #endregion

        # region Options
        /// <summary>
        /// Height of char in pixels (includes LineInterval)
        /// </summary>
        [Browsable(false)]
        public int CharHeight
        {
            get { return charHeight; }
            set
            {
                charHeight = value;
                NeedRecalc();
                OnCharSizeChanged();
            }
        }

        /// <summary>
        /// Width of char in pixels
        /// </summary>
        [Browsable(false)]
        public int CharWidth { get; set; }

        /// <summary>
        /// Interval between lines (in pixels)
        /// </summary>
        [Description("Interval between lines in pixels")]
        [DefaultValue(0)]
        public int LineInterval
        {
            get { return lineInterval; }
            set
            {
                lineInterval = value;
                SetFont(Font);
                Invalidate();
            }
        }


        #endregion

        # region Properties
        /// <summary>
        /// Rectangle where located text
        /// </summary>
        [Browsable(false)]
        public Rectangle TextAreaRect
        {
            get
            {
                int rightPaddingStartX = LeftIndent + maxLineLength * CharWidth + Paddings.Left + 1;
                rightPaddingStartX = Math.Max(ClientSize.Width - Paddings.Right, rightPaddingStartX);
                int bottomPaddingStartY = TextHeight + Paddings.Top;
                bottomPaddingStartY = Math.Max(ClientSize.Height - Paddings.Bottom, bottomPaddingStartY);

                var top = Math.Max(0, Paddings.Top - 1) - VerticalScroll.Value;
                var left = LeftIndent - HorizontalScroll.Value - 2 + Math.Max(0, Paddings.Left - 1);
                var rect = Rectangle.FromLTRB(left, top, rightPaddingStartX - HorizontalScroll.Value, bottomPaddingStartY - VerticalScroll.Value);
                return rect;
            }
        }


        #endregion


        /// <summary>
        /// --Do not use this property--
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden),
         EditorBrowsable(EditorBrowsableState.Never)]
        public new Padding Padding
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Styles
        /// </summary>
        [Browsable(false)]
        public Style[] Styles
        {
            get { return lines.Styles; }
        }

        /// <summary>
        /// Default text style
        /// This style is using when no one other TextStyle is not defined in Char.style
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public TextStyle DefaultStyle
        {
            get { return lines.DefaultStyle; }
            set { lines.DefaultStyle = value; }
        }

        /// <summary>
        /// Style for rendering Selection area
        /// </summary>
        [Browsable(false)]
        public SelectionStyle SelectionStyle { get; set; }

        /// <summary>
        /// TextSource
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public TextSource TextSource
        {
            get { return lines; }
            set { InitTextSource(value); }
        }

        /// <summary>
        /// Returns current visible range of text
        /// </summary>
        [Browsable(false)]
        public Range VisibleRange
        {
            get
            {
                if (visibleRange != null)
                    return visibleRange;
                return GetRange(
                    PointToPlace(new Point(LeftIndent, 0)),
                    PointToPlace(new Point(ClientSize.Width, ClientSize.Height))
                    );
            }
        }

        /// <summary>
        /// Current selection range
        /// </summary>
        [Browsable(false)]
        public Range Selection
        {
            get { return selection; }
            set
            {
                if (value == selection)
                    return;

                selection.BeginUpdate();
                selection.Start = value.Start;
                selection.End = value.End;
                selection.EndUpdate();
                Invalidate();
            }
        }


        /// <summary>
        /// Do not change this property
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public override bool AutoScroll
        {
            get { return base.AutoScroll; }
            set { ; }
        }

        /// <summary>
        /// Count of lines
        /// </summary>
        [Browsable(false)]
        public int LinesCount
        {
            get { return lines.Count; }
        }

        /// <summary>
        /// Gets or sets char and styleId for given place
        /// This property does not fire OnTextChanged event
        /// </summary>
        public Char this[Place place]
        {
            get { return lines[place.iLine][place.iChar]; }
            set { lines[place.iLine][place.iChar] = value; }
        }

        /// <summary>
        /// Gets Line
        /// </summary>
        public Line this[int iLine]
        {
            get { return lines[iLine]; }
        }

        /// <summary>
        /// Text of control
        /// </summary>
        [Browsable(true)]
        [Localizable(true)]
        [Editor(
            "System.ComponentModel.Design.MultilineStringEditor, System.Design, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"
            , typeof (UITypeEditor))]
        [SettingsBindable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Description("Text of the control.")]
        [Bindable(true)]
        public override string Text
        {
            get
            {
                if (LinesCount == 0)
                    return "";
                var sel = new Range(this);
                sel.SelectAll();
                return sel.Text;
            }

            set
            {
                if (value == Text && value != "")
                    return;

                SetAsCurrentTB();

                Selection.ColumnSelectionMode = false;

                Selection.BeginUpdate();
                try
                {
                    Selection.SelectAll();
                    InsertText(value);
                    GoHome();
                }
                finally
                {
                    Selection.EndUpdate();
                }
            }
        }

        /// <summary>
        /// Text lines
        /// </summary>
        [Browsable(false)]
        public IList<string> Lines
        {
            get { return lines.GetLines(); }
        }

        /// <summary>
        /// Gets colored text as HTML
        /// </summary>
        /// <remarks>For more flexibility you can use ExportToHTML class also</remarks>
        [Browsable(false)]
        public string Html
        {
            get
            {
                var exporter = new ExportToHTML();
                exporter.UseNbsp = false;
                exporter.UseStyleTag = false;
                exporter.UseBr = false;
                return "<pre>" + exporter.GetHtml(this) + "</pre>";
            }
        }

        /// <summary>
        /// Gets colored text as RTF
        /// </summary>
        /// <remarks>For more flexibility you can use ExportToRTF class also</remarks>
        [Browsable(false)]
        public string Rtf
        {
            get
            {
                var exporter = new ExportToRTF();
                return exporter.GetRtf(this);
            }
        }

        /// <summary>
        /// Text of current selection
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string SelectedText
        {
            get { return Selection.Text; }
            set { InsertText(value); }
        }

        /// <summary>
        /// Start position of selection
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int SelectionStart
        {
            get { return Math.Min(PlaceToPosition(Selection.Start), PlaceToPosition(Selection.End)); }
            set { Selection.Start = PositionToPlace(value); }
        }

        /// <summary>
        /// Length of selected text
        /// </summary>
        [Browsable(false)]
        [DefaultValue(0)]
        public int SelectionLength
        {
            get { return Math.Abs(PlaceToPosition(Selection.Start) - PlaceToPosition(Selection.End)); }
            set
            {
                if (value > 0)
                    Selection.End = PositionToPlace(SelectionStart + value);
            }
        }

        /// <summary>
        /// Font
        /// </summary>
        /// <remarks>Use only monospaced font</remarks>
        [DefaultValue(typeof (Font), "Courier New, 9.75")]
        public override Font Font
        {
            get { return BaseFont; }
            set {
                originalFont = (Font)value.Clone();
                SetFont(value);
            }
        }


        Font baseFont;
        /// <summary>
        /// Font
        /// </summary>
        /// <remarks>Use only monospaced font</remarks>
        [DefaultValue(typeof(Font), "Courier New, 9.75")]
        private Font BaseFont
        {
            get { return baseFont; }
            set
            {
                baseFont = value;
            }
        }

        private void SetFont(Font newFont)
        {
            BaseFont = newFont;
            //check monospace font
            SizeF sizeM = GetCharSize(BaseFont, 'M');
            SizeF sizeDot = GetCharSize(BaseFont, '.');
            if (sizeM != sizeDot)
                BaseFont = new Font("Courier New", BaseFont.SizeInPoints, FontStyle.Regular, GraphicsUnit.Point);
            //clac size
            SizeF size = GetCharSize(BaseFont, 'M');
            CharWidth = (int) Math.Round(size.Width*1f /*0.85*/) - 1 /*0*/;
            CharHeight = lineInterval + (int) Math.Round(size.Height*1f /*0.9*/) - 1 /*0*/;
            
            NeedRecalc();
            Invalidate();
        }



        private int LeftIndentLine
        {
            get { return LeftIndent - minLeftIndent/2 - 3; }
        }

        /// <summary>
        /// Range of all text
        /// </summary>
        [Browsable(false)]
        public Range Range
        {
            get { return new Range(this, new Place(0, 0), new Place(lines[lines.Count - 1].Count, lines.Count - 1)); }
        }

        /// <summary>
        /// Color of selected area
        /// </summary>
        [DefaultValue(typeof (Color), "Blue")]
        [Description("Color of selected area.")]
        public virtual Color SelectionColor
        {
            get { return selectionColor; }
            set
            {
                selectionColor = value;
                if (selectionColor.A == 255)
                    selectionColor = Color.FromArgb(60, selectionColor);
                SelectionStyle = new SelectionStyle(new SolidBrush(selectionColor));
                Invalidate();
            }
        }

        public override Cursor Cursor
        {
            get { return base.Cursor; }
            set
            {
                defaultCursor = value;
                base.Cursor = value;
            }
        }


        /// <summary>
        /// Occurs when VisibleRange is changed
        /// </summary>
        public virtual void OnVisibleRangeChanged()
        {
            needRecalcFoldingLines = true;

            needRiseVisibleRangeChangedDelayed = true;
            ResetTimer(timer);
            if (VisibleRangeChanged != null)
                VisibleRangeChanged(this, new EventArgs());
        }

        /// <summary>
        /// Invalidates the entire surface of the control and causes the control to be redrawn.
        /// This method is thread safe and does not require Invoke.
        /// </summary>
        public new void Invalidate()
        {
            if (InvokeRequired)
                BeginInvoke(new MethodInvoker(Invalidate));
            else
                base.Invalidate();
        }

        protected virtual void OnCharSizeChanged()
        {
            VerticalScroll.SmallChange = charHeight;
            VerticalScroll.LargeChange = 10*charHeight;
            HorizontalScroll.SmallChange = CharWidth;
        }

        /// <summary>
        /// Occurs when scroolbars are updated
        /// </summary>
        [Browsable(true)]
        [Description("Occurs when scroolbars are updated.")]
        public event EventHandler ScrollbarsUpdated;


        protected void InitTextSource(TextSource ts)
        {
            if (lines != null)
            {
                lines.Dispose();
            }

            lines = ts;


            needRecalc = true;
        }


        /// <summary>
        /// Call this method if the recalc of the position of lines is needed.
        /// </summary>
        public void NeedRecalc(bool forced = false)
        {
            needRecalc = true;

            if (forced)
                Recalc();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            OnScrollbarsUpdated();
        }


        /// <summary>
        /// Gets length of given line
        /// </summary>
        /// <param name="iLine">Line index</param>
        /// <returns>Length of line</returns>
        public int GetLineLength(int iLine)
        {
            if (iLine < 0 || iLine >= lines.Count)
                throw new ArgumentOutOfRangeException("Line index out of range");

            return lines[iLine].Count;
        }

        /// <summary>
        /// Get range of line
        /// </summary>
        /// <param name="iLine">Line index</param>
        public Range GetLine(int iLine)
        {
            if (iLine < 0 || iLine >= lines.Count)
                throw new ArgumentOutOfRangeException("Line index out of range");

            var sel = new Range(this);
            sel.Start = new Place(0, iLine);
            sel.End = new Place(lines[iLine].Count, iLine);
            return sel;
        }

        #region Clipboard
        /// <summary>
        /// Copy selected text into Clipboard
        /// </summary>
        public virtual void Copy(bool copyLineBreaks = true)
        {
            if (Selection.IsEmpty)
                Selection.Expand();
            if (!Selection.IsEmpty)
            {
                var data = new DataObject();
                OnCreateClipboardData(data, copyLineBreaks);
                ClipboardUtils.SetData(data);
            }
        }

        protected virtual void OnCreateClipboardData(DataObject data, bool copyLineBreaks = true)
        {
            string text = Selection.GetText(copyLineBreaks);
            data.SetData(DataFormats.UnicodeText, true, text);
        }
        #endregion

        #region Key-Command

        /// <summary>
        /// Move caret to end of text
        /// </summary>
        public void GoEnd()
        {
            if (lines.Count > 0)
                Selection.Start = new Place(lines[lines.Count - 1].Count, lines.Count - 1);
            else
                Selection.Start = new Place(0, 0);

            DoCaretVisible();
        }

        /// <summary>
        /// Move caret to first position
        /// </summary>
        public void GoHome()
        {
            Selection.Start = new Place(0, 0);
            DoCaretVisible();
        }

        #endregion

        #region Selection
        /// <summary>
        /// Select all chars of text
        /// </summary>
        public void SelectAll()
        {
            Selection.SelectAll();
        }
        #endregion

        /// <summary>
        /// Clear style of all text
        /// </summary>
        public void ClearStyle(StyleIndex styleIndex)
        {
            foreach (Line line in lines)
                line.ClearStyle(styleIndex);

            for (int i = 0; i < LineInfos.Count; i++)
                SetVisibleState(i, VisibleState.Visible);

            Invalidate();
        }


        public static SizeF GetCharSize(Font font, char c)
        {
            Size sz2 = TextRenderer.MeasureText("<" + c.ToString() + ">", font);
            Size sz3 = TextRenderer.MeasureText("<>", font);

            return new SizeF(sz2.Width - sz3.Width + 1, /*sz2.Height*/font.Height);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HSCROLL || m.Msg == WM_VSCROLL)
                if (m.WParam.ToInt32() != SB_ENDSCROLL)
                    Invalidate();
            
            base.WndProc(ref m);
        }

        public void OnScroll(ScrollEventArgs se, bool alignByLines)
        {
            if (se.ScrollOrientation == ScrollOrientation.VerticalScroll)
            {
                //align by line height
                int newValue = se.NewValue;
                //Console.WriteLine("newValue = " + newValue.ToString());
                if (alignByLines)
                    newValue = (int)(Math.Ceiling(1d * newValue / CharHeight) * CharHeight);
                //
                VerticalScroll.Value = Math.Max(VerticalScroll.Minimum, Math.Min(VerticalScroll.Maximum, newValue));
            }
            if (se.ScrollOrientation == ScrollOrientation.HorizontalScroll)
                HorizontalScroll.Value = Math.Max(HorizontalScroll.Minimum, Math.Min(HorizontalScroll.Maximum, se.NewValue));

            UpdateScrollbars();

            Invalidate();
            //
            base.OnScroll(se);
            OnVisibleRangeChanged();
        }

        protected override void OnScroll(ScrollEventArgs se)
        {
            OnScroll(se, true);
        }


        private void Recalc()
        {
           
            if (!needRecalc)
                return;

            Debug.WriteLine("!!! Recalc    LinesCount=" + LinesCount);

#if debug
            var sw = Stopwatch.StartNew();
#endif

            needRecalc = false;
            //calc min left indent
            LeftIndent = LeftPadding;
            long maxLineNumber = LinesCount + lineNumberStartValue - 1;
            int charsForLineNumber = 2 + (maxLineNumber > 0 ? (int) Math.Log10(maxLineNumber) : 0);

            // If there are reserved character for line numbers: correct this
            if (this.ReservedCountOfLineNumberChars + 1 > charsForLineNumber)
                charsForLineNumber = this.ReservedCountOfLineNumberChars + 1;

            if (Created)
            {
                if (ShowLineNumbers)
                    LeftIndent += charsForLineNumber*CharWidth + minLeftIndent + 1;

                //calc wordwrapping
                if (needRecalcWordWrap)
                {
                    RecalcWordWrap(needRecalcWordWrapInterval.X, needRecalcWordWrapInterval.Y);
                    needRecalcWordWrap = false;
                }
            }
            else
                needRecalc = true;

            //calc max line length and count of wordWrapLines
            TextHeight = 0;

            //adjust AutoScrollMinSize
            int minWidth = 0;
                RecalcMaxLineLength(false);
            
            AutoScrollMinSize = new Size(minWidth, TextHeight + Paddings.Top + Paddings.Bottom);
            Debug.WriteLine("AutoScrollMinSize = " + AutoScrollMinSize.ToString() + "   Width = " + Width);
            UpdateScrollbars();
#if debug
            sw.Stop();
            Console.WriteLine("Recalc: " + sw.ElapsedMilliseconds);
#endif
        }

        private void CalcMinAutosizeWidth(out int minWidth, ref int maxLineLength)
        {
            //adjust AutoScrollMinSize
            minWidth = LeftIndent + (maxLineLength)*CharWidth + 2 + Paddings.Left + Paddings.Right;
            if (wordWrap)
                switch (WordWrapMode)
                {
                    case WordWrapMode.WordWrapControlWidth:
                    case WordWrapMode.CharWrapControlWidth:
                        maxLineLength = Math.Min(maxLineLength,
                                                 (ClientSize.Width - LeftIndent - Paddings.Left - Paddings.Right)/
                                                 CharWidth);
                        minWidth = 0;
                        break;
                    case WordWrapMode.WordWrapPreferredWidth:
                    case WordWrapMode.CharWrapPreferredWidth:
                        maxLineLength = Math.Min(maxLineLength, PreferredLineWidth);
                        minWidth = LeftIndent + PreferredLineWidth*CharWidth + 2 + Paddings.Left + Paddings.Right;
                        break;
                }
        }

        private void RecalcScrollByOneLine(int iLine)
        {
            if (iLine >= lines.Count)
                return;

            int maxLineLength = lines[iLine].Count;
            if (this.maxLineLength < maxLineLength && !WordWrap)
                this.maxLineLength = maxLineLength;

            int minWidth;
            CalcMinAutosizeWidth(out minWidth, ref maxLineLength);

            if (AutoScrollMinSize.Width < minWidth)
                AutoScrollMinSize = new Size(minWidth, AutoScrollMinSize.Height);
        }

         protected override void OnClientSizeChanged(EventArgs e)
        {
            base.OnClientSizeChanged(e);
            OnVisibleRangeChanged();
            UpdateScrollbars();
        }

        /// <summary>
        /// Scroll control for display defined rectangle
        /// </summary>
        /// <param name="rect"></param>
        private void DoVisibleRectangle(Rectangle rect)
        {
            int oldV = VerticalScroll.Value;
            int v = VerticalScroll.Value;
            int h = HorizontalScroll.Value;

            if (rect.Bottom > ClientRectangle.Height)
                v += rect.Bottom - ClientRectangle.Height;
            else if (rect.Top < 0)
                v += rect.Top;

            if (rect.Right > ClientRectangle.Width)
                h += rect.Right - ClientRectangle.Width;
            else if (rect.Left < LeftIndent)
                h += rect.Left - LeftIndent;
            //
            if (!Multiline)
                v = 0;
            //
            v = Math.Max(VerticalScroll.Minimum, v); // was 0
            h = Math.Max(HorizontalScroll.Minimum, h); // was 0
            //
            try
            {
                if (VerticalScroll.Visible || !ShowScrollBars)
                    VerticalScroll.Value = Math.Min(v, VerticalScroll.Maximum);
                if (HorizontalScroll.Visible || !ShowScrollBars)
                    HorizontalScroll.Value = Math.Min(h, HorizontalScroll.Maximum);
            }
            catch (ArgumentOutOfRangeException)
            {
                ;
            }

            UpdateScrollbars();
            //
            if (oldV != VerticalScroll.Value)
                OnVisibleRangeChanged();
        }

        /// <summary>
        /// Updates scrollbar position after Value changed
        /// </summary>
        public void UpdateScrollbars()
        {
            if(IsHandleCreated)
                BeginInvoke((MethodInvoker)OnScrollbarsUpdated);
        }

        protected virtual void OnScrollbarsUpdated()
        {           
            if (ScrollbarsUpdated != null)
                ScrollbarsUpdated(this, EventArgs.Empty);
        }

        /// <summary>
        /// Scroll control for display caret
        /// </summary>
        public void DoCaretVisible()
        {
            Invalidate();
            Recalc();
            Point car = PlaceToPoint(Selection.Start);
            car.Offset(-CharWidth, 0);
            DoVisibleRectangle(new Rectangle(car, new Size(2*CharWidth, 2*CharHeight)));
        }

        /// <summary>
        /// Scroll control for display selection area
        /// </summary>
        public void DoSelectionVisible()
        {
            if (LineInfos[Selection.End.iLine].VisibleState != VisibleState.Visible)
                ExpandBlock(Selection.End.iLine);

            if (LineInfos[Selection.Start.iLine].VisibleState != VisibleState.Visible)
                ExpandBlock(Selection.Start.iLine);

            Recalc();
            DoVisibleRectangle(new Rectangle(PlaceToPoint(new Place(0, Selection.End.iLine)),
                                             new Size(2*CharWidth, 2*CharHeight)));

            Point car = PlaceToPoint(Selection.Start);
            Point car2 = PlaceToPoint(Selection.End);
            car.Offset(-CharWidth, -ClientSize.Height/2);
            DoVisibleRectangle(new Rectangle(car, new Size(Math.Abs(car2.X - car.X), ClientSize.Height)));
            //Math.Abs(car2.Y-car.Y) + 2 * CharHeight

            Invalidate();
        }

        /// <summary>
        /// Scroll control for display given range
        /// </summary>
        public void DoRangeVisible(Range range)
        {
            DoRangeVisible(range, false);
        }

        /// <summary>
        /// Scroll control for display given range
        /// </summary>
        public void DoRangeVisible(Range range, bool tryToCentre)
        {
            range = range.Clone();
            range.Normalize();
            range.End = new Place(range.End.iChar,
                                  Math.Min(range.End.iLine, range.Start.iLine + ClientSize.Height/CharHeight));

            if (LineInfos[range.End.iLine].VisibleState != VisibleState.Visible)
                ExpandBlock(range.End.iLine);

            if (LineInfos[range.Start.iLine].VisibleState != VisibleState.Visible)
                ExpandBlock(range.Start.iLine);

            Recalc();
            int h = (1 + range.End.iLine - range.Start.iLine)*CharHeight;
            Point p = PlaceToPoint(new Place(0, range.Start.iLine));
            if (tryToCentre)
            {
                p.Offset(0, -ClientSize.Height/2);
                h = ClientSize.Height;
            }
            DoVisibleRectangle(new Rectangle(p, new Size(2*CharWidth, h)));

            Invalidate();
        }


        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);

            if (e.KeyCode == Keys.ShiftKey)
                lastModifiers &= ~Keys.Shift;
            if (e.KeyCode == Keys.Alt)
                lastModifiers &= ~Keys.Alt;
            if (e.KeyCode == Keys.ControlKey)
                lastModifiers &= ~Keys.Control;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (Focused)//??? 
                lastModifiers = e.Modifiers;

            handledChar = false;

            if (e.Handled)
            {
                handledChar = true;
                return;
            }

            if (ProcessKey(e.KeyData))
                return;

            e.Handled = true;

            DoCaretVisible();
            Invalidate();
        }

        /// <summary>
        /// Process control keys
        /// </summary>
        public virtual bool ProcessKey(Keys keyData)
        {
            KeyEventArgs a = new KeyEventArgs(keyData);

            if(a.KeyCode == Keys.Tab && !AcceptsTab)
                 return false;


            if (HotkeysMapping.ContainsKey(keyData))
            {
                var act = HotkeysMapping[keyData];
                DoAction(act);
                if (scrollActions.ContainsKey(act))
                    return true;
                if (keyData == Keys.Tab || keyData == (Keys.Tab | Keys.Shift))
                {
                    handledChar = true;
                    return true;
                }
            }
            else
            {
                //
                if (a.KeyCode == Keys.Alt)
                    return true;

                if ((a.Modifiers & Keys.Control) != 0)
                    return true;

                if ((a.Modifiers & Keys.Alt) != 0)
                {
                    if ((MouseButtons & MouseButtons.Left) != 0)
                        CheckAndChangeSelectionType();
                    return true;
                }

                if (a.KeyCode == Keys.ShiftKey)
                    return true;
            }

            return false;
        }

        private void DoAction(FCTBAction action)
        {
            switch (action)
            {
                case FCTBAction.ZoomIn:
                    ChangeFontSize(2);
                    break;
                case FCTBAction.ZoomOut:
                    ChangeFontSize(-2);
                    break;
                case FCTBAction.ZoomNormal:
                    RestoreFontSize();
                    break;
                case FCTBAction.ScrollDown:
                    DoScrollVertical(1, -1);
                    break;

                case FCTBAction.ScrollUp:
                    DoScrollVertical(1, 1);
                    break;

                case FCTBAction.GoToDialog:
                    ShowGoToDialog();
                    break;

                case FCTBAction.FindDialog:
                    ShowFindDialog();
                    break;

                case FCTBAction.FindChar:
                    findCharMode = true;
                    break;

                case FCTBAction.FindNext:
                    if (findForm == null || findForm.tbFind.Text == "")
                        ShowFindDialog();
                    else
                        findForm.FindNext(findForm.tbFind.Text);
                    break;

                case FCTBAction.ReplaceDialog:
                    ShowReplaceDialog();
                    break;

                case FCTBAction.Copy:
                    Copy();
                    break;

                case FCTBAction.CommentSelected:
                    CommentSelected();
                    break;

                case FCTBAction.Cut:
                    if (!Selection.ReadOnly)
                        Cut();
                    break;

                case FCTBAction.Paste:
                    if (!Selection.ReadOnly)
                        Paste();
                    break;

                case FCTBAction.SelectAll:
                    Selection.SelectAll();
                    break;

                case FCTBAction.Undo:
                    if (!ReadOnly)
                        Undo();
                    break;

                case FCTBAction.Redo:
                    if (!ReadOnly)
                        Redo();
                    break;

                case FCTBAction.LowerCase:
                    if (!Selection.ReadOnly)
                        LowerCase();
                    break;

                case FCTBAction.UpperCase:
                    if (!Selection.ReadOnly)
                        UpperCase();
                    break;

                case FCTBAction.IndentDecrease:
                    if (!Selection.ReadOnly)
                    {
                        var sel = Selection.Clone();
                        if(sel.Start.iLine == sel.End.iLine)
                        {
                            var line = this[sel.Start.iLine];
                            if (sel.Start.iChar == 0 && sel.End.iChar == line.Count)
                                Selection = new Range(this, line.StartSpacesCount, sel.Start.iLine, line.Count, sel.Start.iLine);
                            else
                            if (sel.Start.iChar == line.Count && sel.End.iChar == 0)
                                Selection = new Range(this, line.Count, sel.Start.iLine, line.StartSpacesCount, sel.Start.iLine);
                        }


                        DecreaseIndent();
                    }
                    break;

                case FCTBAction.IndentIncrease:
                    if (!Selection.ReadOnly)
                    {
                        var sel = Selection.Clone();
                        var inverted = sel.Start > sel.End;
                        sel.Normalize();
                        var spaces = this[sel.Start.iLine].StartSpacesCount;
                        if (sel.Start.iLine != sel.End.iLine || //selected several lines
                           (sel.Start.iChar <= spaces && sel.End.iChar == this[sel.Start.iLine].Count) || //selected whole line
                           sel.End.iChar <= spaces)//selected space prefix
                        {
                            IncreaseIndent();
                            if (sel.Start.iLine == sel.End.iLine && !sel.IsEmpty)
                            {
                                Selection = new Range(this, this[sel.Start.iLine].StartSpacesCount, sel.End.iLine, this[sel.Start.iLine].Count, sel.End.iLine); //select whole line
                                if (inverted)
                                    Selection.Inverse();
                            }
                        }
                        else
                            ProcessKey('\t', Keys.None);
                    }
                    break;

                case FCTBAction.AutoIndentChars:
                    if (!Selection.ReadOnly)
                        DoAutoIndentChars(Selection.Start.iLine);
                    break;

                case FCTBAction.NavigateBackward:
                    NavigateBackward();
                    break;

                case FCTBAction.NavigateForward:
                    NavigateForward();
                    break;

                case FCTBAction.UnbookmarkLine:
                    UnbookmarkLine(Selection.Start.iLine);
                    break;

                case FCTBAction.BookmarkLine:
                    BookmarkLine(Selection.Start.iLine);
                    break;

                case FCTBAction.GoNextBookmark:
                    GotoNextBookmark(Selection.Start.iLine);
                    break;

                case FCTBAction.GoPrevBookmark:
                    GotoPrevBookmark(Selection.Start.iLine);
                    break;

                case FCTBAction.ClearWordLeft:
                    if (OnKeyPressing('\b')) //KeyPress event processed key
                        break;
                    if (!Selection.ReadOnly)
                    {
                        if (!Selection.IsEmpty)
                            ClearSelected();
                        Selection.GoWordLeft(true);
                        if (!Selection.ReadOnly)
                            ClearSelected();
                    }
                    OnKeyPressed('\b');
                    break;

                case FCTBAction.ReplaceMode:
                    if (!ReadOnly)
                        isReplaceMode = !isReplaceMode;
                    break;

                case FCTBAction.DeleteCharRight:
                    if (!Selection.ReadOnly)
                    {
                        if (OnKeyPressing((char) 0xff)) //KeyPress event processed key
                            break;
                        if (!Selection.IsEmpty)
                            ClearSelected();
                        else
                        {
                            //if line contains only spaces then delete line
                            if (this[Selection.Start.iLine].StartSpacesCount == this[Selection.Start.iLine].Count)
                                RemoveSpacesAfterCaret();

                            if (!Selection.IsReadOnlyRightChar())
                                if (Selection.GoRightThroughFolded())
                                {
                                    int iLine = Selection.Start.iLine;

                                    InsertChar('\b');

                                    //if removed \n then trim spaces
                                    if (iLine != Selection.Start.iLine && AutoIndent)
                                        if (Selection.Start.iChar > 0)
                                            RemoveSpacesAfterCaret();
                                }
                        }

                        if (AutoIndentChars)
                            DoAutoIndentChars(Selection.Start.iLine);

                        OnKeyPressed((char) 0xff);
                    }
                    break;

                case FCTBAction.ClearWordRight:
                    if (OnKeyPressing((char) 0xff)) //KeyPress event processed key
                        break;
                    if (!Selection.ReadOnly)
                    {
                        if (!Selection.IsEmpty)
                            ClearSelected();
                        Selection.GoWordRight(true);
                        if (!Selection.ReadOnly)
                            ClearSelected();
                    }
                    OnKeyPressed((char) 0xff);
                    break;

                case FCTBAction.GoWordLeft:
                    Selection.GoWordLeft(false);
                    break;

                case FCTBAction.GoWordLeftWithSelection:
                    Selection.GoWordLeft(true);
                    break;

                case FCTBAction.GoLeft:
                    Selection.GoLeft(false);
                    break;

                case FCTBAction.GoLeftWithSelection:
                    Selection.GoLeft(true);
                    break;

                case FCTBAction.GoLeft_ColumnSelectionMode:
                    CheckAndChangeSelectionType();
                    if (Selection.ColumnSelectionMode)
                        Selection.GoLeft_ColumnSelectionMode();
                    Invalidate();
                    break;

                case FCTBAction.GoWordRight:
                    Selection.GoWordRight(false, true);
                    break;

                case FCTBAction.GoWordRightWithSelection:
                    Selection.GoWordRight(true, true);
                    break;

                case FCTBAction.GoRight:
                    Selection.GoRight(false);
                    break;

                case FCTBAction.GoRightWithSelection:
                    Selection.GoRight(true);
                    break;

                case FCTBAction.GoRight_ColumnSelectionMode:
                    CheckAndChangeSelectionType();
                    if (Selection.ColumnSelectionMode)
                        Selection.GoRight_ColumnSelectionMode();
                    Invalidate();
                    break;

                case FCTBAction.GoUp:
                    Selection.GoUp(false);
                    ScrollLeft();
                    break;

                case FCTBAction.GoUpWithSelection:
                    Selection.GoUp(true);
                    ScrollLeft();
                    break;

                case FCTBAction.GoUp_ColumnSelectionMode:
                    CheckAndChangeSelectionType();
                    if (Selection.ColumnSelectionMode)
                        Selection.GoUp_ColumnSelectionMode();
                    Invalidate();
                    break;

                case FCTBAction.MoveSelectedLinesUp:
                    if (!Selection.ColumnSelectionMode)
                        MoveSelectedLinesUp();
                    break;

                case FCTBAction.GoDown:
                    Selection.GoDown(false);
                    ScrollLeft();
                    break;

                case FCTBAction.GoDownWithSelection:
                    Selection.GoDown(true);
                    ScrollLeft();
                    break;

                case FCTBAction.GoDown_ColumnSelectionMode:
                    CheckAndChangeSelectionType();
                    if (Selection.ColumnSelectionMode)
                        Selection.GoDown_ColumnSelectionMode();
                    Invalidate();
                    break;

                case FCTBAction.MoveSelectedLinesDown:
                    if (!Selection.ColumnSelectionMode)
                        MoveSelectedLinesDown();
                    break;
                case FCTBAction.GoPageUp:
                    Selection.GoPageUp(false);
                    ScrollLeft();
                    break;

                case FCTBAction.GoPageUpWithSelection:
                    Selection.GoPageUp(true);
                    ScrollLeft();
                    break;

                case FCTBAction.GoPageDown:
                    Selection.GoPageDown(false);
                    ScrollLeft();
                    break;

                case FCTBAction.GoPageDownWithSelection:
                    Selection.GoPageDown(true);
                    ScrollLeft();
                    break;

                case FCTBAction.GoFirstLine:
                    Selection.GoFirst(false);
                    break;

                case FCTBAction.GoFirstLineWithSelection:
                    Selection.GoFirst(true);
                    break;

                case FCTBAction.GoHome:
                    GoHome(false);
                    ScrollLeft();
                    break;

                case FCTBAction.GoHomeWithSelection:
                    GoHome(true);
                    ScrollLeft();
                    break;

                case FCTBAction.GoLastLine:
                    Selection.GoLast(false);
                    break;

                case FCTBAction.GoLastLineWithSelection:
                    Selection.GoLast(true);
                    break;

                case FCTBAction.GoEnd:
                    Selection.GoEnd(false);
                    break;

                case FCTBAction.GoEndWithSelection:
                    Selection.GoEnd(true);
                    break;

                case FCTBAction.ClearHints:
                    ClearHints();
                    if(MacrosManager != null)
                        MacrosManager.IsRecording = false;
                    break;

                case FCTBAction.MacroRecord:
                    if(MacrosManager != null)
                    {
                        if (MacrosManager.AllowMacroRecordingByUser)
                            MacrosManager.IsRecording = !MacrosManager.IsRecording;
                        if (MacrosManager.IsRecording)
                            MacrosManager.ClearMacros();
                    }
                    break;

                case FCTBAction.MacroExecute:
                    if (MacrosManager != null)
                    {
                        MacrosManager.IsRecording = false;
                        MacrosManager.ExecuteMacros();
                    }
                    break;
                case FCTBAction.CustomAction1 :
                case FCTBAction.CustomAction2 :
                case FCTBAction.CustomAction3 :
                case FCTBAction.CustomAction4 :
                case FCTBAction.CustomAction5 :
                case FCTBAction.CustomAction6 :
                case FCTBAction.CustomAction7 :
                case FCTBAction.CustomAction8 :
                case FCTBAction.CustomAction9 :
                case FCTBAction.CustomAction10:
                case FCTBAction.CustomAction11:
                case FCTBAction.CustomAction12:
                case FCTBAction.CustomAction13:
                case FCTBAction.CustomAction14:
                case FCTBAction.CustomAction15:
                case FCTBAction.CustomAction16:
                case FCTBAction.CustomAction17:
                case FCTBAction.CustomAction18:
                case FCTBAction.CustomAction19:
                case FCTBAction.CustomAction20:
                    OnCustomAction(new CustomActionEventArgs(action));
                    break;
            }
        }

        protected virtual void OnCustomAction(CustomActionEventArgs e)
        {
            if (CustomAction != null)
                CustomAction(this, e);
        }

        Font originalFont;

        private void RestoreFontSize()
        {
            Zoom = 100;
        }

        private void GoHome(bool shift)
        {
            Selection.BeginUpdate();
            try
            {
                int iLine = Selection.Start.iLine;
                int spaces = this[iLine].StartSpacesCount;
                if (Selection.Start.iChar <= spaces)
                    Selection.GoHome(shift);
                else
                {
                    Selection.GoHome(shift);
                    for (int i = 0; i < spaces; i++)
                        Selection.GoRight(shift);
                }
            }
            finally
            {
                Selection.EndUpdate();
            }
        }

        /// <summary>
        /// Convert selected text to upper case
        /// </summary>
        public virtual void UpperCase()
        {
            Range old = Selection.Clone();
            SelectedText = SelectedText.ToUpper();
            Selection.Start = old.Start;
            Selection.End = old.End;
        }

        /// <summary>
        /// Convert selected text to lower case
        /// </summary>
        public virtual void LowerCase()
        {
            Range old = Selection.Clone();
            SelectedText = SelectedText.ToLower();
            Selection.Start = old.Start;
            Selection.End = old.End;
        }

        /// <summary>
        /// Convert selected text to title case
        /// </summary>
        public virtual void TitleCase()
        {
            Range old = Selection.Clone();
            SelectedText = Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(SelectedText.ToLower());
            Selection.Start = old.Start;
            Selection.End = old.End;
        }

        /// <summary>
        /// Insert/remove comment prefix into selected lines
        /// </summary>
        public void CommentSelected()
        {
            CommentSelected(CommentPrefix);
        }

        /// <summary>
        /// Insert/remove comment prefix into selected lines
        /// </summary>
        public virtual void CommentSelected(string commentPrefix)
        {
            if (string.IsNullOrEmpty(commentPrefix))
                return;
            Selection.Normalize();
            bool isCommented = lines[Selection.Start.iLine].Text.TrimStart().StartsWith(commentPrefix);
            if (isCommented)
                RemoveLinePrefix(commentPrefix);
            else
                InsertLinePrefix(commentPrefix);
        }

        public void OnKeyPressing(KeyPressEventArgs args)
        {
            if (KeyPressing != null)
                KeyPressing(this, args);
        }

        private bool OnKeyPressing(char c)
        {
            if (findCharMode)
            {
                findCharMode = false;
                FindChar(c);
                return true;
            }
            var args = new KeyPressEventArgs(c);
            OnKeyPressing(args);
            return args.Handled;
        }

        public void OnKeyPressed(char c)
        {
            var args = new KeyPressEventArgs(c);
            if (KeyPressed != null)
                KeyPressed(this, args);
        }

        protected override bool ProcessMnemonic(char charCode)
        {
            
            if (Focused)
                return ProcessKey(charCode, lastModifiers) || base.ProcessMnemonic(charCode);
            else
                return false;
        }

        const int WM_CHAR = 0x102;

        protected override bool ProcessKeyMessage(ref Message m)
        {
            if (m.Msg == WM_CHAR)
                ProcessMnemonic(Convert.ToChar(m.WParam.ToInt32()));

            return base.ProcessKeyMessage(ref m);
        }

        /// <summary>
        /// Process "real" keys (no control)
        /// </summary>
        public virtual bool ProcessKey(char c, Keys modifiers)
        {
            if (handledChar)
                return true;

            if (macrosManager != null)
                macrosManager.ProcessKey(c, modifiers);
            /*  !!!!
            if (c == ' ')
                return true;*/

            //backspace
            if (c == '\b' && (modifiers == Keys.None || modifiers == Keys.Shift || (modifiers & Keys.Alt) != 0))
            {
                if (ReadOnly || !Enabled)
                    return false;

                if (OnKeyPressing(c))
                    return true;

                if (Selection.ReadOnly)
                    return false;

                if (!Selection.IsEmpty)
                    ClearSelected();
                else
                    if (!Selection.IsReadOnlyLeftChar()) //is not left char readonly?
                        InsertChar('\b');

                if (AutoIndentChars)
                    DoAutoIndentChars(Selection.Start.iLine);

                OnKeyPressed('\b');
                return true;
            }
 
            /* !!!!
            if (c == '\b' && (modifiers & Keys.Alt) != 0)
                return true;*/

            if (char.IsControl(c) && c != '\r' && c != '\t')
                return false;

            if (ReadOnly || !Enabled)
                return false;


            if (modifiers != Keys.None &&
                modifiers != Keys.Shift &&
                modifiers != (Keys.Control | Keys.Alt) && //ALT+CTRL is special chars (AltGr)
                modifiers != (Keys.Shift | Keys.Control | Keys.Alt) && //SHIFT + ALT + CTRL is special chars (AltGr)
                (modifiers != (Keys.Alt) || char.IsLetterOrDigit(c)) //may be ALT+LetterOrDigit is mnemonic code
                )
                return false; //do not process Ctrl+? and Alt+? keys

            char sourceC = c;
            if (OnKeyPressing(sourceC)) //KeyPress event processed key
                return true;

            //
            if (Selection.ReadOnly)
                return false;
            //
            if (c == '\r' && !AcceptsReturn)
                return false;

            //replace \r on \n
            if (c == '\r')
                c = '\n';
            //replace mode? select forward char
            if (IsReplaceMode)
            {
                Selection.GoRight(true);
                Selection.Inverse();
            }
            //insert char
            if (!Selection.ReadOnly)
            {
                if (!DoAutocompleteBrackets(c))
                    InsertChar(c);
            }

            //do autoindent
            if (c == '\n' || AutoIndentExistingLines)
                DoAutoIndentIfNeed();

            if (AutoIndentChars)
                DoAutoIndentChars(Selection.Start.iLine);

            DoCaretVisible();
            Invalidate();

            OnKeyPressed(sourceC);

            return true;
        }

        #region AutoIndentChars

        /// <summary>
        /// Enables AutoIndentChars mode
        /// </summary>
        [Description("Enables AutoIndentChars mode")]
        [DefaultValue(true)]
        public bool AutoIndentChars { get; set; }

        /// <summary>
        /// Regex patterns for AutoIndentChars (one regex per line)
        /// </summary>
        [Description("Regex patterns for AutoIndentChars (one regex per line)")] 
        [Editor( "System.ComponentModel.Design.MultilineStringEditor, System.Design, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a" , typeof(UITypeEditor))]
        [DefaultValue(@"^\s*[\w\.]+\s*(?<range>=)\s*(?<range>[^;]+);")]
        public string AutoIndentCharsPatterns { get; set; }

        /// <summary>
        /// Do AutoIndentChars
        /// </summary>
        public void DoAutoIndentChars(int iLine)
        {
            var patterns = AutoIndentCharsPatterns.Split(new char[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries);

            foreach (var pattern in patterns)
            {
                var m = Regex.Match(this[iLine].Text, pattern);
                if (m.Success)
                {
                    DoAutoIndentChars(iLine, new Regex(pattern));
                    break;
                }
            }
        }

        protected void DoAutoIndentChars(int iLine, Regex regex)
        {
            var oldSel = Selection.Clone();

            var captures = new SortedDictionary<int, CaptureCollection>();
            var texts = new SortedDictionary<int, string>();
            var maxCapturesCount = 0;

            var spaces = this[iLine].StartSpacesCount;

            for(var i = iLine; i >= 0; i--)
            {
                if (spaces != this[i].StartSpacesCount)
                    break;

                var text = this[i].Text;
                var m = regex.Match(text);
                if (m.Success)
                {
                    captures[i] = m.Groups["range"].Captures;
                    texts[i] = text;

                    if (captures[i].Count > maxCapturesCount)
                        maxCapturesCount = captures[i].Count;
                }
                else
                    break;
            }

            for (var i = iLine + 1; i < LinesCount; i++)
            {
                if (spaces != this[i].StartSpacesCount)
                    break;

                var text = this[i].Text;
                var m = regex.Match(text);
                if (m.Success)
                {
                    captures[i] = m.Groups["range"].Captures;
                    texts[i] = text;

                    if (captures[i].Count > maxCapturesCount)
                        maxCapturesCount = captures[i].Count;

                }
                else
                    break;
            }

            var changed = new Dictionary<int, bool>();
            var was = false;

            for (int iCapture = maxCapturesCount - 1; iCapture >= 0; iCapture--)
            {
                //find max dist
                var maxDist = 0;
                foreach(var i in captures.Keys)
                {
                    var caps = captures[i];
                    if (caps.Count <= iCapture)
                        continue;
                    var dist = 0;
                    var cap = caps[iCapture];

                    var index = cap.Index;

                    var text = texts[i];
                    while (index > 0 && text[index - 1] == ' ') index--;

                    if (iCapture == 0)
                        dist = index;
                    else
                        dist = index - caps[iCapture - 1].Index - 1;

                    if (dist > maxDist)
                        maxDist = dist;
                }

                //insert whitespaces
                foreach(var i in new List<int>(texts.Keys))
                {
                    if (captures[i].Count <= iCapture)
                        continue;

                    var dist = 0;
                    var cap = captures[i][iCapture];

                    if (iCapture == 0)
                        dist = cap.Index;
                    else
                        dist = cap.Index - captures[i][iCapture - 1].Index - 1;

                    var addSpaces = maxDist - dist + 1;//+1 because min space count is 1

                    if (addSpaces == 0)
                        continue;

                    if (oldSel.Start.iLine == i && oldSel.Start.iChar > cap.Index)
                        oldSel.Start = new Place(oldSel.Start.iChar + addSpaces, i);

                    if (addSpaces > 0)
                        texts[i] = texts[i].Insert(cap.Index, new string(' ', addSpaces));
                    else
                        texts[i] = texts[i].Remove(cap.Index + addSpaces, -addSpaces);
                    
                    changed[i] = true;
                    was = true;
                }
            }

            //insert text
            if (was)
            {
                Selection.BeginUpdate();
                BeginAutoUndo();
                BeginUpdate();

                TextSource.Manager.ExecuteCommand(new SelectCommand(TextSource));

                foreach (var i in texts.Keys)
                if (changed.ContainsKey(i))
                {
                    Selection = new Range(this, 0, i, this[i].Count, i);
                    if(!Selection.ReadOnly)
                        InsertText(texts[i]);
                }

                Selection = oldSel;

                EndUpdate();
                EndAutoUndo();
                Selection.EndUpdate();
            }
        }

        #endregion

        private bool DoAutocompleteBrackets(char c)
        {
            if (AutoCompleteBrackets)
            {
                if (!Selection.ColumnSelectionMode)
                    for (int i = 1; i < autoCompleteBracketsList.Length; i += 2)
                        if (c == autoCompleteBracketsList[i] && c == Selection.CharAfterStart)
                        {
                            Selection.GoRight();
                            return true;
                        }

                for (int i = 0; i < autoCompleteBracketsList.Length; i += 2)
                    if (c == autoCompleteBracketsList[i])
                    {
                        InsertBrackets(autoCompleteBracketsList[i], autoCompleteBracketsList[i + 1]);
                        return true;
                    }
            }
            return false;
        }

        private bool InsertBrackets(char left, char right)
        {
            if (Selection.ColumnSelectionMode)
            {
                var range = Selection.Clone();
                range.Normalize();
                Selection.BeginUpdate();
                BeginAutoUndo();
                Selection = new Range(this, range.Start.iChar, range.Start.iLine, range.Start.iChar, range.End.iLine) { ColumnSelectionMode = true };
                InsertChar(left);
                Selection = new Range(this, range.End.iChar + 1, range.Start.iLine, range.End.iChar + 1, range.End.iLine) { ColumnSelectionMode = true };
                InsertChar(right);
                if (range.IsEmpty)
                    Selection = new Range(this, range.End.iChar + 1, range.Start.iLine, range.End.iChar + 1, range.End.iLine) { ColumnSelectionMode = true };
                EndAutoUndo();
                Selection.EndUpdate();
            }
            else
                if (Selection.IsEmpty)
                {
                    InsertText(left + "" + right);
                    Selection.GoLeft();
                }
                else
                    InsertText(left + SelectedText + right);

            return true;
        }

        /// <summary>
        /// Finds given char after current caret position, moves the caret to found pos.
        /// </summary>
        /// <param name="c"></param>
        protected virtual void FindChar(char c)
        {
            if (c == '\r')
                c = '\n';

            var r = Selection.Clone();
            while (r.GoRight())
            {
                if (r.CharBeforeStart == c)
                {
                    Selection = r;
                    DoCaretVisible();
                    return;
                }
            }
        }

        protected int GetMinStartSpacesCount(int fromLine, int toLine)
        {
            if (fromLine > toLine)
                return 0;

            int result = int.MaxValue;
            for (int i = fromLine; i <= toLine; i++)
            {
                int count = lines[i].StartSpacesCount;
                if (count < result)
                    result = count;
            }

            return result;
        }

        protected int GetMaxStartSpacesCount(int fromLine, int toLine)
        {
            if (fromLine > toLine)
                return 0;

            int result = 0;
            for (int i = fromLine; i <= toLine; i++)
            {
                int count = lines[i].StartSpacesCount;
                if (count > result)
                    result = count;
            }

            return result;
        }

        /// <summary>
        /// Undo last operation
        /// </summary>
        public virtual void Undo()
        {
            lines.Manager.Undo();
            DoCaretVisible();
            Invalidate();
        }

        /// <summary>
        /// Redo
        /// </summary>
        public virtual void Redo()
        {
            lines.Manager.Redo();
            DoCaretVisible();
            Invalidate();
        }

        protected override bool IsInputKey(Keys keyData)
        {
            if (keyData == Keys.Tab && !AcceptsTab)
                return false;
            if (keyData == Keys.Enter && !AcceptsReturn)
                return false;

            if ((keyData & Keys.Alt) == Keys.None)
            {
                Keys keys = keyData & Keys.KeyCode;
                if (keys == Keys.Return)
                    return true;
            }

            if ((keyData & Keys.Alt) != Keys.Alt)
            {
                switch ((keyData & Keys.KeyCode))
                {
                    case Keys.Prior:
                    case Keys.Next:
                    case Keys.End:
                    case Keys.Home:
                    case Keys.Left:
                    case Keys.Right:
                    case Keys.Up:
                    case Keys.Down:
                        return true;

                    case Keys.Escape:
                        return false;

                    case Keys.Tab:
                        return (keyData & Keys.Control) == Keys.None;
                }
            }

            return base.IsInputKey(keyData);
        }

        [DllImport("User32.dll")]
        private static extern bool CreateCaret(IntPtr hWnd, int hBitmap, int nWidth, int nHeight);

        [DllImport("User32.dll")]
        private static extern bool SetCaretPos(int x, int y);

        [DllImport("User32.dll")]
        private static extern bool DestroyCaret();

        [DllImport("User32.dll")]
        private static extern bool ShowCaret(IntPtr hWnd);

        [DllImport("User32.dll")]
        private static extern bool HideCaret(IntPtr hWnd);

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (BackBrush == null)
                base.OnPaintBackground(e);
            else
                e.Graphics.FillRectangle(BackBrush, ClientRectangle);
        }

        /// <summary>
        /// Draws text to given Graphics
        /// </summary>
        /// <param name="gr"></param>
        /// <param name="start">Start place of drawing text</param>
        /// <param name="size">Size of drawing</param>
        public void DrawText(Graphics gr, Place start, Size size)
        {
            if (needRecalc)
                Recalc();

            if (needRecalcFoldingLines)
                RecalcFoldingLines();

            var startPoint = PlaceToPoint(start);
            var startY = startPoint.Y + VerticalScroll.Value;
            var startX = startPoint.X + HorizontalScroll.Value - LeftIndent - Paddings.Left;
            int firstChar = start.iChar;
            int lastChar = (startX + size.Width) / CharWidth;

            var startLine = start.iLine;
            //draw text
            for (int iLine = startLine; iLine < lines.Count; iLine++)
            {
                Line line = lines[iLine];
                LineInfo lineInfo = LineInfos[iLine];
                //
                if (lineInfo.startY > startY + size.Height)
                    break;
                if (lineInfo.startY + lineInfo.WordWrapStringsCount * CharHeight < startY)
                    continue;
                if (lineInfo.VisibleState == VisibleState.Hidden)
                    continue;

                int y = lineInfo.startY - startY;
                //
                gr.SmoothingMode = SmoothingMode.None;
                //draw line background
                if (lineInfo.VisibleState == VisibleState.Visible)
                    if (line.BackgroundBrush != null)
                        gr.FillRectangle(line.BackgroundBrush, new Rectangle(0, y, size.Width, CharHeight * lineInfo.WordWrapStringsCount));
                //
                gr.SmoothingMode = SmoothingMode.AntiAlias;

                //draw wordwrap strings of line
                for (int iWordWrapLine = 0; iWordWrapLine < lineInfo.WordWrapStringsCount; iWordWrapLine++)
                {
                    y = lineInfo.startY + iWordWrapLine * CharHeight - startY;
                    //indent 
                    var indent = iWordWrapLine == 0 ? 0 : lineInfo.wordWrapIndent * CharWidth;
                    //draw chars
                    DrawLineChars(gr, firstChar, lastChar, iLine, iWordWrapLine, -startX + indent, y);
                }
            }
        }

        /// <summary>
        /// Draw control
        /// </summary>
        static int OnPaintCounts = 0;
        protected override void OnPaint(PaintEventArgs e)
        {
            OnPaintCounts++;
            Debug.WriteLine("OnPaint  " + OnPaintCounts.ToString());

            if (needRecalc)
                Recalc();

            if (needRecalcFoldingLines)
                RecalcFoldingLines();

            //Mupoc - eliminate blinking while sizeing control to smaller size
            if (LineInfos.Count > 0 && LineInfos[0].startY == -1) {
                // uninitialized - exit oterwise last line will be repainted
                return;
            }

#if debug
            var sw = Stopwatch.StartNew();
#endif
            visibleMarkers.Clear();
            
            e.Graphics.SmoothingMode = SmoothingMode.None;
            //
            var servicePen = new Pen(ServiceLinesColor);
            Brush changedLineBrush = new SolidBrush(ChangedLineColor);
            Brush indentBrush = new SolidBrush(IndentBackColor);
            Brush paddingBrush = new SolidBrush(PaddingBackColor);
            Brush currentLineBrush =
                new SolidBrush(Color.FromArgb(CurrentLineColor.A == 255 ? 50 : CurrentLineColor.A, CurrentLineColor));
            //draw padding area
            var textAreaRect = TextAreaRect;
            //top
            e.Graphics.FillRectangle(paddingBrush, 0, -VerticalScroll.Value, ClientSize.Width, Math.Max(0, Paddings.Top - 1));
            //bottom
            e.Graphics.FillRectangle(paddingBrush, 0, textAreaRect.Bottom, ClientSize.Width,ClientSize.Height);
            //right
            e.Graphics.FillRectangle(paddingBrush, textAreaRect.Right, 0, ClientSize.Width, ClientSize.Height);
            //left
            e.Graphics.FillRectangle(paddingBrush, LeftIndentLine, 0, LeftIndent - LeftIndentLine - 1, ClientSize.Height);
            if (HorizontalScroll.Value <= Paddings.Left)
                e.Graphics.FillRectangle(paddingBrush, LeftIndent - HorizontalScroll.Value - 2, 0,
                                         Math.Max(0, Paddings.Left - 1), ClientSize.Height);
            //
            int leftTextIndent = Math.Max(LeftIndent, LeftIndent + Paddings.Left - HorizontalScroll.Value);
            int textWidth = textAreaRect.Width;
            //draw indent area
            e.Graphics.FillRectangle(indentBrush, 0, 0, LeftIndentLine, ClientSize.Height);
            if (LeftIndent > minLeftIndent)
                e.Graphics.DrawLine(servicePen, LeftIndentLine, 0, LeftIndentLine, ClientSize.Height);
            //draw preferred line width
            if (PreferredLineWidth > 0)
                e.Graphics.DrawLine(servicePen,
                                    new Point(
                                        LeftIndent + Paddings.Left + PreferredLineWidth*CharWidth -
                                        HorizontalScroll.Value + 1, textAreaRect.Top + 1),
                                    new Point(
                                        LeftIndent + Paddings.Left + PreferredLineWidth*CharWidth -
                                        HorizontalScroll.Value + 1, textAreaRect.Bottom - 1));

            //draw text area border
            DrawTextAreaBorder(e.Graphics);
            //
            int firstChar = (Math.Max(0, HorizontalScroll.Value - Paddings.Left))/CharWidth;
            int lastChar = (HorizontalScroll.Value + ClientSize.Width)/CharWidth;
            //
            var x = LeftIndent + Paddings.Left - HorizontalScroll.Value;
            if (x < LeftIndent)
                firstChar++;
            //create dictionary of bookmarks
            var bookmarksByLineIndex = new Dictionary<int, Bookmark>();
            foreach (Bookmark item in bookmarks)
                bookmarksByLineIndex[item.LineIndex] = item;
            //
            
            int startLine = YtoLineIndex(VerticalScroll.Value);
            int iLine;
            if (startLine > 35)
            {
               // int temp = YtoLineIndex(VerticalScroll.Value); ;
            }

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            //draw text
            for (iLine = startLine; iLine < lines.Count; iLine++)
            {
                Line line = lines[iLine];
                LineInfo lineInfo;
                    lineInfo = LineInfo.EMPTY;
                        lineInfo.VisibleState = VisibleState.Visible;
                        lineInfo.startY = iLine*charHeight + Paddings.Top;
                    
                    
                //
                if (lineInfo.startY > VerticalScroll.Value + ClientSize.Height)
                    break;
                if (lineInfo.startY + lineInfo.WordWrapStringsCount*CharHeight < VerticalScroll.Value)
                    continue;
                if (lineInfo.VisibleState == VisibleState.Hidden)
                    continue;

                int y = lineInfo.startY - VerticalScroll.Value;
                //
                e.Graphics.SmoothingMode = SmoothingMode.None;
                //draw line background
                if (lineInfo.VisibleState == VisibleState.Visible)
                    if (line.BackgroundBrush != null)
                        e.Graphics.FillRectangle(line.BackgroundBrush,
                                                 new Rectangle(textAreaRect.Left, y, textAreaRect.Width,
                                                               CharHeight*lineInfo.WordWrapStringsCount));
                //draw current line background
                if (CurrentLineColor != Color.Transparent && iLine == Selection.Start.iLine)
                    if (Selection.IsEmpty)
                        e.Graphics.FillRectangle(currentLineBrush,
                                                 new Rectangle(textAreaRect.Left, y, textAreaRect.Width, CharHeight));
                //draw changed line marker
                if (ChangedLineColor != Color.Transparent && line.IsChanged)
                    e.Graphics.FillRectangle(changedLineBrush,
                                             new RectangleF(-10, y, LeftIndent - minLeftIndent - 2 + 10, CharHeight + 1));
                //
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                //
                //draw bookmark
                if (bookmarksByLineIndex.ContainsKey(iLine))
                    bookmarksByLineIndex[iLine].Paint(e.Graphics,
                                                      new Rectangle(LeftIndent, y, Width,
                                                                    CharHeight*lineInfo.WordWrapStringsCount));
                //OnPaintLine event
                if (lineInfo.VisibleState == VisibleState.Visible)
                    OnPaintLine(new PaintLineEventArgs(iLine,
                                                       new Rectangle(LeftIndent, y, Width,
                                                                     CharHeight*lineInfo.WordWrapStringsCount),
                                                       e.Graphics, e.ClipRectangle));
                
                //create markers
                if (lineInfo.VisibleState == VisibleState.StartOfHiddenBlock)
                    visibleMarkers.Add(new ExpandFoldingMarker(iLine, new Rectangle(LeftIndentLine - 4, y + CharHeight/2 - 3, 8, 8)));

                if (!string.IsNullOrEmpty(line.FoldingStartMarker) && lineInfo.VisibleState == VisibleState.Visible &&
                    string.IsNullOrEmpty(line.FoldingEndMarker))
                        visibleMarkers.Add(new CollapseFoldingMarker(iLine, new Rectangle(LeftIndentLine - 4, y + CharHeight/2 - 3, 8, 8)));

                if (lineInfo.VisibleState == VisibleState.Visible && !string.IsNullOrEmpty(line.FoldingEndMarker) &&
                    string.IsNullOrEmpty(line.FoldingStartMarker))
                    e.Graphics.DrawLine(servicePen, LeftIndentLine, y + CharHeight*lineInfo.WordWrapStringsCount - 1,
                                        LeftIndentLine + 4, y + CharHeight*lineInfo.WordWrapStringsCount - 1);
                //draw wordwrap strings of line
                for (int iWordWrapLine = 0; iWordWrapLine < lineInfo.WordWrapStringsCount; iWordWrapLine++)
                {
                    y = lineInfo.startY + iWordWrapLine*CharHeight - VerticalScroll.Value;
                    //indent
                    var indent = iWordWrapLine == 0 ? 0 : lineInfo.wordWrapIndent * CharWidth;
                    //draw chars
                    DrawLineChars(e.Graphics, firstChar, lastChar, iLine, iWordWrapLine, x + indent, y);
                    if (iLine > 35)
                    {
                        iLine = iLine;
                    }
                }
            }

            int endLine = iLine - 1;

            //draw folding lines
            if (ShowFoldingLines)
                DrawFoldingLines(e, startLine, endLine);

            //draw column selection
            if (Selection.ColumnSelectionMode)
                if (SelectionStyle.BackgroundBrush is SolidBrush)
                {
                    Color color = ((SolidBrush) SelectionStyle.BackgroundBrush).Color;
                    Point p1 = PlaceToPoint(Selection.Start);
                    Point p2 = PlaceToPoint(Selection.End);
                    using (var pen = new Pen(color))
                        e.Graphics.DrawRectangle(pen,
                                                 Rectangle.FromLTRB(Math.Min(p1.X, p2.X) - 1, Math.Min(p1.Y, p2.Y),
                                                                    Math.Max(p1.X, p2.X),
                                                                    Math.Max(p1.Y, p2.Y) + CharHeight));
                }
            
            //
            e.Graphics.SmoothingMode = SmoothingMode.None;
            //draw folding indicator
            
            //draw caret
            Point car = PlaceToPoint(Selection.Start);
            var caretHeight = CharHeight - lineInterval;
            car.Offset(0, lineInterval / 2);

                HideCaret(Handle);
                prevCaretRect = Rectangle.Empty;

            //draw disabled mask
            if (!Enabled)
                using (var brush = new SolidBrush(DisabledColor))
                    e.Graphics.FillRectangle(brush, ClientRectangle);

            //dispose resources
            servicePen.Dispose();
            changedLineBrush.Dispose();
            indentBrush.Dispose();
            currentLineBrush.Dispose();
            paddingBrush.Dispose();
            //
#if debug
            sw.Stop();
            Console.WriteLine("OnPaint: "+ sw.ElapsedMilliseconds);
#endif
            //
            base.OnPaint(e);
        }

        private void DrawMarkers(PaintEventArgs e, Pen servicePen)
        {
            foreach (VisualMarker m in visibleMarkers)
            {
                if(m is CollapseFoldingMarker)
                    using(var bk = new SolidBrush(ServiceColors.CollapseMarkerBackColor))
                    using(var fore = new Pen(ServiceColors.CollapseMarkerForeColor))
                    using(var border = new Pen(ServiceColors.CollapseMarkerBorderColor))
                        (m as CollapseFoldingMarker).Draw(e.Graphics, border, bk, fore);
                else
                if (m is ExpandFoldingMarker)
                    using (var bk = new SolidBrush(ServiceColors.ExpandMarkerBackColor))
                    using (var fore = new Pen(ServiceColors.ExpandMarkerForeColor))
                    using (var border = new Pen(ServiceColors.ExpandMarkerBorderColor))
                        (m as ExpandFoldingMarker).Draw(e.Graphics, border, bk, fore);
                else
                    m.Draw(e.Graphics, servicePen);
            }
        }

        private Rectangle prevCaretRect;

        private void DrawRecordingHint(Graphics graphics)
        {
            const int w = 75;
            const int h = 13;
            var rect = new Rectangle(ClientRectangle.Right - w, ClientRectangle.Bottom - h, w, h);
            var iconRect = new Rectangle(-h/2 + 3, -h/2 + 3, h - 7, h - 7);
            var state = graphics.Save();
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.TranslateTransform(rect.Left + h/2, rect.Top + h/2);
            var ts = new TimeSpan(DateTime.Now.Ticks);
            graphics.RotateTransform(180 * (DateTime.Now.Millisecond/1000f));
            using (var pen = new Pen(Color.Red, 2))
            {
                graphics.DrawArc(pen, iconRect, 0, 90);
                graphics.DrawArc(pen, iconRect, 180, 90);
            }
            graphics.DrawEllipse(Pens.Red, iconRect);
            graphics.Restore(state);
            using (var font = new Font(FontFamily.GenericSansSerif, 8f))
                graphics.DrawString("Recording...", font, Brushes.Red, new PointF(rect.Left + h, rect.Top));
            System.Threading.Timer tm = null;
            tm = new System.Threading.Timer(
                (o) => {
                    Invalidate(rect);
                    tm.Dispose();
                }, null, 200, System.Threading.Timeout.Infinite);
        }

        private void DrawTextAreaBorder(Graphics graphics)
        {
            if (TextAreaBorder == TextAreaBorderType.None)
                return;

            var rect = TextAreaRect;

            if (TextAreaBorder == TextAreaBorderType.Shadow)
            {
                const int shadowSize = 4;
                var rBottom = new Rectangle(rect.Left + shadowSize, rect.Bottom, rect.Width - shadowSize, shadowSize);
                var rCorner = new Rectangle(rect.Right, rect.Bottom, shadowSize, shadowSize);
                var rRight = new Rectangle(rect.Right, rect.Top + shadowSize, shadowSize, rect.Height - shadowSize);

                using (var brush = new SolidBrush(Color.FromArgb(80, TextAreaBorderColor)))
                {
                    graphics.FillRectangle(brush, rBottom);
                    graphics.FillRectangle(brush, rRight);
                    graphics.FillRectangle(brush, rCorner);
                }
            }

            using(Pen pen = new Pen(TextAreaBorderColor))
                graphics.DrawRectangle(pen, rect);
        }

        private void PaintHintBrackets(Graphics gr)
        {
            foreach (Hint hint in hints)
            {
                Range r = hint.Range.Clone();
                r.Normalize();
                Point p1 = PlaceToPoint(r.Start);
                Point p2 = PlaceToPoint(r.End);
                if (GetVisibleState(r.Start.iLine) != VisibleState.Visible ||
                    GetVisibleState(r.End.iLine) != VisibleState.Visible)
                    continue;

                using (var pen = new Pen(hint.BorderColor))
                {
                    pen.DashStyle = DashStyle.Dash;
                    if (r.IsEmpty)
                    {
                        p1.Offset(1, -1);
                        gr.DrawLines(pen, new[] {p1, new Point(p1.X, p1.Y + charHeight + 2)});
                    }
                    else
                    {
                        p1.Offset(-1, -1);
                        p2.Offset(1, -1);
                        gr.DrawLines(pen,
                                     new[]
                                         {
                                             new Point(p1.X + CharWidth/2, p1.Y), p1,
                                             new Point(p1.X, p1.Y + charHeight + 2),
                                             new Point(p1.X + CharWidth/2, p1.Y + charHeight + 2)
                                         });
                        gr.DrawLines(pen,
                                     new[]
                                         {
                                             new Point(p2.X - CharWidth/2, p2.Y), p2,
                                             new Point(p2.X, p2.Y + charHeight + 2),
                                             new Point(p2.X - CharWidth/2, p2.Y + charHeight + 2)
                                         });
                    }
                }
            }
        }

        protected virtual void DrawFoldingLines(PaintEventArgs e, int startLine, int endLine)
        {
            e.Graphics.SmoothingMode = SmoothingMode.None;
            using (var pen = new Pen(Color.FromArgb(200, ServiceLinesColor)) {DashStyle = DashStyle.Dot})
                foreach (var iLine in foldingPairs)
                    if (iLine.Key < endLine && iLine.Value > startLine)
                    {
                        Line line = lines[iLine.Key];
                        int y = LineInfos[iLine.Key].startY - VerticalScroll.Value + CharHeight;
                        y += y%2;

                        int y2;

                        if (iLine.Value >= LinesCount)
                            y2 = LineInfos[LinesCount - 1].startY + CharHeight - VerticalScroll.Value;
                        else if (LineInfos[iLine.Value].VisibleState == VisibleState.Visible)
                        {
                            int d = 0;
                            int spaceCount = line.StartSpacesCount;
                            if (lines[iLine.Value].Count <= spaceCount || lines[iLine.Value][spaceCount].c == ' ')
                                d = CharHeight;
                            y2 = LineInfos[iLine.Value].startY - VerticalScroll.Value + d;
                        }
                        else
                            continue;

                        int x = LeftIndent + Paddings.Left + line.StartSpacesCount*CharWidth - HorizontalScroll.Value;
                        if (x >= LeftIndent + Paddings.Left)
                            e.Graphics.DrawLine(pen, x, y >= 0 ? y : 0, x,
                                                y2 < ClientSize.Height ? y2 : ClientSize.Height);
                    }
        }

        private void DrawLineChars(Graphics gr, int firstChar, int lastChar, int iLine, int iWordWrapLine, int startX,
                                   int y)
        {
            Line line = lines[iLine];
            LineInfo lineInfo;
            if (StaticLineHeight)
            {
                lineInfo = LineInfo.EMPTY;
                lineInfo.VisibleState = VisibleState.Visible;
                lineInfo.startY = iLine * charHeight + Paddings.Top;
            }
            else
            {
                lineInfo = LineInfos[iLine];
            }

            int from = lineInfo.GetWordWrapStringStartPosition(iWordWrapLine);
            int to = lineInfo.GetWordWrapStringFinishPosition(iWordWrapLine, line);

            lastChar = Math.Min(to - from, lastChar);

            gr.SmoothingMode = SmoothingMode.AntiAlias;

            //folded block ?
            if (lineInfo.VisibleState == VisibleState.StartOfHiddenBlock)
            {
                //rendering by FoldedBlockStyle
                FoldedBlockStyle.Draw(gr, new Point(startX + firstChar*CharWidth, y),
                                      new Range(this, from + firstChar, iLine, from + lastChar + 1, iLine));
            }
            else
            {
                //render by custom styles
                StyleIndex currentStyleIndex = StyleIndex.None;
                int iLastFlushedChar = firstChar - 1;

                for (int iChar = firstChar; iChar <= lastChar; iChar++)
                {
                    StyleIndex style = line[from + iChar].style;
                    if (currentStyleIndex != style)
                    {
                        FlushRendering(gr, currentStyleIndex,
                                       new Point(startX + (iLastFlushedChar + 1)*CharWidth, y),
                                       new Range(this, from + iLastFlushedChar + 1, iLine, from + iChar, iLine));
                        iLastFlushedChar = iChar - 1;
                        currentStyleIndex = style;
                    }
                }
                FlushRendering(gr, currentStyleIndex, new Point(startX + (iLastFlushedChar + 1)*CharWidth, y),
                               new Range(this, from + iLastFlushedChar + 1, iLine, from + lastChar + 1, iLine));
            }

            //draw selection
            if (SelectionHighlightingForLineBreaksEnabled  && iWordWrapLine == lineInfo.WordWrapStringsCount - 1) lastChar++;//draw selection for CR
            if (!Selection.IsEmpty && lastChar >= firstChar)
            {
                gr.SmoothingMode = SmoothingMode.None;
                var textRange = new Range(this, from + firstChar, iLine, from + lastChar + 1, iLine);
                textRange = Selection.GetIntersectionWith(textRange);
                if (textRange != null && SelectionStyle != null)
                {
                    SelectionStyle.Draw(gr, new Point(startX + (textRange.Start.iChar - from)*CharWidth, 1 + y),
                                        textRange);
                }
            }
        }

        private void FlushRendering(Graphics gr, StyleIndex styleIndex, Point pos, Range range)
        {
            if (range.End > range.Start)
            {
                int mask = 1;
                bool hasTextStyle = false;
                for (int i = 0; i < Styles.Length; i++)
                {
                    if (Styles[i] != null && ((int) styleIndex & mask) != 0)
                    {
                        Style style = Styles[i];
                        bool isTextStyle = style is TextStyle;
                        if (!hasTextStyle || !isTextStyle || AllowSeveralTextStyleDrawing)
                            //cancelling secondary rendering by TextStyle
                            style.Draw(gr, pos, range); //rendering
                        hasTextStyle |= isTextStyle;
                    }
                    mask = mask << 1;
                }
                //draw by default renderer
                if (!hasTextStyle)
                    DefaultStyle.Draw(gr, pos, range);
            }
        }

        protected override void OnEnter(EventArgs e)
        {
            base.OnEnter(e);
            mouseIsDrag = false;
            mouseIsDragDrop = false;
            draggedRange = null;
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            isLineSelect = false;

            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                if (mouseIsDragDrop)
                    OnMouseClickText(e);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);


            MacrosManager.IsRecording = false;

            Select();
            ActiveControl = null;

            if (e.Button == MouseButtons.Left)
            {
                VisualMarker marker = FindVisualMarkerForPoint(e.Location);
                //click on marker

                mouseIsDrag = true;
                mouseIsDragDrop = false;
                draggedRange = null;
                isLineSelect = (e.Location.X < LeftIndentLine);

                if (!isLineSelect)
                {
                    var p = PointToPlace(e.Location);

                    if (e.Clicks == 2)
                    {
                        mouseIsDrag = false;
                        mouseIsDragDrop = false;
                        draggedRange = null;

                        SelectWord(p);
                        return;
                    }

                    if (Selection.IsEmpty || !Selection.Contains(p) || this[p.iLine].Count <= p.iChar || ReadOnly)
                        OnMouseClickText(e);
                    else
                    {
                        mouseIsDragDrop = true;
                        mouseIsDrag = false;
                    }
                }
                else
                {
                    Selection.BeginUpdate();
                    //select whole line
                    int iLine = PointToPlaceSimple(e.Location).iLine;
                    lineSelectFrom = iLine;
                    Selection.Start = new Place(0, iLine);
                    Selection.End = new Place(GetLineLength(iLine), iLine);
                    Selection.EndUpdate();
                    Invalidate();
                }
            }
            
        }

        private void OnMouseClickText(MouseEventArgs e)
        {
            //click on text
            Place oldEnd = Selection.End;
            Selection.BeginUpdate();

            if (Selection.ColumnSelectionMode)
            {
                Selection.Start = PointToPlaceSimple(e.Location);
                Selection.ColumnSelectionMode = true;
            }
            else
            {
                if (VirtualSpace)
                    Selection.Start = PointToPlaceSimple(e.Location);
                else
                    Selection.Start = PointToPlace(e.Location);
            }

            if ((lastModifiers & Keys.Shift) != 0)
                Selection.End = oldEnd;


            Selection.EndUpdate();
            Invalidate();
            return;
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            Invalidate();

            if (lastModifiers == Keys.Control)
            {
                ChangeFontSize(2 * Math.Sign(e.Delta));
                ((HandledMouseEventArgs)e).Handled = true;
            }
            else
            if (VerticalScroll.Visible || !ShowScrollBars)
            {
                //base.OnMouseWheel(e);

                // Determine scoll offset
                int mouseWheelScrollLinesSetting = GetControlPanelWheelScrollLinesValue();

                DoScrollVertical(mouseWheelScrollLinesSetting, e.Delta);

                ((HandledMouseEventArgs)e).Handled = true;
            }

            DeactivateMiddleClickScrollingMode();
        }

        private void DoScrollVertical(int countLines, int direction)
        {
            if (VerticalScroll.Visible || !ShowScrollBars)
            {
                int numberOfVisibleLines = ClientSize.Height/CharHeight;

                int offset;
                if ((countLines == -1) || (countLines > numberOfVisibleLines))
                    offset = CharHeight*numberOfVisibleLines;
                else
                    offset = CharHeight*countLines;

                var newScrollPos = VerticalScroll.Value - Math.Sign(direction)*offset;

                var ea =
                    new ScrollEventArgs(direction > 0 ? ScrollEventType.SmallDecrement : ScrollEventType.SmallIncrement,
                                        VerticalScroll.Value,
                                        newScrollPos,
                                        ScrollOrientation.VerticalScroll);

                OnScroll(ea);
            }
        }

        /// <summary>
        /// Gets the value for the system control panel mouse wheel scroll settings.
        /// The value returns the number of lines that shall be scolled if the user turns the mouse wheet one step.
        /// </summary>
        /// <remarks>
        /// This methods gets the "WheelScrollLines" value our from the registry key "HKEY_CURRENT_USER\Control Panel\Desktop".
        /// If the value of this option is 0, the screen will not scroll when the mouse wheel is turned.
        /// If the value of this option is -1 or is greater than the number of lines visible in the window,
        /// the screen will scroll up or down by one page.
        /// </remarks>
        /// <returns>
        /// Number of lines to scrol l when the mouse wheel is turned
        /// </returns>
        private static int GetControlPanelWheelScrollLinesValue()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", false))
                {
                    return Convert.ToInt32(key.GetValue("WheelScrollLines"));
                }
            }
            catch
            {
                // Use default value
                return 1;
            }
        }


        public void ChangeFontSize(int step)
        {
            var points = Font.SizeInPoints;
            using (var gr = Graphics.FromHwnd(Handle))
            {
                var dpi = gr.DpiY;
                var newPoints = points + step * 72f / dpi;
                if(newPoints < 1f) return;
                var k = newPoints / originalFont.SizeInPoints;
                 Zoom = (int)(100 * k);
            }
        }

       private void SelectWord(Place p)
        {
            int fromX = p.iChar;
            int toX = p.iChar;

            for (int i = p.iChar; i < lines[p.iLine].Count; i++)
            {
                char c = lines[p.iLine][i].c;
                if (char.IsLetterOrDigit(c) || c == '_')
                    toX = i + 1;
                else
                    break;
            }

            for (int i = p.iChar - 1; i >= 0; i--)
            {
                char c = lines[p.iLine][i].c;
                if (char.IsLetterOrDigit(c) || c == '_')
                    fromX = i;
                else
                    break;
            }

            Selection = new Range(this, toX, p.iLine, fromX, p.iLine);
        }

        private int YtoLineIndex(int y)
        {
            int iLine = y / CharHeight;
            if (iLine >= lines.Count)
                iLine = lines.Count - 1;
            return iLine;
        }

        /// <summary>
        /// Gets nearest line and char position from coordinates
        /// </summary>
        /// <param name="point">Point</param>
        /// <returns>Line and char position</returns>
        public Place PointToPlace(Point point)
        {
            #if debug
            var sw = Stopwatch.StartNew();
            #endif
            point.Offset(HorizontalScroll.Value, VerticalScroll.Value);
            point.Offset(-LeftIndent - Paddings.Left, 0);
            int iLine = YtoLineIndex(point.Y);
            
            if (iLine < 0)
                return Place.Empty;

            int x, start, finish;
            if (StaticLineHeight)
            {
                start = 0;
                finish = lines[iLine].Count - 1;
                x = (int)Math.Round((float)point.X / CharWidth);
            }
            else
            {
                int y = 0;

                for (; iLine < lines.Count; iLine++)
                {
                    y = LineInfos[iLine].startY + LineInfos[iLine].WordWrapStringsCount * CharHeight;
                    if (y > point.Y && LineInfos[iLine].VisibleState == VisibleState.Visible)
                        break;
                }
                if (iLine >= lines.Count)
                    iLine = lines.Count - 1;
                if (LineInfos[iLine].VisibleState != VisibleState.Visible)
                    iLine = FindPrevVisibleLine(iLine);
                //
                int iWordWrapLine = LineInfos[iLine].WordWrapStringsCount;
                do
                {
                    iWordWrapLine--;
                    y -= CharHeight;
                } while (y > point.Y);
                if (iWordWrapLine < 0) iWordWrapLine = 0;

                //
                start = LineInfos[iLine].GetWordWrapStringStartPosition(iWordWrapLine);
                finish = LineInfos[iLine].GetWordWrapStringFinishPosition(iWordWrapLine, lines[iLine]);
                x = (int)Math.Round((float)point.X / CharWidth);
                if (iWordWrapLine > 0)
                    x -= LineInfos[iLine].wordWrapIndent;
            }

            x = x < 0 ? start : start + x;
            if (x > finish)
                x = finish + 1;


            if (x > lines[iLine].Count)
                x = lines[iLine].Count;

#if debug
            Console.WriteLine("PointToPlace: " + sw.ElapsedMilliseconds);
#endif

            return new Place(x, iLine);
        }

        private Place PointToPlaceSimple(Point point)
        {
            point.Offset(HorizontalScroll.Value, VerticalScroll.Value);
            point.Offset(-LeftIndent - Paddings.Left, 0);
            int iLine = YtoLineIndex(point.Y);
            var x = (int) Math.Round((float) point.X/CharWidth);
            if (x < 0) x = 0;
            return new Place(x, iLine);
        }

        /// <summary>
        /// Gets nearest absolute text position for given point
        /// </summary>
        /// <param name="point">Point</param>
        /// <returns>Position</returns>
        public int PointToPosition(Point point)
        {
            return PlaceToPosition(PointToPlace(point));
        }

        /// <summary>
        /// Fires TextChanged event
        /// </summary>
        protected virtual void OnTextChanged(TextChangedEventArgs args)
        {
            //
            args.ChangedRange.Normalize();
            //
            if (updating > 0)
            {
                if (updatingRange == null)
                    updatingRange = args.ChangedRange.Clone();
                else
                {
                    if (updatingRange.Start.iLine > args.ChangedRange.Start.iLine)
                        updatingRange.Start = new Place(0, args.ChangedRange.Start.iLine);
                    if (updatingRange.End.iLine < args.ChangedRange.End.iLine)
                        updatingRange.End = new Place(lines[args.ChangedRange.End.iLine].Count,
                                                      args.ChangedRange.End.iLine);
                    updatingRange = updatingRange.GetIntersectionWith(Range);
                }
                return;
            }
            //
#if debug
            var sw = Stopwatch.StartNew();
            #endif
            CancelToolTip();
            ClearHints();
            IsChanged = true;
            TextVersion++;
            MarkLinesAsChanged(args.ChangedRange);
            ClearFoldingState(args.ChangedRange);
            //
            base.OnTextChanged(args);
            //
            if (TextChanged != null)
                TextChanged(this, args);
            //
            if (BindingTextChanged != null)
                BindingTextChanged(this, EventArgs.Empty);
            //
            base.OnTextChanged(EventArgs.Empty);
            //
#if debug
            Console.WriteLine("OnTextChanged: " + sw.ElapsedMilliseconds);
#endif

            OnVisibleRangeChanged();
        }

        /// <summary>
        /// Fires SelectionChanged event
        /// </summary>
        public virtual void OnSelectionChanged()
        {

            if (SelectionChanged != null)
                SelectionChanged(this, new EventArgs());
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            Invalidate();
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            Invalidate();
        }

        /// <summary>
        /// Gets absolute text position from line and char position
        /// </summary>
        /// <param name="point">Line and char position</param>
        /// <returns>Point of char</returns>
        public int PlaceToPosition(Place point)
        {
            if (point.iLine < 0 || point.iLine >= lines.Count ||
                point.iChar >= lines[point.iLine].Count + Environment.NewLine.Length)
                return -1;

            int result = 0;
            for (int i = 0; i < point.iLine; i++)
                result += lines[i].Count + Environment.NewLine.Length;
            result += point.iChar;

            return result;
        }

        /// <summary>
        /// Gets line and char position from absolute text position
        /// </summary>
        public Place PositionToPlace(int pos)
        {
            if (pos < 0)
                return new Place(0, 0);

            for (int i = 0; i < lines.Count; i++)
            {
                int lineLength = lines[i].Count + Environment.NewLine.Length;
                if (pos < lines[i].Count)
                    return new Place(pos, i);
                if (pos < lineLength)
                    return new Place(lines[i].Count, i);

                pos -= lineLength;
            }

            if (lines.Count > 0)
                return new Place(lines[lines.Count - 1].Count, lines.Count - 1);
            else
                return new Place(0, 0);
            //throw new ArgumentOutOfRangeException("Position out of range");
        }

        /// <summary>
        /// Gets absolute char position from char position
        /// </summary>
        public Point PositionToPoint(int pos)
        {
            return PlaceToPoint(PositionToPlace(pos));
        }

        /// <summary>
        /// Gets point for given line and char position
        /// </summary>
        /// <param name="place">Line and char position</param>
        /// <returns>Coordiantes</returns>

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {

                if (findForm != null)
                    findForm.Dispose();

                if (replaceForm != null)
                    replaceForm.Dispose();
                /*
                if (Font != null)
                    Font.Dispose();

                if (originalFont != null)
                    originalFont.Dispose();*/

                if (TextSource != null)
                    TextSource.Dispose();

                if (ToolTip != null)
                    ToolTip.Dispose();
            }
        }


        #region ISupportInitialize

        void ISupportInitialize.BeginInit()
        {
            //
        }

        void ISupportInitialize.EndInit()
        {
            Selection.Start = Place.Empty;
            DoCaretVisible();
        }

        #endregion
    }


    public enum TextAreaBorderType
    {
        None,
        Single,
        Shadow
    }

    [Flags]
    public enum ScrollDirection : ushort
    {
        None = 0,
        Left = 1,
        Right = 2,
        Up = 4,
        Down = 8
    }

}
