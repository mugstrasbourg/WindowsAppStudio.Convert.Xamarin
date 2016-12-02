using AppStudio.Xamarin.Navigation;
using FormsToolkit;
using WasAppNamespace.Pages;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

//[assembly: XamlCompilation(XamlCompilationOptions.Compile)]

namespace WasAppNamespace
{
	public partial class XamApp : Application
	{

		public XamApp()
		{
			InitializeComponent();

            SubscribeToDisplayAlertMessages();

			// The navigation logic startup needs to diverge per platform in order to meet the UX design requirements
			if (Device.OS == TargetPlatform.Android)
			{
				// if this is an Android device, set the MainPage to a new SplashPage
				MainPage = new SplashPage();
			}
			else
			{
                //				// create a new AppNavigationPage, with a new AcquaintanceListPage set as the Root
                //				var navPage =
                //					new AppNavigationPage(
                //                        new Pages.RootPage()
                ///*						new AcquaintanceListPage()
                //						{
                //							BindingContext = new AcquaintanceListViewModel(),
                //							Title = "Acquaintances"
                //						}*/
                //                        )
                //					{
                //						BarBackgroundColor = Color.FromHex("547799")
                //					};

                //			    navPage.BarTextColor = Color.White;

                //                // set the MainPage of the app to the navPage
                //                MainPage = navPage;
                MainPage = new RootPage();
			}
		}

        public static INavigation AppNavigation
        {
            get
            {
                if (Application.Current.MainPage is NavigationPage)
                    return Application.Current.MainPage.Navigation;
                MasterDetailPage masterDetailPage = Application.Current.MainPage as MasterDetailPage;
                if (masterDetailPage != null)
                    return masterDetailPage.Detail is NavigationPage
                        ? masterDetailPage.Detail.Navigation
                        : masterDetailPage.Master?.Navigation;
                return Application.Current?.MainPage?.Navigation;
            }
        }

        /// <summary>
        /// Subscribes to messages for displaying alerts.
        /// </summary>
        static void SubscribeToDisplayAlertMessages()
		{
			MessagingService.Current.Subscribe<MessagingServiceAlert>(MessageKeys.DisplayAlert, async (service, info) => {
				var task = Current?.MainPage?.DisplayAlert(info.Title, info.Message, info.Cancel);
				if (task != null)
				{
					await task;
					info?.OnCompleted?.Invoke();
				}
			});

			MessagingService.Current.Subscribe<MessagingServiceQuestion>(MessageKeys.DisplayQuestion, async (service, info) => {
				var task = Current?.MainPage?.DisplayAlert(info.Title, info.Question, info.Positive, info.Negative);
				if (task != null)
				{
					var result = await task;
					info?.OnCompleted?.Invoke(result);
				}
			});
		}
	}
}

