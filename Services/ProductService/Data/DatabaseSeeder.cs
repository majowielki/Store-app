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
            return; // Database has been seeded
        }

        var products = new List<Product>
        {
            // Kitchen - Tables
            new Product
            {
                Title = "Modern Oak Dining Table",
                Description = "Beautiful solid oak dining table with sleek contemporary design. Perfect for family gatherings and entertaining guests. Seats up to 6 people comfortably with its spacious rectangular top.",
                Price = 899.99m,
                SalePrice = 799.99m,
                Category = Category.Tables,
                Company = Company.Modenza,
                NewArrival = true,
                Image = "https://images.unsplash.com/photo-1586023492125-27b2c045efd7?auto=format&fit=crop&w=1200&q=80",
                Colors = new List<string> { Colors.Brown.ToString(), Colors.White.ToString() },
                Groups = new List<string> { "kitchen" },
                WidthCm = 180m,
                HeightCm = 75m,
                DepthCm = 90m,
                WeightKg = 55m,
                Materials = new List<string> { "oak", "polyurethane" },
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                UpdatedAt = DateTime.UtcNow.AddDays(-30)
            },
            new Product
            {
                Title = "Round Dining Table",
                Description = "Round oak dining table seating 4, with pedestal base.",
                Price = 599.99m,
                Category = Category.Tables,
                Company = Company.Homestead,
                Image = "https://images.unsplash.com/photo-1524758631624-e2822e304c36?auto=format&fit=crop&w=1200&q=80",
                Colors = new List<string> { Colors.Brown.ToString() },
                Groups = new List<string> { "kitchen" },
                WidthCm = 120m, HeightCm = 75m, DepthCm = 120m, WeightKg = 45m,
                Materials = new List<string> { "oak", "engineered-wood" },
                IsActive = true
            },

            // Living Room - Coffee Tables
            new Product
            {
                Title = "Glass Top Coffee Table",
                Description = "Elegant tempered glass coffee table with chrome legs. Features a spacious lower shelf for storage and adds a modern touch to any living room or office space.",
                Price = 399.99m,
                Category = Category.Tables,
                Company = Company.Luxora,
                Image = "https://images.unsplash.com/photo-1555041469-a586c61ea9bc?auto=format&fit=crop&w=1200&q=80",
                Colors = new List<string> { Colors.Gray.ToString(), Colors.Black.ToString() },
                Groups = new List<string> { "furniture" },
                WidthCm = 110m,
                HeightCm = 45m,
                DepthCm = 60m,
                WeightKg = 22m,
                Materials = new List<string> { "tempered-glass", "chrome-steel" },
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-28),
                UpdatedAt = DateTime.UtcNow.AddDays(-28)
            },
            new Product
            {
                Title = "Industrial Console Table",
                Description = "Rustic industrial-style console table with reclaimed wood top and black metal frame. Perfect for entryways, hallways, or behind sofas. Features two lower shelves for storage.",
                Price = 549.99m,
                DiscountPercent = 10m,
                Category = Category.Tables,
                Company = Company.Artifex,
                Image = "https://images.unsplash.com/photo-1506439773649-6e0eb8cfb237?auto=format&fit=crop&w=1200&q=80",
                Colors = new List<string> { Colors.Brown.ToString(), Colors.Black.ToString() },
                Groups = new List<string> { "furniture" },
                WidthCm = 140m,
                HeightCm = 80m,
                DepthCm = 35m,
                WeightKg = 28m,
                Materials = new List<string> { "reclaimed-wood", "powder-coated-steel" },
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-25),
                UpdatedAt = DateTime.UtcNow.AddDays(-25)
            },

            // Chairs
            new Product
            {
                Title = "Executive Leather Office Chair",
                Description = "Premium ergonomic office chair with genuine leather upholstery and advanced lumbar support. Adjustable height, tilt mechanism, and 5-star base with smooth-rolling casters.",
                Price = 679.99m,
                Category = Category.Chairs,
                Company = Company.Comfora,
                Image = "https://images.unsplash.com/photo-1549497538-303791108f95?auto=format&fit=crop&w=1200&q=80",
                Colors = new List<string> { Colors.Black.ToString(), Colors.Brown.ToString() },
                Groups = new List<string> { "furniture" },
                WidthCm = 65m,
                HeightCm = 120m,
                DepthCm = 70m,
                WeightKg = 18m,
                Materials = new List<string> { "genuine-leather", "high-density-foam", "steel" },
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-22),
                UpdatedAt = DateTime.UtcNow.AddDays(-22)
            },
            new Product
            {
                Title = "Mid-Century Accent Chair",
                Description = "Stylish mid-century modern accent chair with solid wood legs and comfortable cushioned seat. Perfect addition to living rooms, bedrooms, or reading nooks.",
                Price = 299.99m,
                DiscountPercent = 15m,
                Category = Category.Chairs,
                Company = Company.Modenza,
                NewArrival = true,
                Image = "https://images.unsplash.com/photo-1586023492125-27b2c045efd7?auto=format&fit=crop&w=1200&q=80",
                Colors = new List<string> { Colors.Blue.ToString(), Colors.Yellow.ToString(), Colors.Gray.ToString() },
                Groups = new List<string> { "furniture" },
                WidthCm = 70m,
                HeightCm = 85m,
                DepthCm = 75m,
                WeightKg = 12m,
                Materials = new List<string> { "fabric", "solid-wood" },
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-20),
                UpdatedAt = DateTime.UtcNow.AddDays(-20)
            },

            // Sofas
            new Product
            {
                Title = "3-Seater Sectional Sofa",
                Description = "Spacious and comfortable 3-seater sectional sofa with premium fabric upholstery. Features reversible chaise lounge and plush cushions for ultimate relaxation.",
                Price = 1299.99m,
                SalePrice = 1099.99m,
                Category = Category.Sofas,
                Company = Company.Luxora,
                Image = "https://images.unsplash.com/photo-1555041469-a586c61ea9bc?auto=format&fit=crop&w=1200&q=80",
                Colors = new List<string> { Colors.Gray.ToString(), Colors.Blue.ToString() },
                Groups = new List<string> { "furniture" },
                WidthCm = 240m,
                HeightCm = 85m,
                DepthCm = 160m,
                WeightKg = 75m,
                Materials = new List<string> { "fabric", "pine", "foam", "metal" },
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-15),
                UpdatedAt = DateTime.UtcNow.AddDays(-15)
            },
            new Product
            {
                Title = "Leather Reclining Sofa",
                Description = "Luxurious leather reclining sofa with dual power recliners and built-in USB charging ports. Perfect for movie nights and relaxation with family.",
                Price = 1899.99m,
                Category = Category.Sofas,
                Company = Company.Comfora,
                NewArrival = true,
                Image = "https://images.unsplash.com/photo-1549497538-303791108f95?auto=format&fit=crop&w=1200&q=80",
                Colors = new List<string> { Colors.Black.ToString(), Colors.Brown.ToString() },
                Groups = new List<string> { "furniture" },
                WidthCm = 210m,
                HeightCm = 100m,
                DepthCm = 95m,
                WeightKg = 85m,
                Materials = new List<string> { "top-grain-leather", "steel", "foam" },
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-12),
                UpdatedAt = DateTime.UtcNow.AddDays(-12)
            },

            // Bedroom - Beds
            new Product
            {
                Title = "King Size Platform Bed",
                Description = "Modern king size platform bed with upholstered headboard and built-in nightstands. Low-profile design with clean lines and premium materials.",
                Price = 799.99m,
                Category = Category.Beds,
                Company = Company.Modenza,
                Image = "https://images.unsplash.com/photo-1586023492125-27b2c045efd7?auto=format&fit=crop&w=1200&q=80",
                Colors = new List<string> { Colors.Gray.ToString(), Colors.White.ToString() },
                Groups = new List<string> { "furniture" },
                WidthCm = 200m,
                HeightCm = 95m,
                DepthCm = 220m,
                WeightKg = 60m,
                Materials = new List<string> { "upholstery", "engineered-wood", "steel" },
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                UpdatedAt = DateTime.UtcNow.AddDays(-10)
            },
            new Product
            {
                Title = "Storage Bed Frame",
                Description = "Queen size bed frame with built-in storage drawers underneath. Perfect for maximizing bedroom space while maintaining style and comfort.",
                Price = 649.99m,
                DiscountPercent = 5m,
                Category = Category.Beds,
                Company = Company.Homestead,
                Image = "https://images.unsplash.com/photo-1555041469-a586c61ea9bc?auto=format&fit=crop&w=1200&q=80",
                Colors = new List<string> { Colors.Brown.ToString(), Colors.White.ToString() },
                Groups = new List<string> { "furniture" },
                WidthCm = 160m,
                HeightCm = 50m,
                DepthCm = 210m,
                WeightKg = 65m,
                Materials = new List<string> { "engineered-wood", "metal" },
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-8),
                UpdatedAt = DateTime.UtcNow.AddDays(-8)
            },

            // Kids
            new Product
            {
                Title = "Kids Study Desk and Chair Set",
                Description = "Colorful and functional study desk and chair set designed for children. Includes storage compartments and adjustable height chair for growing kids.",
                Price = 229.99m,
                Category = Category.Kids,
                Company = Company.Homestead,
                Image = "https://images.unsplash.com/photo-1506439773649-6e0eb8cfb237?auto=format&fit=crop&w=1200&q=80",
                Colors = new List<string> { Colors.Pink.ToString(), Colors.Blue.ToString(), Colors.Green.ToString(), Colors.White.ToString() },
                Groups = new List<string> { "kids" },
                WidthCm = 100m,
                HeightCm = 75m,
                DepthCm = 55m,
                WeightKg = 20m,
                Materials = new List<string> { "mdf", "plastic", "steel" },
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-6),
                UpdatedAt = DateTime.UtcNow.AddDays(-6)
            },
            new Product
            {
                Title = "Kids Bunk Bed",
                Description = "Safe and sturdy twin-over-twin bunk bed with built-in ladder and safety rails. Perfect for siblings sharing a room or for sleepovers.",
                Price = 459.99m,
                SalePrice = 429.99m,
                Category = Category.Kids,
                Company = Company.Artifex,
                Image = "https://images.unsplash.com/photo-1549497538-303791108f95?auto=format&fit=crop&w=1200&q=80",
                Colors = new List<string> { Colors.White.ToString(), Colors.Brown.ToString(), Colors.Gray.ToString() },
                Groups = new List<string> { "kids" },
                WidthCm = 100m,
                HeightCm = 160m,
                DepthCm = 200m,
                WeightKg = 70m,
                Materials = new List<string> { "pine", "steel" },
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-4),
                UpdatedAt = DateTime.UtcNow.AddDays(-4)
            },

            // Storage related
            new Product
            {
                Title = "Sliding Door Wardrobe",
                Description = "Spacious 3-door wardrobe with mirror panel and adjustable shelves.",
                Price = 799.99m,
                Category = Category.Wardrobes,
                Company = Company.Homestead,
                Image = "https://images.unsplash.com/photo-1519710164239-da123dc03ef4?auto=format&fit=crop&w=1200&q=80",
                Colors = new List<string> { Colors.White.ToString(), Colors.Gray.ToString() },
                Groups = new List<string> { "furniture" },
                WidthCm = 200m, HeightCm = 220m, DepthCm = 60m, WeightKg = 120m,
                Materials = new List<string> { "engineered-wood", "mirror" },
                IsActive = true
            },
            new Product
            {
                Title = "6-Drawer Dresser",
                Description = "Wide dresser with soft-close drawers and metal handles.",
                Price = 549.99m,
                Category = Category.Dressers,
                Company = Company.Luxora,
                Image = "https://images.unsplash.com/photo-1582582621959-3a798edc2cc3?auto=format&fit=crop&w=1200&q=80",
                Colors = new List<string> { Colors.Brown.ToString(), Colors.White.ToString() },
                Groups = new List<string> { "furniture" },
                WidthCm = 160m, HeightCm = 85m, DepthCm = 45m, WeightKg = 70m,
                Materials = new List<string> { "engineered-wood", "metal" },
                IsActive = true
            },

            // Nightstands
            new Product
            {
                Title = "Two-Drawer Nightstand",
                Description = "Compact nightstand with two drawers and hidden cable management.",
                Price = 149.99m,
                Category = Category.Nightstands,
                Company = Company.Comfora,
                Image = "https://images.unsplash.com/photo-1524758631624-581edf1f9d8f?auto=format&fit=crop&w=1200&q=80",
                Colors = new List<string> { Colors.White.ToString(), Colors.Gray.ToString() },
                Groups = new List<string> { "furniture" },
                WidthCm = 45m, HeightCm = 55m, DepthCm = 40m, WeightKg = 15m,
                Materials = new List<string> { "engineered-wood" },
                IsActive = true
            },
            new Product
            {
                Title = "Open Back Bookcase",
                Description = "Versatile 5-tier open back bookcase perfect for books and decor.",
                Price = 219.99m,
                DiscountPercent = 10m,
                Category = Category.Bookcases,
                Company = Company.Artifex,
                Image = "https://images.unsplash.com/photo-1524758631624-8f9814f1a7f8?auto=format&fit=crop&w=1200&q=80",
                Colors = new List<string> { Colors.Brown.ToString(), Colors.Black.ToString() },
                Groups = new List<string> { "furniture" },
                WidthCm = 80m, HeightCm = 180m, DepthCm = 30m, WeightKg = 28m,
                Materials = new List<string> { "engineered-wood", "metal" },
                IsActive = true
            },

            // Sideboards
            new Product
            {
                Title = "Walnut Sideboard",
                Description = "Mid-century sideboard with sliding doors and adjustable shelves.",
                Price = 699.99m,
                Category = Category.Sideboards,
                Company = Company.Modenza,
                Image = "https://images.unsplash.com/photo-1493663284031-b7e3aefcae8e?auto=format&fit=crop&w=1200&q=80",
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
                Category = Category.Entryway,
                Company = Company.Homestead,
                Image = "https://images.unsplash.com/photo-1582582621959-3a798edc2cc3?auto=format&fit=crop&w=1200&q=80",
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
                Image = "https://images.unsplash.com/photo-1518459031867-a89b944bffe0?auto=format&fit=crop&w=1200&q=80",
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
                DiscountPercent = 15m,
                Category = Category.Chairs,
                Company = Company.Comfora,
                Image = "https://images.unsplash.com/photo-1524758631624-c03f3883a72c?auto=format&fit=crop&w=1200&q=80",
                Colors = new List<string> { Colors.Black.ToString(), Colors.Gray.ToString() },
                Groups = new List<string> { "furniture" },
                WidthCm = 65m, HeightCm = 115m, DepthCm = 65m, WeightKg = 13m,
                Materials = new List<string> { "mesh", "steel", "foam" },
                IsActive = true
            },

            // Lamps
            new Product
            {
                Title = "Tripod Floor Lamp",
                Description = "Scandinavian style floor lamp with fabric shade and wooden legs.",
                Price = 129.99m,
                Category = Category.Lighting,
                Company = Company.Luxora,
                Image = "https://images.unsplash.com/photo-1481277542470-605612bd2d61?auto=format&fit=crop&w=1200&q=80",
                Colors = new List<string> { Colors.White.ToString(), Colors.Brown.ToString() },
                Groups = new List<string> { "lamps" },
                WidthCm = 50m, HeightCm = 150m, DepthCm = 50m, WeightKg = 6m,
                Materials = new List<string> { "wood", "fabric", "metal" },
                IsActive = true
            },
            new Product
            {
                Title = "Compact Desk Lamp",
                Description = "LED desk lamp with adjustable arm and USB charging port.",
                Price = 49.99m,
                Category = Category.Lighting,
                Company = Company.Modenza,
                Image = "https://images.unsplash.com/photo-1493663284031-b7e3aefcae8e?auto=format&fit=crop&w=1200&q=80",
                Colors = new List<string> { Colors.Black.ToString(), Colors.White.ToString() },
                Groups = new List<string> { "lamps" },
                WidthCm = 15m, HeightCm = 40m, DepthCm = 30m, WeightKg = 1.2m,
                Materials = new List<string> { "aluminum", "led" },
                IsActive = true
            },

            // Decorations
            new Product
            {
                Title = "Berber Style Rug 200x300",
                Description = "Soft, high-pile rug inspired by traditional Berber patterns.",
                Price = 259.99m,
                Category = Category.Rugs,
                Company = Company.Comfora,
                Image = "https://images.unsplash.com/photo-1582582621959-3a798edc2cc3?auto=format&fit=crop&w=1200&q=80",
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
                Image = "https://images.unsplash.com/photo-1503602642458-232111445657?auto=format&fit=crop&w=1200&q=80",
                Colors = new List<string> { Colors.White.ToString(), Colors.Gray.ToString() },
                Groups = new List<string> { "decorations" },
                WeightKg = 3.2m,
                Materials = new List<string> { "ceramic" },
                IsActive = true
            },

            // Garden
            new Product
            {
                Title = "Outdoor Dining Set (5pcs)",
                Description = "Weather-resistant outdoor dining set with 4 chairs and table.",
                Price = 749.99m,
                Category = Category.Outdoor,
                Company = Company.Artifex,
                Image = "https://images.unsplash.com/photo-1481277542470-605612bd2d61?auto=format&fit=crop&w=1200&q=80",
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
                Category = Category.Outdoor,
                Company = Company.Luxora,
                Image = "https://images.unsplash.com/photo-1519710164239-da123dc03ef4?auto=format&fit=crop&w=1200&q=80",
                Colors = new List<string> { Colors.Gray.ToString() },
                Groups = new List<string> { "garden" },
                WidthCm = 220m, HeightCm = 80m, DepthCm = 160m, WeightKg = 35m,
                Materials = new List<string> { "aluminum", "olefin-fabric", "foam" },
                IsActive = true
            },

            // Kitchen specific
            new Product
            {
                Title = "Kitchen Island with Storage",
                Description = "Mobile kitchen island with butcher block top and two drawers.",
                Price = 499.99m,
                Category = Category.Kitchen,
                Company = Company.Homestead,
                Image = "https://images.unsplash.com/photo-1524758631624-8f9814f1a7f8?auto=format&fit=crop&w=1200&q=80",
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
                Image = "https://images.unsplash.com/photo-1518459031867-a89b944bffe0?auto=format&fit=crop&w=1200&q=80",
                Colors = new List<string> { Colors.Black.ToString(), Colors.Brown.ToString() },
                Groups = new List<string> { "kitchen" },
                WidthCm = 45m, HeightCm = 100m, DepthCm = 45m, WeightKg = 9m,
                Materials = new List<string> { "leather", "steel" },
                IsActive = true
            },

            // Bathroom specific
            new Product
            {
                Title = "Bathroom Vanity 100cm",
                Description = "Wall-mounted bathroom vanity with ceramic sink and two drawers.",
                Price = 699.99m,
                Category = Category.Bathroom,
                Company = Company.Comfora,
                Image = "https://images.unsplash.com/photo-1519710164239-da123dc03ef4?auto=format&fit=crop&w=1200&q=80",
                Colors = new List<string> { Colors.White.ToString(), Colors.Gray.ToString() },
                Groups = new List<string> { "bathroom" },
                WidthCm = 100m, HeightCm = 55m, DepthCm = 48m, WeightKg = 40m,
                Materials = new List<string> { "ceramic", "engineered-wood" },
                IsActive = true
            },
            new Product
            {
                Title = "Shoe Cabinet 3-Drawer",
                Description = "Slim shoe cabinet with tilting drawers, perfect for narrow hallways.",
                Price = 179.99m,
                Category = Category.Storage,
                Company = Company.Artifex,
                Image = "https://images.unsplash.com/photo-1524758631624-c03f3883a72c?auto=format&fit=crop&w=1200&q=80",
                Colors = new List<string> { Colors.White.ToString(), Colors.Black.ToString() },
                Groups = new List<string> { "bathroom" },
                WidthCm = 70m, HeightCm = 120m, DepthCm = 20m, WeightKg = 20m,
                Materials = new List<string> { "engineered-wood", "metal" },
                IsActive = true
            },
        };

        context.Products.AddRange(products);
        await context.SaveChangesAsync();
    }
}