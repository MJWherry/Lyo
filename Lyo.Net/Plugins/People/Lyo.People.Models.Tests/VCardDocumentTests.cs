namespace Lyo.People.Models.Tests;

public class VCardDocumentTests
{
    private readonly VCardDocument _cards = new();

    [Fact]
    public void Read_RepeatedEmailAndPhone_KeepsAll()
    {
        const string text = """
            BEGIN:VCARD
            VERSION:3.0
            FN:Ada Lovelace
            N:Lovelace;Ada;;;
            TEL:111
            TEL:222
            EMAIL:a@x.com
            EMAIL:b@x.com
            END:VCARD
            """;

        var card = _cards.Read(text);

        Assert.Equal(["111", "222"], card.Telephones);
        Assert.Equal(["a@x.com", "b@x.com"], card.Emails);
    }
}
