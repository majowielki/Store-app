using System.Globalization;
using System.Text;
using System.Xml;

namespace Store.BuildingBlocks.Shop;

/// <summary>One page of a sitemap: its absolute address and, when known, the day it last changed.</summary>
public readonly record struct SitemapEntry(string Location, DateTime? LastModified = null);

/// <summary>
/// A sitemap in the sitemaps.org format (a <c>urlset</c>). A service lists the pages of the shop
/// that show its data - the catalogue its products, the content service its editorial pages - and
/// the UI's <c>/sitemap.xml</c> index points search engines at each list.
/// </summary>
public static class Sitemap
{
    public const string ContentType = "application/xml";

    /// <summary>The protocol's limit for one file; a longer list needs a second file.</summary>
    public const int MaxEntries = 50_000;

    private const string Namespace = "http://www.sitemaps.org/schemas/sitemap/0.9";

    /// <summary>The XML of the sitemap; the addresses are escaped, the dates are written as W3C days (UTC).</summary>
    public static string Render(IEnumerable<SitemapEntry> entries)
    {
        var list = entries.ToList();
        if (list.Count > MaxEntries)
        {
            throw new ArgumentOutOfRangeException(nameof(entries), list.Count, $"A sitemap holds at most {MaxEntries} pages");
        }

        var output = new Utf8StringWriter();
        using (var writer = XmlWriter.Create(output, new XmlWriterSettings { Indent = true }))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("urlset", Namespace);
            foreach (var entry in list)
            {
                writer.WriteStartElement("url", Namespace);
                writer.WriteElementString("loc", Namespace, entry.Location);
                if (entry.LastModified is { } changed)
                {
                    writer.WriteElementString("lastmod", Namespace, DateTime.SpecifyKind(changed, DateTimeKind.Utc).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                }
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
            writer.WriteEndDocument();
        }
        return output.ToString();
    }

    /// <summary>A StringWriter declares UTF-16 in the XML header; the response is UTF-8.</summary>
    private sealed class Utf8StringWriter : StringWriter
    {
        public Utf8StringWriter() : base(CultureInfo.InvariantCulture)
        {
        }

        public override Encoding Encoding => Encoding.UTF8;
    }
}
