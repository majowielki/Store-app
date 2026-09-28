using Store.Contracts.Catalog;

namespace Store.ProductService.Models;

/// <summary>
/// Every finish the shop sells its products in, the way the product pictures show them. A product's
/// colours are keys of these finishes; the shop draws each one's swatch and filters them by family.
/// The colours are averages of the pictures where the studio light falls, and the textures crops of
/// them (Scripts/swatches.json lists both; Scripts/make-swatches.cs makes the textures and prints the
/// colours). The few colours no picture shows - variants sold but not photographed - are chosen to
/// sit beside the photographed ones.
/// </summary>
public static class FinishCatalogue
{
    /// <summary>The surfaces and colours finishes are made of, each with the colour the pictures give it.</summary>
    private static class Parts
    {
        // Woods
        public static readonly SwatchPart NaturalOak = new("#cca883", SwatchTexture.NaturalOak);
        public static readonly SwatchPart LightOak = new("#b39571", SwatchTexture.LightOak);
        public static readonly SwatchPart HoneyOak = new("#c8a683", SwatchTexture.HoneyOak);
        public static readonly SwatchPart Walnut = new("#b98c74", SwatchTexture.Walnut);
        public static readonly SwatchPart Teak = new("#d8b17e", SwatchTexture.Teak);
        public static readonly SwatchPart Pine = new("#ae8767", SwatchTexture.Pine);
        public static readonly SwatchPart Birch = new("#a67b53", SwatchTexture.Birch);
        public static readonly SwatchPart Beech = new("#ba8b67", SwatchTexture.Beech);
        public static readonly SwatchPart Bamboo = new("#a48862", SwatchTexture.Bamboo);
        public static readonly SwatchPart Acacia = new("#905a30", SwatchTexture.Acacia);
        public static readonly SwatchPart OliveWood = new("#c0875b", SwatchTexture.OliveWood);
        public static readonly SwatchPart ReclaimedWood = new("#a28167", SwatchTexture.ReclaimedWood);
        public static readonly SwatchPart Rubberwood = new("#936740", SwatchTexture.Rubberwood);

        // Weaves
        public static readonly SwatchPart Rattan = new("#a48367", SwatchTexture.Rattan);
        public static readonly SwatchPart Cane = new("#a5825e", SwatchTexture.Cane);
        public static readonly SwatchPart RattanWeave = new("#ab8968", SwatchTexture.RattanWeave);
        public static readonly SwatchPart GreyRattan = new("#9e8b76", SwatchTexture.GreyRattan);
        public static readonly SwatchPart Seagrass = new("#977252", SwatchTexture.Seagrass);
        public static readonly SwatchPart Jute = new("#b49272", SwatchTexture.Jute);

        // Stone and ceramics
        public static readonly SwatchPart Travertine = new("#bba084", SwatchTexture.Travertine);
        public static readonly SwatchPart Marble = new("#d0cbc6", SwatchTexture.Marble);
        public static readonly SwatchPart Terracotta = new("#875637", SwatchTexture.Terracotta);
        public static readonly SwatchPart SpeckledStoneware = new("#c3b59e", SwatchTexture.SpeckledStoneware);
        public static readonly SwatchPart GreyStoneware = new("#9b9590", SwatchTexture.GreyStoneware);
        public static readonly SwatchPart Stone = new("#c8beb4", SwatchTexture.Stone);
        public static readonly SwatchPart WhiteGlaze = new("#d0cac2", SwatchTexture.WhiteGlaze);

        // Metals
        public static readonly SwatchPart Brass = new("#a1876c", SwatchTexture.Brass);
        public static readonly SwatchPart BlackSteel = new("#6d6c6f", SwatchTexture.BlackSteel);

