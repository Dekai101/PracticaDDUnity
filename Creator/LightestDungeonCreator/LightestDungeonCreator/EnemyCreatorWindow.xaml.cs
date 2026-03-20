using LightestDungeonCreator.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace LightestDungeonCreator
{
    public partial class EnemyCreatorWindow : Window
    {
        // AppDbContext
        AppDbContext db;

        // ── Collections ──────────────────────────────────────────────
        private ObservableCollection<Skill> _assignedSkills;
        private ObservableCollection<Skill> _availableSkills;
        private List<Skill> _allSkills;

        public EnemyCreatorWindow()
        {
            //Initialize DbContext and collections
            db = new AppDbContext();
            _assignedSkills = new ObservableCollection<Skill>();
            _availableSkills = new ObservableCollection<Skill>();
            _allSkills = new List<Skill>();

            //Initialize UI and events
            InitializeComponent();
            HpSlider.ValueChanged += HpSlider_ValueChanged;
            EnergySlider.ValueChanged += EnergySlider_ValueChanged;
            AttackSlider.ValueChanged += AttackSlider_ValueChanged;
            LoadAvailableSkills();
            ApplyFilters();
            AssignedSkillsList.ItemsSource = _assignedSkills;
            AvailableSkillsList.ItemsSource = _availableSkills;
            UpdateNoSkillsPlaceholder();
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
            new EnemyListWindow().Show();
        }


        // ── Image URL inputs ──────────────────────────────────────────
        private void ThumbPathInput_TextChanged(object sender, TextChangedEventArgs e)
            => ThumbPreview.Source = TryLoadBitmap(ThumbPathInput.Text.Trim());

        private void FullPathInput_TextChanged(object sender, TextChangedEventArgs e)
            => FullPreview.Source = TryLoadBitmap(FullPathInput.Text.Trim());

        private static BitmapImage? TryLoadBitmap(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return null;
            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(path, UriKind.RelativeOrAbsolute);
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                return bmp;
            }
            catch { return null; }
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

        private void DefenseSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (DefenseValue == null) return;
            DefenseValue.Text = ((int)e.NewValue).ToString();
        }

        private void SpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (SpeedValue == null) return;
            SpeedValue.Text = ((int)e.NewValue).ToString();
        }

        private void CritChanceSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (CritChanceValue == null) return;
            CritChanceValue.Text = ((int)e.NewValue).ToString() + "%";
        }

        private void CritDamageSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (CritDamageValue == null) return;
            CritDamageValue.Text = ((int)e.NewValue).ToString() + "%";
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
            if (_assignedSkills.Any(s => s.Name == skill.Name))
            {
                MessageBox.Show("You already have this skill assigned", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (_assignedSkills.Count >= 4 && !_assignedSkills.Any(s => s.IsPassive) && !skill.IsPassive)
            {
                MessageBox.Show("You cannot add more than 4 normal skills", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (_assignedSkills.Count > 4)
            {
                MessageBox.Show("Max skill count reached, 4 normal skills and 1 passive skill", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (skill.IsPassive && _assignedSkills.Any(s => s.IsPassive))
            {
                MessageBox.Show("You cannot add more than 1 passive skill", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            _assignedSkills.Add(skill);
            UpdateNoSkillsPlaceholder();
        }

        // ── Filters ───────────────────────────────────────────────────
        private void FilterName_Changed(object sender, TextChangedEventArgs e)
            => ApplyFilters();

        private void FilterEnergySlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (EnergyCostFilterVal == null) return;
            int val = (int)e.NewValue;
            EnergyCostFilterVal.Text = val >= 100 ? "Any" : val.ToString();
            ApplyFilters();
        }

        private void FilterAccuracySlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (AccuracyFilterVal == null) return;
            int val = (int)e.NewValue;
            AccuracyFilterVal.Text = val >= 100 ? "Any" : val.ToString() + "%";
            ApplyFilters();
        }

        private void Filter_CheckChanged(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox cb)
            {
                if (cb == FilterAlly && cb.IsChecked == true)
                    FilterEnemy.IsChecked = FilterSelf.IsChecked = false;
                else if (cb == FilterEnemy && cb.IsChecked == true)
                    FilterAlly.IsChecked = FilterSelf.IsChecked = false;
                else if (cb == FilterSelf && cb.IsChecked == true)
                    FilterAlly.IsChecked = FilterEnemy.IsChecked = false;
                else if (cb == FilterAoe && cb.IsChecked == true)
                    FilterNotAoe.IsChecked = false;
                else if (cb == FilterNotAoe && cb.IsChecked == true)
                    FilterAoe.IsChecked = false;
            }
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            if (_allSkills == null) return;

            string nameFilter = FilterNameInput?.Text?.ToLower() ?? "";
            int maxEnergy = (int)(EnergyCostFilter?.Value ?? 100);
            int maxAccuracy = (int)(AccuracyFilter?.Value ?? 100);
            bool onlyAlly = FilterAlly?.IsChecked == true;
            bool onlyEnemy = FilterEnemy?.IsChecked == true;
            bool onlySelf = FilterSelf?.IsChecked == true;
            bool onlyAoe = FilterAoe?.IsChecked == true;
            bool onlyNotAoe = FilterNotAoe?.IsChecked == true;
            bool onlyPassive = FilterPassive?.IsChecked == true;

            var filtered = _allSkills.Where(s =>
            {
                if (!string.IsNullOrEmpty(nameFilter) && !s.Name.ToLower().Contains(nameFilter))
                    return false;
                if (maxEnergy < 100 && s.EnergyCost > maxEnergy)
                    return false;
                if (maxAccuracy < 100 && s.Accuracy > maxAccuracy)
                    return false;
                // TARGET
                if (onlyAlly && s.TargetType.ToUpper() != "ALLY") return false;
                if (onlyEnemy && s.TargetType.ToUpper() != "ENEMY") return false;
                if (onlySelf && s.TargetType.ToUpper() != "SELF") return false;
                // TYPE
                if (onlyAoe && !s.IsAoe) return false;
                if (onlyNotAoe && s.IsAoe) return false;
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
                MessageBox.Show("Name required", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(ThumbPathInput.Text))
            {
                MessageBox.Show("Thumb Image required", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(FullPathInput.Text))
            {
                MessageBox.Show("Full Image required", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!int.TryParse(LevelInput.Text, out _))
            {
                MessageBox.Show("Level has to be number", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (_assignedSkills.Count == 0)
            {
                MessageBox.Show("It is required to have 1 skill", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            else if (_assignedSkills.Count == 1 && _assignedSkills.First().IsPassive)
            {
                MessageBox.Show("It is required to have 1 normal skill", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int.TryParse(LevelInput.Text, out int level);

            if (level < 1 || level > 100)
            {
                MessageBox.Show("Level has to be a number between 1 and 100");
                return;
            }

            var enemy = new Enemy
            {
                PassiveId = _assignedSkills.FirstOrDefault(s => s.IsPassive)?.Id ?? 0,
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
                    Defense = (int)DefenseSlider.Value,
                    Speed = (int)SpeedSlider.Value,
                    CritChance = (float)CritChanceSlider.Value / 100,
                    CritDamage = (float)CritDamageSlider.Value / 100,
                    AccuracyMultiplier = 1,
                    Skills = _assignedSkills
                },
            };

            db.Entities.Add(enemy.Entity);
            db.SaveChanges();
            db.Enemies.Add(enemy);
            db.SaveChanges();
            MessageBox.Show($"Enemy «{enemy.Entity.Name}» successfully created!",
                            "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            new SplashScreen().Show();
            Close();
        }

        // ── Helpers ───────────────────────────────────────────────────
        private void UpdateNoSkillsPlaceholder()
            => NoSkillsPlaceholder.Visibility =
               _assignedSkills.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        private void LoadAvailableSkills()
        {
            _allSkills = db.Skills.ToList();
        }
    }
}