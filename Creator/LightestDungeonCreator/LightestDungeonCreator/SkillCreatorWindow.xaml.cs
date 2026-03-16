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
    //Auxiliary classes to represent effects that affect status and statistic modifications, with display-friendly properties and convenience for managing each type of effect separately.
    public class StatusEffectVM
    {
        public bool IsClean { get; set; }
        public int StatusId { get; set; }
        public string DisplayName { get; set; } = "";
        public int Turns { get; set; }
        public float Chance { get; set; }   // stored as 0-1 (0.7 = 70%)
        public int EffectLevel { get; set; } = 1;

        // Display properties
        public string ChanceDisplay => $"{Chance * 100:0}%";
    }

    public class StatEffectVM
    {
        public bool IsClean { get; set; }
        public int StatId { get; set; }
        public string DisplayName { get; set; } = "";
        public int Turns { get; set; }
        public float Chance { get; set; }       // stored as 0-1 (0.7 = 70%)
        public float Multiplier { get; set; }   // stored as 0-1 (-0.3 = -30%)
        public int MinFlat { get; set; }
        public int MaxFlat { get; set; }

        // Display properties
        public string ChanceDisplay => $"{Chance * 100:0}%";
        public string ModifierDisplay
        {
            get
            {
                if (Multiplier != 0)
                    return $"{Multiplier * 100:+0;-0}%";
                if (MinFlat != 0 || MaxFlat != 0)
                    return $"{MinFlat}~{MaxFlat}";
                return "—";
            }
        }
    }

    // -- Window -----------------------------------------------------------------------------------

    public partial class SkillCreatorWindow : Window
    {
        //We create each effect type collection to store them separately, up to a max of 3 each
        private readonly ObservableCollection<StatusEffectVM> _statusEffects = new ObservableCollection<StatusEffectVM>();
        private readonly ObservableCollection<StatEffectVM> _statEffects = new ObservableCollection<StatEffectVM>();
        private string? _imageThumbPath;

        public SkillCreatorWindow()
        {
            InitializeComponent();
            StatusEffectsList.ItemsSource = _statusEffects;
            StatEffectsList.ItemsSource = _statEffects;
            StatusCombo.SelectionChanged += StatusCombo_SelectionChanged;
            LoadCombosFromDb();
        }

        // -- Window drag --

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                this.DragMove();
        }

        // -- Navigation ------------------------------------------------------------

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

        // -- Data loading ------------------------------------------------------------

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

        // -- Image picker ------------------------------------------------------------

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

        // -- STATUS effects ------------------------------------------------------------

        private void StatusCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            StatusLevelCombo.Items.Clear();

            if (StatusCombo.SelectedItem is not Status selected)
                return;

            int max = Math.Max(1, selected.MaxLevel);
            for (int i = 1; i <= max; i++)
                StatusLevelCombo.Items.Add(i);

            StatusLevelCombo.SelectedIndex = 0;
        }

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

            int level = StatusLevelCombo.SelectedItem is int lvl ? lvl : 1;
            bool isCleanse = StatusCleanseRb.IsChecked == true;

            _statusEffects.Add(new StatusEffectVM
            {
                IsClean = isCleanse,
                StatusId = selected.Id,
                DisplayName = (isCleanse ? "⌫ " : "") + selected.Name,
                Turns = Math.Max(0, turns),
                Chance = Math.Clamp(chance, 0f, 100f) / 100f,
                EffectLevel = isCleanse ? 0 : level
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

            bool isCleanse = StatCleanseRb.IsChecked == true;

            _statEffects.Add(new StatEffectVM
            {
                IsClean = isCleanse,
                StatId = selected.Id,
                DisplayName = (isCleanse ? "⌫ " : "") + selected.Name,
                Turns = Math.Max(0, turns),
                Chance = Math.Clamp(chance, 0f, 100f) / 100f,
                Multiplier = modifier / 100f,
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

            // -- Build model ------------------------------------------------------------
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
            // EffectLevel = 0 means CLEANSE, >= 1 means APPLY
            foreach (var se in _statusEffects)
            {
                skill.Effects.Add(new Effect
                {
                    StatusId = se.StatusId,
                    Probability = se.Chance,
                    DurationTurns = se.Turns,
                    EffectLevel = se.IsClean ? 0 : se.EffectLevel
                });
            }

            // Stat effects -> Effect entities
            foreach (var se in _statEffects)
            {
                skill.Effects.Add(new Effect
                {
                    StatId = se.StatId,
                    Probability = se.Chance,
                    DurationTurns = se.Turns,
                    StatMultiplier = se.Multiplier != 0 ? (float?)se.Multiplier : null,
                    MinFlatPower = se.MinFlat != 0 ? (int?)se.MinFlat : null,
                    MaxFlatPower = se.MaxFlat != 0 ? (int?)se.MaxFlat : null,
                    EffectLevel = se.IsClean ? 0 : 1
                });
            }

            // -- Persist -----------------------------------------------------------
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

        // -- Reset form ------------------------------------------------------------

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
            StatusLevelCombo.Items.Clear();
        }

        private void ImagePathInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            string path = ImagePathInput.Text.Trim();
            if (string.IsNullOrWhiteSpace(path))
            {
                SkillImagePreview.Source = null;
                _imageThumbPath = null;
                return;
            }

            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(path, UriKind.Absolute);
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                SkillImagePreview.Source = bmp;
                _imageThumbPath = path;
            }
            catch
            {
                SkillImagePreview.Source = null;
                _imageThumbPath = null;
            }
        }
    }
}