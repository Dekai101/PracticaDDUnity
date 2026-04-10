using LightestDungeonCreator.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace LightestDungeonCreator
{
    // ── ViewModel para cada entrada de loot en el panel de detalle ───
    public class LootEntryDisplayVM
    {
        public string ItemName { get; set; } = "";
        public string ItemThumb { get; set; } = "";
        public string MinQuality { get; set; } = "";
        public string MaxQuality { get; set; } = "";
        public float DropChance { get; set; }  // 0-1

        public string DropChanceDisplay => $"{DropChance * 100:0.#}%";
    }

    public partial class EnemyListWindow : Window
    {
        AppDbContext db;

        // ── State ────────────────────────────────────────────────────
        private List<Enemy> _allEnemies = new();
        private List<Enemy> _filtered = new();
        private Enemy? _selected;

        // ── Constructor ──────────────────────────────────────────────
        public EnemyListWindow()
        {
            db = new AppDbContext();

            InitializeComponent();
            LoadEnemies();
        }

        // ── Window drag ──────────────────────────────────────────────
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
            => DragMove();

        // ── Navigation ───────────────────────────────────────────────
        private void BackButton_Click(object sender, RoutedEventArgs e)
            => Close();

        private void NewEnemy_Click(object sender, RoutedEventArgs e)
        {
            var creator = new EnemyCreatorWindow { Owner = this };
            creator.ShowDialog();
            LoadEnemies();
        }

        // ── Data ─────────────────────────────────────────────────────
        private void LoadEnemies()
        {
            try
            {
                _allEnemies = db.Enemies
                                .Include(e => e.Entity)
                                    .ThenInclude(en => en.Skills)
                                .Include(e => e.Loottables)           // ← loot tables del enemic
                                    .ThenInclude(lt => lt.Lootentries) // ← entrades de loot
                                        .ThenInclude(le => le.Item)   // ← item de cada entrada
                                .OrderBy(e => e.Entity.Name)
                                .ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading enemies:\n{ex.Message}",
                                "DB Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                _allEnemies = new List<Enemy>();
            }

            ApplyFilters();
        }

        // ── Filters ──────────────────────────────────────────────────
        private void FilterName_Changed(object sender, TextChangedEventArgs e)
            => ApplyFilters();

        private void FilterLevelSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (MinLevelVal == null) return;
            int val = (int)e.NewValue;
            MinLevelVal.Text = val <= 1 ? "Any" : val.ToString();
            ApplyFilters();
        }

        private void FilterHpSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (MinHpVal == null) return;
            int val = (int)e.NewValue;
            MinHpVal.Text = val == 0 ? "Any" : val.ToString();
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            if (_allEnemies == null || EnemyList == null) return;

            string name = FilterNameInput?.Text?.ToLower() ?? "";
            int minLvl = (int)(MinLevelFilter?.Value ?? 1);
            int minHp = (int)(MinHpFilter?.Value ?? 0);

            _filtered = _allEnemies.Where(enemy =>
            {
                var en = enemy.Entity;
                if (en == null) return false;
                if (!string.IsNullOrEmpty(name) && !en.Name.ToLower().Contains(name))
                    return false;
                if (minLvl > 1 && en.Level < minLvl)
                    return false;
                // HpMax lives on Entity, not on Enemy directly
                if (minHp > 0 && en.HpMax < minHp)
                    return false;
                return true;
            }).ToList();

            EnemyList.ItemsSource = _filtered;
            EnemyCountLabel.Text = $"{_filtered.Count} enem{(_filtered.Count != 1 ? "ies" : "y")}";
            NoEnemiesLabel.Visibility = _filtered.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            FooterLabel.Text = $"{_allEnemies.Count} total  ·  {_filtered.Count} shown";

            if (_selected != null && !_filtered.Contains(_selected))
                ClearDetail();
        }

        // ── List item click ──────────────────────────────────────────
        private void EnemyItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border b && b.Tag is Enemy enemy)
                ShowDetail(enemy);
        }

        // ── Detail panel ─────────────────────────────────────────────
        private void ShowDetail(Enemy enemy)
        {
            _selected = enemy;
            var en = enemy.Entity; // Entity is the navigation property on Enemy

            DetailEmpty.Visibility = Visibility.Collapsed;
            DetailPanel.Visibility = Visibility.Visible;

            // Images — paths stored in Entity
            DetailImage.Source = TryLoadBitmap(en.ImageThumb);
            DetailFullImage.Source = TryLoadBitmap(en.ImageFull);

            // Header
            DetailName.Text = en.Name.ToUpper();
            DetailLevel.Text = en.Level.ToString();
            DetailId.Text = $"Entity ID: {en.Id}  ·  PassiveSkill ID: {enemy.PassiveId}";

            // Description
            DetailDescription.Text = string.IsNullOrWhiteSpace(en.Description)
                ? "— No description —" : en.Description;

            // Combat stats — all on Entity
            DetailHp.Text = $"{en.Hp} / {en.HpMax}";
            DetailEnergy.Text = $"{en.Energy} / {en.EnergyMax}";
            DetailAttack.Text = en.Attack.ToString();
            DetailDefense.Text = en.Defense.ToString();
            DetailSpeed.Text = en.Speed.ToString();
            DetailCrit.Text = $"{en.CritChance:P0}   x{(int)(en.CritDamage * 100)}";

            // Stat bars
            SetBarWidth(BarHp, en.HpMax, 1000);
            SetBarWidth(BarEnergy, en.EnergyMax, 300);
            SetBarWidth(BarAttack, en.Attack, 300);
            SetBarWidth(BarDefense, en.Defense, 300);

            // Skills — Entity.Skills is ICollection<Skill> (many-to-many via Entityskill)
            if (en.Skills != null && en.Skills.Count > 0)
                DetailSkillsList.ItemsSource = en.Skills.OrderBy(s => s.IsPassive).ToList();
            else
                DetailSkillsList.ItemsSource = new List<string> { "— No skills assigned —" };

            // Loot table — Enemy.Loottables → Lootentries → Item
            var lootVMs = enemy.Loottables
                .SelectMany(lt => lt.Lootentries)
                .OrderByDescending(le => le.DropChance)
                .Select(le => new LootEntryDisplayVM
                {
                    ItemName = le.Item?.Name ?? $"Item ID {le.ItemId}",
                    ItemThumb = le.Item?.ImageThumb ?? "",
                    MinQuality = le.MinQuality,
                    MaxQuality = le.MaxQuality,
                    DropChance = le.DropChance,
                })
                .ToList();

            DetailLootList.ItemsSource = (System.Collections.IEnumerable)(lootVMs.Count > 0
                ? (object)lootVMs
                : new List<string> { "— No loot entries —" });
        }

        private void ClearDetail()
        {
            _selected = null;
            DetailPanel.Visibility = Visibility.Collapsed;
            DetailEmpty.Visibility = Visibility.Visible;
        }

        // ── Delete ───────────────────────────────────────────────────
        private void DeleteEnemy_Click(object sender, RoutedEventArgs e)
        {
            if (_selected == null) return;

            var result = MessageBox.Show(
                $"Delete enemy «{_selected.Entity.Name}»?\n\nThis will also remove the Entity and LootTable.",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                // Load with all dependents using the CORRECT navigation names from the scaffold:
                //   Enemy.Loottables  (not LootTables)
                //   Loottable.Lootentries  (not LootEntries)
                var enemy = db.Enemies
                              .Include(e => e.Entity)
                                  .ThenInclude(en => en.Skills)
                              .Include(e => e.Loottables)          // ← Loottables (scaffold name)
                                  .ThenInclude(lt => lt.Lootentries) // ← Lootentries (scaffold name)
                              .FirstOrDefault(e => e.EntityId == _selected.EntityId);

                if (enemy != null)
                {
                    // Remove loot entries, then loot tables
                    foreach (var lt in enemy.Loottables)
                        db.Lootentries.RemoveRange(lt.Lootentries);
                    db.Loottables.RemoveRange(enemy.Loottables);

                    // Disconnect skills from the entity (removes rows in entityskill join table)
                    enemy.Entity.Skills.Clear();

                    db.Enemies.Remove(enemy);
                    db.Entities.Remove(enemy.Entity);
                    db.SaveChanges();
                }

                ClearDetail();
                LoadEnemies();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting enemy:\n{ex.Message}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Helpers ──────────────────────────────────────────────────

        /// <summary>
        /// Sets the inner bar width as a fraction of its parent container's ActualWidth.
        /// Called after Loaded so ActualWidth is available.
        /// </summary>
        private static void SetBarWidth(Border bar, double value, double maxRef)
        {
            double pct = Math.Clamp(value / maxRef, 0.0, 1.0);
            // Store pct in Tag; resolve real pixel width once the element is rendered
            bar.Tag = pct;
            bar.Loaded -= Bar_Loaded;
            bar.Loaded += Bar_Loaded;

            // If already rendered (detail shown more than once), update immediately
            if (bar.IsLoaded && bar.Parent is Border parent && parent.ActualWidth > 0)
                bar.Width = parent.ActualWidth * pct;
        }

        private static void Bar_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is Border bar &&
                bar.Tag is double pct &&
                bar.Parent is Border parent)
            {
                bar.Width = parent.ActualWidth * pct;
            }
        }

        private static BitmapImage? TryLoadBitmap(string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) return null;
            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(path, UriKind.Absolute);
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                return bmp;
            }
            catch { return null; }
        }
    }
}