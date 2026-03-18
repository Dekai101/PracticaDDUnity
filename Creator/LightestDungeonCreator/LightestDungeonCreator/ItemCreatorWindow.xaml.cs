using LightestDungeonCreator.Models;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace LightestDungeonCreator
{
    // We grab StatusEffectVM and StatEffectVM from SkillCreatorWindow

    // -- Window -----------------------------------------------------------------------------------

    public partial class ItemCreatorWindow : Window
    {
        //We create each effect type collection to store them separately
        private readonly ObservableCollection<StatusEffect> _statusEffects = new ObservableCollection<StatusEffect>();
        private readonly ObservableCollection<StatEffect> _statEffects = new ObservableCollection<StatEffect>();
        private string? _imageThumbPath;

        public ItemCreatorWindow()
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

        private void ListItems_Click(object sender, RoutedEventArgs e)
        {
            // TODO: open an ItemListWindow when it's created
            MessageBox.Show("Lista de items — pendiente de implementar.",
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
                Title = "Selecciona la imagen del item"
            };

            if (dlg.ShowDialog() == true)
                ImagePathInput.Text = dlg.FileName; // TextChanged handles the rest
        }

        private void ImagePathInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            string path = ImagePathInput.Text.Trim();
            if (string.IsNullOrWhiteSpace(path))
            {
                ItemImagePreview.Source = null;
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
                ItemImagePreview.Source = bmp;
                _imageThumbPath = path;
            }
            catch
            {
                ItemImagePreview.Source = null;
                _imageThumbPath = null;
            }
        }

        // -- Item properties ------------------------------------------------------------

        // Shows/hides MaxUses field based on Consumable checkbox
        private void ConsumableCheck_Changed(object sender, RoutedEventArgs e)
        {
            if (ConsumableCheck.IsChecked == true)
            {
                MaxUsesInput.IsEnabled = true;
            }
            else
            {
                MaxUsesInput.IsEnabled = false;
                MaxUsesInput.Text = "";
            }
        }


        // -- STATUS effects ------------------------------------------------------------

        // Populates StatusLevelCombo with 1..Status.MaxLevel when the selected status changes
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

            _statusEffects.Add(new StatusEffect
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
            if (sender is Button btn && btn.Tag is StatusEffect vm)
                _statusEffects.Remove(vm);
        }

        // -- STAT effects ------------------------------------------------------------

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

            _statEffects.Add(new StatEffect
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
            if (sender is Button btn && btn.Tag is StatEffect vm)
                _statEffects.Remove(vm);
        }

        // -- Save ------------------------------------------------------------

        private void SaveItem_Click(object sender, RoutedEventArgs e)
        {
            // -- Validation --
            if (string.IsNullOrWhiteSpace(NameInput.Text))
            {
                MessageBox.Show("El nombre es obligatorio.", "Validación",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                NameInput.Focus();
                return;
            }

            var quality = (QualityCombo.SelectedItem as ComboBoxItem)
                              ?.Content?.ToString() ?? "Common";

            bool consumable = ConsumableCheck.IsChecked == true;
            int? maxUses = null;
            if (consumable && int.TryParse(MaxUsesInput.Text, out int parsedUses) && parsedUses > 0)
                maxUses = parsedUses;

            // -- Build model --
            var item = new Item
            {
                Name = NameInput.Text.Trim(),
                Description = string.IsNullOrWhiteSpace(DescInput.Text) ? null : DescInput.Text.Trim(),
                Quality = quality.ToUpper(),
                Consumable = consumable,
                MaxUses = maxUses,
                IsAoe = IsAoeCheck.IsChecked == true,
                TargetType = ((ItemTargetCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Enemy").ToUpper(),
                ImageThumb = _imageThumbPath ?? ""
            };

            // Status effects → Effect entities
            // EffectLevel = 0 means CLEANSE, >= 1 means APPLY
            foreach (var se in _statusEffects)
            {
                item.Effects.Add(new Effect
                {
                    StatusId = se.StatusId,
                    Probability = se.Chance,
                    DurationTurns = se.Turns,
                    EffectLevel = se.IsClean ? 0 : se.EffectLevel
                });
            }

            // Stat effects → Effect entities
            foreach (var se in _statEffects)
            {
                item.Effects.Add(new Effect
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

            // -- Persist --
            try
            {
                using var db = new AppDbContext();
                db.Items.Add(item);
                db.SaveChanges();

                MessageBox.Show($"Item '{item.Name}' guardado con éxito.\n" +
                                $"Efectos: {item.Effects.Count}",
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
            ItemImagePreview.Source = null;
            _imageThumbPath = null;

            _statusEffects.Clear();
            _statEffects.Clear();

            QualityCombo.SelectedIndex = 0;
            ConsumableCheck.IsChecked = false;
            IsAoeCheck.IsChecked = false;
            ItemTargetCombo.SelectedIndex = 0;
            MaxUsesInput.Text = "";
            MaxUsesPanel.Visibility = Visibility.Collapsed;

            StatusChanceInput.Text = "100";
            StatusTurnsInput.Text = "3";
            StatChanceInput.Text = "100";
            StatTurnsInput.Text = "1";
            StatModifierInput.Text = "0";
            StatMinFlatInput.Text = "0";
            StatMaxFlatInput.Text = "0";

            StatusApplyRb.IsChecked = true;
            StatApplyRb.IsChecked = true;

            StatusCombo.SelectedIndex = -1;
            StatCombo.SelectedIndex = -1;
            StatusLevelCombo.Items.Clear();
        }

        
    }
}