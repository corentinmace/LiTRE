using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace LiTRE
{

    public class TextFormatterUtils
    {
        private static readonly Dictionary<int, GenerationData> genData = new Dictionary<int, GenerationData>
        {
            [4] = new GenerationData
            {
                Regex = new RegexData
                {
                    Full = new Regex(@"\\vFF00\\x01(\x01|あ|ぁ)(.*?)\\vFF00\\x01\\x00", RegexOptions.Compiled),
                    Start = new[]
                    {
                    new Regex(@"^\\vFF00\\x01(\x01|あ|ぁ)", RegexOptions.Compiled),
                    new Regex(@"\\vFF00\\x01", RegexOptions.Compiled)
                },
                    Both = new Regex(@"\\vFF00\\x01(\x01|あ|ぁ)|\\vFF00\\x01\\x00", RegexOptions.Compiled),
                    End = new Regex(@"\\vFF00\\x01\\x00", RegexOptions.Compiled),
                    ColorCode = new Regex(@"\\x01|あ|ぁ", RegexOptions.Compiled)
                },
                Colors = new Dictionary<string, string>
                {
                    ["\\x01"] = "text-G4red",
                    ["あ"] = "text-G4green",
                    ["ぁ"] = "text-G4blue"
                },
                Whitespaces = new WhiteSpaces
                {
                    Newline = "\\n",
                    Advance = "\\r",
                    Flow = "\\f"
                }
            }
        };

        public static string RemoveExtraSpaces(string input)
        {
            return Regex.Replace(input ?? "", @"[\f\n\r]+", "\n").Trim();
        }

        private static string StripFormatting(string input, Regex bothRegex)
        {
            return bothRegex.Replace(input, "");
        }

        private static int GetRealIndexFromClean(string original, string cleanTarget, Regex formatRegex)
        {
            int o = 0, c = 0;
            int realIndex = 0;
            while (o < original.Length && c < cleanTarget.Length)
            {
                if (formatRegex.IsMatch(original.Substring(o)))
                {
                    var m = formatRegex.Match(original.Substring(o));
                    o += m.Length;
                    continue;
                }

                if (original[o] == cleanTarget[c])
                {
                    o++; c++;
                }
                else
                {
                    o++;
                }

                realIndex = o;
            }

            return realIndex;
        }


        public static string TextToLiTRE(string input, int gen = 4, bool flow = true, int index = 39, int lines = 2)
        {
            var g = genData[gen];
            input = RemoveExtraSpaces(input);
            string clean = StripFormatting(input, g.Regex.Both);
            string original = input;

            List<string> chunks = new List<string>();
            int lineCount = 0;

            while (!string.IsNullOrEmpty(clean))
            {
                int sliceLength = Math.Min(index, clean.Length);
                string cleanChunk = clean.Substring(0, sliceLength);

                // Essayer de couper au dernier espace si possible
                int lastSpace = cleanChunk.LastIndexOf(' ');
                if (lastSpace > 0) cleanChunk = cleanChunk.Substring(0, lastSpace);

                // Calcul de la position réelle dans la chaîne originale (avec balises)
                int realCut = GetRealIndexFromClean(original, cleanChunk, g.Regex.Both);
                string chunk = original.Substring(0, realCut).Trim();

                // Ajout du saut de ligne ou de page si nécessaire
                lineCount++;
                if (lineCount % lines == 0)
                    chunk += flow ? g.Whitespaces.Flow : g.Whitespaces.Advance;
                else
                    chunk += g.Whitespaces.Newline;

                chunks.Add(chunk);
                original = original.Substring(realCut).TrimStart();
                clean = StripFormatting(original, g.Regex.Both);
            }

            // Supprimer le dernier saut (\n ou \f) si présent
            if (chunks.Count > 0)
            {
                string last = chunks[chunks.Count - 1];
                if (last.EndsWith(g.Whitespaces.Newline) || last.EndsWith(g.Whitespaces.Flow) || last.EndsWith(g.Whitespaces.Advance))
                {
                    chunks[chunks.Count - 1] = last.Substring(0, last.Length - 2);
                }
            }

            return string.Join("", chunks);
        }


        public static string LiTREToHTML(string input, int gen = 4)
        {
            var g = genData[gen];
            if (g.Regex.Full.IsMatch(input))
            {
                input = g.Regex.End.Replace(input, "</span>");
                input = g.Regex.Start[1].Replace(input, "<span class=\"");
                input = g.Regex.ColorCode.Replace(input, m => $"{g.Colors[m.Value]}\">");
            }
            return Regex.Replace(input, @"\\n|\\r|\\f|\\c", "\n");
        }
    }

    public class GenerationData
    {
        public RegexData Regex { get; set; }
        public Dictionary<string, string> Colors { get; set; }
        public WhiteSpaces Whitespaces { get; set; }
    }

    public class RegexData
    {
        public Regex Full { get; set; }
        public Regex[] Start { get; set; }
        public Regex Both { get; set; }
        public Regex End { get; set; }
        public Regex ColorCode { get; set; }
    }

    public class WhiteSpaces
    {
        public string Newline { get; set; }
        public string Advance { get; set; }
        public string Flow { get; set; }
    }

    public class ColorFormatter
    {
        public static string AddColorCode(string text, string color, int gen)
        {
            switch (gen)
            {
                case 4:
                    switch (color)
                    {
                        case "red": return $"\vFF00\x0001\x0001{text}\vFF00\x0001\x0000";
                        case "green": return $"\vFF00\x0001あ{text}\vFF00\x0001\x0000";
                        case "blue": return $"\vFF00\x0001ぁ{text}\vFF00\x0001\x0000";
                        default: return text;
                    }
            }
            return text;
        }
    }


}