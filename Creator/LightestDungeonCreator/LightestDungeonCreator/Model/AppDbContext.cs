using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Scaffolding.Internal;

namespace LightestDungeonCreator.Model;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Effect> Effects { get; set; }

    public virtual DbSet<Enemy> Enemies { get; set; }

    public virtual DbSet<Entity> Entities { get; set; }

    public virtual DbSet<Item> Items { get; set; }

    public virtual DbSet<Lootentry> Lootentries { get; set; }

    public virtual DbSet<Loottable> Loottables { get; set; }

    public virtual DbSet<Player> Players { get; set; }

    public virtual DbSet<Skill> Skills { get; set; }

    public virtual DbSet<Statistic> Statistics { get; set; }

    public virtual DbSet<Status> Statuses { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseMySql("server=localhost;database=lightestdungeon;uid=root", Microsoft.EntityFrameworkCore.ServerVersion.Parse("10.4.32-mariadb"));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_general_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<Effect>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("effect");

            entity.HasIndex(e => e.StatId, "stat_id");

            entity.HasIndex(e => e.StatusId, "status_id");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.DurationTurns)
                .HasColumnType("int(11)")
                .HasColumnName("duration_turns");
            entity.Property(e => e.EffectLevel)
                .HasColumnType("int(11)")
                .HasColumnName("effect_level");
            entity.Property(e => e.Probability).HasColumnName("probability");
            entity.Property(e => e.StatId)
                .HasColumnType("int(11)")
                .HasColumnName("stat_id");
            entity.Property(e => e.StatMultiplier).HasColumnName("stat_multiplier");
            entity.Property(e => e.StatusId)
                .HasColumnType("int(11)")
                .HasColumnName("status_id");

            entity.HasOne(d => d.Stat).WithMany(p => p.Effects)
                .HasForeignKey(d => d.StatId)
                .HasConstraintName("effect_ibfk_1");

            entity.HasOne(d => d.Status).WithMany(p => p.Effects)
                .HasForeignKey(d => d.StatusId)
                .HasConstraintName("effect_ibfk_2");
        });

        modelBuilder.Entity<Enemy>(entity =>
        {
            entity.HasKey(e => e.EntityId).HasName("PRIMARY");

            entity.ToTable("enemy");

            entity.Property(e => e.EntityId)
                .ValueGeneratedNever()
                .HasColumnType("int(11)")
                .HasColumnName("entity_id");
            entity.Property(e => e.PassiveId)
                .HasColumnType("int(11)")
                .HasColumnName("passiveId");

            entity.HasOne(d => d.Entity).WithOne(p => p.Enemy)
                .HasForeignKey<Enemy>(d => d.EntityId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("enemy_ibfk_1");
        });

        modelBuilder.Entity<Entity>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("entity");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.AccuracyMultiplier).HasColumnName("accuracy_multiplier");
            entity.Property(e => e.Attack)
                .HasColumnType("int(11)")
                .HasColumnName("attack");
            entity.Property(e => e.CritChance).HasColumnName("crit_chance");
            entity.Property(e => e.CritDamage).HasColumnName("crit_damage");
            entity.Property(e => e.Defense)
                .HasColumnType("int(11)")
                .HasColumnName("defense");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasColumnName("description");
            entity.Property(e => e.Energy)
                .HasColumnType("int(11)")
                .HasColumnName("energy");
            entity.Property(e => e.EnergyMax)
                .HasColumnType("int(11)")
                .HasColumnName("energy_max");
            entity.Property(e => e.Hp)
                .HasColumnType("int(11)")
                .HasColumnName("hp");
            entity.Property(e => e.HpMax)
                .HasColumnType("int(11)")
                .HasColumnName("hp_max");
            entity.Property(e => e.ImageFull)
                .HasMaxLength(500)
                .HasColumnName("image_full");
            entity.Property(e => e.ImageThumb)
                .HasMaxLength(500)
                .HasColumnName("image_thumb");
            entity.Property(e => e.Level)
                .HasColumnType("int(11)")
                .HasColumnName("level");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
            entity.Property(e => e.Speed)
                .HasColumnType("int(11)")
                .HasColumnName("speed");

            entity.HasMany(d => d.Skills).WithMany(p => p.Entities)
                .UsingEntity<Dictionary<string, object>>(
                    "Entityskill",
                    r => r.HasOne<Skill>().WithMany()
                        .HasForeignKey("SkillId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("entityskill_ibfk_2"),
                    l => l.HasOne<Entity>().WithMany()
                        .HasForeignKey("EntityId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("entityskill_ibfk_1"),
                    j =>
                    {
                        j.HasKey("EntityId", "SkillId")
                            .HasName("PRIMARY")
                            .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });
                        j.ToTable("entityskill");
                        j.HasIndex(new[] { "SkillId" }, "skill_id");
                        j.IndexerProperty<int>("EntityId")
                            .HasColumnType("int(11)")
                            .HasColumnName("entity_id");
                        j.IndexerProperty<int>("SkillId")
                            .HasColumnType("int(11)")
                            .HasColumnName("skill_id");
                    });
        });

        modelBuilder.Entity<Item>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("item");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Consumable).HasColumnName("consumable");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasColumnName("description");
            entity.Property(e => e.ImageThumb)
                .HasMaxLength(500)
                .HasColumnName("image_thumb");
            entity.Property(e => e.MaxUses)
                .HasColumnType("int(11)")
                .HasColumnName("max_uses");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.Quality)
                .HasMaxLength(20)
                .HasColumnName("quality");

            entity.HasMany(d => d.Effects).WithMany(p => p.Items)
                .UsingEntity<Dictionary<string, object>>(
                    "Itemeffect",
                    r => r.HasOne<Effect>().WithMany()
                        .HasForeignKey("EffectId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("itemeffect_ibfk_2"),
                    l => l.HasOne<Item>().WithMany()
                        .HasForeignKey("ItemId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("itemeffect_ibfk_1"),
                    j =>
                    {
                        j.HasKey("ItemId", "EffectId")
                            .HasName("PRIMARY")
                            .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });
                        j.ToTable("itemeffect");
                        j.HasIndex(new[] { "EffectId" }, "effect_id");
                        j.IndexerProperty<int>("ItemId")
                            .HasColumnType("int(11)")
                            .HasColumnName("item_id");
                        j.IndexerProperty<int>("EffectId")
                            .HasColumnType("int(11)")
                            .HasColumnName("effect_id");
                    });
        });

        modelBuilder.Entity<Lootentry>(entity =>
        {
            entity.HasKey(e => e.Num).HasName("PRIMARY");

            entity.ToTable("lootentry");

            entity.HasIndex(e => e.ItemId, "item_id");

            entity.HasIndex(e => e.LootTableId, "loot_table_id");

            entity.Property(e => e.Num)
                .HasColumnType("int(11)")
                .HasColumnName("num");
            entity.Property(e => e.DropChance).HasColumnName("drop_chance");
            entity.Property(e => e.ItemId)
                .HasColumnType("int(11)")
                .HasColumnName("item_id");
            entity.Property(e => e.LootTableId)
                .HasColumnType("int(11)")
                .HasColumnName("loot_table_id");
            entity.Property(e => e.MaxQuality)
                .HasMaxLength(20)
                .HasColumnName("max_quality");
            entity.Property(e => e.MinQuality)
                .HasMaxLength(20)
                .HasColumnName("min_quality");

            entity.HasOne(d => d.Item).WithMany(p => p.Lootentries)
                .HasForeignKey(d => d.ItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("lootentry_ibfk_2");

            entity.HasOne(d => d.LootTable).WithMany(p => p.Lootentries)
                .HasForeignKey(d => d.LootTableId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("lootentry_ibfk_1");
        });

        modelBuilder.Entity<Loottable>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("loottable");

            entity.HasIndex(e => e.EnemyId, "enemy_id");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.EnemyId)
                .HasColumnType("int(11)")
                .HasColumnName("enemy_id");

            entity.HasOne(d => d.Enemy).WithMany(p => p.Loottables)
                .HasForeignKey(d => d.EnemyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("loottable_ibfk_1");
        });

        modelBuilder.Entity<Player>(entity =>
        {
            entity.HasKey(e => e.EntityId).HasName("PRIMARY");

            entity.ToTable("player");

            entity.Property(e => e.EntityId)
                .ValueGeneratedNever()
                .HasColumnType("int(11)")
                .HasColumnName("entity_id");
            entity.Property(e => e.SkillPoints)
                .HasColumnType("int(11)")
                .HasColumnName("skill_points");
            entity.Property(e => e.XpPoints)
                .HasColumnType("int(11)")
                .HasColumnName("xp_points");

            entity.HasOne(d => d.Entity).WithOne(p => p.Player)
                .HasForeignKey<Player>(d => d.EntityId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("player_ibfk_1");
        });

        modelBuilder.Entity<Skill>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("skill");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Accuracy).HasColumnName("accuracy");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasColumnName("description");
            entity.Property(e => e.EnergyCost)
                .HasColumnType("int(11)")
                .HasColumnName("energy_cost");
            entity.Property(e => e.Hits)
                .HasColumnType("int(11)")
                .HasColumnName("hits");
            entity.Property(e => e.ImageThumb)
                .HasMaxLength(500)
                .HasColumnName("image_thumb");
            entity.Property(e => e.IsAoe).HasColumnName("is_aoe");
            entity.Property(e => e.IsPassive).HasColumnName("is_passive");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
            entity.Property(e => e.TargetType)
                .HasMaxLength(20)
                .HasColumnName("target_type");

            entity.HasMany(d => d.Effects).WithMany(p => p.Skills)
                .UsingEntity<Dictionary<string, object>>(
                    "Skilleffect",
                    r => r.HasOne<Effect>().WithMany()
                        .HasForeignKey("EffectId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("skilleffect_ibfk_2"),
                    l => l.HasOne<Skill>().WithMany()
                        .HasForeignKey("SkillId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("skilleffect_ibfk_1"),
                    j =>
                    {
                        j.HasKey("SkillId", "EffectId")
                            .HasName("PRIMARY")
                            .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });
                        j.ToTable("skilleffect");
                        j.HasIndex(new[] { "EffectId" }, "effect_id");
                        j.IndexerProperty<int>("SkillId")
                            .HasColumnType("int(11)")
                            .HasColumnName("skill_id");
                        j.IndexerProperty<int>("EffectId")
                            .HasColumnType("int(11)")
                            .HasColumnName("effect_id");
                    });
        });

        modelBuilder.Entity<Statistic>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("statistic");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Status>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("status");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasColumnName("description");
            entity.Property(e => e.MaxLevel)
                .HasColumnType("int(11)")
                .HasColumnName("max_level");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
            entity.Property(e => e.ScalingFormula)
                .HasMaxLength(250)
                .HasColumnName("scaling_formula");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
