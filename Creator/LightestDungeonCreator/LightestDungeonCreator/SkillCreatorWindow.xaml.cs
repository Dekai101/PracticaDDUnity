using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using LightestDungeonCreator.Models;
using Microsoft.Win32;

namespace LightestDungeonCreator
{
    // ─── ViewModels for the effect table rows ─────────────────────────────────

    /// <summary>Represents a STATUS effect row in the UI list.</summary>
    public class StatusEffectVM
    {
        public bool IsClean { get; set; }
        public int StatusId { get; set; }
        public string DisplayName { get; set; } = "";
        public int Turns { get; set; }
        public float Chance { get; set; }
        public string Target { get; set; } = "";
        public int EffectLevel { get; set; } = 1;

        // Computed display properties
        public string ChanceDisplay => $"{Chance:0}%";
    }

    /// <summary>Represents a STAT effect row in the UI list.</summary>
    public class StatEffectVM
    {
        public bool IsClean { get; set; }
        public int StatId { get; set; }
        public string DisplayName { get; set; } = "";
        public int Turns { get; set; }
        public float Chance { get; set; }
        public string Target { get; set; } = "";
        public float Modifier { get; set; }   // percent, e.g. -30 = -30%
        public int MinFlat { get; set; }
        public int MaxFlat { get; set; }

        // Computed display properties
        public string ChanceDisplay => $"{Chance:0}%";
        public string ModifierDisplay
        {
            get
            {
                if (Modifier != 0)
                    return $"{Modifier:+0;-0}%";
                if (MinFlat != 0 || MaxFlat != 0)
                    return $"{MinFlat}~{MaxFlat}";
                return "—";
            }
        }
    }

    // ─── Window ───────────────────────────────────────────────────────────────

    public partial class SkillCreatorWindow : Window
    {
        private readonly ObservableCollection<StatusEffectVM> _statusEffects = new();
        private readonly ObservableCollection<StatEffectVM> _statEffects = new();
        private string? _imageThumbPath;

        public SkillCreatorWindow()
        {
            InitializeComponent();
            StatusEffectsList.ItemsSource = _statusEffects;
            StatEffectsList.ItemsSource = _statEffects;
            LoadCombosFromDb();
        }

        // ── Window drag (since WindowStyle=None) ─────────────────────────────

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                this.DragMove();
        }

