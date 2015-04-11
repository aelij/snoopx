using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Snoop.Controls
{
    public class TreeListView : TreeView
    {
        static TreeListView()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TreeListView), new FrameworkPropertyMetadata(typeof(TreeListView)));
        }

        public TreeListView()
        {
            SetValue(ColumnsPropertyKey, new GridViewColumnCollection());
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            return new TreeListViewItem(0);
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is TreeListViewItem;
        }

        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);
            GridView.SetColumnCollection(element, Columns);
        }

        private static readonly DependencyPropertyKey ColumnsPropertyKey = DependencyProperty.RegisterReadOnly(
            "Columns", typeof(GridViewColumnCollection), typeof(TreeListView), new FrameworkPropertyMetadata());

        public static readonly DependencyProperty ColumnsProperty = ColumnsPropertyKey.DependencyProperty;

        public GridViewColumnCollection Columns
        {
            get { return (GridViewColumnCollection)GetValue(ColumnsProperty); }
        }
    }

    public class TreeListViewToggleButton : ToggleButton
    {
        static TreeListViewToggleButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TreeListViewToggleButton), new FrameworkPropertyMetadata(typeof(TreeListViewToggleButton)));
        }
    }

    public class TreeListViewItem : TreeViewItem
    {
        static TreeListViewItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TreeListViewItem), new FrameworkPropertyMetadata(typeof(TreeListViewItem)));
        }

        private int _level;

        public TreeListViewItem()
            : this(-1)
        {
        }

        public TreeListViewItem(int level)
        {
            _level = level;
        }

        /// <summary>
        /// Item's hierarchy in the tree
        /// </summary>
        public int Level
        {
            get
            {
                if (_level == -1)
                {
                    var parent = ItemsControlFromItemContainer(this) as TreeListViewItem;
                    _level = (parent != null) ? parent.Level + 1 : 0;
                }
                return _level;
            }
        }

        protected override DependencyObject
                           GetContainerForItemOverride()
        {
            return new TreeListViewItem(_level + 1);
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is TreeListViewItem;
        }

        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);
            GridView.SetColumnCollection(element, GridView.GetColumnCollection(this));
        }
    }
}