using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;
using FastColoredTextBoxNS;
using Char = FastColoredTextBoxNS.Char;

namespace DNARichText
{
    /// <summary>
    /// Text source readonly provider for 'FastColoredTextBox'.
    /// Uses minimum memory and very speed optimized for big data.
    /// 
    /// Line content provided dynamically for visible region of component (where user scrolled, just few lines)
    /// 
    /// Each line has same width.
    /// Text should'nt has "\n" separators or tabs, just plain text.
    /// </summary>
    public class DynamicTextSource : TextSource
    {
        // store line index that are loaded
        private List<int> loadedLines = new List<int>();

        int charsCountInLine;
        int linesCount;
        int linesCountInWindow;
        readonly Timer timer_UnloadUnusedLines = new Timer();

        public StyledText StyledText;

        /// <summary>
        /// Contructor
        /// </summary>
        /// <param name="fctb">FastColoredTextBox component</param>
        public DynamicTextSource(FastColoredTextBox fctb)
            : base(fctb)
        {
            timer_UnloadUnusedLines.Interval = 1000;
            timer_UnloadUnusedLines.Tick += timer_UnloadUnusedLines_Tick;
            timer_UnloadUnusedLines.Enabled = true;

            // we need to subcrive to this event to refresh our line width and other support data
            fctb.Layout += fctb_Layout;
            fctb.SizeChanged += fctb_SizeChanged;
        }

