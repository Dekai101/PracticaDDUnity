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
    public partial class ItemListWindow : Window
    {
        AppDbContext db;

        // ── State ────────────────────────────────────────────────────
        private List<Item> _allItems = new();
        private List<Item> _filtered = new();
        private Item?      _selected;

        // ── Constructor ──────────────────────────────────────────────
        public ItemListWindow()
        {
            db = new AppDbContext();

            InitializeComponent();
            LoadItems();
        }

        // ── Window drag ──────────────────────────────────────────────
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
            => DragMove();

        // ── Navigation ───────────────────────────────────────────────
        private void BackButton_Click(object sender, RoutedEventArgs e)
            => Close();

        // ── Data ─────────────────────────────────────────────────────
        private void LoadItems()
        {
            try
            {
                _allItems = db.Items
                              .Include(i => i.Effects)
                                  .ThenInclude(e => e.Stat)
                              .Include(i => i.Effects)
                                  .ThenInclude(e => e.Status)
                              .OrderBy(i => i.Name)
                              .ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading items:\n{ex.Message}",
                                "DB Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                _allItems = new List<Item>();
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
            if (_allItems == null || ItemList == null) return;

            string name           = FilterNameInput?.Text?.ToLower() ?? "";
            bool   onlyAlly       = FilterAlly?.IsChecked       == true;
            bool   onlyEnemy      = FilterEnemy?.IsChecked      == true;
            bool   onlySelf       = FilterSelf?.IsChecked       == true;
            bool   onlyAoe        = FilterAoe?.IsChecked        == true;
            bool   onlyConsumable = FilterConsumable?.IsChecked == true;
            bool   onlyCommon     = FilterCommon?.IsChecked     == true;
            bool   onlyUncommon   = FilterUncommon?.IsChecked   == true;
            bool   onlyRare       = FilterRare?.IsChecked       == true;
            bool   onlyEpic       = FilterEpic?.IsChecked       == true;
            bool   onlyLegendary  = FilterLegendary?.IsChecked  == true;

            // Quality filter: if none checked → show all
            bool anyQuality = onlyCommon || onlyUncommon || onlyRare || onlyEpic || onlyLegendary;

            _filtered = _allItems.Where(i =>
            {
                if (!string.IsNullOrEmpty(name) && !i.Name.ToLower().Contains(name))
                    return false;
                // TARGET
                if (onlyAlly  && !i.TargetType.Equals("ALLY",  StringComparison.OrdinalIgnoreCase)) return false;
                if (onlyEnemy && !i.TargetType.Equals("ENEMY", StringComparison.OrdinalIgnoreCase)) return false;
                if (onlySelf  && !i.TargetType.Equals("SELF",  StringComparison.OrdinalIgnoreCase)) return false;
                // TYPE
                if (onlyAoe        && !i.IsAoe)       return false;
                if (onlyConsumable && !i.Consumable)  return false;
                // QUALITY (OR logic: any selected quality matches)
                if (anyQuality)
                {
                    bool match =
                        (onlyCommon    && i.Quality.Equals("COMMON",    StringComparison.OrdinalIgnoreCase)) ||
                        (onlyUncommon  && i.Quality.Equals("UNCOMMON",  StringComparison.OrdinalIgnoreCase)) ||
                        (onlyRare      && i.Quality.Equals("RARE",      StringComparison.OrdinalIgnoreCase)) ||
                        (onlyEpic      && i.Quality.Equals("EPIC",      StringComparison.OrdinalIgnoreCase)) ||
                        (onlyLegendary && i.Quality.Equals("LEGENDARY", StringComparison.OrdinalIgnoreCase));
                    if (!match) return false;
                }
                return true;
            }).ToList();

            ItemList.ItemsSource   = _filtered;
            ItemCountLabel.Text    = $"{_filtered.Count} item{(_filtered.Count != 1 ? "s" : "")}";
            NoItemsLabel.Visibility = _filtered.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            FooterLabel.Text       = $"{_allItems.Count} total  ·  {_filtered.Count} shown";

            if (_selected != null && !_filtered.Contains(_selected))
                ClearDetail();
        }

        // ── List item click ──────────────────────────────────────────
        private void ItemRow_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border b && b.Tag is Item item)
                ShowDetail(item);
        }

        // ── Detail panel ─────────────────────────────────────────────
        private void ShowDetail(Item item)
        {
            _selected = item;

            DetailEmpty.Visibility = Visibility.Collapsed;
            DetailPanel.Visibility = Visibility.Visible;

            DetailImage.Source = TryLoadBitmap(item.ImageThumb);
            DetailName.Text    = item.Name.ToUpper();
            DetailId.Text      = $"ID: {item.Id}";

            // Badges
            DetailBadges.Children.Clear();
            DetailBadges.Children.Add(MakeBadge(item.Quality, QualityColor(item.Quality), QualityBg(item.Quality), QualityBorder(item.Quality)));
            DetailBadges.Children.Add(MakeBadge(item.TargetType, "#7A6A5A", "#1A1208", "#3A2A18"));
            if (item.IsAoe)
                DetailBadges.Children.Add(MakeBadge("AoE", "#3A8B3A", "#0A1A0A", "#1A4A1A"));
            if (item.Consumable)
                DetailBadges.Children.Add(MakeBadge("CONSUMABLE", "#8B8B1A", "#1A1A08", "#4A4A1E"));

            DetailDescription.Text = string.IsNullOrWhiteSpace(item.Description)
                ? "— No description —" : item.Description;

            DetailQuality.Text = item.Quality;
            DetailTarget.Text  = item.TargetType;

            if (item.MaxUses.HasValue)
            {
                DetailMaxUses.Text           = item.MaxUses.Value.ToString();
                DetailMaxUsesChip.Visibility = Visibility.Visible;
            }
            else
            {
                DetailMaxUsesChip.Visibility = Visibility.Collapsed;
            }

            // ── Efectos ──────────────────────────────────────────────
            var vms = new List<EffectDisplayVM>();

            foreach (var eff in item.Effects)
            {
                bool isCleanse = eff.EffectLevel == 0;

                string name;
                string description = "";

                if (eff.Stat != null)
                {
                    name = eff.Stat.Name;
                }
                else if (eff.Status != null)
                {
                    name        = eff.Status.Name;
                    description = eff.Status.Description ?? "";
                }
                else
                {
                    name = eff.StatId.HasValue
                        ? $"Stat ID {eff.StatId}"
                        : eff.StatusId.HasValue
                            ? $"Status ID {eff.StatusId}"
                            : "Unknown effect";
                }

                // Nivel
                string levelStr  = "";
                bool   showLevel = false;
                if (eff.EffectLevel.HasValue && eff.EffectLevel.Value > 0)
                {
                    levelStr  = eff.EffectLevel.Value.ToString();
                    showLevel = true;
                }

                // Flat damage
                bool   showFlat  = (eff.MinFlatPower.HasValue && eff.MinFlatPower.Value != 0)
                                || (eff.MaxFlatPower.HasValue && eff.MaxFlatPower.Value != 0);
                string flatRange = showFlat
                    ? $"{eff.MinFlatPower ?? 0} – {eff.MaxFlatPower ?? 0}"
                    : "";

                // Stat multiplier
                bool   showMult = eff.StatMultiplier.HasValue && eff.StatMultiplier.Value != 0f;
                string multStr  = showMult
                    ? eff.StatMultiplier!.Value.ToString("+0.##;-0.##")
                    : "";

                vms.Add(new EffectDisplayVM
                {
                    EffectName            = (isCleanse ? "⌫ " : "") + name,
                    Turns                 = eff.DurationTurns.ToString(),
                    Chance                = $"{eff.Probability:P0}",
                    Level                 = levelStr,
                    Description           = description,
                    FlatRange             = flatRange,
                    Multiplier            = multStr,
                    LevelVisibility       = showLevel ? Visibility.Visible : Visibility.Collapsed,
                    CleanseVisibility     = isCleanse ? Visibility.Visible : Visibility.Collapsed,
                    DescriptionVisibility = !string.IsNullOrWhiteSpace(description)
                                            ? Visibility.Visible : Visibility.Collapsed,
                    FlatVisibility        = showFlat ? Visibility.Visible : Visibility.Collapsed,
                    MultiplierVisibility  = showMult ? Visibility.Visible : Visibility.Collapsed,
                    PowerRowVisibility    = (showFlat || showMult) ? Visibility.Visible : Visibility.Collapsed,
                });
            }

            if (vms.Count == 0)
                vms.Add(new EffectDisplayVM
                {
                    EffectName            = "— No effects —",
                    Turns                 = "", Chance = "",
                    LevelVisibility       = Visibility.Collapsed,
                    CleanseVisibility     = Visibility.Collapsed,
                    DescriptionVisibility = Visibility.Collapsed,
                    FlatVisibility        = Visibility.Collapsed,
                    MultiplierVisibility  = Visibility.Collapsed,
                    PowerRowVisibility    = Visibility.Collapsed,
                });

            DetailEffectsList.ItemsSource = vms;
        }

        private void ClearDetail()
        {
            _selected              = null;
            DetailPanel.Visibility = Visibility.Collapsed;
            DetailEmpty.Visibility = Visibility.Visible;
        }

        // ── Delete ───────────────────────────────────────────────────
        private void DeleteItem_Click(object sender, RoutedEventArgs e)
        {
            if (_selected == null) return;

            var result = MessageBox.Show(
                $"Delete item «{_selected.Name}»?\n\nThis will remove all associated effects.",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                var item = db.Items
                             .Include(i => i.Effects)
                             .FirstOrDefault(i => i.Id == _selected.Id);

                if (item != null)
                {
                    // We must clear the lootentries and effects associations before deleting the item
                    item.Effects.Clear();
                    db.Lootentries.RemoveRange(db.Lootentries.Where(le => le.ItemId == item.Id));

                    // Remove the item itself
                    db.Items.Remove(item);
                    db.SaveChanges();
                }

                ClearDetail();
                LoadItems();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting item:\n{ex.Message}",
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
                bmp.UriSource   = new Uri(path, UriKind.Absolute);
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
                Background      = new SolidColorBrush((Color)ColorConverter.ConvertFromString(bg)),
                BorderBrush     = new SolidColorBrush((Color)ColorConverter.ConvertFromString(border)),
                BorderThickness = new Thickness(1),
                CornerRadius    = new CornerRadius(2),
                Padding         = new Thickness(5, 1, 5, 1),
                Margin          = new Thickness(0, 0, 4, 0),
                Child           = new TextBlock
                {
                    Text       = text,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(fg)),
                    FontFamily = new FontFamily("Georgia"),
                    FontSize   = 9
                }
            };
        }

        // Quality badge colors
        private static string QualityColor(string q) => q.ToUpper() switch
        {
            "LEGENDARY" => "#C8922A",
            "EPIC"      => "#9B59B6",
            "RARE"      => "#3498DB",
            "UNCOMMON"  => "#27AE60",
            _           => "#7A6A5A",   // COMMON
        };
        private static string QualityBg(string q) => q.ToUpper() switch
        {
            "LEGENDARY" => "#1A1208",
            "EPIC"      => "#160A1A",
            "RARE"      => "#0A1018",
            "UNCOMMON"  => "#0A1A0E",
            _           => "#1A1208",
        };
        private static string QualityBorder(string q) => q.ToUpper() switch
        {
            "LEGENDARY" => "#8A6015",
            "EPIC"      => "#5C2A7A",
            "RARE"      => "#1A4A8B",
            "UNCOMMON"  => "#1A5C2A",
            _           => "#3A2A18",
        };
    }
}
