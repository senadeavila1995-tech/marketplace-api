using Microsoft.EntityFrameworkCore;
using Marketplace.API.Models;

namespace Marketplace.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<CartItem> CartItems => Set<CartItem>();

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Payment> Payments => Set<Payment>();

    public DbSet<SellerPaymentAccount> SellerPaymentAccounts => Set<SellerPaymentAccount>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // =========================
        // USERS
        // =========================
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash");
            entity.Property(e => e.Role).HasColumnName("role");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        });

        // =========================
        // ORDERS
        // =========================
        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("orders");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Total).HasColumnName("total");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.HasOne(o => o.User)
                .WithMany()
                .HasForeignKey(o => o.UserId);
        });

        // =========================
        // ORDER ITEMS
        // =========================
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("order_items");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.Price).HasColumnName("price");

            entity.HasOne(oi => oi.Order)
                .WithMany(o => o.Items)
                .HasForeignKey(oi => oi.OrderId);

            entity.HasOne(oi => oi.Product)
                .WithMany()
                .HasForeignKey(oi => oi.ProductId);
        });

        // =========================
        // PAYMENTS (ESCROW SYSTEM)
        // =========================
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable("payments");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.Method).HasColumnName("method");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.PaidAt).HasColumnName("paid_at");

            entity.Property(e => e.SellerId).HasColumnName("seller_id");
            entity.Property(e => e.StoreId).HasColumnName("store_id");

            entity.Property(e => e.ReleaseDate).HasColumnName("release_date");
            entity.Property(e => e.AdminConfirmed).HasColumnName("admin_confirmed");
            entity.Property(e => e.EscrowStatus).HasColumnName("escrow_status");
           
            entity.HasOne(p => p.Order)
                .WithMany()
                .HasForeignKey(p => p.OrderId);
        });

        // =========================
        // SELLER PAYMENT ACCOUNTS (PAYPAL)
        // =========================
      modelBuilder.Entity<SellerPaymentAccount>(entity =>
        {
            entity.ToTable("seller_payment_accounts");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.StoreId).HasColumnName("store_id");

            entity.Property(e => e.Provider)
                .HasColumnName("provider");

            entity.Property(e => e.PaypalEmail).HasColumnName("paypal_email");

            entity.Property(e => e.IsVerified).HasColumnName("is_verified");
            entity.Property(e => e.IsActive).HasColumnName("is_active");

            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId);

            entity.HasOne(x => x.Store)
                .WithMany()
                .HasForeignKey(x => x.StoreId);
        });

        // =========================
        // STORES
        // =========================
        modelBuilder.Entity<Store>(entity =>
        {
            entity.ToTable("stores");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId);
        });

        // =========================
        // PRODUCTS
        // =========================
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("products");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Price).HasColumnName("price");
            entity.Property(e => e.Stock).HasColumnName("stock");

            entity.Property(e => e.ImageUrl).HasColumnName("image_url");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.Property(e => e.StoreId).HasColumnName("store_id");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");

            entity.HasOne(p => p.Store)
                .WithMany(s => s.Products)
                .HasForeignKey(p => p.StoreId);

            entity.HasOne(p => p.Category)
                .WithMany()
                .HasForeignKey(p => p.CategoryId);
        });

        // =========================
        // CATEGORIES
        // =========================
        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("categories");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        });

        // =========================
        // CART ITEMS
        // =========================
        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.ToTable("cart_items");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId);

            entity.HasOne(c => c.Product)
                .WithMany()
                .HasForeignKey(c => c.ProductId);
        });
    }
}