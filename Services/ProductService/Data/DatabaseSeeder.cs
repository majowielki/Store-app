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
            // Tables
            new Product
            {
                Title = "Modern Oak Dining Table",
                Description = "Beautiful solid oak dining table with sleek contemporary design. Perfect for family gatherings and entertaining guests. Seats up to 6 people comfortably with its spacious rectangular top.",
                Price = 899.99m,
                Category = Category.Tables,
                Company = Company.Modenza,
                Image = "https://images.unsplash.com/photo-1586023492125-27b2c045efd7?ixlib=rb-4.0.3&auto=format&fit=crop&w=800&q=80",
                Colors = new List<string> { Colors.Brown.ToString(), Colors.White.ToString() },
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                UpdatedAt = DateTime.UtcNow.AddDays(-30)
            },
            new Product
            {
                Title = "Glass Top Coffee Table",
                Description = "Elegant tempered glass coffee table with chrome legs. Features a spacious lower shelf for storage and adds a modern touch to any living room or office space.",
                Price = 399.99m,
                Category = Category.Tables,
                Company = Company.Luxora,
                Image = "https://images.unsplash.com/photo-1555041469-a586c61ea9bc?ixlib=rb-4.0.3&auto=format&fit=crop&w=800&q=80",
                Colors = new List<string> { Colors.Gray.ToString(), Colors.Black.ToString() },
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-28),
                UpdatedAt = DateTime.UtcNow.AddDays(-28)
            },
            new Product
            {
                Title = "Industrial Console Table",
                Description = "Rustic industrial-style console table with reclaimed wood top and black metal frame. Perfect for entryways, hallways, or behind sofas. Features two lower shelves for storage.",
                Price = 549.99m,
                Category = Category.Tables,
                Company = Company.Artifex,
                Image = "https://images.unsplash.com/photo-1506439773649-6e0eb8cfb237?ixlib=rb-4.0.3&auto=format&fit=crop&w=800&q=80",
                Colors = new List<string> { Colors.Brown.ToString(), Colors.Black.ToString() },
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
                Image = "https://images.unsplash.com/photo-1549497538-303791108f95?ixlib=rb-4.0.3&auto=format&fit=crop&w=800&q=80",
                Colors = new List<string> { Colors.Black.ToString(), Colors.Brown.ToString() },
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-22),
                UpdatedAt = DateTime.UtcNow.AddDays(-22)
            },
            new Product
            {
                Title = "Mid-Century Accent Chair",
                Description = "Stylish mid-century modern accent chair with solid wood legs and comfortable cushioned seat. Perfect addition to living rooms, bedrooms, or reading nooks.",
                Price = 299.99m,
                Category = Category.Chairs,
                Company = Company.Modenza,
                Image = "https://images.unsplash.com/photo-1586023492125-27b2c045efd7?ixlib=rb-4.0.3&auto=format&fit=crop&w=800&q=80",
                Colors = new List<string> { Colors.Blue.ToString(), Colors.Yellow.ToString(), Colors.Gray.ToString() },
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-20),
                UpdatedAt = DateTime.UtcNow.AddDays(-20)
            },
            new Product
            {
                Title = "Dining Chair Set (4 pieces)",
                Description = "Set of 4 contemporary dining chairs with padded seats and solid wood construction. Comfortable and durable, perfect for dining rooms and kitchens.",
                Price = 199.99m,
                Category = Category.Chairs,
                Company = Company.Homestead,
                Image = "https://images.unsplash.com/photo-1506439773649-6e0eb8cfb237?ixlib=rb-4.0.3&auto=format&fit=crop&w=800&q=80",
                Colors = new List<string> { Colors.White.ToString(), Colors.Black.ToString(), Colors.Brown.ToString() },
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-18),
                UpdatedAt = DateTime.UtcNow.AddDays(-18)
            },

            // Sofas
            new Product
            {
                Title = "3-Seater Sectional Sofa",
                Description = "Spacious and comfortable 3-seater sectional sofa with premium fabric upholstery. Features reversible chaise lounge and plush cushions for ultimate relaxation.",
                Price = 1299.99m,
                Category = Category.Sofas,
                Company = Company.Luxora,
                Image = "https://images.unsplash.com/photo-1555041469-a586c61ea9bc?ixlib=rb-4.0.3&auto=format&fit=crop&w=800&q=80",
                Colors = new List<string> { Colors.Gray.ToString(), Colors.Blue.ToString() },
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
                Image = "https://images.unsplash.com/photo-1549497538-303791108f95?ixlib=rb-4.0.3&auto=format&fit=crop&w=800&q=80",
                Colors = new List<string> { Colors.Black.ToString(), Colors.Brown.ToString() },
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-12),
                UpdatedAt = DateTime.UtcNow.AddDays(-12)
            },

            // Beds
            new Product
            {
                Title = "King Size Platform Bed",
                Description = "Modern king size platform bed with upholstered headboard and built-in nightstands. Low-profile design with clean lines and premium materials.",
                Price = 799.99m,
                Category = Category.Beds,
                Company = Company.Modenza,
                Image = "https://images.unsplash.com/photo-1586023492125-27b2c045efd7?ixlib=rb-4.0.3&auto=format&fit=crop&w=800&q=80",
                Colors = new List<string> { Colors.Gray.ToString(), Colors.White.ToString() },
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                UpdatedAt = DateTime.UtcNow.AddDays(-10)
            },
            new Product
            {
                Title = "Storage Bed Frame",
                Description = "Queen size bed frame with built-in storage drawers underneath. Perfect for maximizing bedroom space while maintaining style and comfort.",
                Price = 649.99m,
                Category = Category.Beds,
                Company = Company.Homestead,
                Image = "https://images.unsplash.com/photo-1555041469-a586c61ea9bc?ixlib=rb-4.0.3&auto=format&fit=crop&w=800&q=80",
                Colors = new List<string> { Colors.Brown.ToString(), Colors.White.ToString() },
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-8),
                UpdatedAt = DateTime.UtcNow.AddDays(-8)
            },

            // Kids Furniture
            new Product
            {
                Title = "Kids Study Desk and Chair Set",
                Description = "Colorful and functional study desk and chair set designed for children. Includes storage compartments and adjustable height chair for growing kids.",
                Price = 229.99m,
                Category = Category.Kids,
                Company = Company.Homestead,
                Image = "https://images.unsplash.com/photo-1506439773649-6e0eb8cfb237?ixlib=rb-4.0.3&auto=format&fit=crop&w=800&q=80",
                Colors = new List<string> { Colors.Pink.ToString(), Colors.Blue.ToString(), Colors.Green.ToString(), Colors.White.ToString() },
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-6),
                UpdatedAt = DateTime.UtcNow.AddDays(-6)
            },
            new Product
            {
                Title = "Kids Bunk Bed",
                Description = "Safe and sturdy twin-over-twin bunk bed with built-in ladder and safety rails. Perfect for siblings sharing a room or for sleepovers.",
                Price = 459.99m,
                Category = Category.Kids,
                Company = Company.Artifex,
                Image = "https://images.unsplash.com/photo-1549497538-303791108f95?ixlib=rb-4.0.3&auto=format&fit=crop&w=800&q=80",
                Colors = new List<string> { Colors.White.ToString(), Colors.Brown.ToString(), Colors.Gray.ToString() },
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-4),
                UpdatedAt = DateTime.UtcNow.AddDays(-4)
            },

            // Electronics
            new Product
            {
                Title = "4K Smart TV 55 inch",
                Description = "Ultra HD 4K Smart TV with HDR support, built-in streaming apps, and voice control. Crystal clear picture quality with vibrant colors and deep contrasts.",
                Price = 599.99m,
                Category = Category.Electronics,
                Company = Company.Luxora,
                Image = "https://images.unsplash.com/photo-1586023492125-27b2c045efd7?ixlib=rb-4.0.3&auto=format&fit=crop&w=800&q=80",
                Colors = new List<string> { Colors.Black.ToString() },
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                UpdatedAt = DateTime.UtcNow.AddDays(-2)
            },
            new Product
            {
                Title = "Wireless Bluetooth Speaker",
                Description = "Portable high-quality Bluetooth speaker with 360-degree sound, waterproof design, and 12-hour battery life. Perfect for indoor and outdoor use.",
                Price = 89.99m,
                Category = Category.Electronics,
                Company = Company.Modenza,
                Image = "https://images.unsplash.com/photo-1555041469-a586c61ea9bc?ixlib=rb-4.0.3&auto=format&fit=crop&w=800&q=80",
                Colors = new List<string> { Colors.Black.ToString(), Colors.Blue.ToString(), Colors.Red.ToString(), Colors.White.ToString() },
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            },

            // Clothing
            new Product
            {
                Title = "Premium Cotton T-Shirt",
                Description = "Comfortable and breathable 100% organic cotton t-shirt with modern fit. Perfect for casual wear and everyday comfort.",
                Price = 29.99m,
                Category = Category.Clothing,
                Company = Company.Comfora,
                Image = "https://images.unsplash.com/photo-1549497538-303791108f95?ixlib=rb-4.0.3&auto=format&fit=crop&w=800&q=80",
                Colors = new List<string> { Colors.White.ToString(), Colors.Black.ToString(), Colors.Gray.ToString(), Colors.Red.ToString() },
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Product
            {
                Title = "Denim Jeans Classic Fit",
                Description = "Classic fit denim jeans made from premium cotton blend. Durable construction with comfortable stretch and timeless styling.",
                Price = 79.99m,
                Category = Category.Clothing,
                Company = Company.Artifex,
                Image = "https://images.unsplash.com/photo-1506439773649-6e0eb8cfb237?ixlib=rb-4.0.3&auto=format&fit=crop&w=800&q=80",
                Colors = new List<string> { Colors.Blue.ToString(), Colors.Black.ToString() },
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },

            // Books
            new Product
            {
                Title = "Programming Best Practices Guide",
                Description = "Comprehensive guide to modern programming practices, design patterns, and software architecture. Essential reading for developers of all levels.",
                Price = 49.99m,
                Category = Category.Books,
                Company = Company.Luxora,
                Image = "https://images.unsplash.com/photo-1586023492125-27b2c045efd7?ixlib=rb-4.0.3&auto=format&fit=crop&w=800&q=80",
                Colors = new List<string> { Colors.White.ToString(), Colors.Brown.ToString() },
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Product
            {
                Title = "Modern Web Development",
                Description = "Learn the latest web development technologies including React, Node.js, and cloud deployment. Practical examples and real-world projects included.",
                Price = 39.99m,
                Category = Category.Books,
                Company = Company.Modenza,
                Image = "https://images.unsplash.com/photo-1555041469-a586c61ea9bc?ixlib=rb-4.0.3&auto=format&fit=crop&w=800&q=80",
                Colors = new List<string> { Colors.White.ToString(), Colors.Blue.ToString() },
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },

            // Home Decor
            new Product
            {
                Title = "Ceramic Table Lamp Set",
                Description = "Set of 2 elegant ceramic table lamps with fabric lampshades. Perfect for bedside tables, living room, or office spaces. Energy-efficient LED compatible.",
                Price = 149.99m,
                Category = Category.Home,
                Company = Company.Homestead,
                Image = "https://images.unsplash.com/photo-1549497538-303791108f95?ixlib=rb-4.0.3&auto=format&fit=crop&w=800&q=80",
                Colors = new List<string> { Colors.White.ToString(), Colors.Blue.ToString(), Colors.Gray.ToString() },
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Product
            {
                Title = "Wall Art Canvas Print Set",
                Description = "Set of 3 modern abstract canvas prints. High-quality printing on premium canvas with wooden frames. Ready to hang wall art for contemporary spaces.",
                Price = 89.99m,
                Category = Category.Home,
                Company = Company.Artifex,
                Image = "https://images.unsplash.com/photo-1506439773649-6e0eb8cfb237?ixlib=rb-4.0.3&auto=format&fit=crop&w=800&q=80",
                Colors = new List<string> { Colors.Blue.ToString(), Colors.Black.ToString(), Colors.White.ToString() },
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },

            // Additional products for remaining categories
            new Product
            {
                Title = "Yoga Mat Premium",
                Description = "Non-slip premium yoga mat with excellent cushioning and durability. Made from eco-friendly materials with alignment guides for perfect poses.",
                Price = 59.99m,
                Category = Category.Sports,
                Company = Company.Comfora,
                Image = "https://images.unsplash.com/photo-1586023492125-27b2c045efd7?ixlib=rb-4.0.3&auto=format&fit=crop&w=800&q=80",
                Colors = new List<string> { Colors.Purple.ToString(), Colors.Blue.ToString(), Colors.Green.ToString(), Colors.Pink.ToString() },
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Product
            {
                Title = "LED Makeup Mirror",
                Description = "Professional LED makeup mirror with adjustable brightness and magnification options. Touch-sensitive controls and 360-degree rotation for perfect lighting.",
                Price = 79.99m,
                Category = Category.Beauty,
                Company = Company.Modenza,
                Image = "https://images.unsplash.com/photo-1549497538-303791108f95?ixlib=rb-4.0.3&auto=format&fit=crop&w=800&q=80",
                Colors = new List<string> { Colors.White.ToString(), Colors.Pink.ToString() },
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Product
            {
                Title = "Educational Building Blocks Set",
                Description = "Creative building blocks set with 500+ pieces including vehicles, buildings, and figures. Develops creativity, problem-solving, and fine motor skills.",
                Price = 69.99m,
                Category = Category.Toys,
                Company = Company.Homestead,
                Image = "https://images.unsplash.com/photo-1586023492125-27b2c045efd7?ixlib=rb-4.0.3&auto=format&fit=crop&w=800&q=80",
                Colors = new List<string> { Colors.Red.ToString(), Colors.Blue.ToString(), Colors.Green.ToString(), Colors.Yellow.ToString() },
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Product
            {
                Title = "Smart Home Security Camera",
                Description = "WiFi-enabled security camera with night vision, motion detection, and smartphone app control. Easy installation and cloud storage included.",
                Price = 129.99m,
                Category = Category.Other,
                Company = Company.Artifex,
                Image = "https://images.unsplash.com/photo-1549497538-303791108f95?ixlib=rb-4.0.3&auto=format&fit=crop&w=800&q=80",
                Colors = new List<string> { Colors.White.ToString(), Colors.Black.ToString() },
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        context.Products.AddRange(products);
        await context.SaveChangesAsync();
    }
}