using Microsoft.Win32;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace LightestDungeonCreator
{
    /// <summary>
    /// Lógica de interacción para CreadorHabilitat.xaml
    /// </summary>
    public partial class CreadorHabilitat : Window
    {
        // ─── Model temporal per acumular efectes ────────────────────────────
        private readonly List<EffectEntry> _effects = new();

        // ─── Constructor ────────────────────────────────────────────────────
        public CreadorHabilitat()
        {
            InitializeComponent();
        }

        // ─── Window drag ─────────────────────────────────────────────────────
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        // ─── Browse imatge miniatura ─────────────────────────────────────────
        private void BtnBrowseThumb_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title  = "Selecciona una imatge miniatura",
                Filter = "Imatges|*.png;*.jpg;*.jpeg;*.bmp;*.gif|Tots els arxius|*.*"
            };
            if (dlg.ShowDialog() == true)
                TxtImageThumb.Text = dlg.FileName;
        }

        // ─── Input validation helpers ────────────────────────────────────────
        private void NumericOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^\d+$");
        }

        private void DecimalOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var tb     = (TextBox)sender;
            var future = tb.Text.Insert(tb.CaretIndex, e.Text);
            e.Handled  = !Regex.IsMatch(future, @"^\d*\.?\d*$");
        }

        // ─── Afegir efecte a la llista ───────────────────────────────────────
        private void BtnAfegirEfecte_Click(object sender, RoutedEventArgs e)
        {
            // Validació mínima de l'efecte
            if (!float.TryParse(TxtEffProbability.Text, out float prob) || prob < 0 || prob > 1)
            {
                MessageBox.Show("La probabilitat ha d'estar entre 0.0 i 1.0.", "Validació",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int? statId   = int.TryParse(TxtEffStatId.Text,   out int s) ? s : (int?)null;
            int? statusId = int.TryParse(TxtEffStatusId.Text, out int st) ? st : (int?)null;

            if (statId == null && statusId == null && prob != 1.0f)
            {
                MessageBox.Show("L'efecte necessita un Stat ID o un Status ID (o ambdós).", "Validació",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var entry = new EffectEntry
            {
                MinFlatPower   = int.TryParse(TxtEffMinPower.Text,    out int mn)   ? mn    : 0,
                MaxFlatPower   = int.TryParse(TxtEffMaxPower.Text,    out int mx)   ? mx    : 0,
                StatMultiplier = float.TryParse(TxtEffMultiplier.Text, out float m) ? m     : 1f,
                Probability    = prob,
                DurationTurns  = int.TryParse(TxtEffDuration.Text,    out int dur)  ? dur   : 1,
                EffectLevel    = int.TryParse(TxtEffLevel.Text,       out int lvl)  ? lvl   : 1,
                StatId         = statId,
                StatusId       = statusId
            };

            _effects.Add(entry);
            RefreshEffectsList();
            ClearEffectForm();
        }

        // ─── Eliminar efecte de la llista ────────────────────────────────────
        private void BtnRemoveEffect_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int idx && idx < _effects.Count)
            {
                _effects.RemoveAt(idx);
                RefreshEffectsList();
            }
        }

        // ─── Render de la llista d'efectes ───────────────────────────────────
        private void RefreshEffectsList()
        {
            // Netejar tot excepte el TextBlock de "cap efecte"
            while (PanelEffectsList.Children.Count > 1)
                PanelEffectsList.Children.RemoveAt(1);

            TxtNoEffects.Visibility = _effects.Count == 0
                ? Visibility.Visible : Visibility.Collapsed;

            for (int i = 0; i < _effects.Count; i++)
            {
                var eff    = _effects[i];
                var idx    = i; // capturar per closure
                var statTxt   = eff.StatId.HasValue   ? $"Stat:{eff.StatId}"     : "";
                var statusTxt = eff.StatusId.HasValue  ? $"Status:{eff.StatusId}" : "";
                var label  = $"Efecte {i + 1}  |  Poder [{eff.MinFlatPower}–{eff.MaxFlatPower}]" +
                             $"  ×{eff.StatMultiplier}  |  Prob:{eff.Probability}  |  " +
                             $"Dur:{eff.DurationTurns}t  Niv:{eff.EffectLevel}" +
                             (statTxt   != "" ? $"  {statTxt}"   : "") +
                             (statusTxt != "" ? $"  {statusTxt}" : "");

                var row = new Border
                {
                    Background          = new SolidColorBrush(Color.FromArgb(40, 200, 146, 42)),
                    BorderBrush         = new SolidColorBrush(Color.FromArgb(80, 92, 58, 30)),
                    BorderThickness     = new Thickness(1),
                    CornerRadius        = new CornerRadius(2),
                    Padding             = new Thickness(8, 6, 8, 6),
                    Margin              = new Thickness(0, 0, 0, 4)
                };

                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var txt = new TextBlock
                {
                    Text              = label,
                    Foreground        = new SolidColorBrush(Color.FromRgb(212, 197, 169)),
                    FontFamily        = new FontFamily("Georgia"),
                    FontSize          = 11,
                    TextWrapping      = TextWrapping.Wrap,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var del = new Button
                {
                    Content         = "✕",
                    Tag             = idx,
                    Background      = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Foreground      = new SolidColorBrush(Color.FromRgb(140, 60, 60)),
                    FontSize        = 14,
                    Cursor          = Cursors.Hand,
                    Margin          = new Thickness(8, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                del.Click += BtnRemoveEffect_Click;

                Grid.SetColumn(txt, 0);
                Grid.SetColumn(del, 1);
                grid.Children.Add(txt);
                grid.Children.Add(del);
                row.Child = grid;
                PanelEffectsList.Children.Add(row);
            }
        }

        // ─── Netejar formulari d'efecte ──────────────────────────────────────
        private void ClearEffectForm()
        {
            TxtEffMinPower.Text   = "0";
            TxtEffMaxPower.Text   = "0";
            TxtEffMultiplier.Text = "1.0";
            TxtEffProbability.Text = "1.0";
            TxtEffDuration.Text   = "1";
            TxtEffLevel.Text      = "1";
            TxtEffStatId.Text     = "";
            TxtEffStatusId.Text   = "";
        }

        // ─── Cancel ──────────────────────────────────────────────────────────
        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // ─── Guardar ─────────────────────────────────────────────────────────
        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateForm()) return;

            var nom        = TxtNom.Text.Trim();
            var descripcio = TxtDescripcio.Text.Trim();
            var energyCost = int.Parse(TxtEnergyCost.Text);
            var accuracy   = float.Parse(TxtAccuracy.Text);
            var hits       = int.Parse(TxtHits.Text);
            var targetType = (CmbTargetType.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Single";
            var isAoe      = ChkIsAoe.IsChecked == true;
            var isPassive  = ChkIsPassive.IsChecked == true;
            var imageThumb = TxtImageThumb.Text.Trim();

            // TODO: Instanciar el model Skill + llista d'Effects i
            //       cridar al repositori / DbContext per guardar a la BD.
            //
            // Exemple futur:
            //   var skill = new Skill
            //   {
            //       Name        = nom,
            //       Description = descripcio,
            //       EnergyCost  = energyCost,
            //       Accuracy    = accuracy,
            //       Hits        = hits,
            //       TargetType  = targetType,
            //       IsAoe       = isAoe,
            //       IsPassive   = isPassive,
            //       ImageThumb  = imageThumb
            //   };
            //   _context.Skills.Add(skill);
            //   _context.SaveChanges();
            //   foreach (var eff in _effects) { ... }

            MessageBox.Show(
                $"Habilitat «{nom}» creada amb {_effects.Count} efecte(s)!",
                "Guardat",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            DialogResult = true;
            Close();
        }

        // ─── Form validation ─────────────────────────────────────────────────
        private bool ValidateForm()
        {
            TxtValidation.Visibility = Visibility.Collapsed;

            if (string.IsNullOrWhiteSpace(TxtNom.Text))
                return ShowValidationError("El nom de l'habilitat és obligatori.");

            if (!int.TryParse(TxtEnergyCost.Text, out int cost) || cost < 0)
                return ShowValidationError("El cost d'energia ha de ser un enter positiu.");

            if (!float.TryParse(TxtAccuracy.Text, out float acc) || acc < 0 || acc > 1)
                return ShowValidationError("La precisió ha d'estar entre 0.0 i 1.0.");

            if (!int.TryParse(TxtHits.Text, out int hits) || hits < 1)
                return ShowValidationError("El nombre d'impactes ha de ser almenys 1.");

            return true;
        }

        private bool ShowValidationError(string message)
        {
            TxtValidation.Text       = "⚠ " + message;
            TxtValidation.Visibility = Visibility.Visible;
            return false;
        }
    }

    // ─── DTO temporal per als efectes (fins que tinguis els models del scaffold) ──
    internal class EffectEntry
    {
        public int    MinFlatPower   { get; set; }
        public int    MaxFlatPower   { get; set; }
        public float  StatMultiplier { get; set; }
        public int?   StatusId       { get; set; }
        public int?   StatId         { get; set; }
        public int    EffectLevel    { get; set; }
        public float  Probability    { get; set; }
        public int    DurationTurns  { get; set; }
    }
}
