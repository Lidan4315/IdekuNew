using Microsoft.EntityFrameworkCore;
using Ideku.Models.Entities;

namespace Ideku.Data.Context
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Employee> Employees { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Idea> Ideas { get; set; }
        public DbSet<Divisi> Divisi { get; set; }
        public DbSet<Departement> Departement { get; set; }
        public DbSet<Category> Category { get; set; }
        public DbSet<Event> Event { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // 🔥 FIX: Configure relationships dengan NO ACTION untuk avoid cascade conflicts
            
            // Employee -> Departement (NO ACTION)
            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Departement)
                .WithMany(d => d.Employees)
                .HasForeignKey(e => e.DepartementId)
                .OnDelete(DeleteBehavior.NoAction);

            // Employee -> Divisi (NO ACTION)
            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Divisi)
                .WithMany(d => d.Employees)
                .HasForeignKey(e => e.DivisiId)
                .OnDelete(DeleteBehavior.NoAction);

            // Departement -> Divisi (RESTRICT - karena ini main relationship)
            modelBuilder.Entity<Departement>()
                .HasOne(d => d.Divisi)
                .WithMany(div => div.Departements)
                .HasForeignKey(d => d.DivisiId)
                .OnDelete(DeleteBehavior.Restrict);

            // User -> Employee (RESTRICT)
            modelBuilder.Entity<User>()
                .HasOne(u => u.Employee)
                .WithMany()
                .HasForeignKey(u => u.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            // 🔥 UPDATED: User -> Role (RESTRICT) with string foreign key
            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany()
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            // Idea relationships (SET NULL - safe)
            modelBuilder.Entity<Idea>()
                .HasOne(i => i.Category)
                .WithMany()
                .HasForeignKey(i => i.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Idea>()
                .HasOne(i => i.Event)
                .WithMany()
                .HasForeignKey(i => i.EventId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}