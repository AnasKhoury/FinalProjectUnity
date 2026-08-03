using System.Collections.Generic;
using System.Text;

public static class RtlTextFormatter
{
    private readonly struct ArabicForms
    {
        public readonly char Isolated;
        public readonly char Final;
        public readonly char Initial;
        public readonly char Medial;
        public readonly bool JoinsLeft;

        public ArabicForms(int isolated, int final, int initial = 0, int medial = 0)
        {
            Isolated = (char)isolated;
            Final = (char)final;
            Initial = initial == 0 ? (char)isolated : (char)initial;
            Medial = medial == 0 ? (char)final : (char)medial;
            JoinsLeft = initial != 0;
        }
    }

    private static readonly Dictionary<char, ArabicForms> Arabic = new()
    {
        ['ء'] = new(0xFE80, 0xFE80), ['آ'] = new(0xFE81, 0xFE82), ['أ'] = new(0xFE83, 0xFE84),
        ['ؤ'] = new(0xFE85, 0xFE86), ['إ'] = new(0xFE87, 0xFE88), ['ئ'] = new(0xFE89, 0xFE8A, 0xFE8B, 0xFE8C),
        ['ا'] = new(0xFE8D, 0xFE8E), ['ب'] = new(0xFE8F, 0xFE90, 0xFE91, 0xFE92),
        ['ة'] = new(0xFE93, 0xFE94), ['ت'] = new(0xFE95, 0xFE96, 0xFE97, 0xFE98),
        ['ث'] = new(0xFE99, 0xFE9A, 0xFE9B, 0xFE9C), ['ج'] = new(0xFE9D, 0xFE9E, 0xFE9F, 0xFEA0),
        ['ح'] = new(0xFEA1, 0xFEA2, 0xFEA3, 0xFEA4), ['خ'] = new(0xFEA5, 0xFEA6, 0xFEA7, 0xFEA8),
        ['د'] = new(0xFEA9, 0xFEAA), ['ذ'] = new(0xFEAB, 0xFEAC), ['ر'] = new(0xFEAD, 0xFEAE),
        ['ز'] = new(0xFEAF, 0xFEB0), ['س'] = new(0xFEB1, 0xFEB2, 0xFEB3, 0xFEB4),
        ['ش'] = new(0xFEB5, 0xFEB6, 0xFEB7, 0xFEB8), ['ص'] = new(0xFEB9, 0xFEBA, 0xFEBB, 0xFEBC),
        ['ض'] = new(0xFEBD, 0xFEBE, 0xFEBF, 0xFEC0), ['ط'] = new(0xFEC1, 0xFEC2, 0xFEC3, 0xFEC4),
        ['ظ'] = new(0xFEC5, 0xFEC6, 0xFEC7, 0xFEC8), ['ع'] = new(0xFEC9, 0xFECA, 0xFECB, 0xFECC),
        ['غ'] = new(0xFECD, 0xFECE, 0xFECF, 0xFED0), ['ف'] = new(0xFED1, 0xFED2, 0xFED3, 0xFED4),
        ['ق'] = new(0xFED5, 0xFED6, 0xFED7, 0xFED8), ['ك'] = new(0xFED9, 0xFEDA, 0xFEDB, 0xFEDC),
        ['ل'] = new(0xFEDD, 0xFEDE, 0xFEDF, 0xFEE0), ['م'] = new(0xFEE1, 0xFEE2, 0xFEE3, 0xFEE4),
        ['ن'] = new(0xFEE5, 0xFEE6, 0xFEE7, 0xFEE8), ['ه'] = new(0xFEE9, 0xFEEA, 0xFEEB, 0xFEEC),
        ['و'] = new(0xFEED, 0xFEEE), ['ى'] = new(0xFEEF, 0xFEF0), ['ي'] = new(0xFEF1, 0xFEF2, 0xFEF3, 0xFEF4),
        ['پ'] = new(0xFB56, 0xFB57, 0xFB58, 0xFB59), ['چ'] = new(0xFB7A, 0xFB7B, 0xFB7C, 0xFB7D),
        ['ڤ'] = new(0xFB6A, 0xFB6B, 0xFB6C, 0xFB6D), ['گ'] = new(0xFB92, 0xFB93, 0xFB94, 0xFB95)
    };

    public static string FormatHebrew(string text) => ReverseLinesPreservingLtrRuns(text, false);

    public static string FormatArabic(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        StringBuilder shaped = new(text.Length);
        for (int i = 0; i < text.Length; i++)
        {
            char current = text[i];
            if (!Arabic.TryGetValue(current, out ArabicForms forms))
            {
                shaped.Append(current);
                continue;
            }

            int previousIndex = PreviousArabicIndex(text, i - 1);
            int nextIndex = NextArabicIndex(text, i + 1);
            bool joinsPrevious = previousIndex >= 0 && Arabic[text[previousIndex]].JoinsLeft;
            bool joinsNext = nextIndex >= 0 && forms.JoinsLeft;
            shaped.Append(joinsPrevious && joinsNext ? forms.Medial : joinsPrevious ? forms.Final : joinsNext ? forms.Initial : forms.Isolated);
        }

        return ReverseLinesPreservingLtrRuns(shaped.ToString(), true);
    }

    private static int PreviousArabicIndex(string text, int index)
    {
        while (index >= 0 && IsArabicMark(text[index])) index--;
        return index >= 0 && Arabic.ContainsKey(text[index]) ? index : -1;
    }

    private static int NextArabicIndex(string text, int index)
    {
        while (index < text.Length && IsArabicMark(text[index])) index++;
        return index < text.Length && Arabic.ContainsKey(text[index]) ? index : -1;
    }

    private static bool IsArabicMark(char c) => c >= 0x064B && c <= 0x065F;

    private static string ReverseLinesPreservingLtrRuns(string text, bool arabic)
    {
        if (string.IsNullOrEmpty(text)) return text;
        string[] lines = text.Replace("\r\n", "\n").Split('\n');
        for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            char[] chars = lines[lineIndex].ToCharArray();
            System.Array.Reverse(chars);
            int i = 0;
            while (i < chars.Length)
            {
                if (!IsLtrRunChar(chars[i])) { i++; continue; }
                int start = i;
                while (i < chars.Length && IsLtrRunChar(chars[i])) i++;
                System.Array.Reverse(chars, start, i - start);
            }
            lines[lineIndex] = new string(chars);
        }
        return string.Join("\n", lines);
    }

    private static bool IsLtrRunChar(char c) => char.IsLetterOrDigit(c) && !(c >= 0x0590 && c <= 0x08FF);
}
