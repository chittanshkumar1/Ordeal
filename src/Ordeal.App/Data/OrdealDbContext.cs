using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Ordeal.App.Models;

namespace Ordeal.App.Data;

public class OrdealDbContext : DbContext
{
    public DbSet<Project> Projects => Set<Project>();

    public DbSet<Task> Tasks => Set<Task>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Task>()
            .HasOne<Project>()
            .WithMany()
            .HasForeignKey(task => task.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Task>()
            .HasOne<Task>()
            .WithMany()
            .HasForeignKey(task => task.ParentTaskId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    protected override void OnConfiguring(
        DbContextOptionsBuilder optionsBuilder)
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData),
            "Ordeal");

        Directory.CreateDirectory(appDataPath);

        var databasePath = Path.Combine(
            appDataPath,
            "ordeal.db");

        optionsBuilder.UseSqlite(
            $"Data Source={databasePath}");
    }
}
