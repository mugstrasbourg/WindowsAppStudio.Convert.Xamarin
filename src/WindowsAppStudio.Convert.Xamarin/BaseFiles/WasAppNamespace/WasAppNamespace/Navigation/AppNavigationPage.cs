using System;
using Xamarin.Forms;

namespace WasAppNamespace.Navigation
{
    public class AppNavigationPage : NavigationPage
    {
        public AppNavigationPage(Page root) : base(root)
        {
            Init();
        }

        public AppNavigationPage()
        {
            Init();
        }

        void Init()
        {

            BarBackgroundColor = Color.FromHex("#F3A9F4");
            BarTextColor = Color.White;
        }
    }
}

