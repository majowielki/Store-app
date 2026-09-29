using Store.BuildingBlocks.Pictures;
using Store.ContentService.Models;
using Store.Contracts.Catalog;

namespace Store.ContentService.Data;

/// <summary>
/// The content a new shop starts with: the five makers behind the catalogue, three collections,
/// three journal articles and five lookbooks. Products are named by their catalogue slug; the
/// pictures live next to the product pictures (Blobs/ in the repository).
/// </summary>
public static class DemoContent
{
    public static List<Maker> Makers(PictureLinks pictures) =>
    [
        new()
        {
            Slug = "modenza", Name = "Modenza", Company = Company.Modenza, IsPublished = true,
            Tagline = "Modern classics in oak and walnut", Location = "Udine, Italy", FoundedYear = 1978,
            CoverImage = pictures.Of("Maker-Modenza.webp"),
            Story = """
                Modenza began as a two-bench joinery on the edge of Udine, making dining tables for the families around it. Forty-odd years later the benches are longer, but the way of working has hardly changed: solid oak and walnut, joints cut and glued rather than screwed, and a finish of oil and hard wax you can renew yourself.

                ## What they make

                Dining tables and chairs, sideboards, desks and the quieter pieces of a bedroom - a floating nightstand, a dresser with woven cane fronts. The shapes are modern, the construction is old-fashioned: mortise and tenon, dovetailed drawers, backs that are made to be seen.

                ## Why we work with them

                Every Modenza piece can be repaired. Scratches sand out, a leg can be replaced, and the finish comes back with a cloth and a tin of oil. That is the kind of furniture we want to sell.
                """,
        },
        new()
        {
            Slug = "luxora", Name = "Luxora", Company = Company.Luxora, IsPublished = true,
            Tagline = "Soft living: upholstery, glass and ceramics", Location = "Porto, Portugal", FoundedYear = 1994,
            CoverImage = pictures.Of("Maker-Luxora.webp"),
            Story = """
                Luxora started in a small upholstery workshop in Porto and grew into a studio that dresses whole rooms: sofas in bouclé and washed linen, lamps with paper and linen shades, stoneware for the table and brass for the walls.

                ## What they make

                Sofas and armchairs on kiln-dried pine frames with feather-wrapped cushions, glazed stoneware from a pottery two streets away, and mirrors and lamps finished by hand.

                ## Why we work with them

                Luxora cuts every cover to come off and go in the wash, and keeps making spare covers for pieces it no longer sells. A sofa that can be recovered is a sofa that stays.
                """,
        },
        new()
        {
            Slug = "artifex", Name = "Artifex", Company = Company.Artifex, IsPublished = true,
            Tagline = "Steel and solid wood, built to last", Location = "Wrocław, Poland", FoundedYear = 2006,
            CoverImage = pictures.Of("Maker-Artifex.webp"),
            Story = """
                Artifex is a metal shop and a wood shop under one roof. The steel frames are welded and powder-coated next door to where the teak, acacia and oak are cut, so the two halves of a piece are made to fit each other rather than a catalogue number.

                ## What they make

                Shelving and clothes rails, counter stools, garden loungers, benches and folding balcony sets, and children's shelves in birch plywood with rounded corners.

                ## Why we work with them

                Their outdoor pieces use only woods that stand the weather on their own, with stainless fixings that do not stain. Indoors, everything comes apart for a move and goes back together with the same bolts.
                """,
        },
        new()
        {
            Slug = "comfora", Name = "Comfora", Company = Company.Comfora, IsPublished = true,
            Tagline = "Comfort engineered, from chairs to mattresses", Location = "Aarhus, Denmark", FoundedYear = 1989,
            CoverImage = pictures.Of("Maker-Comfora.webp"),
            Story = """
                Comfora tests before it sells. Its studio outside Aarhus has a room full of chairs, beds and sofas that people sit and sleep on for weeks, with notes on what aches and what does not.

                ## What they make

                Natural latex mattresses, upholstered beds, reading armchairs and linen sofas, chairs for working at home, rugs, and the textiles that go with all of them: stonewashed linen bedding, throws and cushion covers.

                ## Why we work with them

                Comfora publishes what goes into each piece - latex, wool, cotton, the density of every foam - and uses nothing it would not sleep on itself.
                """,
        },
        new()
        {
            Slug = "homestead", Name = "Homestead", Company = Company.Homestead, IsPublished = true,
            Tagline = "Practical pieces for family homes", Location = "North Yorkshire, England", FoundedYear = 1962,
            CoverImage = pictures.Of("Maker-Homestead.webp"),
            Story = """
                Homestead has made furniture for family homes for three generations: pine beds children can climb into, pantry cabinets with room for everything, benches for the hallway and baskets for the rest.

                ## What they make

                Kitchen and pantry cabinets, round tables, children's beds and play tables, entryway benches and coat racks, seagrass baskets and terracotta pots for the garden.

                ## Why we work with them

                Homestead builds for real life: painted finishes that can be touched up, rounded edges, drawers on solid wooden runners. Nothing is precious, and everything lasts.
                """,
        },
    ];

