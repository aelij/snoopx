using System.Collections.Generic;
using System.Windows.Forms;
using System.Windows.Forms.Integration;

namespace Snoop.VisualTree
{
    public class FormsTreeItem : VisualTreeItem
    {
        public Control Control { get; }

        public FormsTreeItem(Control control, VisualTreeItem parent) 
            : base(control, parent)
        {
            Control = control;
        }


        protected override string GetName()
        {
            return Control.Name;
        }

        protected override void Reload(List<VisualTreeItem> toBeRemoved)
        {
            base.Reload(toBeRemoved);

            var elementHost = Control as ElementHost;
            if (elementHost != null)
            {
                Children.Add(Construct(elementHost.Child, this));
            }
            foreach (var child in Control.Controls)
            {
                Children.Add(Construct(child, this));
            }
        }
    }
}