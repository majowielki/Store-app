using Microsoft.EntityFrameworkCore;
using Store.Shared.Models;
using Store.Shared.Utility;

namespace Store.ProductService.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(ProductDbContext context)
    {
        if (await context.Products.AnyAsync())
        {
            return;
        }

        var products = new List<Product>
        {
            new Product
            {
                Title = "Modern Oak Dining Table",
                Description = "Beautiful solid oak dining table with sleek contemporary design. Perfect for family gatherings and entertaining guests. Seats up to 6 people comfortably with its spacious rectangular top.",
                Price = 899.99m,
                SalePrice = 799.99m,
                Category = Category.Tables,
                Company = Company.Modenza,
                Image = "https://storeapplication.blob.core.windows.net/product-images/ModernOakDiningTable.jpg",
                Colors = new List<string> { Colors.Brown.ToString(), Colors.White.ToString() },
                Groups = new List<string> { "furniture" },
                WidthCm = 180m,
                HeightCm = 75m,
                DepthCm = 90m,
                WeightKg = 55m,
                Materials = new List<string> { "oak", "polyurethane" },
                IsActive = true
            },
            new Product
            {
                Title = "Round Dining Table",
                Description = "Round oak dining table seating 4, with pedestal base.",
                Price = 599.99m,
                Category = Category.Tables,
                Company = Company.Homestead,
                NewArrival = true,
                Image = "https://storeapplication.blob.core.windows.net/product-images/RoundDiningTable.jpg",
                Colors = new List<string> { Colors.Brown.ToString() },
                Groups = new List<string> { "furniture" },
                WidthCm = 120m, HeightCm = 75m, DepthCm = 120m, WeightKg = 45m,
                Materials = new List<string> { "oak", "engineered-wood" },
                IsActive = true
            },

            new Product
            {
                Title = "Glass Top Coffee Table",
                Description = "Elegant tempered glass coffee table with chrome legs. Features a spacious lower shelf for storage and adds a modern touch to any living room or office space.",
                Price = 399.99m,
                Category = Category.Tables,
                Company = Company.Luxora,
                Image = "https://storeapplication.blob.core.windows.net/product-images/GlassTopCoffeeTable.jpg",
                Colors = new List<string> { Colors.Gray.ToString(), Colors.Black.ToString() },
                Groups = new List<string> { "furniture" },
                WidthCm = 110m,
                HeightCm = 45m,
                DepthCm = 60m,
                WeightKg = 22m,
                Materials = new List<string> { "tempered-glass", "chrome-steel" },
                IsActive = true
            },
            new Product
            {
                Title = "Industrial Console Table",
                Description = "Rustic industrial-style console table with reclaimed wood top and black metal frame. Perfect for entryways, hallways, or behind sofas. Features two lower shelves for storage.",
                Price = 549.99m,
                SalePrice = 499.99m,
                Category = Category.Tables,
                Company = Company.Artifex,
                Image = "https://storeapplication.blob.core.windows.net/product-images/IndustrialConsoleTable.jpg",
                Colors = new List<string> { Colors.Brown.ToString(), Colors.Black.ToString() },
                Groups = new List<string> { "furniture" },
                WidthCm = 140m,
                HeightCm = 80m,
                DepthCm = 35m,
                WeightKg = 28m,
                Materials = new List<string> { "reclaimed-wood", "powder-coated-steel" },
                IsActive = true
            },

            new Product
            {
                Title = "Executive Leather Office Chair",
                Description = "Premium ergonomic office chair with genuine leather upholstery and advanced lumbar support. Adjustable height, tilt mechanism, and 5-star base with smooth-rolling casters.",
                Price = 679.99m,
                Category = Category.Chairs,
                Company = Company.Comfora,
                NewArrival = true,
                Image = "https://storeapplication.blob.core.windows.net/product-images/ExecutiveLeatherOfficeChair.jpg",
                Colors = new List<string> { Colors.Black.ToString(), Colors.Brown.ToString() },
                Groups = new List<string> { "furniture" },
                WidthCm = 65m,
                HeightCm = 120m,
                DepthCm = 70m,
                WeightKg = 18m,
                Materials = new List<string> { "genuine-leather", "high-density-foam", "steel" },
                IsActive = true
            },
            new Product
            {
                Title = "Mid-Century Accent Chair",
                Description = "Stylish mid-century modern accent chair with solid wood legs and comfortable cushioned seat. Perfect addition to living rooms, bedrooms, or reading nooks.",
                Price = 299.99m,
                SalePrice = 239.99m,
                Category = Category.Chairs,
                Company = Company.Modenza,
                Image = "https://storeapplication.blob.core.windows.net/product-images/MidCenturyAccentChair.jpg",
                Colors = new List<string> { Colors.Blue.ToString(), Colors.Yellow.ToString(), Colors.Gray.ToString() },
                Groups = new List<string> { "furniture" },
                WidthCm = 70m,
                HeightCm = 85m,
                DepthCm = 75m,
                WeightKg = 12m,
                Materials = new List<string> { "fabric", "solid-wood" },
                IsActive = true
            },

            new Product
            {
                Title = "3-Seater Sectional Sofa",
                Description = "Spacious and comfortable 3-seater sectional sofa with premium fabric upholstery. Features reversible chaise lounge and plush cushions for ultimate relaxation.",
                Price = 1299.99m,
                SalePrice = 999.99m,
                Category = Category.Sofas,
                Company = Company.Luxora,
                Image = "https://storeapplication.blob.core.windows.net/product-images/3SeaterSectionalSofa.jpg",
                Colors = new List<string> { Colors.Gray.ToString(), Colors.Blue.ToString() },
                Groups = new List<string> { "furniture" },
                WidthCm = 240m,
                HeightCm = 85m,
                DepthCm = 160m,
                WeightKg = 75m,
                Materials = new List<string> { "fabric", "pine", "foam", "metal" },
                IsActive = true
            },
            new Product
            {
                Title = "Leather Reclining Sofa",
                Description = "Luxurious leather reclining sofa with dual power recliners and built-in USB charging ports. Perfect for movie nights and relaxation with family.",
                Price = 1899.99m,
                Category = Category.Sofas,
                Company = Company.Comfora,
                NewArrival = true,
                Image = "https://storeapplication.blob.core.windows.net/product-images/LeatherRecliningSofa.jpg",
                Colors = new List<string> { Colors.Black.ToString(), Colors.Brown.ToString() },
                Groups = new List<string> { "furniture" },
                WidthCm = 210m,
                HeightCm = 100m,
                DepthCm = 95m,
                WeightKg = 85m,
                Materials = new List<string> { "top-grain-leather", "steel", "foam" },
                IsActive = true
            },

            new Product
            {
                Title = "King Size Platform Bed",
                Description = "Modern king size platform bed with upholstered headboard and built-in nightstands. Low-profile design with clean lines and premium materials.",
                Price = 799.99m,
                Category = Category.Beds,
                Company = Company.Modenza,
                Image = "https://storeapplication.blob.core.windows.net/product-images/KingSizePlatformBed.jpg",
                Colors = new List<string> { Colors.Gray.ToString(), Colors.White.ToString() },
                Groups = new List<string> { "bedroom" },
                WidthCm = 200m,
                HeightCm = 95m,
                DepthCm = 220m,
                WeightKg = 60m,
                Materials = new List<string> { "upholstery", "engineered-wood", "steel" },
                IsActive = true
            },
            new Product
            {
                Title = "Storage Bed Frame",
                Description = "Queen size bed frame with built-in storage drawers underneath. Perfect for maximizing bedroom space while maintaining style and comfort.",
                Price = 649.99m,
                SalePrice = 619.99m,
                Category = Category.Beds,
                Company = Company.Homestead,
                Image = "https://storeapplication.blob.core.windows.net/product-images/StorageBedFrame.jpg",
                Colors = new List<string> { Colors.Brown.ToString(), Colors.White.ToString() },
                Groups = new List<string> { "bedroom" },
                WidthCm = 160m,
                HeightCm = 50m,
                DepthCm = 210m,
                WeightKg = 65m,
                Materials = new List<string> { "engineered-wood", "metal" },
                IsActive = true
            },

            new Product
            {
                Title = "Kids Study Desk and Chair Set",
                Description = "Colorful and functional study desk and chair set designed for children. Includes storage compartments and adjustable height chair for growing kids.",
                Price = 229.99m,
                Category = Category.KidsDesks,
                Company = Company.Homestead,
                Image = "https://storeapplication.blob.core.windows.net/product-images/KidsStudyDeskandChairSet.jpg",
                Colors = new List<string> { Colors.Pink.ToString(), Colors.Blue.ToString(), Colors.Green.ToString(), Colors.White.ToString() },
                Groups = new List<string> { "kids" },
                WidthCm = 100m,
                HeightCm = 75m,
                DepthCm = 55m,
                WeightKg = 20m,
                Materials = new List<string> { "mdf", "plastic", "steel" },
                IsActive = true
            },
            new Product
            {
                Title = "Kids Bunk Bed",
                Description = "Safe and sturdy twin-over-twin bunk bed with built-in ladder and safety rails. Perfect for siblings sharing a room or for sleepovers.",
                Price = 489.99m,
                SalePrice = 429.99m,
                Category = Category.KidsBeds,
                Company = Company.Artifex,
                Image = "https://storeapplication.blob.core.windows.net/product-images/KidsBunkBed.jpg",
                Colors = new List<string> { Colors.White.ToString(), Colors.Brown.ToString(), Colors.Gray.ToString() },
                Groups = new List<string> { "kids" },
                WidthCm = 100m,
                HeightCm = 160m,
                DepthCm = 200m,
                WeightKg = 70m,
                Materials = new List<string> { "pine", "steel" },
                IsActive = true
            },

            new Product
            {
                Title = "Sliding Door Wardrobe",
                Description = "Spacious 3-door wardrobe with mirror panel and adjustable shelves.",
                Price = 799.99m,
                Category = Category.Wardrobes,
                Company = Company.Homestead,
                Image = "https://storeapplication.blob.core.windows.net/product-images/SlidingDoorWardrobe.jpg",
                Colors = new List<string> { Colors.White.ToString(), Colors.Gray.ToString() },
                Groups = new List<string> { "bedroom" },
                WidthCm = 200m, HeightCm = 220m, DepthCm = 60m, WeightKg = 120m,
                Materials = new List<string> { "engineered-wood", "mirror" },
                IsActive = true
            },
            new Product
            {
                Title = "8-Drawer Dresser",
                Description = "Wide dresser with soft-close drawers and metal handles.",
                Price = 549.99m,
                Category = Category.Dressers,
                Company = Company.Luxora,
                NewArrival = true,
                Image = "https://storeapplication.blob.core.windows.net/product-images/8DrawerDresser.jpg",
                Colors = new List<string> { Colors.Brown.ToString(), Colors.White.ToString() },
                Groups = new List<string> { "bedroom" },
                WidthCm = 160m, HeightCm = 85m, DepthCm = 45m, WeightKg = 70m,
                Materials = new List<string> { "engineered-wood", "metal" },
                IsActive = true
            },

            new Product
            {
                Title = "Two-Drawer Nightstand",
                Description = "Compact nightstand with two drawers and hidden cable management.",
                Price = 149.99m,
                Category = Category.Nightstands,
                Company = Company.Comfora,
                Image = "https://storeapplication.blob.core.windows.net/product-images/TwoDrawerNightstand.jpg",
                Colors = new List<string> { Colors.White.ToString(), Colors.Gray.ToString() },
                Groups = new List<string> { "bedroom" },
                WidthCm = 45m, HeightCm = 55m, DepthCm = 40m, WeightKg = 15m,
                Materials = new List<string> { "engineered-wood" },
                IsActive = true
            },
            new Product
            {
                Title = "Open Back Bookcase",
                Description = "Versatile 6-tier open back bookcase perfect for books and decor.",
                Price = 219.99m,
                SalePrice = 179.99m,
                Category = Category.Bookcases,
                Company = Company.Artifex,
                Image = "https://storeapplication.blob.core.windows.net/product-images/OpenBackBookcase.jpg",
                Colors = new List<string> { Colors.Brown.ToString(), Colors.Black.ToString() },
                Groups = new List<string> { "furniture" },
                WidthCm = 80m, HeightCm = 180m, DepthCm = 30m, WeightKg = 28m,
                Materials = new List<string> { "engineered-wood", "metal" },
                IsActive = true
            },

            new Product
            {
                Title = "Walnut Sideboard",
                Description = "Mid-century sideboard with sliding doors and adjustable shelves.",
                Price = 699.99m,
                Category = Category.Sideboards,
                Company = Company.Modenza,
                Image = "https://storeapplication.blob.core.windows.net/product-images/WalnutSideboard.jpg",
                Colors = new List<string> { Colors.Brown.ToString() },
                Groups = new List<string> { "furniture" },
                WidthCm = 180m, HeightCm = 80m, DepthCm = 45m, WeightKg = 65m,
                Materials = new List<string> { "walnut-veneer", "engineered-wood" },
                IsActive = true
            },
            new Product
            {
                Title = "Entryway Bench with Storage",
                Description = "Upholstered bench with shoe storage and side pockets.",
                Price = 199.99m,
                Category = Category.EntrywayFurniture,
                Company = Company.Homestead,
                Image = "https://storeapplication.blob.core.windows.net/product-images/EntrywayBenchwithStorage.jpg",
                Colors = new List<string> { Colors.Gray.ToString(), Colors.Brown.ToString() },
                Groups = new List<string> { "furniture" },
                WidthCm = 110m, HeightCm = 50m, DepthCm = 40m, WeightKg = 18m,
                Materials = new List<string> { "fabric", "engineered-wood" },
                IsActive = true
            },

            // Office
            new Product
            {
                Title = "Ergonomic Corner Desk",
                Description = "L-shaped desk with cable tray and height-adjustable legs.",
                Price = 429.99m,
                Category = Category.Desks,
                Company = Company.Artifex,
                Image = "https://storeapplication.blob.core.windows.net/product-images/ErgonomicCornerDesk.jpg",
                Colors = new List<string> { Colors.Black.ToString(), Colors.White.ToString() },
                Groups = new List<string> { "furniture" },
                WidthCm = 160m, HeightCm = 75m, DepthCm = 140m, WeightKg = 40m,
                Materials = new List<string> { "engineered-wood", "steel" },
                IsActive = true
            },
            new Product
            {
                Title = "Mesh Office Chair",
                Description = "Breathable mesh chair with lumbar support and adjustable armrests.",
                Price = 189.99m,
                SalePrice = 159.99m,
                Category = Category.Chairs,
                Company = Company.Comfora,
                Image = "https://storeapplication.blob.core.windows.net/product-images/MeshOfficeChair.jpg",
                Colors = new List<string> { Colors.Black.ToString(), Colors.Gray.ToString() },
                Groups = new List<string> { "furniture" },
                WidthCm = 65m, HeightCm = 115m, DepthCm = 65m, WeightKg = 13m,
                Materials = new List<string> { "mesh", "steel", "foam" },
                IsActive = true
            },

            new Product
            {
                Title = "Tripod Floor Lamp",
                Description = "Scandinavian style floor lamp with fabric shade and wooden legs.",
                Price = 129.99m,
                Category = Category.FloorLamps,
                Company = Company.Luxora,
                Image = "https://storeapplication.blob.core.windows.net/product-images/TripodFloorLamp.jpg",
                Colors = new List<string> { Colors.White.ToString(), Colors.Brown.ToString() },
                Groups = new List<string> { "furniture" },
                WidthCm = 50m, HeightCm = 150m, DepthCm = 50m, WeightKg = 6m,
                Materials = new List<string> { "wood", "fabric", "metal" },
                IsActive = true
            },
            new Product
            {
                Title = "Compact Desk Lamp",
                Description = "LED desk lamp with adjustable arm and USB charging port.",
                Price = 29.99m,
                Category = Category.TableLamps,
                Company = Company.Modenza,
                Image = "https://storeapplication.blob.core.windows.net/product-images/CompactDeskLamp.jpg",
                Colors = new List<string> { Colors.Black.ToString(), Colors.White.ToString() },
                Groups = new List<string> { "bedroom" },
                WidthCm = 15m, HeightCm = 40m, DepthCm = 30m, WeightKg = 1.2m,
                Materials = new List<string> { "aluminum", "led" },
                IsActive = true
            },

            new Product
            {
                Title = "Berber Style Rug 200x300",
                Description = "Soft, high-pile rug inspired by traditional Berber patterns.",
                Price = 259.99m,
                Category = Category.Rugs,
                Company = Company.Comfora,
                Image = "https://storeapplication.blob.core.windows.net/product-images/BerberStyleRug.jpg",
                Colors = new List<string> { Colors.White.ToString(), Colors.Brown.ToString() },
                Groups = new List<string> { "decorations" },
                WidthCm = 200m, HeightCm = 3m, DepthCm = 300m, WeightKg = 12m,
                Materials = new List<string> { "polypropylene" },
                IsActive = true
            },
            new Product
            {
                Title = "Decorative Vase Set",
                Description = "Set of 3 ceramic vases with matte finish, perfect for modern interiors.",
                Price = 69.99m,
                Category = Category.Decor,
                Company = Company.Luxora,
                Image = "https://storeapplication.blob.core.windows.net/product-images/DecorativeVaseSet.jpg",
                Colors = new List<string> { Colors.White.ToString(), Colors.Gray.ToString() },
                Groups = new List<string> { "decorations" },
                WeightKg = 3.2m,
                Materials = new List<string> { "ceramic" },
                IsActive = true
            },

            new Product
            {
                Title = "Outdoor Dining Set",
                Description = "Weather-resistant outdoor dining set with 4 chairs and table.",
                Price = 749.99m,
                Category = Category.GardenSets,
                Company = Company.Artifex,
                Image = "https://storeapplication.blob.core.windows.net/product-images/OutdoorDiningSet.jpg",
                Colors = new List<string> { Colors.Gray.ToString(), Colors.Black.ToString() },
                Groups = new List<string> { "garden" },
                WidthCm = 140m, HeightCm = 75m, DepthCm = 140m, WeightKg = 45m,
                Materials = new List<string> { "aluminum", "polywood" },
                IsActive = true
            },
            new Product
            {
                Title = "Patio Sofa with Cushions",
                Description = "Modular patio sofa with washable cushions and aluminum frame.",
                Price = 899.99m,
                DiscountPercent = 10m,
                Category = Category.OutdoorFurniture,
                Company = Company.Luxora,
                Image = "https://storeapplication.blob.core.windows.net/product-images/PatioSofawithCushions.jpg",
                Colors = new List<string> { Colors.Gray.ToString() },
                Groups = new List<string> { "garden" },
                WidthCm = 220m, HeightCm = 80m, DepthCm = 160m, WeightKg = 35m,
                Materials = new List<string> { "aluminum", "olefin-fabric", "foam" },
                IsActive = true
            },

            new Product
            {
                Title = "Kitchen Island with Storage",
                Description = "Mobile kitchen island with butcher block top and two drawers.",
                Price = 499.99m,
                Category = Category.KitchenIslands,
                Company = Company.Homestead,
                Image = "https://storeapplication.blob.core.windows.net/product-images/KitchenIslandwithStorage.jpg",
                Colors = new List<string> { Colors.White.ToString() },
                Groups = new List<string> { "kitchen" },
                WidthCm = 120m, HeightCm = 90m, DepthCm = 60m, WeightKg = 55m,
                Materials = new List<string> { "rubberwood", "engineered-wood" },
                IsActive = true
            },
            new Product
            {
                Title = "Leather Bar Stool",
                Description = "Adjustable height bar stool with leather seat and footrest.",
                Price = 159.99m,
                Category = Category.Chairs,
                Company = Company.Modenza,
                Image = "https://storeapplication.blob.core.windows.net/product-images/LeatherBarStool.jpg",
                Colors = new List<string> { Colors.Black.ToString(), Colors.Brown.ToString() },
                Groups = new List<string> { "furniture" },
                WidthCm = 45m, HeightCm = 100m, DepthCm = 45m, WeightKg = 9m,
                Materials = new List<string> { "leather", "steel" },
                IsActive = true
            },

            new Product
            {
                Title = "Bathroom Vanity 100cm",
                Description = "Wall-mounted bathroom vanity with ceramic sink and two drawers.",
                Price = 699.99m,
                Category = Category.BathroomFurniture,
                Company = Company.Comfora,
                Image = "https://storeapplication.blob.core.windows.net/product-images/BathroomVanity.jpg",
                Colors = new List<string> { Colors.White.ToString(), Colors.Gray.ToString() },
                Groups = new List<string> { "bathroom" },
                WidthCm = 100m, HeightCm = 55m, DepthCm = 48m, WeightKg = 40m,
                Materials = new List<string> { "ceramic", "engineered-wood" },
                IsActive = true
            },

            new Product
            {
                Title = "Minimalist TV Stand",
                Description = "Sleek TV stand with cable management and two drawers.",
                Price = 299.99m,
                Category = Category.TVStands,
                Company = Company.Luxora,
                NewArrival = true,
                Image = "https://storeapplication.blob.core.windows.net/product-images/MinimalistTVStand.jpg",
                Colors = new List<string> { Colors.White.ToString(), Colors.Black.ToString() },
                Groups = new List<string> { "furniture" },
                WidthCm = 140m, HeightCm = 45m, DepthCm = 40m, WeightKg = 30m,
                Materials = new List<string> { "engineered-wood", "metal" },
                IsActive = true
            },
            new Product
            {
                Title = "Memory Foam Mattress",
                Description = "Queen size memory foam mattress with cooling gel layer.",
                Price = 499.99m,
                Category = Category.Mattresses,
                Company = Company.Comfora,
                Image = "https://storeapplication.blob.core.windows.net/product-images/MemoryFoamMattress.jpg",
                Colors = new List<string> { Colors.White.ToString() },
                Groups = new List<string> { "bedroom" },
                WidthCm = 160m, HeightCm = 25m, DepthCm = 200m, WeightKg = 25m,
                Materials = new List<string> { "memory-foam", "fabric" },
                IsActive = true
            },

            new Product
            {
                Title = "Bathroom Linen Cabinet",
                Description = "Tall, slim linen cabinet with adjustable shelves and a moisture-resistant finish. Perfect for storing towels and toiletries in the bathroom.",
                Price = 249.99m,
                Category = Category.BathroomStorage,
                Company = Company.Homestead,
                Image = "https://storeapplication.blob.core.windows.net/product-images/BathroomLinenCabinet.jpg",
                Colors = new List<string> { Colors.White.ToString(), Colors.Gray.ToString() },
                Groups = new List<string> { "bathroom" },
                WidthCm = 40m, HeightCm = 180m, DepthCm = 35m, WeightKg = 30m,
                Materials = new List<string> { "engineered-wood", "moisture-resistant-coating" },
                IsActive = true
            },

            new Product
            {
                Title = "Bathroom Wall Mirror Cabinet",
                Description = "Wall-mounted bathroom mirror cabinet with LED lighting and storage shelves.",
                Price = 399.99m,
                Category = Category.BathroomMirrors,
                Company = Company.Comfora,
                Image = "https://storeapplication.blob.core.windows.net/product-images/BathroomWallMirrorCabinet.jpg",
                Colors = new List<string> { Colors.White.ToString(), Colors.Gray.ToString() },
                Groups = new List<string> { "bathroom" },
                WidthCm = 80m, HeightCm = 70m, DepthCm = 18m, WeightKg = 18m,
                Materials = new List<string> { "glass", "engineered-wood", "led" },
                IsActive = true
            },
            new Product
            {
                Title = "Classic Kitchen Cabinet Set",
                Description = "Set of upper and lower kitchen cabinets with soft-close doors and drawers.",
                Price = 1999.99m,
                Category = Category.KitchenCabinets,
                Company = Company.Homestead,
                Image = "https://storeapplication.blob.core.windows.net/product-images/ClassicKitchenCabinetSet.jpg",
                Colors = new List<string> { Colors.White.ToString(), Colors.Brown.ToString() },
                Groups = new List<string> { "kitchen" },
                WidthCm = 240m, HeightCm = 220m, DepthCm = 60m, WeightKg = 120m,
                Materials = new List<string> { "engineered-wood", "metal" },
                IsActive = true
            },
            new Product
            {
                Title = "Contemporary Walnut Sideboard",
                Description = "Spacious walnut sideboard with three doors and three drawers for versatile storage.",
                Price = 799.99m,
                Category = Category.Sideboards,
                Company = Company.Luxora,
                Image = "https://storeapplication.blob.core.windows.net/product-images/ContemporaryWalnutSideboard.jpg",
                Colors = new List<string> { Colors.Brown.ToString() },
                Groups = new List<string> { "furniture" },
                WidthCm = 180m, HeightCm = 85m, DepthCm = 45m, WeightKg = 70m,
                Materials = new List<string> { "walnut-veneer", "engineered-wood" },
                IsActive = true
            },
            new Product
            {
                Title = "Garden Bistro Set",
                Description = "Compact garden bistro set with two chairs and a round table, perfect for balconies and patios.",
                Price = 299.99m,
                Category = Category.GardenSets,
                Company = Company.Artifex,
                Image = "https://storeapplication.blob.core.windows.net/product-images/GardenBistroSet.jpg",
                Colors = new List<string> { Colors.Black.ToString(), Colors.Gray.ToString() },
                Groups = new List<string> { "garden" },
                WidthCm = 60m, HeightCm = 75m, DepthCm = 60m, WeightKg = 18m,
                Materials = new List<string> { "steel", "tempered-glass" },
                IsActive = true
            },
            new Product
            {
                Title = "Moroccan Pattern Rug",
                Description = "Vibrant Moroccan-style rug with geometric patterns and soft pile.",
                Price = 189.99m,
                Category = Category.Rugs,
                Company = Company.Comfora,
                Image = "https://storeapplication.blob.core.windows.net/product-images/MoroccanPatternRug.jpg",
                Colors = new List<string> { Colors.Blue.ToString(), Colors.White.ToString(), Colors.Yellow.ToString() },
                Groups = new List<string> { "decorations" },
                WidthCm = 160m, HeightCm = 2.5m, DepthCm = 230m, WeightKg = 8m,
                Materials = new List<string> { "polypropylene" },
                IsActive = true
            },
            new Product
            {
                Title = "Abstract Ceramic Sculpture",
                Description = "Handcrafted abstract ceramic sculpture, perfect as a centerpiece for modern interiors.",
                Price = 89.99m,
                Category = Category.Decor,
                Company = Company.Luxora,
                Image = "https://storeapplication.blob.core.windows.net/product-images/AbstractCeramicSculpture.jpg",
                Colors = new List<string> { Colors.White.ToString(), Colors.Gray.ToString() },
                Groups = new List<string> { "decorations" },
                WeightKg = 2.5m,
                Materials = new List<string> { "ceramic" },
                IsActive = true
            },
        };

        context.Products.AddRange(products);
        await context.SaveChangesAsync();
    }
}