        /// <summary>
        /// Test method for storing text in file as lines delimited by line break
        /// </summary>
        /// <returns>Lines delimited by line break</returns>
        public string GetTextAsLines()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < lines.Count - 1; i++)
            {
                string line = StyledText.Text.Substring(i * charsCountInLine, charsCountInLine);
                sb.AppendLine(line);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Load styledText - recalculate line width, repaint control, update scrollbars, recolor text.
        /// </summary>
        /// <param name="styledText">Class which stores text data along with styles for each character.</param>
        public void Load(StyledText styledText, bool refreshSizeOnly = false)
        {
            //
            // Calculates line width based on FCTB component size (line widht is same for all lines)
            //

            int indent = SystemInformation.VerticalScrollBarWidth + 5;
            int prev_linesCount = linesCount;
            charsCountInLine = (CurrentTB.Width - indent) / CurrentTB.CharWidth - 1;
            linesCount = styledText.Length / charsCountInLine + 1;
            linesCountInWindow = Math.Min(linesCount, CurrentTB.Height / CurrentTB.CharHeight);

            if (CurrentTB.ShowLineNumbers)
            {
                indent = linesCount.ToString().Length * CurrentTB.CharWidth + 30;
                charsCountInLine = (CurrentTB.Width - indent) / CurrentTB.CharWidth - 1;
                linesCount = styledText.Length / charsCountInLine + 1;
                linesCountInWindow = Math.Min(linesCount, CurrentTB.Height / CurrentTB.CharHeight);
            }

            if (refreshSizeOnly && linesCount == prev_linesCount)
            {
                // here we exit, since we dont need to reload data - we have same linesCount
                //Debug.WriteLine("DynamicTextSource.Load skipped");
                return;
            }



            StyledText = styledText;
            loadedLines.Clear();

            //
            // Copy styles from "StyledText" to our class 'DynamicTextSource'
            //
            for (int i = 0; i < Styles.Length; i++)
            {
                Styles[i] = (i < styledText.Styles.Count)
                    ? styledText.Styles[i]
                    : null;
            }

            
            //
            // Recreate lines
            //

            UnloadUnusedLines(true); // clear all defined items in base.lines
            if (prev_linesCount > linesCount)
            {
                // remove items
                base.lines.RemoveRange(linesCount, prev_linesCount - linesCount);
                //Debug.WriteLine("Removed  " + (prev_linesCount - linesCount));
            }
            else
            {
                //add items
                base.lines.AddRange(new Line[linesCount - prev_linesCount]);
                //Debug.WriteLine("Added  " + (linesCount - prev_linesCount));
            }


            //
            // Load first visible lines
            //

            for (int i = 0; i < linesCountInWindow; i++)
                LoadLineFromSourceString(i);


            //
            // Recalculate scrollbars and other data for rendering controll
            //

            if (CurrentTB is DNAFastColoredTextBox)
            {
                // call custom method which is speed optimized
                (CurrentTB as DNAFastColoredTextBox).RecalcMy();
            }
            else
            {
                CurrentTB.LineInfos.Clear();
                CurrentTB.LineInfos.AddRange(new LineInfo[linesCount]);
                NeedRecalc(new TextChangedEventArgs(0, linesCount - 1));
                if (CurrentTB.WordWrap)
                    OnRecalcWordWrap(new TextChangedEventArgs(0, linesCount - 1));
                CurrentTB.NeedRecalc(true, false);
            }

        }

        #region Private Methods

        /// <summary>
        /// Event raises when FCTB size changed.
        /// When this happend we need to reload text (update line width)
        /// </summary>
        private void fctb_Layout(object sender, LayoutEventArgs e)
        {
            //Debug.WriteLine("fctb_Layout");
            if (CurrentTB.Width == 0)
            {
                return;
            }

            if (CurrentTB.TextSource is DynamicTextSource)
            {
                // reload text
                var watch = Stopwatch.StartNew();
                Load(StyledText, true);
                watch.Stop();
                if (watch.ElapsedMilliseconds > 1)
                {
                    Debug.WriteLine("fctb_Layout   time ms: " + watch.ElapsedMilliseconds);
                }
            }
        }


        /// <summary>
        /// Event raises when FCTB size changed.
        /// When this happend we need to reload text (update line width)
        /// </summary>
        private void fctb_SizeChanged(object sender, EventArgs e)
        {
            /*Debug.WriteLine("fctb_SizeChanged");
            if (CurrentTB.Width == 0)
            {
                return;
            }

            if (CurrentTB.TextSource is DynamicTextSource)
            {
                // reload text
                //Load(StyledText);
            }*/
        }

        /// <summary>
        /// Dynamicaly return one line.
        /// </summary>
        /// <param name="i">Line index</param>
        private void LoadLineFromSourceString(int i)
        {

            //Debug.WriteLine("LoadLineFromSourceString " + (i + 1).ToString());

            if (i >= base.lines.Count) return;

            var line = CreateLine();
            int pos = charsCountInLine * i;

            var chars = StyledText.CopyChars(pos, charsCountInLine);
            line.AddRange(chars);

            base.lines[i] = line;
            loadedLines.Add(i);

            if (CurrentTB.WordWrap)
                OnRecalcWordWrap(new TextChangedEventArgs(i, i));
        }

        /// <summary>
        /// Every second we remove unused lines from memory. (memory optimization)
        /// </summary>
        private void timer_UnloadUnusedLines_Tick(object sender, EventArgs e)
        {
            UnloadUnusedLines();
        }

        /// <summary>
        /// Unload unused lines from memory. (memory optimization)
        /// </summary>
        private void UnloadUnusedLines(bool forceUnloadAll = false)
        {
            try
            {
                var removedLines = new List<int>();
                var loaded = new List<int>(loadedLines);
                foreach (var i in loaded)
                {
                    if (i < CurrentTB.VisibleRange.Start.iLine - 10
                        || i > CurrentTB.VisibleRange.End.iLine + 10
                        || forceUnloadAll)
                    {
                        base.lines[i] = null;
                        removedLines.Add(i);
                    }
                }

                foreach (var i in removedLines)
                {
                    loadedLines.Remove(i);
                }

                if (removedLines.Count > 0)
                {
                    Debug.WriteLine("UnloadUnusedLines: " + removedLines.Count);
                }
            }
            catch
            {
                loadedLines.Clear();

            }
        }

        #endregion

        #region Override Methods and Properies

        public override void ClearIsChanged()
        {
            foreach (var line in lines)
                if (line != null)
                    line.IsChanged = false;
        }

        public override Line this[int i]
        {
            get
            {
                if (base.lines[i] != null)
                    return lines[i];

                LoadLineFromSourceString(i);
                return lines[i];
            }
            set { throw new NotImplementedException(); }
        }

        public override int Count
        {
            get
            {
                // refresh LineInfos if something went wrong (happend in Visual stusio Form Editor)
                if (linesCount != CurrentTB.LineInfos.Count)
                {
                    if (CurrentTB is DNAFastColoredTextBox)
                    {
                        (CurrentTB as DNAFastColoredTextBox).UpdateLineInfos(linesCount);
                    }
                    else
                    {
                        CurrentTB.LineInfos.Clear();
                        CurrentTB.LineInfos.Capacity = linesCount;
                        int paddingTop = CurrentTB.Paddings.Top;
                        int startY = paddingTop;
                        int charHeight = CurrentTB.CharHeight;
                        for (int i = 0; i < linesCount; i++)
                        {
                            CurrentTB.LineInfos.Add(new LineInfo(startY));
                            startY += charHeight;
                        }
                    }
                }
                return linesCount;
            }
        }

        public override void RemoveLine(int index, int count)
        {

        }

        public override int GetLineLength(int i)
        {
            if (base.lines[i] == null)
                return 0;

            return charsCountInLine;
        }

        public override bool LineHasFoldingStartMarker(int iLine)
        {
            if (lines[iLine] == null)
                return false;

            return !string.IsNullOrEmpty(lines[iLine].FoldingStartMarker);
        }

        public override bool LineHasFoldingEndMarker(int iLine)
        {
            if (lines[iLine] == null)
                return false;

            return !string.IsNullOrEmpty(lines[iLine].FoldingEndMarker);
        }

        public override void Dispose()
        {
            timer_UnloadUnusedLines.Dispose();
            CurrentTB.SizeChanged -= fctb_SizeChanged;
        }

        internal void UnloadLine(int iLine)
        {
            if (lines[iLine] != null && !lines[iLine].IsChanged)
                lines[iLine] = null;
        }

        #endregion

    }
}
