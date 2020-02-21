using System;
using System.IO;

namespace SrtStudio
{
    public static class StringOperations
    {
        public static string RemoveNewlines(string s)
        {
            var stringReader = new StringReader(s);
            string str = "";
            while ((s = stringReader.ReadLine()) != null) {
                if (str != "")
                    str += " ";
                str += s;
            }
            return str;
        }

        public static string TrimSpaces(string s)
        {
            var stringReader = new StringReader(s);
            string paragraph = "";
            while ((s = stringReader.ReadLine()) != null) {
                s += " ";
                string sentence = "";
                while (s != "" && s[0] == ' ')
                    s = s.Remove(0, 1);
                while (s != "") {
                    int length = s.IndexOf(' ');
                    string word = s.Substring(0, length);
                    if (sentence != "")
                        sentence += " ";
                    sentence += word;
                    s = s.Remove(0, length + 1);
                    while (s != "" && s[0] == ' ')
                        s = s.Remove(0, 1);
                }
                if (paragraph != "")
                    paragraph += Environment.NewLine;
                paragraph += sentence;
            }
            return paragraph;
        }
    }
}
