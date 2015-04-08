// (c) 2015 Eli Arbel
// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System.Windows;
using Snoop.Infrastructure;

namespace Snoop
{
    public static class SnoopWindowUtils
    {
        public static Window FindOwnerWindow()
        {
            Window ownerWindow = null;

            if (SnoopModes.MultipleDispatcherMode)
            {
                foreach (PresentationSource presentationSource in PresentationSource.CurrentSources)
                {
                    var window = presentationSource.RootVisual as Window;
                    if (window != null &&
                        window.Dispatcher.CheckAccess() &&
                        window.Visibility == Visibility.Visible)
                    {
                        ownerWindow = window;
                        break;
                    }
                }
            }
            else if (Application.Current != null)
            {
                if (Application.Current.MainWindow != null && Application.Current.MainWindow.Visibility == Visibility.Visible)
                {
                    // first: set the owner window as the current application's main window, if visible.
                    ownerWindow = Application.Current.MainWindow;
                }
                else
                {
                    // second: try and find a visible window in the list of the current application's windows
                    foreach (Window window in Application.Current.Windows)
                    {
                        if (window.Visibility == Visibility.Visible)
                        {
                            ownerWindow = window;
                            break;
                        }
                    }
                }
            }

            if (ownerWindow != null)
            {
                return ownerWindow;
            }

            // third: try and find a visible window in the list of current presentation sources
            foreach (PresentationSource presentationSource in PresentationSource.CurrentSources)
            {
                var window = presentationSource.RootVisual as Window;
                if (window != null &&
                    window.Visibility == Visibility.Visible)
                {
                    ownerWindow = window;
                    break;
                }
            }

            return ownerWindow;
        }
    }
}
