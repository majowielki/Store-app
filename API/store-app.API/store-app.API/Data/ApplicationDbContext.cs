using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using store_app.API.Models;
using store_app.API.Utility;

namespace store_app.API.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<ApplicationUser> ApplicationUsers { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<Cart> Carts { get; set; } // <-- Add this line
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Checkout> Checkouts { get; set; }
        //public DbSet<OrdersResponse> OrdersResponses { get; set; }
        //public DbSet<ProductsResponse> ProductsResponses { get; set; }
        //public DbSet<SingleProductResponse> SingleProductResponses { get; set; }
        //public DbSet<CartState> CartStates { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Product>().Property(p => p.Price).HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Product>().HasData(
                new Product
                {
                    Id = 1,
                    Category = Category.Kids,
                    Company = Company.Modenza,
                    Description = "Lorem ipsum dolor sit amet, consectetur adipiscing elit.",
                    Featured = true,
                    Image = "https://images.pexels.com/photos/943150/pexels-photo-943150.jpeg?auto=compress&cs=tinysrgb&w=1600",
                    Price = 17999,
                    Shipping = false,
                    Title = "avant-garde lamp",
                    Colors = new List<string> { "#33FF57", "#3366FF" }
                },
                new Product
                {
                    Id = 2,
                    Category = Category.Chairs,
                    Company = Company.Luxora,
                    Description = "Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.",
                    Featured = false,
                    Image = "https://images.pexels.com/photos/5705090/pexels-photo-5705090.jpeg?auto=compress&cs=tinysrgb&w=1600",
                    Price = 33999,
                    Shipping = true,
                    Title = "chic chair",
                    Colors = new List<string> { "#FF5733", "#33FF57", "#3366FF" }
                },
                new Product
                {
                    Id = 3,
                    Category = Category.Tables,
                    Company = Company.Modenza,
                    Description = "Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris.",
                    Featured = true,
                    Image = "https://images.pexels.com/photos/3679601/pexels-photo-3679601.jpeg?auto=compress&cs=tinysrgb&w=1260&h=750&dpr=2",
                    Price = 17999,
                    Shipping = false,
                    Title = "coffee table",
                    Colors = new List<string> { "#FF5733", "#FFFF00" }
                },
                new Product
                {
                    Id = 4,
                    Category = Category.Beds,
                    Company = Company.Homestead,
                    Description = "Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore.",
                    Featured = true,
                    Image = "https://images.pexels.com/photos/1034584/pexels-photo-1034584.jpeg?auto=compress&cs=tinysrgb&w=1600",
                    Price = 12999,
                    Shipping = false,
                    Title = "comfy bed",
                    Colors = new List<string> { "#FF5733" }
                },
                new Product
                {
                    Id = 5,
                    Category = Category.Sofas,
                    Company = Company.Comfora,
                    Description = "Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia.",
                    Featured = false,
                    Image = "https://images.pexels.com/photos/1571459/pexels-photo-1571459.jpeg?auto=compress&cs=tinysrgb&w=1600",
                    Price = 15999,
                    Shipping = false,
                    Title = "contemporary sofa",
                    Colors = new List<string> { "#FFFF00" }
                },
                new Product
                {
                    Id = 6,
                    Category = Category.Beds,
                    Company = Company.Homestead,
                    Description = "Mollit anim id est laborum.",
                    Featured = false,
                    Image = "https://images.pexels.com/photos/2029694/pexels-photo-2029694.jpeg?auto=compress&cs=tinysrgb&w=1600",
                    Price = 8499,
                    Shipping = true,
                    Title = "cutting-edge bed",
                    Colors = new List<string> { "#FFFF00", "#3366FF" }
                },
                new Product
                {
                    Id = 7,
                    Category = Category.Kids,
                    Company = Company.Luxora,
                    Description = "Lorem ipsum dolor sit amet, consectetur adipiscing elit.",
                    Featured = false,
                    Image = "https://images.pexels.com/photos/2177482/pexels-photo-2177482.jpeg?auto=compress&cs=tinysrgb&w=1600",
                    Price = 9499,
                    Shipping = true,
                    Title = "futuristic shelves",
                    Colors = new List<string> { "#33FF57", "#FFFF00" }
                },
                new Product
                {
                    Id = 8,
                    Category = Category.Tables,
                    Company = Company.Modenza,
                    Description = "Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.",
                    Featured = false,
                    Image = "https://images.pexels.com/photos/1571452/pexels-photo-1571452.jpeg?auto=compress&cs=tinysrgb&w=1600",
                    Price = 15999,
                    Shipping = false,
                    Title = "glass table",
                    Colors = new List<string> { "#FF5733", "#3366FF" }
                },
                new Product
                {
                    Id = 9,
                    Category = Category.Beds,
                    Company = Company.Homestead,
                    Description = "Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris.",
                    Featured = false,
                    Image = "https://images.pexels.com/photos/6489083/pexels-photo-6489083.jpeg?auto=compress&cs=tinysrgb&w=1600",
                    Price = 18999,
                    Shipping = true,
                    Title = "King Bed",
                    Colors = new List<string> { "#FF5733" }
                },
                new Product
                {
                    Id = 10,
                    Category = Category.Chairs,
                    Company = Company.Luxora,
                    Description = "Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore.",
                    Featured = false,
                    Image = "https://images.pexels.com/photos/2082090/pexels-photo-2082090.jpeg?auto=compress&cs=tinysrgb&w=1600",
                    Price = 25999,
                    Shipping = false,
                    Title = "Lounge Chair",
                    Colors = new List<string> { "#FF5733", "#33FF57", "#3366FF" }
                }
            );
        }


    }

}
