using System;
using System.Threading.Tasks;
using Xamarin.Forms;
using System.Collections.Generic;
using WasAppNamespace.Navigation;

namespace WasAppNamespace.Pages
{
    public class RootPage : MasterDetailPage
    {
        public RootPage()
        {
            var homePage = new HomePage();
            Master = new AppNavigationPage(new ShellPage(this)) {Title = homePage.Title = homePage.ViewModel.Title};
            Detail = new AppNavigationPage(homePage);
            MasterBehavior = MasterBehavior.Popover;

            InvalidateMeasure();
        }

        public async Task NavigateAsync()
        {
        }

        /*
            public async Task NavigateAsync(MenuType id)
            {

                if (Detail != null)
                {
                    if (IsUWPDesktop || Device.Idiom != TargetIdiom.Tablet)
                        IsPresented = false;

                    if (Device.OS == TargetPlatform.Android)
                        await Task.Delay(300);
                }

                Page newPage;
                if (!Pages.ContainsKey(id))
                {

                    switch (id)
                    {
                        case MenuType.About:
                            Pages.Add(id, new HanselmanNavigationPage(new AboutPage()));
                            break;
                        case MenuType.Blog:
                            Pages.Add(id, new HanselmanNavigationPage(new BlogPage()));
                            break;
                        case MenuType.DeveloperLife:
                            Pages.Add(id, new HanselmanNavigationPage(new PodcastPage(id)));
                            break;
                        case MenuType.Hanselminutes:
                            Pages.Add(id, new HanselmanNavigationPage(new PodcastPage(id)));
                            break;
                        case MenuType.Ratchet:
                            Pages.Add(id, new HanselmanNavigationPage(new PodcastPage(id)));
                            break;
                        case MenuType.Twitter:
                            Pages.Add(id, new HanselmanNavigationPage(new TwitterPage()));
                            break;
                    }
                }

                newPage = Pages[id];
                if (newPage == null)
                    return;

                //pop to root for Windows Phone
                if (Detail != null && Device.OS == TargetPlatform.WinPhone)
                {
                    await Detail.Navigation.PopToRootAsync();
                }

                Detail = newPage;
            }
            */

        }
    }

