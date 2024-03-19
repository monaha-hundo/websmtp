﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using websmtp.Database;

#nullable disable

namespace websmtp.Migrations
{
    [DbContext(typeof(DataContext))]
    [Migration("20240318101410_MultiUsers2")]
    partial class MultiUsers2
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.1")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            MySqlModelBuilderExtensions.AutoIncrementColumns(modelBuilder);

            modelBuilder.Entity("websmtp.Database.Models.Message", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("char(36)");

                    b.Property<int>("AttachementsCount")
                        .HasColumnType("int");

                    b.Property<string>("Bcc")
                        .IsRequired()
                        .HasMaxLength(1000)
                        .HasColumnType("varchar(1000)");

                    b.Property<string>("Cc")
                        .IsRequired()
                        .HasMaxLength(1000)
                        .HasColumnType("varchar(1000)");

                    b.Property<bool>("Deleted")
                        .HasColumnType("tinyint(1)");

                    b.Property<bool>("DkimFailed")
                        .HasColumnType("tinyint(1)");

                    b.Property<bool>("DmarcFailed")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("From")
                        .IsRequired()
                        .HasMaxLength(1000)
                        .HasColumnType("varchar(1000)");

                    b.Property<string>("HtmlContent")
                        .HasColumnType("longtext");

                    b.Property<string>("Importance")
                        .IsRequired()
                        .HasMaxLength(8)
                        .HasColumnType("varchar(8)");

                    b.Property<Guid>("RawMessageId")
                        .HasColumnType("char(36)");

                    b.Property<bool>("Read")
                        .HasColumnType("tinyint(1)");

                    b.Property<DateTimeOffset>("ReceivedOn")
                        .HasColumnType("datetime(6)");

                    b.Property<bool>("Sent")
                        .HasColumnType("tinyint(1)");

                    b.Property<int>("SpfStatus")
                        .HasColumnType("int");

                    b.Property<bool>("Stared")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("Subject")
                        .IsRequired()
                        .HasMaxLength(1000)
                        .HasColumnType("varchar(1000)");

                    b.Property<string>("TextContent")
                        .HasColumnType("longtext");

                    b.Property<string>("To")
                        .IsRequired()
                        .HasMaxLength(1000)
                        .HasColumnType("varchar(1000)");

                    b.Property<int>("UserId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("RawMessageId");

                    b.HasIndex("UserId");

                    b.ToTable("Messages");
                });

            modelBuilder.Entity("websmtp.Database.Models.MessageAttachement", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("char(36)");

                    b.Property<string>("Content")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("ContentId")
                        .IsRequired()
                        .HasMaxLength(1000)
                        .HasColumnType("varchar(1000)");

                    b.Property<string>("Filename")
                        .IsRequired()
                        .HasMaxLength(1000)
                        .HasColumnType("varchar(1000)");

                    b.Property<Guid?>("MessageId")
                        .HasColumnType("char(36)");

                    b.Property<string>("MimeType")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("varchar(255)");

                    b.HasKey("Id");

                    b.HasIndex("MessageId");

                    b.ToTable("MessageAttachement");
                });

            modelBuilder.Entity("websmtp.Database.Models.RawMessage", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("char(36)");

                    b.Property<byte[]>("Content")
                        .IsRequired()
                        .HasColumnType("longblob");

                    b.HasKey("Id");

                    b.ToTable("RawMessages");
                });

            modelBuilder.Entity("websmtp.Database.Models.User", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("OtpSecret")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("varchar(20)");

                    b.Property<string>("PasswordHash")
                        .IsRequired()
                        .HasMaxLength(1000)
                        .HasColumnType("varchar(1000)");

                    b.Property<string>("Roles")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("varchar(100)");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasMaxLength(1000)
                        .HasColumnType("varchar(1000)");

                    b.HasKey("Id");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("websmtp.UserMailbox", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("DisplayName")
                        .IsRequired()
                        .HasMaxLength(256)
                        .HasColumnType("varchar(256)");

                    b.Property<string>("Host")
                        .IsRequired()
                        .HasMaxLength(1000)
                        .HasColumnType("varchar(1000)");

                    b.Property<string>("Identity")
                        .IsRequired()
                        .HasMaxLength(1000)
                        .HasColumnType("varchar(1000)");

                    b.Property<int>("UserId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("Mailboxes");
                });

            modelBuilder.Entity("websmtp.Database.Models.Message", b =>
                {
                    b.HasOne("websmtp.Database.Models.RawMessage", "RawMessage")
                        .WithMany()
                        .HasForeignKey("RawMessageId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("websmtp.Database.Models.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("RawMessage");

                    b.Navigation("User");
                });

            modelBuilder.Entity("websmtp.Database.Models.MessageAttachement", b =>
                {
                    b.HasOne("websmtp.Database.Models.Message", null)
                        .WithMany("Attachements")
                        .HasForeignKey("MessageId");
                });

            modelBuilder.Entity("websmtp.UserMailbox", b =>
                {
                    b.HasOne("websmtp.Database.Models.User", "User")
                        .WithMany("Mailboxes")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("websmtp.Database.Models.Message", b =>
                {
                    b.Navigation("Attachements");
                });

            modelBuilder.Entity("websmtp.Database.Models.User", b =>
                {
                    b.Navigation("Mailboxes");
                });
#pragma warning restore 612, 618
        }
    }
}
