using RichTextBlockSample.HtmlConverter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Xml;

namespace SrtStudio
{
    class InlineTextBlock : TextBlock
    {
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            "Value",
            typeof(string),
            typeof(InlineTextBlock),
            new UIPropertyMetadata(null, new PropertyChangedCallback(OnContentChanged))
            );

        public string Value {
            get { return GetValue(ValueProperty) as string; }
            set { SetValue(ValueProperty, value); }
        }

        static void OnContentChanged(DependencyObject target, DependencyPropertyChangedEventArgs e) {
            InlineTextBlock control = target as InlineTextBlock;
            control.createParts(e.NewValue as string);
        }

        void createParts(string text) {
//            this.Text = "";

            //Inlines.Clear();


            Inlines.Add(text);

  //          this.Text = text;
            // Here I write my own algorithm for determine which characters highlight
        }
    }

    public class Attached {
        public static readonly DependencyProperty FormattedTextProperty = DependencyProperty.RegisterAttached(
            "FormattedText",
            typeof(string),
            typeof(Attached),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsMeasure, FormattedTextPropertyChanged));

        public static void SetFormattedText(DependencyObject textBlock, string value) {
            textBlock.SetValue(FormattedTextProperty, value);
        }

        public static string GetFormattedText(DependencyObject textBlock) {
            return (string)textBlock.GetValue(FormattedTextProperty);
        }

        private static void FormattedTextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            //var textBlock = (TextBlock)d;
            //if (textBlock == null) {
            //    return;
            //}
            //var text = (e.NewValue ?? string.Empty).ToString();
            //var xaml = HtmlToXamlConverter.ConvertHtmlToXaml(text, false);
            ////var flowDocument = (Span)XamlReader.Parse(xaml);

            //using (var xmlReader = XmlReader.Create(new StringReader(xaml))) {
            //    var result = (Span)XamlReader.Load(xmlReader);
            //    textBlock.Inlines.Add(result);
            //}

            //textBlock.Inlines.Add(flowDocument);

            //richTextBox.Document = flowDocument;





            var textBlock = d as TextBlock;
            if (textBlock == null) {
                return;
            }

            var formattedText = (string)e.NewValue ?? string.Empty;


            bool hasOpenTag = formattedText.Contains("<i>");
            bool hasCloseTag = formattedText.Contains("</i>");
            if (hasOpenTag && !hasCloseTag) {
                formattedText += "</i>";
            }
            if (hasCloseTag && !hasOpenTag) {
                formattedText = formattedText.Replace("</i>", "");
            }

            hasOpenTag = formattedText.Contains("<b>");
            hasCloseTag = formattedText.Contains("</b>");
            if (hasOpenTag && !hasCloseTag) {
                formattedText += "</b>";
            }
            if (hasCloseTag && !hasOpenTag) {
                formattedText = formattedText.Replace("</b>", "");
            }

            formattedText = formattedText.Replace("<i>", "<Italic FontStyle=\"Oblique\">");
            formattedText = formattedText.Replace("</i>", "</Italic>");
            formattedText = formattedText.Replace("<b>", "<Bold FontWeight=\"ExtraBold\">");
            formattedText = formattedText.Replace("</b>", "</Bold>");

            Console.WriteLine();
            Console.WriteLine(formattedText);



            formattedText = string.Format("<Span xml:space=\"preserve\" xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\">{0}</Span>", formattedText);



            textBlock.Inlines.Clear();
            using (var xmlReader = XmlReader.Create(new StringReader(formattedText))) {
                var result = (Span)XamlReader.Load(xmlReader);
                textBlock.Inlines.Add(result);
            }
        }
    }
}