        // Fabrics, leathers and paints
        public static readonly SwatchPart CreamBoucle = new("#d7cfc4");
        public static readonly SwatchPart IvoryLinen = new("#e3dcd6");
        public static readonly SwatchPart OatmealLinen = new("#c9bfb3");
        public static readonly SwatchPart NavyLinen = new("#364358");
        public static readonly SwatchPart Teal = new("#4e6972");
        public static readonly SwatchPart SandLinen = new("#dbcec0");
        public static readonly SwatchPart SlateLeather = new("#716c69");
        public static readonly SwatchPart CognacLeather = new("#945b41");
        public static readonly SwatchPart CreamPaint = new("#c8bfb0");
        public static readonly SwatchPart WhitePaint = new("#eee5db");
        public static readonly SwatchPart SagePaint = new("#b8baaa");
        public static readonly SwatchPart Ivory = new("#eae1d4");
        public static readonly SwatchPart RustLinen = new("#ad634f");
        public static readonly SwatchPart CushionCream = new("#e1d5cb");
        public static readonly SwatchPart CushionRust = new("#8e462e");
        public static readonly SwatchPart NaturalLinen = new("#d9d5cb");
        public static readonly SwatchPart BlushBoucle = new("#e3cac4");
        public static readonly SwatchPart PatioOatmeal = new("#c0b2a1");
        public static readonly SwatchPart PatioTaupe = new("#bbac98");
        public static readonly SwatchPart Canvas = new("#dacec2");
        public static readonly SwatchPart RugCream = new("#e6d7c2");
        public static readonly SwatchPart RugRust = new("#bf7e66");
        public static readonly SwatchPart HeadboardOatmeal = new("#dfd7ce");
        public static readonly SwatchPart HeadboardLinen = new("#c6bcb2");
        public static readonly SwatchPart ShadeWhite = new("#cdc6b7");
        public static readonly SwatchPart ShadePleated = new("#c2ac97");
        public static readonly SwatchPart ShadeOatmeal = new("#c9bcaf");

        // Sold but not photographed
        public static readonly SwatchPart PebbleGrey = new("#b9b3a9");
        public static readonly SwatchPart StoneGrey = new("#a39e96");
        public static readonly SwatchPart SageCushion = new("#a7ae98");
        public static readonly SwatchPart BlushPattern = new("#c98a7c");
    }

    // Woods, weaves, stone and metal on their own
    public static readonly Finish NaturalOak = Of("natural-oak", "Natural oak", Color.Brown, Parts.NaturalOak);
    public static readonly Finish LightOak = Of("light-oak", "Light oak", Color.Brown, Parts.LightOak);
    public static readonly Finish HoneyOak = Of("honey-oak", "Honey oak", Color.Brown, Parts.HoneyOak);
    public static readonly Finish Walnut = Of("walnut", "Walnut", Color.Brown, Parts.Walnut);
    public static readonly Finish Teak = Of("teak", "Teak", Color.Brown, Parts.Teak);
    public static readonly Finish Pine = Of("pine", "Pine", Color.Brown, Parts.Pine);
    public static readonly Finish Birch = Of("birch", "Birch", Color.Brown, Parts.Birch);
    public static readonly Finish Bamboo = Of("bamboo", "Bamboo", Color.Brown, Parts.Bamboo);
    public static readonly Finish Acacia = Of("acacia", "Acacia", Color.Brown, Parts.Acacia);
    public static readonly Finish OliveWood = Of("olive-wood", "Olive wood", Color.Brown, Parts.OliveWood);
    public static readonly Finish Rubberwood = Of("rubberwood", "Rubberwood", Color.Brown, Parts.Rubberwood);
    public static readonly Finish Rattan = Of("rattan", "Rattan", Color.Brown, Parts.Rattan);
    public static readonly Finish Seagrass = Of("seagrass", "Seagrass", Color.Brown, Parts.Seagrass);
    public static readonly Finish Jute = Of("jute", "Jute", Color.Brown, Parts.Jute);
    public static readonly Finish GreyRattan = Of("grey-rattan", "Grey rattan", Color.Gray, Parts.GreyRattan);
    public static readonly Finish Travertine = Of("travertine", "Travertine", Color.White, Parts.Travertine);
    public static readonly Finish Terracotta = Of("terracotta", "Terracotta", Color.Orange, Parts.Terracotta);
    public static readonly Finish SpeckledCream = Of("speckled-cream", "Speckled cream", Color.White, Parts.SpeckledStoneware);
    public static readonly Finish SpeckledGrey = Of("speckled-grey", "Speckled grey", Color.Gray, Parts.GreyStoneware);
    public static readonly Finish Brass = Of("brass", "Brass", Color.Gold, Parts.Brass);

