using Store.ReviewService.Models;

namespace Store.ReviewService.Data;

/// <summary>
/// The reviews the demo catalogue starts with: three to six per product, rated 2 to 5, short and
/// long, written from a bank of sentences for the kind of product. They are the same in every
/// database - picked by a hash of the slug, never at random - and published from the start, so a
/// product page never looks empty. The products are named by slug: the catalogue's ids differ
/// between databases (<see cref="ReviewSeeder"/> looks them up).
/// </summary>
public static class DemoReviews
{
    /// <summary>1 - the first reviews of the demo catalogue.</summary>
    public const int Version = 1;

    /// <summary>What kind of piece a product is, for the sentences its reviews are written from.</summary>
    public enum Kind
    {
        Seating,
        Surface,
        Bedroom,
        Storage,
        Lighting,
        Rug,
        Decor,
        Garden
    }

    /// <summary>Every product of the demo catalogue by slug (kept equal to it by a test).</summary>
    public static IReadOnlyDictionary<string, Kind> Products { get; } = new Dictionary<string, Kind>
    {
        ["3-seater-sectional-sofa"] = Kind.Seating,
        ["8-drawer-dresser"] = Kind.Storage,
        ["abstract-ceramic-sculpture"] = Kind.Decor,
        ["acacia-garden-bench"] = Kind.Garden,
        ["adjustable-kids-desk"] = Kind.Surface,
        ["arched-oak-bookcase"] = Kind.Storage,
        ["backless-oak-counter-stool"] = Kind.Seating,
        ["balcony-folding-set"] = Kind.Garden,
        ["bamboo-towel-ladder"] = Kind.Storage,
        ["bathroom-linen-cabinet"] = Kind.Storage,
        ["bathroom-vanity-100cm"] = Kind.Storage,
        ["bathroom-wall-mirror-cabinet"] = Kind.Decor,
        ["bentwood-dining-chair"] = Kind.Seating,
        ["berber-style-rug-200x300"] = Kind.Rug,
        ["birch-book-display-shelf"] = Kind.Storage,
        ["boucle-modular-sofa"] = Kind.Seating,
        ["boucle-swivel-tub-chair"] = Kind.Seating,
        ["brass-task-lamp"] = Kind.Lighting,
        ["canvas-play-tent"] = Kind.Decor,
        ["ceramic-table-lamp"] = Kind.Lighting,
        ["classic-kitchen-cabinet-set"] = Kind.Storage,
        ["compact-oak-desk"] = Kind.Surface,
        ["contemporary-walnut-sideboard"] = Kind.Surface,
        ["decorative-vase-set"] = Kind.Decor,
        ["ergonomic-corner-desk"] = Kind.Surface,
        ["floating-oak-nightstand"] = Kind.Bedroom,
        ["fluted-oak-media-console"] = Kind.Surface,
        ["garden-bistro-set"] = Kind.Garden,
        ["hanging-rattan-egg-chair"] = Kind.Garden,
        ["house-wall-shelf"] = Kind.Storage,
        ["industrial-console-table"] = Kind.Surface,
        ["jute-round-rug-160"] = Kind.Rug,
        ["jute-rug-200x300"] = Kind.Rug,
        ["kids-bunk-bed"] = Kind.Bedroom,
        ["kids-play-table-with-2-stools"] = Kind.Surface,
        ["kids-rocking-armchair"] = Kind.Seating,
        ["kids-toy-storage-shelf"] = Kind.Storage,
        ["king-size-platform-bed"] = Kind.Bedroom,
        ["kitchen-island-with-storage"] = Kind.Surface,
        ["leather-bar-stool"] = Kind.Seating,
        ["leather-dining-chair"] = Kind.Seating,
        ["linen-cushion-cover-set"] = Kind.Decor,
        ["linen-lounge-armchair"] = Kind.Seating,
        ["linen-slipcover-sofa"] = Kind.Seating,
        ["marble-top-kitchen-island"] = Kind.Surface,
        ["mid-century-accent-chair"] = Kind.Seating,
        ["minimalist-tv-stand"] = Kind.Surface,
        ["modern-oak-dining-table"] = Kind.Surface,
        ["montessori-floor-bed"] = Kind.Bedroom,
        ["moroccan-pattern-rug"] = Kind.Rug,
        ["natural-latex-mattress-160x200"] = Kind.Bedroom,
        ["oak-bath-stool"] = Kind.Storage,
        ["oak-desk-chair"] = Kind.Seating,
        ["oak-dining-chair-set-of-2"] = Kind.Seating,
        ["oak-entryway-bench"] = Kind.Surface,
        ["oak-pantry-cabinet"] = Kind.Storage,
        ["oak-pedestal-side-table"] = Kind.Surface,
        ["oak-plank-coffee-table"] = Kind.Surface,
        ["oak-writing-desk"] = Kind.Surface,
        ["oatmeal-linen-sofa"] = Kind.Seating,
        ["olive-wood-serving-boards-set-of-3"] = Kind.Decor,
        ["open-back-bookcase"] = Kind.Storage,
        ["open-clothes-rail"] = Kind.Storage,
        ["open-oak-wall-shelves"] = Kind.Storage,
        ["outdoor-dining-set"] = Kind.Garden,
        ["paper-arc-floor-lamp"] = Kind.Lighting,
        ["patio-sofa-with-cushions"] = Kind.Garden,
        ["pine-kids-daybed"] = Kind.Bedroom,
        ["rattan-counter-stool"] = Kind.Seating,
        ["rattan-front-dresser"] = Kind.Storage,
        ["rattan-round-wall-mirror"] = Kind.Decor,
        ["round-brass-wall-mirror"] = Kind.Decor,
        ["round-dining-table"] = Kind.Surface,
        ["round-oak-kitchen-table"] = Kind.Surface,
        ["seagrass-basket-set"] = Kind.Decor,
        ["sliding-door-wardrobe"] = Kind.Storage,
        ["stoneware-dinner-set-16-pieces"] = Kind.Decor,
        ["stoneware-drum-table-lamp"] = Kind.Lighting,
        ["stonewashed-linen-bedding-set"] = Kind.Decor,
        ["stonewashed-linen-throw"] = Kind.Decor,
        ["storage-bed-frame"] = Kind.Bedroom,
        ["teak-garden-lounger"] = Kind.Garden,
        ["teak-shower-bench"] = Kind.Storage,
        ["terracotta-planter-set-3-pieces"] = Kind.Decor,
        ["travertine-bath-set-4-pieces"] = Kind.Storage,
        ["travertine-coffee-table"] = Kind.Surface,
        ["tripod-floor-lamp"] = Kind.Lighting,
        ["two-drawer-nightstand"] = Kind.Bedroom,
        ["upholstered-linen-bed"] = Kind.Bedroom,
        ["wall-coat-rack"] = Kind.Surface,
        ["walnut-sideboard"] = Kind.Surface,
    };

