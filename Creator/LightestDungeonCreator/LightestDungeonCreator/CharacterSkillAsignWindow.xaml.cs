using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using LightestDungeonCreator.Models;
using Microsoft.EntityFrameworkCore;

namespace LightestDungeonCreator
{
    public partial class CharacterSkillAssignWindow : Window
    {
        AppDbContext db;

        private readonly ObservableCollection<Skill> _assignedSkills = new();
        private List<Skill> _allSkills = new();
        private int? _selectedEntityId;
        private List<Player> _allChars = new();

        public CharacterSkillAssignWindow()
        {
            db = new AppDbContext();

            InitializeComponent();
            AssignedSkillsList.ItemsSource = _assignedSkills;
            _assignedSkills.CollectionChanged += (_, _) => UpdatePlaceholder();
            LoadCharactersFromDb();
            LoadAllSkillsFromDb();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed) DragMove();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            new SplashScreen().Show();
            this.Close();
        }

        // ── Load characters ───────────────────────────────────────────────────

        private void LoadCharactersFromDb()
        {
            try
            {
                _allChars = db.Players
                    .Include(p => p.Entity)                          // load the entity
                    .Where(p => p.Entity != null && p.Entity.Name != null)
                    .OrderBy(p => p.Entity.Name).Include(p => p.Entity.Skills)
                    .ToList();

                CharacterList.ItemsSource = _allChars;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading characters:\n{ex.Message}",
                                "DB Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void LoadAllSkillsFromDb()
        {
            try
            {
                _allSkills = db.Skills.OrderBy(s => s.Name).Except(db.Skills.Where(s => s.IsPassive)).ToList();

                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading skills:\n{ex.Message}",
                                "DB Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // ── Character selection ───────────────────────────────────────────────

        private void CharacterList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CharacterList.SelectedItem is not Player selected) return;
            LoadCharacterDetail(selected.EntityId);
        }

        private void LoadCharacterDetail(int entityId)
        {
            try
            {
                var entity = db.Entities.Find(entityId);

                if (entity == null) return;

                _selectedEntityId = entityId;

                // Fill card
                DetailName.Text = entity.Name;
                DetailLevel.Text = $"Level {entity.Level}";
                DetailDesc.Text = entity.Description ?? "";
                DetailThumb.Source = LoadBitmap(entity.ImageThumb);
                DetailFull.Source = LoadBitmap(entity.ImageFull);

                // Fill stats
                StatHp.Text = $"{entity.Hp}/{entity.HpMax}";
                StatEnergy.Text = $"{entity.Energy}/{entity.EnergyMax}";
                StatAtk.Text = entity.Attack.ToString();
                StatDef.Text = entity.Defense.ToString();
                StatSpd.Text = entity.Speed.ToString();
                StatCrit.Text = $"{entity.CritChance * 100:0.#}%";
                StatCritDmg.Text = $"x{entity.CritDamage * 100:0.#}%";
                StatAcc.Text = $"{(entity.AccuracyMultiplier * 100)}%";

                // Fill assigned skills
                _assignedSkills.Clear();
                foreach (var sk in entity.Skills)
                    _assignedSkills.Add(sk);

                // Show detail panel
                NoSelectionPanel.Visibility = Visibility.Collapsed;
                CharDetailPanel.Visibility = Visibility.Visible;
                SaveBtn.IsEnabled = true;
                FooterHint.Text = $"Editing skills of: {entity.Name}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading details:\n{ex.Message}",
                                "DB Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // ── Skill assignment ──────────────────────────────────────────────────

        private void AddSelectedSkill_Click(object sender, RoutedEventArgs e)
        {
            if (AvailableSkillsList.SelectedItem is not Skill selected) return;
            if (_assignedSkills.Any(s => s.Name == selected.Name))
            {
                MessageBox.Show("You already have this skill assigned", "Error",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (_assignedSkills.Count >= 4)
            {
                MessageBox.Show("You cannot add more than 4 normal skills", "Error",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            _assignedSkills.Add(selected);
        }

        private void RemoveSkill_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Skill vm)
                _assignedSkills.Remove(vm);
        }

        private void UpdatePlaceholder()
        {
            NoSkillsPlaceholder.Visibility =
                _assignedSkills.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        // ── Filters ───────────────────────────────────────────────────────────

        private void CharSearch_Changed(object sender, TextChangedEventArgs e)
        {
            if (CharacterList == null || _allChars == null) return;

            var q = CharSearchInput?.Text?.Trim();
            if (string.IsNullOrEmpty(q))
            {
                CharacterList.ItemsSource = _allChars;
                return;
            }

            CharacterList.ItemsSource = _allChars
                .Where(c => !string.IsNullOrEmpty(c?.Entity?.Name) &&
                            c.Entity.Name.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
        }

        private void Filter_Changed(object sender, TextChangedEventArgs e) => ApplyFilters();

        private void Filter_CheckChanged(object sender, RoutedEventArgs e)
        {
            if(sender is CheckBox cb)
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

        private void FilterSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (EnergyCostFilterVal != null)
                EnergyCostFilterVal.Text = (int)EnergyCostFilter.Value == 100
                    ? "Any" : ((int)EnergyCostFilter.Value).ToString();
            if (AccuracyFilterVal != null)
                AccuracyFilterVal.Text = $"{(int)AccuracyFilter.Value}%";
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            if (AvailableSkillsList == null || _allSkills == null) return;

            var name = FilterNameInput?.Text?.Trim().ToLower() ?? "";
            int maxCost = (int)(EnergyCostFilter?.Value ?? 100);
            float minAcc = (float)(AccuracyFilter?.Value ?? 0) / 100f;

            bool onlyAlly = FilterAlly?.IsChecked == true;
            bool onlyEnemy = FilterEnemy?.IsChecked == true;
            bool onlySelf = FilterSelf?.IsChecked == true;
            bool onlyAoe = FilterAoe?.IsChecked == true;
            bool onlyNotAoe = FilterNotAoe?.IsChecked == true;

            var result = _allSkills.Where(s =>
            {
                if (!string.IsNullOrEmpty(name) && !s.Name.ToLower().Contains(name))
                    return false;
                if (maxCost < 100 && s.EnergyCost > maxCost)
                    return false;
                if (s.Accuracy < minAcc)
                    return false;
                // TARGET
                if (onlyAlly && s.TargetType != "ALLY") return false;
                if (onlyEnemy && s.TargetType != "ENEMY") return false;
                if (onlySelf && s.TargetType != "SELF") return false;
                // TYPE
                if (onlyAoe && !s.IsAoe) return false;
                if (onlyNotAoe && s.IsAoe) return false;
                return true;
            }).ToList();

            AvailableSkillsList.ItemsSource = result;
        }

        // ── Save ─────────────────────────────────────────────────────────────

        private void SaveAssignment_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedEntityId == null) return;

            try
            {
                Entity en = db.Entities
                    .Where(en => en.Id == _selectedEntityId)
                    .FirstOrDefault();

                if (e == null) return;

                // Clear existing
                en.Skills.Clear();

                // Re-assign
                var ids = _assignedSkills.Select(s => s.Id).ToList();
                var tracked = db.Skills.Where(s => ids.Contains(s.Id)).ToList();
                foreach (var sk in tracked) en.Skills.Add(sk);

                db.SaveChanges();

                MessageBox.Show($"Skills of '{en.Name}' saved successfully.\n{tracked.Count} skill(s) assigned.",
                                "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving:\n{ex.Message}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static BitmapImage? LoadBitmap(string? path)
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