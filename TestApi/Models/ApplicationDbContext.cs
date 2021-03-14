using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TestApi.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<User> Users { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Product>().HasData(
                new Product
                {
                    Id = 1, Name = "Product 1", Price = 100
                },
            new Product
                {
                    Id = 2,
                    Name = "Product 2",
                    Price = 200
                },
                new Product
                {
                    Id = 3,
                    Name = "Product 3",
                    Price = 300
                }

            );
        }
    }
}
