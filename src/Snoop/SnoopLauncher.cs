using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Snoop.Infrastructure;
using Snoop.Views;

namespace Snoop
{
    public static class SnoopLauncher
    {
        public static bool GoBabyGo()
        {
            try
            {
                SnoopApplication();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"There was an error snooping! Message = {ex.Message}\n\nStack Trace:\n{ex.StackTrace}", "Error Snooping", MessageBoxButton.OK);
                return false;
            }
        }

        public static void SnoopApplication()
        {
            var dispatcher = FindDispatcher();

            if (dispatcher?.CheckAccess() == true)
            {
                CheckForOtherDispatchers(dispatcher);

                LaunchSnoopApplicationOnDispatcher();
            }
            else
            {
                bool created = false;
                if (dispatcher == null)
                {
                    dispatcher = Dispatcher.CurrentDispatcher;
                    created = true;
                }
                dispatcher.InvokeAsync(LaunchSnoopApplicationOnDispatcher);
                if (created)
                {
                    Dispatcher.Run();
                }
            }
        }

        private static List<DispatcherInfo> GetAllDispatchers()
        {
            var dispatchers = new List<DispatcherInfo>();
            if (Application.Current != null)
            {
                AddDispatcherAndVisual(dispatchers, Application.Current.Dispatcher, Application.Current);
            }
            foreach (var source in PresentationSource.CurrentSources.OfType<PresentationSource>())
            {
                AddDispatcherAndVisual(dispatchers, source.Dispatcher, source.RootVisual);
            }
            foreach (var form in System.Windows.Forms.Application.OpenForms.OfType<System.Windows.Forms.Form>())
            {
                var dispatcher = (Dispatcher)form.Invoke(new Func<Dispatcher>(() => Dispatcher.CurrentDispatcher));
                AddDispatcherAndVisual(dispatchers, dispatcher, form);
            }
            return dispatchers;
        }

        private static void AddDispatcherAndVisual(List<DispatcherInfo> dispatchers, Dispatcher dispatcher, object visual)
        {
            var dispatcherInfo = dispatchers.FirstOrDefault(x => x.Dispatcher == dispatcher);
            if (dispatcherInfo == null)
            {
                dispatcherInfo = new DispatcherInfo(dispatcher);
                dispatchers.Add(dispatcherInfo);
            }
            dispatcherInfo.AddVisual(visual);
        }

        private static void LaunchSnoopApplicationOnDispatcher()
        {
            var snoop = new SnoopUI();
            var title = TryGetMainWindowTitle();
            if (!string.IsNullOrEmpty(title))
            {
                snoop.Title = $"{title} - SnoopX";
            }

            snoop.Inspect();
        }

        private static string TryGetMainWindowTitle()
        {
            if (Application.Current != null && Application.Current.MainWindow != null &&
                Application.Current.MainWindow.CheckAccess())
            {
                return Application.Current.MainWindow.Title;
            }
            return string.Empty;
        }

        private static Dispatcher FindDispatcher()
        {
            if (Application.Current != null)
            {
                return Application.Current.Dispatcher;
            }
            if (System.Windows.Forms.Application.OpenForms.Count > 0)
            {
                return (Dispatcher)System.Windows.Forms.Application.OpenForms[0].Invoke(
                    new Func<Dispatcher>(() => Dispatcher.CurrentDispatcher));
            }
            var source = PresentationSource.CurrentSources.OfType<PresentationSource>().FirstOrDefault();
            return source?.Dispatcher;
        }

        private static void CheckForOtherDispatchers(Dispatcher mainDispatcher)
        {
            // check and see if any of the root visuals have a different mainDispatcher
            // if so, ask the user if they wish to enter multiple mainDispatcher mode.
            // if they do, launch a snoop ui for every additional mainDispatcher.
            // see http://snoopwpf.codeplex.com/workitem/6334 for more info.

            var rootVisuals = new List<Visual>();
            var dispatchers = new List<Dispatcher> { mainDispatcher };
            foreach (PresentationSource presentationSource in PresentationSource.CurrentSources)
            {
                var presentationSourceRootVisual = presentationSource.RootVisual;

                if (!(presentationSourceRootVisual is Window))
                    continue;

                var presentationSourceRootVisualDispatcher = presentationSourceRootVisual.Dispatcher;

                if (dispatchers.IndexOf(presentationSourceRootVisualDispatcher) == -1)
                {
                    rootVisuals.Add(presentationSourceRootVisual);
                    dispatchers.Add(presentationSourceRootVisualDispatcher);
                }
            }

            if (rootVisuals.Count > 0)
            {
                var result =
                    MessageBox.Show
                        (
                            "Snoop has noticed windows running in multiple dispatchers!\n\n" +
                            "Would you like to enter multiple dispatcher mode, and have a separate Snoop window for each dispatcher?\n\n" +
                            "Without having a separate Snoop window for each dispatcher, you will not be able to Snoop the windows in the dispatcher threads outside of the main dispatcher. " +
                            "Also, note, that if you bring up additional windows in additional dispatchers (after Snooping), you will need to Snoop again in order to launch Snoop windows for those additional dispatchers.",
                            "Enter Multiple Dispatcher Mode",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question
                        );

                if (result == MessageBoxResult.Yes)
                {
                    SnoopModes.MultipleDispatcherMode = true;
                    var thread = new Thread(DispatchOut);
                    thread.Start(rootVisuals);
                }
            }
        }

        private static void DispatchOut(object o)
        {
            var visuals = (List<Visual>)o;
            foreach (var visual in visuals)
            {
                visual.Dispatcher.InvokeAsync(() =>
                {
                    var snoopOtherDispatcher = new SnoopUI();
                    snoopOtherDispatcher.Inspect(visual, visual as Window);
                });
            }
        }
    }


    class DispatcherInfo
    {
        public Dispatcher Dispatcher { get; }

        public IEnumerable<object> Visuals
        {
            get
            {
                foreach (var weakReference in _visuals)
                {
                    object visual;
                    if (weakReference.TryGetTarget(out visual))
                    {
                        yield return visual;
                    }
                }
            }
        }

        private readonly List<WeakReference<object>> _visuals;

        public DispatcherInfo(Dispatcher dispatcher)
        {
            Dispatcher = dispatcher;
            _visuals = new List<WeakReference<object>>();
        }

        public void AddVisual(object visual)
        {
            _visuals.Add(new WeakReference<object>(visual));
        }
    }
}