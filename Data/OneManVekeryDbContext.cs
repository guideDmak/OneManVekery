using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using OneManVekery.Models;

namespace OneManVekery.Data;

public partial class OneManVekeryDbContext : DbContext
{
    public OneManVekeryDbContext(DbContextOptions<OneManVekeryDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_unicode_ci")
            .HasCharSet("utf8mb4");

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
