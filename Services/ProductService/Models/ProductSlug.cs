using System.Text;

namespace Store.ProductService.Models;

/// <summary>
/// The address-friendly name of a product ("Bouclé Modular Sofa" -> "boucle-modular-sofa"). It is
/// made once, from the title the product is created with, and stays when the title changes, so
/// content that refers to a product by its slug keeps working. The accents folded here are the
/// ones the migration that introduced slugs folds in SQL; keep the two lists equal.
/// </summary>
public static class ProductSlug
{
    /// <summary>Accented letters and their plain equivalents, index for index.</summary>
    public const string Accented = "àáâãäåąçćčďèéêëęěìíîïłńňñòóôõöøřśšťùúûüůýÿźżž";
    public const string Plain = "aaaaaaacccdeeeeeeiiiilnnnoooooorsstuuuuuyyzzz";

    /// <summary>What a product whose title holds no letter or digit is called.</summary>
    public const string Fallback = "product";

    public static string From(string title)
    {
        var slug = new StringBuilder(title.Length);
        var dash = false;
        foreach (var character in title.ToLowerInvariant())
        {
            var index = Accented.IndexOf(character, StringComparison.Ordinal);
            var letter = index >= 0 ? Plain[index] : character;
            if (letter is >= 'a' and <= 'z' or >= '0' and <= '9')
            {
                slug.Append(letter);
                dash = false;
            }
            else if (slug.Length > 0 && !dash)
            {
                slug.Append('-');
                dash = true;
            }
        }

        var result = slug.ToString().TrimEnd('-');
        return result.Length == 0 ? Fallback : result;
    }
}