    // Two materials or colours, the one covering more of the product first
    public static readonly Finish ReclaimedWoodBlackSteel = Of("reclaimed-wood-black-steel", "Reclaimed wood and black steel", Color.Brown, Parts.ReclaimedWood, Parts.BlackSteel);
    public static readonly Finish OakBlackSteel = Of("oak-black-steel", "Oak and black steel", Color.Brown, Parts.NaturalOak, Parts.BlackSteel);
    public static readonly Finish OakOatmeal = Of("oak-oatmeal", "Oak and oatmeal", Color.Brown, Parts.NaturalOak, Parts.OatmealLinen);
    public static readonly Finish RattanBlackSteel = Of("rattan-black-steel", "Rattan and black steel", Color.Brown, Parts.RattanWeave, Parts.BlackSteel);
    public static readonly Finish CognacLeatherOak = Of("cognac-leather-oak", "Cognac leather and oak", Color.Brown, Parts.CognacLeather, Parts.NaturalOak);
    public static readonly Finish WalnutOatmeal = Of("walnut-oatmeal", "Walnut and oatmeal", Color.Brown, Parts.Walnut, Parts.HeadboardLinen);
    public static readonly Finish OakCane = Of("oak-cane", "Oak and cane", Color.Brown, Parts.NaturalOak, Parts.Cane);
    public static readonly Finish AcaciaBlackSteel = Of("acacia-black-steel", "Acacia and black steel", Color.Brown, Parts.Acacia, Parts.BlackSteel);
    public static readonly Finish BlackSteelOak = Of("black-steel-oak", "Black steel and oak", Color.Black, Parts.BlackSteel, Parts.NaturalOak);
    public static readonly Finish TealWalnut = Of("teal-walnut", "Teal and walnut", Color.Teal, Parts.Teal, Parts.Walnut);
    public static readonly Finish TealBeech = Of("teal-beech", "Teal and beech", Color.Teal, Parts.Teal, Parts.Beech);
    public static readonly Finish LinenOak = Of("linen-oak", "Linen and oak", Color.White, Parts.ShadeWhite, Parts.NaturalOak);
    public static readonly Finish PaperBlackSteel = Of("paper-black-steel", "Paper and black steel", Color.White, Parts.WhitePaint, Parts.BlackSteel);
    public static readonly Finish OakCream = Of("oak-cream", "Oak and cream", Color.White, Parts.NaturalOak, Parts.CreamPaint);
    public static readonly Finish MarbleWhite = Of("marble-white", "Marble and white", Color.White, Parts.Marble, Parts.WhitePaint);
    public static readonly Finish OatmealOak = Of("oatmeal-oak", "Oatmeal and oak", Color.White, Parts.HeadboardOatmeal, Parts.NaturalOak);
    public static readonly Finish WhiteOak = Of("white-oak", "White and oak", Color.White, Parts.WhitePaint, Parts.NaturalOak);
    public static readonly Finish WhiteGlazeLinen = Of("white-glaze-linen", "White glaze and linen", Color.White, Parts.WhiteGlaze, Parts.ShadePleated);
    public static readonly Finish StoneOatmeal = Of("stone-oatmeal", "Stone and oatmeal", Color.White, Parts.Stone, Parts.ShadeOatmeal);
    public static readonly Finish CreamRustStripe = Of("cream-rust-stripe", "Cream with rust stripes", Color.White, Parts.RugCream, Parts.RugRust);
    public static readonly Finish WhiteBirch = Of("white-birch", "White and birch", Color.White, Parts.WhitePaint, Parts.Birch);
    public static readonly Finish OatmealTaupe = Of("oatmeal-taupe", "Oatmeal and taupe", Color.White, Parts.PatioOatmeal, Parts.PatioTaupe);
    public static readonly Finish SlateLeatherOak = Of("slate-leather-oak", "Slate leather and oak", Color.Gray, Parts.SlateLeather, Parts.NaturalOak);
    public static readonly Finish PebbleBeech = Of("pebble-beech", "Pebble and beech", Color.Gray, Parts.PebbleGrey, Parts.Beech);
    public static readonly Finish SageOak = Of("sage-oak", "Sage and oak", Color.Green, Parts.SagePaint, Parts.NaturalOak);
    public static readonly Finish CreamSage = Of("cream-sage", "Cream and sage", Color.Green, Parts.CushionCream, Parts.SageCushion);
    public static readonly Finish CreamRust = Of("cream-rust", "Cream and rust", Color.Orange, Parts.CushionCream, Parts.CushionRust);
    public static readonly Finish TerracottaLinen = Of("terracotta-linen", "Terracotta and linen", Color.Orange, Parts.Terracotta, Parts.ShadePleated);
    public static readonly Finish BrassMarble = Of("brass-marble", "Brass and marble", Color.Gold, Parts.Brass, Parts.Marble);
    public static readonly Finish CreamBlush = Of("cream-blush", "Cream and blush", Color.Pink, Parts.RugCream, Parts.BlushPattern);
    public static readonly Finish BlushBeech = Of("blush-beech", "Blush and beech", Color.Pink, Parts.BlushBoucle, Parts.Beech);

