﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ReadersChronicle.Data;

#nullable disable

namespace ReadersChronicle.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20241023194013_UpdateUserPasswordLength")]
    partial class UpdateUserPasswordLength
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.10")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("ReadersChronicle.Data.BookJournal", b =>
                {
                    b.Property<int>("JournalID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("JournalID"));

                    b.Property<string>("AdditionalNotes")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("AuthorsAim")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("EndDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("Insights")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("OverallImpression")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<double?>("OverallRating")
                        .HasColumnType("float");

                    b.Property<string>("Recommendation")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("StartDate")
                        .HasColumnType("datetime2");

                    b.Property<int>("UserBookID")
                        .HasColumnType("int");

                    b.HasKey("JournalID");

                    b.HasIndex("UserBookID")
                        .IsUnique();

                    b.ToTable("BookJournals");
                });

            modelBuilder.Entity("ReadersChronicle.Data.Friendship", b =>
                {
                    b.Property<int>("FriendshipID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("FriendshipID"));

                    b.Property<string>("FriendshipStatus")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("UserID1")
                        .HasColumnType("int");

                    b.Property<int>("UserID2")
                        .HasColumnType("int");

                    b.HasKey("FriendshipID");

                    b.HasIndex("UserID1");

                    b.HasIndex("UserID2");

                    b.ToTable("Friendships");
                });

            modelBuilder.Entity("ReadersChronicle.Data.Message", b =>
                {
                    b.Property<int>("MessageID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("MessageID"));

                    b.Property<string>("Content")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("IsRead")
                        .HasColumnType("bit");

                    b.Property<int>("ReceiverID")
                        .HasColumnType("int");

                    b.Property<int>("SenderID")
                        .HasColumnType("int");

                    b.Property<DateTime>("Timestamp")
                        .HasColumnType("datetime2");

                    b.HasKey("MessageID");

                    b.HasIndex("ReceiverID");

                    b.HasIndex("SenderID");

                    b.ToTable("Messages");
                });

            modelBuilder.Entity("ReadersChronicle.Data.Profile", b =>
                {
                    b.Property<int>("ProfileID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("ProfileID"));

                    b.Property<string>("Bio")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ProfilePicture")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("UserID")
                        .HasColumnType("int");

                    b.HasKey("ProfileID");

                    b.HasIndex("UserID")
                        .IsUnique();

                    b.ToTable("Profiles");
                });

            modelBuilder.Entity("ReadersChronicle.Data.Review", b =>
                {
                    b.Property<int>("ReviewID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("ReviewID"));

                    b.Property<string>("Content")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("Timestamp")
                        .HasColumnType("datetime2");

                    b.Property<int>("UserID")
                        .HasColumnType("int");

                    b.HasKey("ReviewID");

                    b.HasIndex("UserID");

                    b.ToTable("Reviews");
                });

            modelBuilder.Entity("ReadersChronicle.Data.User", b =>
                {
                    b.Property<int>("UserID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("UserID"));

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("JoinDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("Password")
                        .IsRequired()
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)");

                    b.Property<string>("UserName")
                        .IsRequired()
                        .HasMaxLength(25)
                        .HasColumnType("nvarchar(25)");

                    b.Property<string>("UserType")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("UserID");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("ReadersChronicle.Data.UserBook", b =>
                {
                    b.Property<int>("UserBookID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("UserBookID"));

                    b.Property<string>("Author")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("BookID")
                        .HasColumnType("int");

                    b.Property<int>("CurrentPage")
                        .HasColumnType("int");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime?>("EndDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("Folder")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<double?>("OverallRating")
                        .HasColumnType("float");

                    b.Property<DateTime?>("StartDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("UserID")
                        .HasColumnType("int");

                    b.HasKey("UserBookID");

                    b.HasIndex("UserID");

                    b.ToTable("UserBooks");
                });

            modelBuilder.Entity("ReadersChronicle.Data.BookJournal", b =>
                {
                    b.HasOne("ReadersChronicle.Data.UserBook", "UserBook")
                        .WithOne("BookJournal")
                        .HasForeignKey("ReadersChronicle.Data.BookJournal", "UserBookID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("UserBook");
                });

            modelBuilder.Entity("ReadersChronicle.Data.Friendship", b =>
                {
                    b.HasOne("ReadersChronicle.Data.User", "User1")
                        .WithMany("Friendships1")
                        .HasForeignKey("UserID1")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("ReadersChronicle.Data.User", "User2")
                        .WithMany("Friendships2")
                        .HasForeignKey("UserID2")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("User1");

                    b.Navigation("User2");
                });

            modelBuilder.Entity("ReadersChronicle.Data.Message", b =>
                {
                    b.HasOne("ReadersChronicle.Data.User", "Receiver")
                        .WithMany("ReceivedMessages")
                        .HasForeignKey("ReceiverID")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("ReadersChronicle.Data.User", "Sender")
                        .WithMany("SentMessages")
                        .HasForeignKey("SenderID")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Receiver");

                    b.Navigation("Sender");
                });

            modelBuilder.Entity("ReadersChronicle.Data.Profile", b =>
                {
                    b.HasOne("ReadersChronicle.Data.User", "User")
                        .WithOne("Profile")
                        .HasForeignKey("ReadersChronicle.Data.Profile", "UserID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("ReadersChronicle.Data.Review", b =>
                {
                    b.HasOne("ReadersChronicle.Data.User", "User")
                        .WithMany("Reviews")
                        .HasForeignKey("UserID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("ReadersChronicle.Data.UserBook", b =>
                {
                    b.HasOne("ReadersChronicle.Data.User", "User")
                        .WithMany("UserBooks")
                        .HasForeignKey("UserID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("ReadersChronicle.Data.User", b =>
                {
                    b.Navigation("Friendships1");

                    b.Navigation("Friendships2");

                    b.Navigation("Profile")
                        .IsRequired();

                    b.Navigation("ReceivedMessages");

                    b.Navigation("Reviews");

                    b.Navigation("SentMessages");

                    b.Navigation("UserBooks");
                });

            modelBuilder.Entity("ReadersChronicle.Data.UserBook", b =>
                {
                    b.Navigation("BookJournal")
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
