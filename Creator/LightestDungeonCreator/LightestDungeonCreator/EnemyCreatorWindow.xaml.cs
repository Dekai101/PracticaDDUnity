using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using LightestDungeonCreator.Models;
using Microsoft.Win32;

namespace LightestDungeonCreator
{
    public partial class EnemyCreatorWindow : Window
    {
        // ── Collections ──────────────────────────────────────────────
        private ObservableCollection<Skill> _assignedSkills = new();
        private ObservableCollection<Skill> _availableSkills = new();
        private List<Skill> _allSkills = new();

        public EnemyCreatorWindow()
        {
            InitializeComponent();
            HpSlider.ValueChanged += HpSlider_ValueChanged;
            EnergySlider.ValueChanged += EnergySlider_ValueChanged;
            AttackSlider.ValueChanged += AttackSlider_ValueChanged;
            LoadAvailableSkills();
            AssignedSkillsList.ItemsSource = _assignedSkills;
            AvailableSkillsList.ItemsSource = _availableSkills;
            UpdateNoSkillsPlaceholder();
        }

        private void EnergySlider_ValueChanged1(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            throw new NotImplementedException();
        }

        // ── Window drag ───────────────────────────────────────────────
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
            => DragMove();

        // ── Title-bar buttons ─────────────────────────────────────────
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            new SplashScreen().Show();
            Close();
        }

        private void ListEnemies_Click(object sender, RoutedEventArgs e)
        {
            // TODO: open enemy list / browser window
            MessageBox.Show("Llista d'enemics (pendent d'implementar).",
                            "Enemics", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ── Image browsing ────────────────────────────────────────────
        private void BrowseThumb_Click(object sender, RoutedEventArgs e)
        {
            var path = PickImage();
            if (path == null) return;
            ThumbPathInput.Text = path;
            ThumbPreview.Source = LoadBitmap(path);
        }

        private void BrowseFull_Click(object sender, RoutedEventArgs e)
        {
            var path = PickImage();
            if (path == null) return;
            FullPathInput.Text = path;
            FullPreview.Source = LoadBitmap(path);
        }

        private static string? PickImage()
        {
            var dlg = new OpenFileDialog
            {
                Title = "Selecciona imatge",
                Filter = "Imatges|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.webp|Tots els arxius|*.*"
            };
            return dlg.ShowDialog() == true ? dlg.FileName : null;
        }

        private static BitmapImage? LoadBitmap(string path)
        {
            if (!File.Exists(path)) return null;
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = new Uri(path, UriKind.Absolute);
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.EndInit();
            return bmp;
        }

        // ── Stat sliders ──────────────────────────────────────────────
        private void HpSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (HpValue == null) return;
            HpValue.Text = ((int)e.NewValue).ToString();
        }

        private void EnergySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (EnergyValue == null) return;
            EnergyValue.Text = ((int)e.NewValue).ToString();
        }