    private static readonly string[] Authors =
    [
        "Anna K.", "Tom W.", "Marta S.", "James P.", "Olivia R.", "Piotr D.", "Emma L.", "Lucas B.",
        "Sofia M.", "Noah G.", "Zofia T.", "Ethan H.", "Maja C.", "Jack F.", "Hannah J.", "Kuba N.",
        "Grace O.", "Ola Z.", "Daniel V.", "Julia E.", "Leo A.", "Ewa R.", "Sam T.", "Nina P."
    ];

    /// <summary>Mostly fours and fives, the odd three and two - the way real reviews fall.</summary>
    private static readonly int[] Ratings = [5, 4, 5, 3, 5, 4, 5, 5, 4, 2, 4, 5, 3, 5, 4];

    private static readonly Dictionary<int, string[]> Openers = new()
    {
        [5] = ["Exactly what we hoped for.", "Even nicer in person than in the photos.", "Worth every penny.", "We absolutely love it.",
               "Beautifully made and a joy to live with.", "The best thing we have bought for the flat this year."],
        [4] = ["Very happy with it overall.", "Lovely piece, with one small niggle.", "Good quality for the price.", "Looks great and feels solid.", "Nearly perfect."],
        [3] = ["It is fine, but not more than that.", "Mixed feelings about this one.", "Nice enough, though not quite what the photos suggested."],
        [2] = ["A bit of a disappointment.", "Not what I expected for the price."],
    };

    private static readonly Dictionary<int, string[]> Titles = new()
    {
        [5] = ["Love it", "Beautiful and solid", "Perfect for our home", "Better than expected", "Simply lovely"],
        [4] = ["Very good", "Happy with it", "Great value", "Nearly perfect"],
        [3] = ["It is fine", "Okay, not great", "Mixed feelings"],
        [2] = ["Disappointed", "Not for us"],
    };

