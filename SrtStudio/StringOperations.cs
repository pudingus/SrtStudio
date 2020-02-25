using System;
using System.IO;
using System.Text;

namespace SrtStudio
{
    public static class StringOperations
    {

        public static string ShiftLineBreak(string text, int index)
        {
            if (text == null)
                return null;

            var wordBuilder = new StringBuilder(text.Length);

            bool insertSpace = false;
            int wordCount = 0;
            bool writingWord = false;
            for (int i = 0; i < text.Length; i++) {
                char curChar = text[i];

                bool isWhiteSpace = char.IsWhiteSpace(curChar);

                if (writingWord && isWhiteSpace) {
                    //změna stavu
                    wordCount++;
                    writingWord = false;
                    wordBuilder.Append(' ');
                }

                
                if (!isWhiteSpace) {   //když není mezera = je písmeno
                    wordBuilder.Append(curChar);
                    writingWord = true;
                }
            }

            if (writingWord) {
                wordCount++;
                wordBuilder.Append(' ');
            }
            wordBuilder.Remove(wordBuilder.Length - 1, 1);



            Console.WriteLine($"wordCount: {wordCount}");

            return wordBuilder.ToString();
        }


        /*public static string ShiftLineBreak(string text, int index)
        {
            if (text == null)
                return null;

            var reader = new StringReader(text);
            var builder = new StringBuilder();
            string line;
            while ((line = reader.ReadLine()) != null) {
                builder.Append(line);
                builder.Append(" ");
            }
            builder.Remove(builder.Length-1, 1);

            if (index >= builder.Length) {
                //builder.Insert(builder.Length, Environment.NewLine);
            }
            else {
                builder.Insert(index, Environment.NewLine);
            }

            return builder.ToString();
        }*/

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
