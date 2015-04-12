using System.Windows.Documents;
using System.Windows.Input;

namespace Snoop.Controls
{
    public class NoFocusHyperlink : Hyperlink
    {
        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            OnClick();
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            e.Handled = true;
        }
    }
}