        private void AttackSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (AttackValue == null) return;
            AttackValue.Text = ((int)e.NewValue).ToString();
        }

        // ── Skills: assigned list ─────────────────────────────────────
        private void RemoveSkill_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Skill skill)
            {
                _assignedSkills.Remove(skill);
                UpdateNoSkillsPlaceholder();
            }
        }

        // ── Skills: available list ────────────────────────────────────
        private void AddSelectedSkill_Click(object sender, RoutedEventArgs e)
        {
            if (AvailableSkillsList.SelectedItem is not Skill skill) return;
            if (_assignedSkills.Any(s => s.Name == skill.Name)) return; // no duplicates
            _assignedSkills.Add(skill);
            UpdateNoSkillsPlaceholder();
        }

        // ── Filters ───────────────────────────────────────────────────
        private void Filter_Changed(object sender, TextChangedEventArgs e)
            => ApplyFilters();

        private void FilterSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (EnergyCostFilterVal == null) return;
            int val = (int)e.NewValue;
            EnergyCostFilterVal.Text = val >= 100 ? "Any" : val.ToString();
            ApplyFilters();
        }

        private void Filter_CheckChanged(object sender, RoutedEventArgs e)
            => ApplyFilters();

        private void ApplyFilters()
        {
            if (_allSkills == null) return;

            string nameFilter = FilterNameInput?.Text?.ToLower() ?? "";
            int maxEnergy = (int)(EnergyCostFilter?.Value ?? 100);
            bool onlyAoe = FilterAoe?.IsChecked == true;
            bool onlyPassive = FilterPassive?.IsChecked == true;

            var filtered = _allSkills.Where(s =>
            {
                if (!string.IsNullOrEmpty(nameFilter) && !s.Name.ToLower().Contains(nameFilter))
                    return false;
                if (maxEnergy < 100 && s.EnergyCost > maxEnergy)
                    return false;
                if (onlyAoe && !s.IsAoe) return false;
                if (onlyPassive && !s.IsPassive) return false;
                return true;
            });

            _availableSkills.Clear();
            foreach (var s in filtered)
                _availableSkills.Add(s);
        }

        // ── Save ──────────────────────────────────────────────────────
        private void SaveEnemy_Click(object sender, RoutedEventArgs e)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(NameInput.Text))
            {
                MessageBox.Show("El nom és obligatori.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (ThumbPathInput.Text == "sin selección...")
            {
                MessageBox.Show("Cal seleccionar la imatge miniatura.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (FullPathInput.Text == "sin selección...")
            {
                MessageBox.Show("Cal seleccionar la imatge completa.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!int.TryParse(DefenseInput.Text, out int defense))
            {
                MessageBox.Show("Defensa ha de ser un número enter.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!int.TryParse(SpeedInput.Text, out int speed))
            {
                MessageBox.Show("Velocitat ha de ser un número enter.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!double.TryParse(AccuracyMultInput.Text, out double accMult))
            {
                MessageBox.Show("Accuracy Mult ha de ser un número decimal.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int.TryParse(CritChanceInput.Text, out int critChance);
            int.TryParse(CritDamageInput.Text, out int critDamage);
            int.TryParse(LevelInput.Text, out int level);

            var enemy = new Enemy
            {
                Entity = new Entity
                {
                    Name = NameInput.Text.Trim(),
                    Description = DescInput.Text.Trim(),
                    ImageThumb = ThumbPathInput.Text,
                    ImageFull = FullPathInput.Text,
                    Level = level,
                    Hp = (int)HpSlider.Value,
                    Energy = (int)EnergySlider.Value,
                    Attack = (int)AttackSlider.Value,
                    Defense = defense,
                    Speed = speed,
                    CritChance = critChance,
                    CritDamage = critDamage,
                    //AccuracyMult = accMult,
                    //Skills = _assignedSkills
                },
            };

            MessageBox.Show($"Enemic «{enemy.Entity.Name}» creat correctament!",
                            "Èxit", MessageBoxButton.OK, MessageBoxImage.Information);
            Close();
        }

        // ── Helpers ───────────────────────────────────────────────────
        private void UpdateNoSkillsPlaceholder()
            => NoSkillsPlaceholder.Visibility =
               _assignedSkills.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        private void LoadAvailableSkills()
        {
            // TODO: load from your real data source (JSON / DB / etc.)
            _allSkills = new List<Skill>
            {
                new() { Name = "Slash",       TargetType = "Single", EnergyCost = 10, Accuracy = 90, Hits = 1, IsAoe = false },
                new() { Name = "Fireball",    TargetType = "All",    EnergyCost = 30, Accuracy = 85, Hits = 1, IsAoe = true  },
                new() { Name = "Poison Bite", TargetType = "Single", EnergyCost = 15, Accuracy = 80, Hits = 1, IsAoe = false },
                new() { Name = "War Cry",     TargetType = "Self",   EnergyCost = 20, Accuracy = 100,Hits = 0, IsPassive = true },
            };

            foreach (var s in _allSkills)
                _availableSkills.Add(s);
        }
    }
}