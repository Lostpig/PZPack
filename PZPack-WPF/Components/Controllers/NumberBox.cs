using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Input;

namespace PZPack.View.Controllers
{
    internal class NumberBox : TextBox
    {
        public NumberBox() : base() 
        {
            this.PreviewTextInput += NumberValidationTextBox;
        }

        private Regex numberRegex = new Regex("[^0-9]+");
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            e.Handled = numberRegex.IsMatch(e.Text);
        }
    }
}
