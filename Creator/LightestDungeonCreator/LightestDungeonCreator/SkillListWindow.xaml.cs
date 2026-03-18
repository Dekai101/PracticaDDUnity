using LightestDungeonCreator.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LightestDungeonCreator
{
    // ── ViewModel para cada efecto en el panel de detalle ────────────
    public class EffectDisplayVM
    {
        public string EffectName { get; set; } = "";
        public string Turns { get; set; } = "";
        public string Chance { get; set; } = "";
        public string Level { get; set; } = "";
        public string Description { get; set; } = "";
        public string FlatRange { get; set; } = "";  // e.g. "5 – 12"
        public string Multiplier { get; set; } = "";  // e.g. "1.5"

        public Visibility LevelVisibility { get; set; } = Visibility.Collapsed;
        public Visibility CleanseVisibility { get; set; } = Visibility.Collapsed;
        public Visibility DescriptionVisibility { get; set; } = Visibility.Collapsed;
        public Visibility FlatVisibility { get; set; } = Visibility.Collapsed;
        public Visibility MultiplierVisibility { get; set; } = Visibility.Collapsed;
        // Fila entera de power — visible si al menos uno de los dos tiene valor
        public Visibility PowerRowVisibility { get; set; } = Visibility.Collapsed;
    }

    public partial class SkillListWindow : Window
    {
        // ── State ────────────────────────────────────────────────────
        private List<Skill> _allSkills = new();
        private List<Skill> _filtered = new();
        private Skill? _selected;

        // ── Constructor ──────────────────────────────────────────────
        public SkillListWindow()
        {
            InitializeComponent();
            LoadSkills();
        }

        // ── Window drag ──────────────────────────────────────────────
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
            => DragMove();

        // ── Navigation ───────────────────────────────────────────────
        private void BackButton_Click(object sender, RoutedEventArgs e)
            => Close();

        // ── Data ─────────────────────────────────────────────────────
        private void LoadSkills()
        {
            try
            {
                using var db = new AppDbContext();
                // Include Effects y sus Stat/Status para poder mostrar nombres
                _allSkills = db.Skills
                               .Include(s => s.Effects)
                                   .ThenInclude(e => e.Stat)
                               .Include(s => s.Effects)
                                   .ThenInclude(e => e.Status)
                               .OrderBy(s => s.Name)
                               .ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading skills:\n{ex.Message}",
                                "DB Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                _allSkills = new List<Skill>();
            }

            ApplyFilters();
        }

        // ── Filters ──────────────────────────────────────────────────
        private void FilterName_Changed(object sender, TextChangedEventArgs e)
            => ApplyFilters();

        private void Filter_CheckChanged(object sender, RoutedEventArgs e)
            => ApplyFilters();

        private void ApplyFilters()
        {
            if (_allSkills == null || SkillList == null) return;

            string name = FilterNameInput?.Text?.ToLower() ?? "";
            bool onlyAlly = FilterAlly?.IsChecked == true;
            bool onlyEnemy = FilterEnemy?.IsChecked == true;
            bool onlySelf = FilterSelf?.IsChecked == true;
            bool onlyAoe = FilterAoe?.IsChecked == true;
            bool onlyPassive = FilterPassive?.IsChecked == true;

            _filtered = _allSkills.Where(s =>
            {
                if (!string.IsNullOrEmpty(name) && !s.Name.ToLower().Contains(name))
                    return false;
                // TARGET filters — any checked must match TargetType
                if (onlyAlly && s.TargetType != "ALLY") return false;
                if (onlyEnemy && s.TargetType != "ENEMY") return false;
                if (onlySelf && s.TargetType != "SELF") return false;
                // TYPE filters
                if (onlyAoe && !s.IsAoe) return false;
                if (onlyPassive && !s.IsPassive) return false;
                return true;
            }).ToList();

            SkillList.ItemsSource = _filtered;
            SkillCountLabel.Text = $"{_filtered.Count} skill{(_filtered.Count != 1 ? "s" : "")}";
            NoSkillsLabel.Visibility = _filtered.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            FooterLabel.Text = $"{_allSkills.Count} total  ·  {_filtered.Count} shown";

            if (_selected != null && !_filtered.Contains(_selected))
                ClearDetail();
        }

        // ── List item click ──────────────────────────────────────────
        private void SkillItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border b && b.Tag is Skill skill)
                ShowDetail(skill);
        }

        // ── Detail panel ─────────────────────────────────────────────
        private void ShowDetail(Skill skill)
        {
            _selected = skill;

            DetailEmpty.Visibility = Visibility.Collapsed;
            DetailPanel.Visibility = Visibility.Visible;

            DetailImage.Source = TryLoadBitmap(skill.ImageThumb);
            DetailName.Text = skill.Name.ToUpper();
            DetailId.Text = $"ID: {skill.Id}";

            // Badges
            DetailBadges.Children.Clear();
            if (skill.IsPassive)
                DetailBadges.Children.Add(MakeBadge("PASSIVE", "#8B1A1A", "#1A0808", "#5C1010"));
            if (skill.IsAoe)
                DetailBadges.Children.Add(MakeBadge("AoE", "#3A8B3A", "#0A1A0A", "#1A4A1A"));
            DetailBadges.Children.Add(MakeBadge(skill.TargetType, "#7A6A5A", "#1A1208", "#3A2A18"));

            DetailDescription.Text = string.IsNullOrWhiteSpace(skill.Description)
                ? "— No description —" : skill.Description;

            DetailHits.Text = skill.Hits.ToString();
            DetailCost.Text = skill.EnergyCost.ToString();
            DetailAccuracy.Text = $"{skill.Accuracy:P0}";
            DetailTarget.Text = skill.TargetType;

            // ── Efectos: construir EffectDisplayVM por cada Effect ───
            var vms = new List<EffectDisplayVM>();

            foreach (var eff in skill.Effects)
            {
                bool isCleanse = eff.EffectLevel == 0;

                // Nombre: prioridad Stat, luego Status, luego fallback
                string name;
                string description = "";

                if (eff.Stat != null)
                {
                    name = eff.Stat.Name;
                    // Los Stats no tienen descripción en el modelo, dejamos vacío
                }
                else if (eff.Status != null)
                {
                    name = eff.Status.Name;
                    description = eff.Status.Description ?? "";
                }
                else
                {
                    // Fallback si las navegaciones no están cargadas
                    name = eff.StatId.HasValue
                        ? $"Stat ID {eff.StatId}"
                        : eff.StatusId.HasValue
                            ? $"Status ID {eff.StatusId}"
                            : "Unknown effect";
                }

                // Nivel: mostrar solo si hay un nivel significativo (> 0 y existe)
                string levelStr = "";
                bool showLevel = false;
                if (eff.EffectLevel.HasValue && eff.EffectLevel.Value > 0)
                {
                    levelStr = eff.EffectLevel.Value.ToString();
                    showLevel = true;
                }

                // Flat damage: mostrar solo si min o max son != 0 y != null
                bool showFlat = (eff.MinFlatPower.HasValue && eff.MinFlatPower.Value != 0)
                                || (eff.MaxFlatPower.HasValue && eff.MaxFlatPower.Value != 0);
                string flatRange = showFlat
                    ? $"{eff.MinFlatPower ?? 0} – {eff.MaxFlatPower ?? 0}"
                    : "";

                // Stat multiplier: mostrar solo si != 0 y != null
                bool showMult = eff.StatMultiplier.HasValue && eff.StatMultiplier.Value != 0f;
                string multStr = showMult
                    ? eff.StatMultiplier!.Value.ToString("+0.##;-0.##")
                    : "";

                vms.Add(new EffectDisplayVM
                {
                    EffectName = (isCleanse ? "⌫ " : "") + name,
                    Turns = eff.DurationTurns.ToString(),
                    Chance = $"{eff.Probability:P0}",
                    Level = levelStr,
                    Description = description,
                    FlatRange = flatRange,
                    Multiplier = multStr,
                    LevelVisibility = showLevel ? Visibility.Visible : Visibility.Collapsed,
                    CleanseVisibility = isCleanse ? Visibility.Visible : Visibility.Collapsed,
                    DescriptionVisibility = !string.IsNullOrWhiteSpace(description)
                                            ? Visibility.Visible : Visibility.Collapsed,
                    FlatVisibility = showFlat ? Visibility.Visible : Visibility.Collapsed,
                    MultiplierVisibility = showMult ? Visibility.Visible : Visibility.Collapsed,
                    PowerRowVisibility = (showFlat || showMult) ? Visibility.Visible : Visibility.Collapsed,
                });
            }

            if (vms.Count == 0)
                vms.Add(new EffectDisplayVM
                {
                    EffectName = "— No effects —",
                    Turns = "",
                    Chance = "",
                    LevelVisibility = Visibility.Collapsed,
                    CleanseVisibility = Visibility.Collapsed,
                    DescriptionVisibility = Visibility.Collapsed,
                    FlatVisibility = Visibility.Collapsed,
                    MultiplierVisibility = Visibility.Collapsed,
                    PowerRowVisibility = Visibility.Collapsed,
                });

            DetailEffectsList.ItemsSource = vms;
        }

        private void ClearDetail()
        {
            _selected = null;
            DetailPanel.Visibility = Visibility.Collapsed;
            DetailEmpty.Visibility = Visibility.Visible;
        }

        // ── Delete ───────────────────────────────────────────────────
        private void DeleteSkill_Click(object sender, RoutedEventArgs e)
        {
            if (_selected == null) return;

            var result = MessageBox.Show(
                $"Delete skill «{_selected.Name}»?\n\nThis will remove all associated effects.",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                using var db = new AppDbContext();
                var skill = db.Skills
                              .Include(s => s.Effects)
                              .FirstOrDefault(s => s.Id == _selected.Id);

                if (skill != null)
                {
                    skill.Effects.Clear();
                    db.Skills.Remove(skill);
                    db.SaveChanges();
                }

                ClearDetail();
                LoadSkills();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting skill:\n{ex.Message}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Helpers ──────────────────────────────────────────────────
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

        private static Border MakeBadge(string text, string fg, string bg, string border)
        {
            return new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(bg)),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(border)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(2),
                Padding = new Thickness(5, 1, 5, 1),
                Margin = new Thickness(0, 0, 4, 0),
                Child = new TextBlock
                {
                    Text = text,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(fg)),
                    FontFamily = new FontFamily("Georgia"),
                    FontSize = 9
                }
            };
        }
    }
}