    public static List<Collection> Collections(PictureLinks pictures) =>
    [
        new()
        {
            Slug = "warm-minimal", Title = "Warm Minimal", SortOrder = 1, IsPublished = true,
            CoverImage = pictures.Of("Collection-WarmMinimal.webp"),
            Summary = "Fewer things, chosen well: oak, linen, bouclé and stone in a room that has space to breathe.",
            Body = """
                Warm minimalism is not an empty room. It is a room where every piece earns its place and the materials do the decorating: the grain of oak, the loops of bouclé, the pores of travertine, linen that creases the way it wants to.

                Start with one generous piece to sit on, add a low table in a natural material and a single source of soft light. Leave the floor mostly bare - one rug, not three - and let one or two ceramics carry the colour.
                """,
            ProductSlugs =
            [
                "boucle-modular-sofa", "boucle-swivel-tub-chair", "travertine-coffee-table", "oak-plank-coffee-table",
                "paper-arc-floor-lamp", "fluted-oak-media-console", "linen-lounge-armchair", "stoneware-drum-table-lamp",
                "decorative-vase-set", "jute-rug-200x300",
            ],
        },
        new()
        {
            Slug = "small-spaces", Title = "Small Spaces", SortOrder = 2, IsPublished = true,
            CoverImage = pictures.Of("Collection-SmallSpaces.webp"),
            Summary = "Pieces that do two jobs, fold away or hang on the wall - for flats where every metre counts.",
            Body = """
                In a small flat the floor is the most precious surface you have, so get things off it: a nightstand that hangs on the wall, a coat rack with a shelf, open shelves above the counter. Then choose furniture that works twice - a desk that doubles as a table for two, a bench that stores shoes, a folding set for the balcony.

                Keep the palette light and the legs visible: a room looks bigger when you can see the floor run under the furniture.
                """,
            ProductSlugs =
            [
                "floating-oak-nightstand", "compact-oak-desk", "wall-coat-rack", "open-clothes-rail", "oak-entryway-bench",
                "round-oak-kitchen-table", "backless-oak-counter-stool", "open-oak-wall-shelves", "oak-pedestal-side-table",
                "balcony-folding-set",
            ],
        },
        new()
        {
            Slug = "outdoor-season", Title = "Outdoor Season", SortOrder = 3, IsPublished = true,
            CoverImage = pictures.Of("Collection-OutdoorSeason.webp"),
            Summary = "Teak, acacia and woven rattan for long evenings on the terrace, the balcony and the lawn.",
            Body = """
                Outdoor furniture should be the easiest furniture you own. Teak and acacia need nothing but a coat of oil in spring, woven synthetic rattan shrugs off the rain, and cushions with washable covers go in the machine when summer is over.

                Plan the terrace like a room: somewhere to eat, somewhere to lie down and somewhere to sit with a drink - and pots of herbs to tie it all together.
                """,
            ProductSlugs =
            [
                "outdoor-dining-set", "teak-garden-lounger", "hanging-rattan-egg-chair", "garden-bistro-set",
                "patio-sofa-with-cushions", "acacia-garden-bench", "balcony-folding-set", "terracotta-planter-set-3-pieces",
            ],
        },
    ];

