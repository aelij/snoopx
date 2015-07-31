// (c) 2015 Eli Arbel
// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using Snoop.Annotations;
using Application = System.Windows.Application;

namespace Snoop.VisualTree
{
    public class VisualTreeItem : INotifyPropertyChanged
    {
        [SuppressMessage("ReSharper", "CanBeReplacedWithTryCastAndCheckForNull")]
        public static VisualTreeItem Construct(object target, VisualTreeItem parent)
        {
            VisualTreeItem visualTreeItem;

            if (target is Visual)
                visualTreeItem = new VisualItem((Visual)target, parent);
            else if (target is ResourceDictionary)
                visualTreeItem = new ResourceDictionaryItem((ResourceDictionary)target, parent);
            else if (target is Application)
                visualTreeItem = new ApplicationTreeItem((Application)target, parent);
            else if (target is System.Windows.Forms.Control)
                visualTreeItem = new FormsTreeItem((System.Windows.Forms.Control)target, parent);
            else
                visualTreeItem = new VisualTreeItem(target, parent);

            visualTreeItem.Reload();

            return visualTreeItem;
        }
        protected VisualTreeItem(object target, VisualTreeItem parent)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            Target = target;
            Parent = parent;
            if (parent != null)
                Depth = parent.Depth + 1;
        }


        public override string ToString()
        {
            var sb = new StringBuilder(50);

            // [depth] name (type) numberOfChildren
            sb.AppendFormat("[{0}] {1} ({2})", Depth.ToString("D3"), _name, Target.GetType().Name);
            if (_visualChildrenCount != 0)
            {
                sb.Append(' ');
                sb.Append(_visualChildrenCount.ToString());
            }

            return sb.ToString();
        }


        /// <summary>
        /// The WPF object that this VisualTreeItem is wrapping
        /// </summary>
        public object Target { get; }

        /// <summary>
        /// The VisualTreeItem parent of this VisualTreeItem
        /// </summary>
        public VisualTreeItem Parent { get; }

        /// <summary>
        /// The depth (in the visual tree) of this VisualTreeItem
        /// </summary>
        public int Depth { get; }

        /// <summary>
        /// The VisualTreeItem children of this VisualTreeItem
        /// </summary>
        public ObservableCollection<VisualTreeItem> Children { get; } = new ObservableCollection<VisualTreeItem>();


        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;

                    // Need to expand all ancestors so this will be visible in the tree.
                    if (_isSelected)
                        Parent?.ExpandTo();

                    OnPropertyChanged(nameof(IsSelected));
                    OnSelectionChanged();
                }
            }
        }
        protected virtual void OnSelectionChanged()
        {
        }
        private bool _isSelected;

        /// <summary>
        /// Need this to databind to TreeView so we can display to hidden items.
        /// </summary>
        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    OnPropertyChanged(nameof(IsExpanded));
                }
            }
        }
        /// <summary>
        /// Expand this element and all elements leading to it.
        /// Used to show this element in the tree view.
        /// </summary>
        private void ExpandTo()
        {
            Parent?.ExpandTo();

            IsExpanded = true;
        }
        private bool _isExpanded;


        public virtual Visual MainVisual => null;

        public virtual Brush TreeBackgroundBrush => new SolidColorBrush(Color.FromArgb(255, 240, 240, 240));

        public virtual Brush VisualBrush => null;

        /// <summary>
        /// Checks to see if any property on this element has a binding error.
        /// </summary>
        public virtual bool HasBindingError => false;


        /// <summary>
        /// Update the view of this visual, rebuild children as necessary
        /// </summary>
        public void Reload()
        {
            _name = GetName();

            _nameLower = (_name ?? "").ToLower();
            _typeNameLower = Target?.GetType().Name.ToLower() ?? string.Empty;

            var toBeRemoved = new List<VisualTreeItem>(Children);
            Reload(toBeRemoved);
            foreach (var item in toBeRemoved)
                RemoveChild(item);

            // calculate the number of visual children
            foreach (var child in Children)
            {
                if (child is VisualItem)
                    _visualChildrenCount++;

                _visualChildrenCount += child._visualChildrenCount;
            }
        }

        protected virtual string GetName()
        {
            return string.Empty;
        }

        protected virtual void Reload(List<VisualTreeItem> toBeRemoved)
        {
        }



        public VisualTreeItem FindNode(object node)
        {
            // it might be faster to have a map for the lookup
            // check into this at some point
            return Target == node
                ? this
                : Children.Select(child => child.FindNode(node)).FirstOrDefault(n => n != null);
        }


        /// <summary>
        /// Used for tree search.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool Filter(string value)
        {
            if (_typeNameLower.Contains(value))
                return true;
            if (_nameLower.Contains(value))
                return true;
            int n;
            if (int.TryParse(value, out n) && n == Depth)
                return true;
            return false;
        }


        protected void RemoveChild(VisualTreeItem item)
        {
            item.IsSelected = false;
            Children.Remove(item);
        }

        private string _name;
        private string _nameLower = string.Empty;
        private string _typeNameLower = string.Empty;
        private int _visualChildrenCount;

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
