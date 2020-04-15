using System;
using System.Collections.Generic;
using FastColoredTextBoxNS;
using System.Threading.Tasks;
using System.Threading;

namespace DNARichText
{
    /// <summary>
    /// Class for storing big text and styles for it
    /// Each character can has own style
    /// Available methods for applying styles and retrieving styles
    /// </summary>
    public class StyledText
    {
        /// <summary>
        /// Text
        /// </summary>
        public readonly string Text;

        /// <summary>
        /// Styles for each character from 'Text'
        /// Length of this list is same as for 'Text'
        /// </summary>
        private byte[] stylesIndexes8;
        private short[] stylesIndexes16;
        private int[] stylesIndexes32;
        private int stylesIndexesSize;

        /// <summary>
        /// All unique Styles that where passed in method 'StyledText'
        /// </summary>
        public List<Style> Styles = new List<Style>();

        /// <summary>
        /// Text Length
        /// </summary>
        public int Length
        {
            get { return Text.Length; }
        }

        #region Contructor

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="text">Any text, size is not limited.</param>
        public StyledText(string text)
        {
            Text = text;
            stylesIndexesSize = 8;
            stylesIndexes8 = new byte[Length];
        }

        #endregion




        #region Public Methods

        /// <summary>
        /// Search for substring in Text and store information in stylesIndexes
        /// </summary>
        /// <param name="subString">searched substring in Text</param>
        /// <param name="styles">set of styles for this substring</param>
        public void ApplyStyle(string subString, Style[] styles)
        {
            StyleIndex styleIndex = GetStyleIndexMask(styles);
            
            int searchLength = subString.Length;

            int pos = Text.IndexOf(subString, 0, StringComparison.Ordinal);
            while (pos != -1)
            {
                switch (stylesIndexesSize)
                {
                    case 8:
                        for (int i = 0; i < searchLength; i++) stylesIndexes8[pos + i] = (byte)styleIndex;
                        break;
                    case 16:
                        for (int i = 0; i < searchLength; i++) stylesIndexes16[pos + i] = (short)styleIndex;
                        break;
                    case 32:
                        for (int i = 0; i < searchLength; i++) stylesIndexes32[pos + i] = (int)styleIndex;
                        break;
                }
                pos += searchLength;
                pos = Text.IndexOf(subString, pos, StringComparison.Ordinal);
            }
            
        }

        /// <summary>
        /// Search for substring in Text and store information in stylesIndexes
        /// (overloaded method)
        /// </summary>
        /// <param name="subString">searched substring in Text</param>
        /// <param name="style">style for this substring</param>
        public void ApplyStyle(string subString, Style style)
        {
            ApplyStyle(subString, new[] { style });
        }

        /// <summary>
        /// Copyies chars at some position with defined length.
        /// </summary>
        /// <param name="pos">starting position to copy chars</param>
        /// <param name="length">length of copied chars</param>
        /// <returns></returns>
        public List<FastColoredTextBoxNS.Char> CopyChars(int pos, int length)
        {
            var result = new List<FastColoredTextBoxNS.Char>();
            if (pos >= Length)
            {
                return result;
            }

            int copyLength = Math.Min(length, Length - pos);
            string s = Text.Substring(pos, copyLength);

            result.Capacity = copyLength;
            foreach (var c in s)
            {
                var fctb_c = new FastColoredTextBoxNS.Char
                {
                    c = c,
                    style = GetStyleIndex(pos)
                };
                result.Add(fctb_c);
                pos++;
            }

            return result;
        }

        #endregion





        #region Private Methods

        private StyleIndex GetStyleIndex(int pos)
        {
            var style = StyleIndex.None;
            
            switch (stylesIndexesSize)
            {
                case 8:
                    style = (StyleIndex)stylesIndexes8[pos];
                    break;
                case 16:
                    style = (StyleIndex)stylesIndexes16[pos];
                    break;
                case 32:
                    style = (StyleIndex)stylesIndexes32[pos];
                    break;
            }
            return style;
        }

        private int GetStyleIndexesLength()
        {
            var res = 0;
            switch (stylesIndexesSize)
            {
                case 8:
                    res = stylesIndexes8.Length;
                    break;
                case 16:
                    res = stylesIndexes16.Length;
                    break;
                case 32:
                    res = stylesIndexes32.Length;
                    break;
            }
            return res;
        }

        /// <summary>
        /// Find style for character at position 'pos'
        /// If character has some non-default style - return true and our parameter 'styleIndex'
        /// </summary>
        /// <param name="pos">character position in Text</param>
        /// <param name="styleIndex">out parameter - return style index if character has some non-default style</param>
        /// <returns>True if character has non-default style</returns>
        public bool Find(int pos, out StyleIndex styleIndex)
        {
            styleIndex = StyleIndex.None;
            if (pos < 0 || pos >= GetStyleIndexesLength())
            {
                return false;
            }
            styleIndex = GetStyleIndex(pos);
            return true;
        }

        /// <summary>
        /// Method duplicate functionality from FastColoredTextBox - this is to avoid dependency on this class.
        /// </summary>
        /// <param name="astyles"></param>
        /// <returns></returns>
        private StyleIndex GetStyleIndexMask(IEnumerable<Style> astyles)
        {
            var mask = StyleIndex.None;
            foreach (Style style in astyles)
            {
                int i = Styles.IndexOf(style);
                if (i < 0)
                {
                    Styles.Add(style);
                    i = (Styles.Count - 1);
                }
                if (i >= 0)
                    mask |= (StyleIndex)(1 << i);
            }
            RedimStylesIndexesIfNeeded();
            return mask;
        }

        private void RedimStylesIndexesIfNeeded()
        {
            if (Styles.Count <= stylesIndexesSize) return;

            switch (stylesIndexesSize)
            {
                case 8:
                    stylesIndexes16 = new short[Length];
                    Array.Copy(stylesIndexes8, stylesIndexes16, Length);
                    stylesIndexes8 = null;
                    stylesIndexesSize = 16;
                    break;
                case 16:
                    stylesIndexes32 = new int[Length];
                    Array.Copy(stylesIndexes16, stylesIndexes32, Length);
                    stylesIndexes16 = null;
                    stylesIndexesSize = 32;
                    break;
                case 32:
                    throw new Exception("Maximum size of Styles riched in StyledText!");
            }
        }

        #endregion

    }
}