        // ── Navigation ───────────────────────────────────────────────────────

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            new SplashScreen().Show();
            this.Close();
        }

        private void ListSkills_Click(object sender, RoutedEventArgs e)
        {
            // TODO: open a SkillListWindow when it's created
            MessageBox.Show("Lista de habilidades — pendiente de implementar.",
                            "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ── Data loading ─────────────────────────────────────────────────────

        private void LoadCombosFromDb()
        {
            try
            {
                using var db = new AppDbContext();
                StatusCombo.ItemsSource = db.Statuses.OrderBy(s => s.Name).ToList();
                StatCombo.ItemsSource = db.Statistics.OrderBy(s => s.Name).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo conectar a la base de datos:\n{ex.Message}",
                                "DB Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // ── Image picker ─────────────────────────────────────────────────────

        private void BrowseImage_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Imágenes|*.png;*.jpg;*.jpeg;*.bmp;*.gif|Todos los archivos|*.*",
                Title = "Selecciona la imagen de la habilidad"
            };

            if (dlg.ShowDialog() == true)
            {
                _imageThumbPath = dlg.FileName;
                ImagePathInput.Text = dlg.SafeFileName;
                try
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.UriSource = new Uri(dlg.FileName);
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.EndInit();
                    SkillImagePreview.Source = bmp;
                }
                catch
                {
                    SkillImagePreview.Source = null;
                }
            }
        }

        // ── STATUS effects ───────────────────────────────────────────────────

        private void AddStatusEffect_Click(object sender, RoutedEventArgs e)
        {
            if (StatusCombo.SelectedItem is not Status selected)
            {
                MessageBox.Show("Selecciona un status primero.", "Atención",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!float.TryParse(StatusChanceInput.Text, out float chance)) chance = 100f;
            if (!int.TryParse(StatusTurnsInput.Text, out int turns)) turns = 3;

            var target = (StatusTargetCombo.SelectedItem as ComboBoxItem)
                             ?.Content?.ToString() ?? "1 Enemy";

            bool isCleanse = StatusCleanseRb.IsChecked == true;

            _statusEffects.Add(new StatusEffectVM
            {
                IsClean = isCleanse,
                StatusId = selected.Id,
                DisplayName = (isCleanse ? "⌫ " : "") + selected.Name,
                Turns = Math.Max(0, turns),
                Chance = Math.Clamp(chance, 0f, 100f),
                Target = target,
                EffectLevel = 1
            });
        }

        private void RemoveStatusEffect_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is StatusEffectVM vm)
                _statusEffects.Remove(vm);
        }

        // ── STAT effects ─────────────────────────────────────────────────────

        private void AddStatEffect_Click(object sender, RoutedEventArgs e)
        {
            if (StatCombo.SelectedItem is not Statistic selected)
            {
                MessageBox.Show("Selecciona un stat primero.", "Atención",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!float.TryParse(StatChanceInput.Text, out float chance)) chance = 100f;
            if (!int.TryParse(StatTurnsInput.Text, out int turns)) turns = 1;
            if (!float.TryParse(StatModifierInput.Text, out float modifier)) modifier = 0f;
            if (!int.TryParse(StatMinFlatInput.Text, out int minFlat)) minFlat = 0;
            if (!int.TryParse(StatMaxFlatInput.Text, out int maxFlat)) maxFlat = 0;

            var target = (StatTargetCombo.SelectedItem as ComboBoxItem)
                                ?.Content?.ToString() ?? "Self";
            bool isCleanse = StatCleanseRb.IsChecked == true;

            _statEffects.Add(new StatEffectVM
            {
                IsClean = isCleanse,
                StatId = selected.Id,
                DisplayName = (isCleanse ? "⌫ " : "") + selected.Name,
                Turns = Math.Max(0, turns),
                Chance = Math.Clamp(chance, 0f, 100f),
                Target = target,
                Modifier = modifier,
                MinFlat = minFlat,
                MaxFlat = maxFlat
            });
        }

        private void RemoveStatEffect_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is StatEffectVM vm)
                _statEffects.Remove(vm);
        }

        // ── Save ─────────────────────────────────────────────────────────────

        private void SaveSkill_Click(object sender, RoutedEventArgs e)
        {
            // ── Validation ──────────────────────────────────────────────────
            if (string.IsNullOrWhiteSpace(NameInput.Text))
            {
                MessageBox.Show("El nombre es obligatorio.", "Validación",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                NameInput.Focus();
                return;
            }

            if (!int.TryParse(HitsInput.Text, out int hits)) hits = 1;
            if (!int.TryParse(CostInput.Text, out int cost)) cost = 0;
            if (!float.TryParse(AccuracyInput.Text, out float accuracy)) accuracy = 100f;

            var targetType = (SkillTargetCombo.SelectedItem as ComboBoxItem)
                                 ?.Content?.ToString() ?? "Single";

            // ── Build model ─────────────────────────────────────────────────
            var skill = new Skill
            {
                Name = NameInput.Text.Trim(),
                Description = string.IsNullOrWhiteSpace(DescInput.Text)
                                  ? null
                                  : DescInput.Text.Trim(),
                EnergyCost = Math.Max(0, cost),
                Accuracy = accuracy / 100f,
                Hits = Math.Max(1, hits),
                TargetType = targetType,
                IsAoe = IsAoeCheck.IsChecked == true,
                IsPassive = IsPassiveCheck.IsChecked == true,
                ImageThumb = _imageThumbPath ?? ""
            };

            // Status effects → Effect entities
            // Convention: EffectLevel = 0 means CLEANSE, >= 1 means APPLY
            foreach (var se in _statusEffects)
            {
                skill.Effects.Add(new Effect
                {
                    StatusId = se.StatusId,
                    Probability = se.Chance / 100f,
                    DurationTurns = se.Turns,
                    EffectLevel = se.IsClean ? 0 : se.EffectLevel
                });
            }

            // Stat effects → Effect entities
            foreach (var se in _statEffects)
            {
                skill.Effects.Add(new Effect
                {
                    StatId = se.StatId,
                    Probability = se.Chance / 100f,
                    DurationTurns = se.Turns,
                    StatMultiplier = se.Modifier != 0 ? (float?)(se.Modifier / 100f) : null,
                    MinFlatPower = se.MinFlat != 0 ? (int?)se.MinFlat : null,
                    MaxFlatPower = se.MaxFlat != 0 ? (int?)se.MaxFlat : null,
                    EffectLevel = se.IsClean ? 0 : 1
                });
            }

            // ── Persist ─────────────────────────────────────────────────────
            try
            {
                using var db = new AppDbContext();
                db.Skills.Add(skill);
                db.SaveChanges();

                MessageBox.Show($"Habilidad '{skill.Name}' guardada con éxito.\n" +
                                $"Efectos: {skill.Effects.Count}",
                                "✦ Guardado", MessageBoxButton.OK, MessageBoxImage.Information);
                ResetForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar:\n{ex.Message}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Reset form ───────────────────────────────────────────────────────

        private void ResetForm()
        {
            NameInput.Text = "";
            DescInput.Text = "";
            ImagePathInput.Text = "";
            SkillImagePreview.Source = null;
            _imageThumbPath = null;

            _statusEffects.Clear();
            _statEffects.Clear();

            StatusChanceInput.Text = "100";
            StatusTurnsInput.Text = "3";
            StatChanceInput.Text = "100";
            StatTurnsInput.Text = "1";
            StatModifierInput.Text = "0";
            StatMinFlatInput.Text = "0";
            StatMaxFlatInput.Text = "0";

            HitsInput.Text = "1";
            CostInput.Text = "0";
            AccuracyInput.Text = "100";

            IsAoeCheck.IsChecked = false;
            IsPassiveCheck.IsChecked = false;

            StatusApplyRb.IsChecked = true;
            StatApplyRb.IsChecked = true;

            StatusCombo.SelectedIndex = -1;
            StatCombo.SelectedIndex = -1;
        }
    }
}