    public static List<Article> Articles(PictureLinks pictures) =>
    [
        new()
        {
            Slug = "how-to-choose-a-sofa", Title = "How to choose a sofa for your room", IsPublished = true,
            Author = "The Store journal", PublishedAt = new DateTime(2026, 9, 10, 8, 0, 0, DateTimeKind.Utc),
            CoverImage = pictures.Of("Journal-ChoosingASofa.webp"),
            Excerpt = "Measure twice, sit for ten minutes and think about who will use it: a short guide to size, depth, fabric and shape.",
            ProductSlugs = ["linen-slipcover-sofa", "oatmeal-linen-sofa", "boucle-modular-sofa", "3-seater-sectional-sofa"],
            Body = """
                A sofa is the piece you use most and replace least, so it is worth an evening of thinking before it arrives.

                ## Measure the room, then the way in

                Mark the sofa's footprint on the floor with tape and live with it for a day: you want at least 45 cm between the sofa and a coffee table, and a clear path around it. Then measure the doors, the stairs and the lift - the most common reason a sofa goes back is that it never got in.

                ## Depth and height

                Seat depth decides how you sit. Around 55 cm suits upright sitting and shorter legs; 60 cm and more is for curling up. A low back looks lighter in a small room, a high back holds your head while you read. If you can, sit on it for ten minutes - the first minute tells you nothing.

                ## Fabric

                - **Linen** breathes, softens and creases; a removable slipcover makes it the easy choice with children.
                - **Bouclé** hides wear in its loops and feels warm, but catches a cat's claws.
                - **Tightly woven fabrics** forgive the most in everyday life.

                ## Shape

                A straight three-seater fits most rooms and moves house easily. A chaise or a corner turns a sofa into a place to lie down, and a modular sofa lets you change your mind when you move.

                Whatever you choose, look at the legs: a sofa that stands on legs makes a room look larger than one that sits on the floor.
                """,
        },
        new()
        {
            Slug = "caring-for-oak", Title = "Caring for oak furniture", IsPublished = true,
            Author = "The Store journal", PublishedAt = new DateTime(2026, 9, 3, 8, 0, 0, DateTimeKind.Utc),
            CoverImage = pictures.Of("Journal-CaringForOak.webp"),
            Excerpt = "Oiled oak asks for very little: a damp cloth, a coaster and a new coat of oil once or twice a year.",
            ProductSlugs = ["modern-oak-dining-table", "round-dining-table", "oak-plank-coffee-table", "oak-writing-desk"],
            Body = """
                Most of the oak furniture we sell is finished with oil and hard wax. The oil soaks into the wood instead of sitting on top of it, which means the surface can be repaired - and that it likes a little attention.

                ## Every day

                Wipe with a cloth wrung out in water and dry the wood straight after. Skip sprays and polishes: they leave a film the next coat of oil cannot get through. Put coasters under hot mugs and wipe up red wine and cooking oil before they soak in.

                ## Twice a year

                When the top looks dry or water no longer beads on it, clean it, let it dry and rub in a thin coat of oil with a lint-free cloth, along the grain. After ten minutes wipe off whatever the wood has not taken, and let it harden overnight before you lay the table.

                Oily cloths can catch fire on their own as they dry: spread them flat outdoors or soak them in water before you throw them away.

                ## Marks and scratches

                Light scratches usually disappear under a coat of oil. For deeper ones, sand gently along the grain with fine paper, then oil the spot; it blends in within a few weeks.

                One more thing: oak darkens and warms over the years. Move lamps and bowls around now and then so the top ages evenly.
                """,
        },
        new()
        {
            Slug = "small-flat-big-ideas", Title = "Small flat, big ideas", IsPublished = true,
            Author = "The Store journal", PublishedAt = new DateTime(2026, 8, 27, 8, 0, 0, DateTimeKind.Utc),
            CoverImage = pictures.Of("Journal-SmallFlatBigIdeas.webp"),
            Excerpt = "Five ideas that make a small flat feel generous, from walls that work to furniture with two jobs.",
            ProductSlugs = ["floating-oak-nightstand", "wall-coat-rack", "compact-oak-desk", "open-clothes-rail", "balcony-folding-set"],
            Body = """
                A small flat is less a problem to solve than a set of choices to make. These five change the most.

                ## 1. Clear the floor

                The more floor you can see, the bigger the room feels. Hang what you can: a floating nightstand, shelves above the desk, a coat rack by the door.

                ## 2. Give every piece two jobs

                A compact desk is also a table for two; a bench in the hall stores shoes; a round kitchen table fits where a rectangular one blocks the way.

                ## 3. Keep the clothes rail honest

                An open rail holds only what you actually wear - and it saves the wall a wardrobe would swallow.

                ## 4. Use light, not paint, to make space

                Sheer curtains, pale floors and lamps at different heights do more for a small room than any colour on the walls.

                ## 5. Count the balcony as a room

                Two folding chairs and a small table turn a balcony into the breakfast room you had no space for.
                """,
        },
    ];

