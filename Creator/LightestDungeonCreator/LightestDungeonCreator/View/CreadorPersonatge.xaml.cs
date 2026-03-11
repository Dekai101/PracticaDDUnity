using Microsoft.Win32;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LightestDungeonCreator
{
    /// <summary>
    /// Lógica de interacción para CreadorPersonatge.xaml
    /// </summary>
    public partial class CreadorPersonatge : Window
    {
        // ─── Constructor ────────────────────────────────────────────────────
        public CreadorPersonatge()
        {
            InitializeComponent();
        }

        // ─── Window drag (WindowStyle=None) ─────────────────────────────────
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        // ─── Tipus selector: mostra panel Player o Enemy ─────────────────────
        private void CmbTipus_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PanelPlayer == null || PanelEnemy == null) return;

            var selected = (CmbTipus.SelectedItem as ComboBoxItem)?.Tag?.ToString();
            PanelPlayer.Visibility = selected == "Player" ? Visibility.Visible : Visibility.Collapsed;
            PanelEnemy.Visibility  = selected == "Enemy"  ? Visibility.Visible : Visibility.Collapsed;
        }

        // ─── Slider nivell sincronitzat amb TextBox ──────────────────────────
        private void SliderNivell_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TxtNivell != null)
                TxtNivell.Text = ((int)SliderNivell.Value).ToString();
        }

        // ─── Browse imatges ──────────────────────────────────────────────────
        private void BtnBrowseThumb_Click(object sender, RoutedEventArgs e)
        {
            var path = OpenImageDialog();
            if (path != null) TxtImageThumb.Text = path;
        }

        private void BtnBrowseFull_Click(object sender, RoutedEventArgs e)
        {
            var path = OpenImageDialog();
            if (path != null) TxtImageFull.Text = path;
        }

        private static string? OpenImageDialog()
        {
            var dlg = new OpenFileDialog
            {
                Title  = "Selecciona una imatge",
                Filter = "Imatges|*.png;*.jpg;*.jpeg;*.bmp;*.gif|Tots els arxius|*.*"
            };
            return dlg.ShowDialog() == true ? dlg.FileName : null;
        }

        // ─── Input validation helpers ────────────────────────────────────────
        /// <summary>Només permet dígits (enters sense signe).</summary>
        private void NumericOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^\d+$");
        }

        /// <summary>Permet dígits i un punt decimal.</summary>
        private void DecimalOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var tb    = (TextBox)sender;
            var future = tb.Text.Insert(tb.CaretIndex, e.Text);
            e.Handled = !Regex.IsMatch(future, @"^\d*\.?\d*$");
        }

        // ─── Cancel ──────────────────────────────────────────────────────────
        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // ─── Guardar ─────────────────────────────────────────────────────────
        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            // 1. Validació bàsica
            if (!ValidateForm()) return;

            // 2. Recollir dades
            var nom         = TxtNom.Text.Trim();
            var nivell      = int.Parse(TxtNivell.Text);
            var hp          = int.Parse(TxtHp.Text);
            var hpMax       = int.Parse(TxtHpMax.Text);
            var energy      = int.Parse(TxtEnergy.Text);
            var energyMax   = int.Parse(TxtEnergyMax.Text);
            var attack      = int.Parse(TxtAttack.Text);
            var defense     = int.Parse(TxtDefense.Text);
            var speed       = int.Parse(TxtSpeed.Text);
            var critChance  = float.Parse(TxtCritChance.Text);
            var critDamage  = float.Parse(TxtCritDamage.Text);
            var accuracy    = float.Parse(TxtAccuracy.Text);
            var imageThumb  = TxtImageThumb.Text.Trim();
            var imageFull   = TxtImageFull.Text.Trim();
            var descripcio  = TxtDescripcio.Text.Trim();
            var tipusTag    = (CmbTipus.SelectedItem as ComboBoxItem)?.Tag?.ToString();

            // 3. TODO: Instanciar el model (Entity + Player/Enemy) i
            //          cridar al repositori / DbContext per guardar a la BD.
            //
            // Exemple futur:
            //   var entity = new Entity { Name = nom, Level = nivell, ... };
            //   _context.Entities.Add(entity);
            //   _context.SaveChanges();
            //   if (tipusTag == "Player") { ... }
            //   else                      { ... }

            MessageBox.Show(
                $"Personatge «{nom}» creat correctament!",
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
                return ShowValidationError("El nom del personatge és obligatori.");

            if (!int.TryParse(TxtHp.Text, out int hp) || hp < 0)
                return ShowValidationError("HP ha de ser un enter positiu.");

            if (!int.TryParse(TxtHpMax.Text, out int hpMax) || hpMax < 1)
                return ShowValidationError("HP Màxim ha de ser almenys 1.");

            if (hp > hpMax)
                return ShowValidationError("HP no pot superar HP Màxim.");

            if (!int.TryParse(TxtEnergy.Text, out int en) || en < 0)
                return ShowValidationError("Energia ha de ser un enter positiu.");

            if (!int.TryParse(TxtEnergyMax.Text, out int enMax) || enMax < 1)
                return ShowValidationError("Energia Màxima ha de ser almenys 1.");

            if (en > enMax)
                return ShowValidationError("Energia no pot superar l'Energia Màxima.");

            if (!float.TryParse(TxtCritChance.Text, out float crit) || crit < 0 || crit > 1)
                return ShowValidationError("La probabilitat crítica ha d'estar entre 0.0 i 1.0.");

            if (!float.TryParse(TxtCritDamage.Text, out _))
                return ShowValidationError("El dany crític ha de ser un número vàlid.");

            if (!float.TryParse(TxtAccuracy.Text, out _))
                return ShowValidationError("La precisió ha de ser un número vàlid.");

            var tipusTag = (CmbTipus.SelectedItem as ComboBoxItem)?.Tag?.ToString();
            if (tipusTag == "Enemy")
            {
                if (!int.TryParse(TxtPassiveId.Text, out int passiveId) || passiveId < 0)
                    return ShowValidationError("L'ID de la habilitat passiva ha de ser un enter vàlid.");
            }

            return true;
        }

        private bool ShowValidationError(string message)
        {
            TxtValidation.Text       = "⚠ " + message;
            TxtValidation.Visibility = Visibility.Visible;
            return false;
        }
    }
}
