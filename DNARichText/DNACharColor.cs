using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DNARichText
{
    /// <summary>
    /// Class for storing color along with font styles.
    /// 4 bytes size structure.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Explicit, Size = 4)]
    public struct DNACharColor
    {
        [FieldOffset(0)] private readonly byte _r;
        [FieldOffset(1)] private readonly byte _g;
        [FieldOffset(2)] private readonly byte _b;
        [FieldOffset(3)] private readonly byte _styles;
        
        public DNACharColor(Color color, IEnumerable<FontStyle> fontStyles)
        {
            _r = color.R;
            _g = color.G;
            _b = color.B;
            _styles = 0;
            foreach (var fontStyle in fontStyles)
            {
                if (fontStyle == FontStyle.Regular)
                {
                    _styles += (byte)(FontStyle.Strikeout);
                }
                else
                {
                    _styles += (byte) (fontStyle);
                }
            }
            if (color.IsKnownColor)
            {
                _styles += 16;
            }
        }

        /// <summary>
        /// Color
        /// </summary>
        public Color Color
        {
            get
            {
                return Color.FromArgb(_r, _g, _b);
            }
        }

        /// <summary>
        /// List of font styles.
        /// </summary>
        public FontStyle[] Styles
        {
            get
            {
                var res = new List<FontStyle>();
                if ((FontStyle)(_styles & (byte)FontStyle.Strikeout) == FontStyle.Strikeout) res.Add(FontStyle.Regular);
                if ((FontStyle)(_styles & (byte)FontStyle.Bold) == FontStyle.Bold) res.Add(FontStyle.Bold);
                if ((FontStyle)(_styles & (byte)FontStyle.Italic) == FontStyle.Italic) res.Add(FontStyle.Italic);
                if ((FontStyle)(_styles & (byte)FontStyle.Underline) == FontStyle.Underline) res.Add(FontStyle.Underline);
                return res.ToArray();
            }
        }

        /// <summary>
        /// String represenation for debug porpose
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var styles = new List<FontStyle>(Styles);
            var sb = new StringBuilder();
            sb.Append(Color.ToString());
            sb.Append("  ");
            if (styles.IndexOf(FontStyle.Regular) >= 0) sb.Append(" Regular");
            if (styles.IndexOf(FontStyle.Bold) >= 0) sb.Append(" Bold");
            if (styles.IndexOf(FontStyle.Italic) >= 0) sb.Append(" Italic");
            if (styles.IndexOf(FontStyle.Underline) >= 0) sb.Append(" Underline");
            return sb.ToString();
        }
    }
}
