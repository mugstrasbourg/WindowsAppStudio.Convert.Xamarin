using System.Windows.Input;
using System.Threading.Tasks;
using System.Collections.Generic;
#if UWP
using AppStudio.Uwp.Commands;
#else
using AppStudio.Xamarin.Commands;
#endif
using AppStudio.DataProviders;

namespace WasAppNamespace.ViewModels
{
    public abstract class ListViewModelBase : PageViewModelBase
    {
        protected bool _isDataProviderInitialized;
        protected bool _isFirstPage;

        protected List<SchemaBase> SourceItems { get; } = new List<SchemaBase>();

        public abstract Task LoadDataAsync(bool forceRefresh = false, SchemaBase connected = null);
        public abstract Task LoadNextPageAsync();

        private bool _hasItems;
        public bool HasItems
        {
            get { return _hasItems; }
            protected set { SetProperty(ref _hasItems, value); }
        }

        private bool _isLoadingNextPage;
        public bool IsLoadingNextPage
        {
            get { return _isLoadingNextPage; }
            set { SetProperty(ref _isLoadingNextPage, value); }
        }

        public RelayCommand<ItemViewModel> ItemClickCommand
        {
            get
            {
                return new RelayCommand<ItemViewModel>(
                async (item) =>
                {
                    await this.Navigate(item, SourceItems);
                });
            }
        }

        public ICommand Refresh
        {
            get
            {
                return new RelayCommand(async () =>
                {
#if UWP
                    ShellPage.Current.Frame.ScrollToTop();
#endif
                    await LoadDataAsync(true);
                });
            }
        }
    }
}
