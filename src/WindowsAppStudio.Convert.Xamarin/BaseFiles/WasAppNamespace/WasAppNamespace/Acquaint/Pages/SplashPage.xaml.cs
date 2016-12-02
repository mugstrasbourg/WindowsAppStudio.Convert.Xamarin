using Xamarin.Forms;
using System.Threading.Tasks;
using Acquaint.Util;
using WasAppNamespace.Pages;

namespace WasAppNamespace
{
    /// <summary>
    /// Splash Page that is used on Android only. iOS splash characteristics are NOT defined here, ub tn the iOS prject settings.
    /// </summary>
    public partial class SplashPage : ContentPage
    {
        bool _ShouldDelayForSplash = true;

        public SplashPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (_ShouldDelayForSplash)
                // delay for a few seconds on the splash screen
                await Task.Delay(3000);

            // create a new NavigationPage, with a new AcquaintanceListPage set as the Root
            Application.Current.MainPage = new NavigationPage(new RootPage());
        }
    }
}

