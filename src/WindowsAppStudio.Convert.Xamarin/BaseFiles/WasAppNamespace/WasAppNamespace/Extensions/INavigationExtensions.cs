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
using WasAppNamespace.Navigation;
using WasAppNamespace.ViewModels;

namespace WasAppNamespace
{
    public static class INavigationExtensions
    {
        public static async Task Navigate(this INavigation navigation, ItemViewModel item, IEnumerable<SchemaBase> items)
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
                        await navigation.NavigateToPage(item.NavInfo.TargetPage, param);
                    }
                    else
                    {
                        await navigation.NavigateToPage(item.NavInfo.TargetPage);
                    }
                }
                else if (item.NavInfo.NavigationType == NavType.DeepLink)
                {
                    Device.OpenUri(item.NavInfo.TargetUri);
                }
            }
        }
    }
}