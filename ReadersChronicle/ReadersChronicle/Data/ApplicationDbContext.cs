using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ReadersChronicle.Data
{
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }
        public DbSet<Profile> Profiles { get; set; }
        public DbSet<Friendship> Friendships { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<UserBook> UserBooks { get; set; }
        public DbSet<Article> Articles { get; set; }
        public DbSet<BookJournal> BookJournals { get; set; }
        public DbSet<ArticleRating> ArticleRatings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configuring User and Friendship relationship
            modelBuilder.Entity<Friendship>()
                .HasOne(f => f.User1)
                .WithMany(u => u.Friendships1)
                .HasForeignKey(f => f.UserID1)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascading deletes if needed

            modelBuilder.Entity<Friendship>()
                .HasOne(f => f.User2)
                .WithMany(u => u.Friendships2)
                .HasForeignKey(f => f.UserID2)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascading deletes if needed

            modelBuilder.Entity<Message>()
            .HasOne(m => m.Sender)
            .WithMany(u => u.SentMessages)
            .HasForeignKey(m => m.SenderID)
            .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Receiver)
                .WithMany(u => u.ReceivedMessages)
                .HasForeignKey(m => m.ReceiverID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
        .HasKey(u => u.Id);

            modelBuilder.Entity<Profile>()
        .HasOne(p => p.User)
        .WithOne(u => u.Profile) // Assuming one-to-one relationship
        .HasForeignKey<Profile>(p => p.UserID) // Set foreign key to UserID
        .OnDelete(DeleteBehavior.Restrict); // Configure deletion behavior as needed

            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ArticleRating>()
            .HasOne(ar => ar.Article)
            .WithMany(a => a.ArticleRatings) // Add navigation property to Article model
            .HasForeignKey(ar => ar.ArticleId)
            .OnDelete(DeleteBehavior.Cascade); // Retain cascade here

            modelBuilder.Entity<ArticleRating>()
                .HasOne(ar => ar.User)
                .WithMany() // No navigation property for User model
                .HasForeignKey(ar => ar.UserId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete here
        }
    }
}
