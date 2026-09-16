namespace Lyo.Book;

/// <summary>Word and character counts for page and chapter bodies.</summary>
public static class BookText
{
    public static int CountCharacters(string? text) => text?.Length ?? 0;

    public static int CountWords(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        var count = 0;
        var inWord = false;
        foreach (var c in text) {
            if (char.IsWhiteSpace(c)) {
                inWord = false;
                continue;
            }

            if (inWord)
                continue;

            inWord = true;
            count++;
        }

        return count;
    }
}
