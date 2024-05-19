using Microsoft.Extensions.Configuration;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

internal class PDFContract
{
    public static byte[] Generate(Book book)
    {
        // Loading fonts and using licence (only for authorized use)
        QuestPDF.Settings.License = LicenseType.Community;
        FontManager.RegisterFont(File.OpenRead("Lato-Regular.ttf"));
        FontManager.RegisterFont(File.OpenRead("Lato-Semibold.ttf"));

        // Composing a sample document
        Document doc = Document.Create(document => {
            document.Page(page =>
            {
                page.DefaultTextStyle(x => x.FontFamily("Lato"));
                page.Margin(25, Unit.Millimetre);
                page.Header().Text("Authoring contract for a book called '" + book.Title + "'").FontSize(32).SemiBold().FontColor(Colors.Blue.Medium);
                page.Content().Padding(20, Unit.Millimetre).Column(c =>
                {
                    c.Item().Text("The present document establishes a contract between ");
                    c.Item().Text("DemoEditor (10 Guttenberg Street 75000 Paris, France)").SemiBold();
                    c.Item().Text(" and ");
                    c.Item().Text(book.Editing.mainAuthor.Title).SemiBold();
                    c.Item().Text(" (" + book.Editing.mainAuthor.UserEmailAddress + ")" + Environment.NewLine);
                    c.Item().Text(Placeholders.LoremIpsum());
                });
            });
        });

        // Generates the PDF document
        return doc.GeneratePdf();
    }
}
