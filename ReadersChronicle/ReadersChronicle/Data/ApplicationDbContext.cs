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
        public DbSet<Message> Messages { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<UserBook> UserBooks { get; set; }
        public DbSet<Article> Articles { get; set; }
        public DbSet<BookJournal> BookJournals { get; set; }
        public DbSet<ArticleRating> ArticleRatings { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<CommentRating> CommentRatings { get; set; }
        public DbSet<PendingFriendship> PendingFriendships { get; set; }
    public DbSet<Friendship> Friendships { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PendingFriendship>()
            .HasKey(pf => pf.FriendshipID);

            modelBuilder.Entity<PendingFriendship>()
                .HasOne(pf => pf.InitiatorUser)
                .WithMany()
                .HasForeignKey(pf => pf.InitiatorUserID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PendingFriendship>()
                .HasOne(pf => pf.ApprovingUser)
                .WithMany()
                .HasForeignKey(pf => pf.ApprovingUserID)
                .OnDelete(DeleteBehavior.Restrict);

            

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
                .WithOne(u => u.Profile)
                .HasForeignKey<Profile>(p => p.UserID)
                .OnDelete(DeleteBehavior.Restrict);

            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Friendship>(entity =>
            {
                entity.HasKey(f => f.FriendshipID);

                // Explicitly configure User1 relationship
                entity.HasOne(f => f.User1)
                    .WithMany()
                    .HasForeignKey(f => f.User1ID)
                    .OnDelete(DeleteBehavior.Restrict);

                // Explicitly configure User2 relationship
                entity.HasOne(f => f.User2)
                    .WithMany()
                    .HasForeignKey(f => f.User2ID)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ArticleRating>()
            .HasOne(ar => ar.Article)
            .WithMany(a => a.ArticleRatings)
            .HasForeignKey(ar => ar.ArticleId)
            .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ArticleRating>()
                .HasOne(ar => ar.User)
                .WithMany()
                .HasForeignKey(ar => ar.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Comment>()
            .HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Article>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CommentRating>()
                .HasOne(cr => cr.Comment)
                .WithMany()
                .HasForeignKey(cr => cr.CommentId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