    private static readonly Dictionary<Kind, (string[] Good, string[] Bad)> Details = new()
    {
        [Kind.Seating] = (
            ["The seat is firm at first and softens just enough after a couple of weeks.", "The fabric feels hard-wearing and the stitching is neat all round.",
             "It is deep enough to curl up in with a book.", "The legs are solid and it does not wobble at all.", "Our guests always ask where it is from."],
            ["The cushions lose their shape quickly and need plumping every day.", "The colour is a shade darker than on the screen.",
             "It is lower than I thought, so check the height before ordering."]),
        [Kind.Surface] = (
            ["The top has a lovely grain and the finish is easy to look after.", "It is sturdy - nothing moves when you lean on it.",
             "The size is just right for our small dining corner.", "The edges are nicely rounded, which matters with little ones around."],
            ["The surface marks easily if you forget a coaster.", "One of the legs needed a shim on our old floor.",
             "It is smaller in real life than it looks in the room picture."]),
        [Kind.Bedroom] = (
            ["We both sleep better since it arrived.", "It is quiet - no creaks when you turn over.",
             "The height is just right to sit on the edge in the morning.", "The wood looks warm and calm, exactly what a bedroom needs."],
            ["It creaks a little on one side.", "It took a couple of weeks for the new smell to go.", "The drawer runs a bit stiff."]),
        [Kind.Storage] = (
            ["It swallows far more than you would think.", "The drawers glide smoothly and close without a bang.",
             "The shelves stay level even when full of books.", "It makes the whole room feel tidier."],
            ["The back panel is thinner than I expected.", "One door hangs slightly lower than the other.",
             "It needs fixing to the wall, so plan for that."]),
        [Kind.Lighting] = (
            ["The light is soft and warm, perfect for evenings.", "The shade is beautifully made and throws a lovely glow.",
             "It turns a dark corner into the cosiest spot in the flat."],
            ["The cable is short, so it has to stand close to a socket.", "The switch feels a little cheap for the price.",
             "It gives mood light rather than enough light to read by."]),
        [Kind.Rug] = (
            ["Soft underfoot, and the colours are calm and natural.", "It stays flat and does not curl at the corners.",
             "It hides crumbs and paw prints remarkably well."],
            ["It shed quite a bit in the first weeks.", "It needs an underlay or it slides on a wooden floor.",
             "The edge is less even than I hoped."]),
        [Kind.Decor] = (
            ["It adds exactly the finishing touch the room needed.", "The texture is lovely and it feels well made.",
             "The colour matches the photos perfectly.", "It makes a great gift - my sister loved hers."],
            ["It is smaller than it looks in the pictures.", "The finish has a few uneven spots.", "Nice, but a little pricey for what it is."]),
        [Kind.Garden] = (
            ["It has stood outside through rain and sun and still looks good.", "Comfortable enough to spend a whole afternoon in.",
             "It packs away easily for the winter."],
            ["The cushions take ages to dry after rain.", "It needs oiling more often than the description says.",
             "Putting it together outdoors was fiddly."]),
    };

    private static readonly (string[] Good, string[] Bad) Delivery = (
        ["Delivery was quick and the courier carried it all the way up.", "Assembly took about twenty minutes with the tools in the box.",
         "It came well packed, without a scratch.", "Customer service answered my question within a day."],
        ["Delivery was a few days late.", "A screw was missing, but support sent one quickly.", "The box arrived dented, though the piece itself was fine."]);

    /// <summary>The reviews of one product, dated back from <paramref name="now"/>.</summary>
    public static List<Review> For(string slug, Kind kind, DateTime now)
    {
        var hash = Hash(slug);
        var count = 3 + (int)(hash % 4);
        var reviews = new List<Review>(count);
        for (var i = 0; i < count; i++)
        {
            var seed = hash + (uint)i * 2654435761u;
            var rating = Ratings[(int)((hash + (uint)i * 7) % (uint)Ratings.Length)];
            var createdAt = now.Date.AddDays(-(3 + (int)((seed >> 3) % 240))).AddHours(8 + (int)(seed % 12));
            reviews.Add(new Review
            {
                Id = Guid.NewGuid(),
                ProductSlug = slug,
                AuthorName = Pick(Authors, seed >> 5),
                Rating = rating,
                Title = Pick(Titles[rating], seed >> 7),
                Body = Body(kind, rating, seed),
                Status = ReviewStatus.Published,
                Source = ReviewSource.Seed,
                VerifiedPurchase = true,
                CreatedAt = createdAt,
                SubmittedAt = createdAt,
                UpdatedAt = createdAt
            });
        }

        return reviews;
    }

    /// <summary>An opener in the review's mood, one or two details about the piece and, now and then, the delivery.</summary>
    private static string Body(Kind kind, int rating, uint seed)
    {
        var (good, bad) = Details[kind];
        var sentences = new List<string> { Pick(Openers[rating], seed) };
        switch (rating)
        {
            case >= 4:
                sentences.Add(Pick(good, seed >> 9));
                if (seed % 3 == 0) sentences.Add(Pick(good, seed >> 11) is var second && second != sentences[^1] ? second : Pick(good, (seed >> 11) + 1));
                if (rating == 4) sentences.Add(Pick(bad, seed >> 13));
                if (seed % 2 == 0) sentences.Add(Pick(Delivery.Good, seed >> 15));
                break;
            case 3:
                sentences.Add(Pick(good, seed >> 9));
                sentences.Add(Pick(bad, seed >> 11));
                if (seed % 2 == 0) sentences.Add(Pick(Delivery.Bad, seed >> 15));
                break;
            default:
                sentences.Add(Pick(bad, seed >> 9));
                sentences.Add(Pick(Delivery.Bad, seed >> 11));
                break;
        }

        return string.Join(' ', sentences.Distinct());
    }

    private static string Pick(string[] options, uint seed) => options[(int)(seed % (uint)options.Length)];

    /// <summary>FNV-1a: the same number for the same slug on every machine, unlike string.GetHashCode.</summary>
    private static uint Hash(string text)
    {
        var hash = 2166136261u;
        foreach (var c in text)
        {
            hash = (hash ^ c) * 16777619u;
        }

        return hash;
    }
}
