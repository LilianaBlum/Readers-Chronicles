using Microsoft.EntityFrameworkCore;

namespace ReadersChronicle.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }
        public DbSet<User> Users { get; set; }
        public DbSet<Profile> Profiles { get; set; }
        public DbSet<Friendship> Friendships { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<UserBook> UserBooks { get; set; }
        public DbSet<BookJournal> BookJournals { get; set; }

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
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascading deletes if needed

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Receiver)
                .WithMany(u => u.ReceivedMessages)
                .HasForeignKey(m => m.ReceiverID)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascading deletes if needed

            base.OnModelCreating(modelBuilder);
        }
    }
}