    // Fabrics and paints on their own
    public static readonly Finish CreamBoucle = Of("cream-boucle", "Cream bouclé", Color.White, Parts.CreamBoucle);
    public static readonly Finish IvoryLinen = Of("ivory-linen", "Ivory linen", Color.White, Parts.IvoryLinen);
    public static readonly Finish OatmealLinen = Of("oatmeal-linen", "Oatmeal linen", Color.White, Parts.OatmealLinen);
    public static readonly Finish SandLinen = Of("sand-linen", "Sand linen", Color.White, Parts.SandLinen);
    public static readonly Finish PaintedWhite = Of("painted-white", "Painted white", Color.White, Parts.WhitePaint);
    public static readonly Finish Ivory = Of("ivory", "Ivory", Color.White, Parts.Ivory);
    public static readonly Finish NaturalLinen = Of("natural-linen", "Natural linen", Color.White, Parts.NaturalLinen);
    public static readonly Finish CreamLinen = Of("cream-linen", "Cream linen", Color.White, Parts.CushionCream);
    public static readonly Finish NaturalCanvas = Of("natural-canvas", "Natural canvas", Color.White, Parts.Canvas);
    public static readonly Finish PebbleBoucle = Of("pebble-boucle", "Pebble bouclé", Color.Gray, Parts.PebbleGrey);
    public static readonly Finish StoneGreyLinen = Of("stone-grey-linen", "Stone grey linen", Color.Gray, Parts.StoneGrey);
    public static readonly Finish NavyLinen = Of("navy-linen", "Navy linen", Color.Navy, Parts.NavyLinen);
    public static readonly Finish RustLinen = Of("rust-linen", "Rust linen", Color.Orange, Parts.RustLinen);

    /// <summary>Every finish, in the order the admin palette shows them: woods and materials, pairs, fabrics.</summary>
    public static IReadOnlyList<Finish> All { get; } =
    [
        NaturalOak, LightOak, HoneyOak, Walnut, Teak, Pine, Birch, Bamboo, Acacia, OliveWood, Rubberwood,
        Rattan, Seagrass, Jute, GreyRattan, Travertine, Terracotta, SpeckledCream, SpeckledGrey, Brass,
        ReclaimedWoodBlackSteel, OakBlackSteel, OakOatmeal, RattanBlackSteel, CognacLeatherOak, WalnutOatmeal,
        OakCane, AcaciaBlackSteel, BlackSteelOak, TealWalnut, TealBeech, LinenOak, PaperBlackSteel, OakCream,
        MarbleWhite, OatmealOak, WhiteOak, WhiteGlazeLinen, StoneOatmeal, CreamRustStripe, WhiteBirch,
        OatmealTaupe, SlateLeatherOak, PebbleBeech, SageOak, CreamSage, CreamRust, TerracottaLinen, BrassMarble,
        CreamBlush, BlushBeech,
        CreamBoucle, IvoryLinen, OatmealLinen, SandLinen, PaintedWhite, Ivory, NaturalLinen, CreamLinen,
        NaturalCanvas, PebbleBoucle, StoneGreyLinen, NavyLinen, RustLinen,
    ];

    private static readonly Dictionary<string, Finish> ByKey = All.ToDictionary(finish => finish.Key, StringComparer.Ordinal);

    private static readonly ILookup<Color, Finish> ByFamily = All.ToLookup(finish => finish.Family);

    /// <summary>The finish of a product colour, null for a key the shop does not know.</summary>
    public static Finish? Find(string key) => ByKey.GetValueOrDefault(key);

    /// <summary>Whether <paramref name="key"/> names a finish; keys are lowercase, as products store them.</summary>
    public static bool Exists(string? key) => key is not null && ByKey.ContainsKey(key);

    /// <summary>The finishes the colour filter shows under <paramref name="family"/>.</summary>
    public static IEnumerable<Finish> OfFamily(Color family) => ByFamily[family];

    private static Finish Of(string key, string name, Color family, params SwatchPart[] swatch) => new(key, name, family, swatch);
}
