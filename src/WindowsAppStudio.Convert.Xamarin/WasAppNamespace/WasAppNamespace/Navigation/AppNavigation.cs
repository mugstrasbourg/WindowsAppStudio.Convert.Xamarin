using System;
using System.Collections.Generic;
using System.Threading.Tasks;
#if UWP
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.System;
using AppStudio.Uwp;
using AppStudio.Uwp.Controls;
using AppStudio.Uwp.Navigation;
#else
using Xamarin.Forms;
using AppStudio.Xamarin;
using AppStudio.Xamarin.Controls;
using AppStudio.Xamarin.Navigation;
#endif

using AppStudio.DataProviders;
using WasAppNamespace.ViewModels;

namespace WasAppNamespace.Navigation
{
    public static class AppNavigation
    {
#if UWP
        private static Brush NavigationItemColor { get { return "NavigationPaneText".Resource() as Brush; } }
        public static void Navigate(ItemViewModel item, IEnumerable<SchemaBase> items)
        {
            if (item.NavInfo != null)
            {
                if (item.NavInfo.NavigationType == NavType.Page)
                {
                    if (item.NavInfo.IsDetail)
                    {
                        var param = new NavDetailParameter
                        {
                            SelectedId = item.Id,
                            Items = items
                        };
                        NavigationService.NavigateToPage(item.NavInfo.TargetPage, param);
                    }
                    else
                    {
                        NavigationService.NavigateToPage(item.NavInfo.TargetPage);
                    }
                }
                else if (item.NavInfo.NavigationType == NavType.DeepLink)
                {
                    Launcher.LaunchUriAsync(item.NavInfo.TargetUri).AsTask().FireAndForget();
                }
            }
        }
        public static NavigationItem NodeFromAction(string id, string caption, Action<NavigationItem> onClick, IconElement icon = null, Image image = null)
        {
            var node = new NavigationItem(id, caption);
            node.OnClick = onClick;
            node.Icon = icon;
            node.Image = image;
            return node;
        }
        public static NavigationItem NodeFromSubitems(string id, string caption, IEnumerable<NavigationItem> subitems, IconElement icon = null, Image image = null)
        {
            var node = new NavigationItem(id, caption);
            node.SubItems = subitems;
            node.Icon = icon;
            node.Image = image;
            return node;
        }
        public static NavigationItem NodeFromControl(string id, string caption, Control control, IconElement icon = null, Image image = null)
        {
            var node = new NavigationItem(id, caption);
            node.Icon = icon;
            node.Control = control;
            return node;
        }
        public static IconElement IconFromImage(Uri imageUri) => NavigationItem.CreateIcon(imageUri, NavigationItemColor);
        public static IconElement IconFromGlyph(string glyph) => NavigationItem.CreateIcon(glyph, NavigationItemColor);
        public static IconElement IconFromSymbol(Symbol symbol) => NavigationItem.CreateIcon(symbol, NavigationItemColor);

        public static Action<NavigationItem> ActionFromPage(string pageName) => (ni) => NavigationService.NavigateToPage(pageName);
#else
        public static string IconFromImage(Uri imageUri) => "";
        public static string IconFromGlyph(string glyph) => glyph;
        public static string IconFromSymbol(Symbol symbol) => "";
        public static NavigationItem NodeFromAction(string id, string caption, Action<NavigationItem> onClick, string icon = null, Image image = null)
        {
            return new NavigationItem(id, caption)
            {
                OnClick = onClick,
                Icon = icon,
                Image = image
            };
        }
        public static NavigationItem NodeFromSubitems(string id, string caption, IEnumerable<NavigationItem> subitems, string icon = null, Image image = null)
        {
            return new NavigationItem(id, caption)
            {
                SubItems = subitems,
                Icon = icon,
                Image = image
            };
        }
        public static NavigationItem NodeFromControl(string id, string caption, VisualElement control, string icon = null, Image image = null)
        {
            return new NavigationItem(id, caption)
            {
                Icon = icon,
                Control = control
            };
        }

#endif
    }
}