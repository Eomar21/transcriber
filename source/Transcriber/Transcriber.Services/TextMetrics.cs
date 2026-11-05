using System.Globalization;
using System.Text;

/// <summary>Text normalisation and word error rate, used to compare Whisper output to a reference.</summary>
public static class TextMetrics
{
    /// <summary>
    /// Lowercases, strips punctuation, and removes combining marks (Latin accents, Arabic harakat),
    /// then folds Arabic letter variants, so "أحراراً" and "احرارا" compare equal.
    /// </summary>
    public static string Normalize(string text)
    {
        var decomposed = text.Normalize(NormalizationForm.FormD).ToLowerInvariant();
        var sb = new StringBuilder(decomposed.Length);
        foreach (var ch in decomposed)
        {
            var cat = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (cat == UnicodeCategory.NonSpacingMark || ch == 'ـ') continue; // marks, tatweel

            switch (ch)
            {
                case 'أ': case 'إ': case 'آ': sb.Append('ا'); break;
                case 'ى': sb.Append('ي'); break;
                case 'ة': sb.Append('ه'); break;
                default:
                    if (char.IsLetterOrDigit(ch)) sb.Append(ch);
                    else sb.Append(' ');
                    break;
            }
        }
        return string.Join(' ', sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>Word error rate: word-level Levenshtein distance divided by reference word count.</summary>
    public static double WordErrorRate(string reference, string hypothesis)
    {
        var r = Normalize(reference).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var h = Normalize(hypothesis).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (r.Length == 0) return h.Length == 0 ? 0 : 1;

        var prev = new int[h.Length + 1];
        var curr = new int[h.Length + 1];
        for (var j = 0; j <= h.Length; j++) prev[j] = j;

        for (var i = 1; i <= r.Length; i++)
        {
            curr[0] = i;
            for (var j = 1; j <= h.Length; j++)
            {
                var cost = r[i - 1] == h[j - 1] ? 0 : 1;
                curr[j] = Math.Min(Math.Min(curr[j - 1] + 1, prev[j] + 1), prev[j - 1] + cost);
            }
            (prev, curr) = (curr, prev);
        }
        return (double)prev[h.Length] / r.Length;
    }
}
