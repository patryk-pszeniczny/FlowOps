using System;
using FlowOps.Domain.Customers;
using FlowOps.Domain.Subscriptions;
using FlowOps.Infrastructure.Persistence;
using FlowOps.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace FlowOps.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(FlowOpsDbContext))]
    partial class FlowOpsDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.0");

            modelBuilder.Entity("FlowOps.Domain.Customers.Customer", b =>
            {
                b.Property<Guid>("Id")
                    .ValueGeneratedNever()
                    .HasColumnType("uniqueidentifier");

                b.Property<DateTime>("CreatedAt")
                    .HasColumnType("datetime2");

                b.Property<string>("Email")
                    .HasMaxLength(256)
                    .HasColumnType("nvarchar(256)");

                b.Property<string>("Name")
                    .IsRequired()
                    .HasMaxLength(200)
                    .HasColumnType("nvarchar(200)");

                b.Property<string>("TaxId")
                    .HasMaxLength(32)
                    .HasColumnType("nvarchar(32)");

                b.HasKey("Id");

                b.ToTable("Customers", (string)null);
            });

            modelBuilder.Entity("FlowOps.Domain.Subscriptions.Subscription", b =>
            {
                b.Property<Guid>("Id")
                    .ValueGeneratedNever()
                    .HasColumnType("uniqueidentifier");

                b.Property<DateTime?>("ActivatedAt")
                    .HasColumnType("datetime2");

                b.Property<DateTime?>("CancelledAt")
                    .HasColumnType("datetime2");

                b.Property<Guid>("CustomerId")
                    .HasColumnType("uniqueidentifier");

                b.Property<DateTime?>("ExpiresAt")
                    .HasColumnType("datetime2");

                b.Property<string>("PlanCode")
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnType("nvarchar(100)");

                b.Property<DateTime?>("ResumedAt")
                    .HasColumnType("datetime2");

                b.Property<SubscriptionStatus>("Status")
                    .HasConversion<string>()
                    .HasMaxLength(24)
                    .HasColumnType("nvarchar(24)");

                b.Property<DateTime?>("SuspendedAt")
                    .HasColumnType("datetime2");

                b.HasKey("Id");

                b.HasIndex("CustomerId");

                b.ToTable("Subscriptions", (string)null);
            });

            modelBuilder.Entity("FlowOps.Infrastructure.Persistence.Entities.ActiveSubscription", b =>
            {
                b.Property<Guid>("CustomerId")
                    .HasColumnType("uniqueidentifier");

                b.Property<Guid>("SubscriptionId")
                    .HasColumnType("uniqueidentifier");

                b.HasKey("CustomerId", "SubscriptionId");

                b.HasIndex("SubscriptionId");

                b.ToTable("ActiveSubscriptionIds", (string)null);
            });

            modelBuilder.Entity("FlowOps.Infrastructure.Persistence.Entities.CustomerDirectoryEntry", b =>
            {
                b.Property<Guid>("CustomerId")
                    .ValueGeneratedNever()
                    .HasColumnType("uniqueidentifier");

                b.Property<DateTime>("CreatedAt")
                    .HasColumnType("datetime2");

                b.Property<string>("Email")
                    .HasMaxLength(256)
                    .HasColumnType("nvarchar(256)");

                b.Property<string>("Name")
                    .IsRequired()
                    .HasMaxLength(200)
                    .HasColumnType("nvarchar(200)");

                b.Property<string>("TaxId")
                    .HasMaxLength(32)
                    .HasColumnType("nvarchar(32)");

                b.HasKey("CustomerId");

                b.ToTable("CustomerDirectory", (string)null);
            });

            modelBuilder.Entity("FlowOps.Infrastructure.Persistence.Entities.CustomerReport", b =>
            {
                b.Property<Guid>("CustomerId")
                    .HasColumnType("uniqueidentifier");

                b.Property<int>("ActiveSubscriptions")
                    .HasColumnType("int");

                b.Property<decimal>("TotalInvoiced")
                    .HasPrecision(18, 2)
                    .HasColumnType("decimal(18,2)");

                b.Property<decimal>("TotalPaid")
                    .HasPrecision(18, 2)
                    .HasColumnType("decimal(18,2)");

                b.HasKey("CustomerId");

                b.ToTable("CustomerReports", (string)null);
            });

            modelBuilder.Entity("FlowOps.Infrastructure.Persistence.Entities.IdempotencyKeyEntity", b =>
            {
                b.Property<string>("Key")
                    .HasMaxLength(200)
                    .HasColumnType("nvarchar(200)");

                b.Property<Guid>("SubscriptionId")
                    .HasColumnType("uniqueidentifier");

                b.HasKey("Key");

                b.ToTable("IdempotencyKeys", (string)null);
            });

            modelBuilder.Entity("FlowOps.Infrastructure.Persistence.Entities.InboxMessage", b =>
            {
                b.Property<string>("Consumer")
                    .HasMaxLength(200)
                    .HasColumnType("nvarchar(200)");

                b.Property<Guid>("EventId")
                    .HasColumnType("uniqueidentifier");

                b.Property<DateTime>("ProcessedAt")
                    .HasColumnType("datetime2");

                b.HasKey("Consumer", "EventId");

                b.HasIndex("ProcessedAt");

                b.ToTable("InboxMessages", (string)null);
            });

            modelBuilder.Entity("FlowOps.Infrastructure.Persistence.Entities.IntegrationEventEntity", b =>
            {
                b.Property<Guid>("Id")
                    .HasColumnType("uniqueidentifier");

                b.Property<DateTime>("OccurredAt")
                    .HasColumnType("datetime2");

                b.Property<string>("PayLoadJson")
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                b.Property<string>("TypeName")
                    .IsRequired()
                    .HasMaxLength(256)
                    .HasColumnType("nvarchar(256)");

                b.Property<int>("Version")
                    .HasColumnType("int");

                b.HasKey("Id");

                b.ToTable("IntegrationEvents", (string)null);
            });

            modelBuilder.Entity("FlowOps.Domain.Subscriptions.Subscription", b =>
            {
                b.HasOne("FlowOps.Domain.Customers.Customer", null)
                    .WithMany()
                    .HasForeignKey("CustomerId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();
            });

            modelBuilder.Entity("FlowOps.Infrastructure.Persistence.Entities.ActiveSubscription", b =>
            {
                b.HasOne("FlowOps.Domain.Customers.Customer", null)
                    .WithMany()
                    .HasForeignKey("CustomerId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.HasOne("FlowOps.Domain.Subscriptions.Subscription", null)
                    .WithMany()
                    .HasForeignKey("SubscriptionId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();
            });
#pragma warning restore 612, 618
        }
    }
}