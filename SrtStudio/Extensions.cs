using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace SrtStudio
{
    public static class Extensions
    {
        public static void LinesLeft(this ScrollViewer scroll, int lines)
        {
            for (int i = 0; i < lines; i++)
                scroll.LineLeft();
        }

        public static void LinesRight(this ScrollViewer scroll, int lines)
        {
            for (int i = 0; i < lines; i++)
                scroll.LineRight();
        }

        public static string ToShortForm(this TimeSpan t)
        {
            string sep = Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            int h = t.Days * 24;
            h += t.Hours;
            int m = t.Minutes;
            int s = t.Seconds;
            int ms = t.Milliseconds;

            string str = "";
            if (h > 0)
            {
                str = $"{h:D1}:{m:D2}:{s:D2}{sep}{ms:D3}";
            }
            else if (t.Minutes > 0)
            {
                str = $"{m:D1}:{s:D2}{sep}{ms:D3}";
            }
            else if (t.Seconds > 0 || t.Milliseconds >= 0)
            {
                str = $"{s:D1}{sep}{ms:D3}";
            }
            return str;
        }
    }
}
