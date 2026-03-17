using System;
using System.Collections.Generic;
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
    public partial class SplashScreen : Window
    {
        public SplashScreen()
        {
            InitializeComponent();
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void CreateCharacter_Click(object sender, RoutedEventArgs e)
        {
            new CharacterSkillAssignWindow().Show();
            this.Close();
        }

        private void CreateSkill_Click(object sender, RoutedEventArgs e)
        {
            new SkillCreatorWindow().Show();
            this.Close();
        }

        private void EditCharacterSkillsBtn_Click(object sender, RoutedEventArgs e)
        {
            new CharacterSkillAssignWindow().Show();
            this.Close();
        }

        private void CreateEnemyBtn_Click(object sender, RoutedEventArgs e)
        {
            new EnemyCreatorWindow().Show();
            this.Close();
        }

        private void CreateItemBtn_Click(object sender, RoutedEventArgs e)
        {
            new ItemCreatorWindow().Show();
            this.Close();
        }
    }
}