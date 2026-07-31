using Microsoft.EntityFrameworkCore;
using ProgramDesigner.Domain;

namespace ProgramDesigner.Infrastructure;

public class ProgramDesignerDbContext : DbContext
{
    public ProgramDesignerDbContext(DbContextOptions<ProgramDesignerDbContext> options)
        : base(options)
    {
    }

    public DbSet<ProgramNode> Nodes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ProgramNode>()
            .ToTable("ProgramNodes")
            .HasDiscriminator<string>("NodeType")
            .HasValue<StepNode>("Step")
            .HasValue<GroupNode>("Group");

        // Self-referencing relationship on ParentId (no cascade delete)
        modelBuilder.Entity<ProgramNode>()
            .HasOne<GroupNode>()
            .WithMany(g => g.Children)
            .HasForeignKey(n => n.ParentId)
            .OnDelete(DeleteBehavior.Restrict); 
    }
}
