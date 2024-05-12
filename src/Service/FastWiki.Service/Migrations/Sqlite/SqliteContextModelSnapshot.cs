﻿// <auto-generated />
using System;
using FastWiki.Service.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace FastWiki.Service.Migrations.Sqlite
{
    [DbContext(typeof(SqliteContext))]
    partial class SqliteContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.0");

            modelBuilder.Entity("FastWiki.Service.Domain.ChatApplications.Aggregates.ChatApplication", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("TEXT");

                    b.Property<string>("ChatModel")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("CreationTime")
                        .HasColumnType("TEXT");

                    b.Property<Guid?>("Creator")
                        .HasColumnType("TEXT");

                    b.Property<string>("Extend")
                        .HasColumnType("TEXT");

                    b.Property<string>("FunctionIds")
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsDeleted")
                        .HasColumnType("INTEGER");

                    b.Property<int>("MaxResponseToken")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("ModificationTime")
                        .HasColumnType("TEXT");

                    b.Property<Guid?>("Modifier")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.Property<string>("NoReplyFoundTemplate")
                        .HasColumnType("TEXT");

                    b.Property<string>("Opener")
                        .HasColumnType("TEXT");

                    b.Property<string>("Parameter")
                        .HasColumnType("TEXT");

                    b.Property<string>("Prompt")
                        .HasColumnType("TEXT");

                    b.Property<int>("ReferenceUpperLimit")
                        .HasColumnType("INTEGER");

                    b.Property<double>("Relevancy")
                        .HasColumnType("REAL");

                    b.Property<bool>("ShowSourceFile")
                        .HasColumnType("INTEGER");

                    b.Property<double>("Temperature")
                        .HasColumnType("REAL");

                    b.Property<string>("Template")
                        .HasColumnType("TEXT");

                    b.Property<string>("WikiIds")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("Name");

                    b.ToTable("wiki-chat-application", (string)null);
                });

            modelBuilder.Entity("FastWiki.Service.Domain.ChatApplications.Aggregates.ChatRecord", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("TEXT");

                    b.Property<string>("ApplicationId")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("CreationTime")
                        .HasColumnType("TEXT");

                    b.Property<Guid>("Creator")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("ModificationTime")
                        .HasColumnType("TEXT");

                    b.Property<Guid>("Modifier")
                        .HasColumnType("TEXT");

                    b.Property<string>("Question")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("CreationTime");

                    b.ToTable("wiki-chat-record", (string)null);
                });

            modelBuilder.Entity("FastWiki.Service.Domain.ChatApplications.Aggregates.ChatShare", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("TEXT");

                    b.Property<string>("APIKey")
                        .HasColumnType("TEXT");

                    b.Property<int>("AvailableQuantity")
                        .HasColumnType("INTEGER");

                    b.Property<long>("AvailableToken")
                        .HasColumnType("INTEGER");

                    b.Property<string>("ChatApplicationId")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("CreationTime")
                        .HasColumnType("TEXT");

                    b.Property<Guid>("Creator")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("Expires")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("ModificationTime")
                        .HasColumnType("TEXT");

                    b.Property<Guid>("Modifier")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.Property<long>("UsedToken")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("ChatApplicationId");

                    b.ToTable("wiki-chat-share", (string)null);
                });

            modelBuilder.Entity("FastWiki.Service.Domain.ChatApplications.Aggregates.Questions", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("TEXT");

                    b.Property<string>("ApplicationId")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("CreationTime")
                        .HasColumnType("TEXT");

                    b.Property<int>("Order")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Question")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("CreationTime");

                    b.ToTable("wiki-questions", (string)null);
                });

            modelBuilder.Entity("FastWiki.Service.Domain.Function.Aggregates.FastWikiFunctionCall", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Content")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("CreationTime")
                        .HasColumnType("TEXT");

                    b.Property<Guid>("Creator")
                        .HasColumnType("TEXT");

                    b.Property<string>("Description")
                        .HasColumnType("TEXT");

                    b.Property<bool>("Enable")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Imports")
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsDeleted")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Items")
                        .HasColumnType("TEXT");

                    b.Property<string>("Main")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("ModificationTime")
                        .HasColumnType("TEXT");

                    b.Property<Guid>("Modifier")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.Property<string>("Parameters")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("CreationTime");

                    b.ToTable("wiki-function-calls", (string)null);
                });

            modelBuilder.Entity("FastWiki.Service.Domain.Storage.Aggregates.FileStorage", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("CreationTime")
                        .HasColumnType("TEXT");

                    b.Property<Guid?>("Creator")
                        .HasColumnType("TEXT");

                    b.Property<string>("FullName")
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsCompression")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsDeleted")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("ModificationTime")
                        .HasColumnType("TEXT");

                    b.Property<Guid?>("Modifier")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .HasMaxLength(100)
                        .HasColumnType("TEXT");

                    b.Property<string>("Path")
                        .HasMaxLength(200)
                        .HasColumnType("TEXT");

                    b.Property<long>("Size")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("wiki-file-storages", (string)null);
                });

            modelBuilder.Entity("FastWiki.Service.Domain.Users.Aggregates.User", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("Account")
                        .HasColumnType("TEXT");

                    b.Property<string>("Avatar")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("CreationTime")
                        .HasColumnType("TEXT");

                    b.Property<Guid?>("Creator")
                        .HasColumnType("TEXT");

                    b.Property<string>("Email")
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsDeleted")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsDisable")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("ModificationTime")
                        .HasColumnType("TEXT");

                    b.Property<Guid?>("Modifier")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .HasMaxLength(100)
                        .HasColumnType("TEXT");

                    b.Property<string>("Password")
                        .HasMaxLength(100)
                        .HasColumnType("TEXT");

                    b.Property<string>("Phone")
                        .HasColumnType("TEXT");

                    b.Property<int>("Role")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Salt")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("wiki-users", (string)null);

                    b.HasData(
                        new
                        {
                            Id = new Guid("e45c6b3f-70b4-4ba2-a992-2179607d752b"),
                            Account = "admin",
                            Avatar = "https://blog-simple.oss-cn-shenzhen.aliyuncs.com/Avatar.jpg",
                            CreationTime = new DateTime(2024, 5, 12, 14, 58, 27, 64, DateTimeKind.Utc).AddTicks(7298),
                            Email = "239573049@qq.com",
                            IsDeleted = false,
                            IsDisable = false,
                            ModificationTime = new DateTime(2024, 5, 12, 14, 58, 27, 64, DateTimeKind.Utc).AddTicks(7301),
                            Name = "admin",
                            Password = "40237cc3bff510e141de01a3f036be71",
                            Phone = "13049809673",
                            Role = 2,
                            Salt = "445acb533eca439b90ebd887084438ab"
                        });
                });

            modelBuilder.Entity("FastWiki.Service.Domain.Wikis.Aggregates.Wiki", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("CreationTime")
                        .HasColumnType("TEXT");

                    b.Property<Guid?>("Creator")
                        .HasColumnType("TEXT");

                    b.Property<string>("EmbeddingModel")
                        .HasColumnType("TEXT");

                    b.Property<string>("Icon")
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsDeleted")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Model")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("ModificationTime")
                        .HasColumnType("TEXT");

                    b.Property<Guid?>("Modifier")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .HasMaxLength(100)
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("Name");

                    b.ToTable("wiki-wikis", (string)null);
                });

            modelBuilder.Entity("FastWiki.Service.Domain.Wikis.Aggregates.WikiDetail", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("CreationTime")
                        .HasColumnType("TEXT");

                    b.Property<long>("Creator")
                        .HasColumnType("INTEGER");

                    b.Property<int>("DataCount")
                        .HasColumnType("INTEGER");

                    b.Property<long>("FileId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("FileName")
                        .HasMaxLength(100)
                        .HasColumnType("TEXT");

                    b.Property<int>("MaxTokensPerLine")
                        .HasColumnType("INTEGER");

                    b.Property<int>("MaxTokensPerParagraph")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Mode")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("ModificationTime")
                        .HasColumnType("TEXT");

                    b.Property<long>("Modifier")
                        .HasColumnType("INTEGER");

                    b.Property<int>("OverlappingTokens")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Path")
                        .HasMaxLength(200)
                        .HasColumnType("TEXT");

                    b.Property<string>("QAPromptTemplate")
                        .HasColumnType("TEXT");

                    b.Property<int>("State")
                        .HasColumnType("INTEGER");

                    b.Property<int>("TrainingPattern")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Type")
                        .HasMaxLength(100)
                        .HasColumnType("TEXT");

                    b.Property<long>("WikiId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("wiki-wiki-details", (string)null);
                });
#pragma warning restore 612, 618
        }
    }
}