    public static List<Lookbook> Lookbooks(PictureLinks pictures) =>
    [
        Look(pictures, "living-room", "A warm living room", "Bouclé, travertine and oak in the afternoon light.", "Lookbook-LivingRoom", 1,
            ("paper-arc-floor-lamp", 24m, 30m),
            ("linen-cushion-cover-set", 34m, 57m),
            ("boucle-modular-sofa", 44m, 66m),
            ("decorative-vase-set", 54.5m, 61m),
            ("travertine-coffee-table", 54.5m, 76m),
            ("berber-style-rug-200x300", 70m, 87m),
            ("fluted-oak-media-console", 85m, 71m)),
        Look(pictures, "kitchen", "A sage green kitchen", "Shaker cabinets, open oak shelves and a marble island with rattan stools.", "Lookbook-Kitchen", 2,
            ("open-oak-wall-shelves", 17m, 26m),
            ("classic-kitchen-cabinet-set", 47m, 24m),
            ("olive-wood-serving-boards-set-of-3", 62m, 58m),
            ("marble-top-kitchen-island", 57.5m, 73m),
            ("stoneware-dinner-set-16-pieces", 79.5m, 57m),
            ("rattan-counter-stool", 77m, 70m)),
        Look(pictures, "bedroom", "A calm bedroom", "Washed linen, oak and woven cane in soft morning light.", "Lookbook-Bedroom", 3,
            ("ceramic-table-lamp", 21m, 49m),
            ("floating-oak-nightstand", 21m, 62m),
            ("linen-cushion-cover-set", 36m, 54m),
            ("upholstered-linen-bed", 57.5m, 42m),
            ("stonewashed-linen-bedding-set", 41.5m, 71m),
            ("rattan-front-dresser", 74m, 63m),
            ("jute-round-rug-160", 62.5m, 90m),
            ("seagrass-basket-set", 92.5m, 73m)),
        Look(pictures, "kids-room", "A room to grow in", "A tent to hide in, a bed they climb into on their own and a table at their height.", "Lookbook-KidsRoom", 4,
            ("canvas-play-tent", 19m, 63m),
            ("house-wall-shelf", 37m, 39m),
            ("montessori-floor-bed", 64m, 50m),
            ("kids-play-table-with-2-stools", 66m, 70m),
            ("moroccan-pattern-rug", 50m, 87m),
            ("kids-rocking-armchair", 87m, 65m)),
        Look(pictures, "garden", "An evening terrace", "Teak, acacia and rattan among the grasses, with pots of herbs.", "Lookbook-Garden", 5,
            ("hanging-rattan-egg-chair", 14.5m, 63m),
            ("teak-garden-lounger", 33m, 72m),
            ("terracotta-planter-set-3-pieces", 47m, 74m),
            ("garden-bistro-set", 67.5m, 67m),
            ("acacia-garden-bench", 87m, 68m)),
    ];

    private static Lookbook Look(PictureLinks pictures, string slug, string title, string summary, string picture, int sortOrder,
        params (string Product, decimal X, decimal Y)[] points) => new()
        {
            Slug = slug,
            Title = title,
            Summary = summary,
            Image = pictures.Of(picture + ".webp"),
            SortOrder = sortOrder,
            IsPublished = true,
            Hotspots = points.Select(p => new Hotspot { ProductSlug = p.Product, X = p.X, Y = p.Y }).ToList(),
        };
}
