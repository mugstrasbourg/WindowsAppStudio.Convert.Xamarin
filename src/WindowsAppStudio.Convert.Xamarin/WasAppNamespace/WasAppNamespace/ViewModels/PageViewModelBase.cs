using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Threading.Tasks;
using AppStudio.DataProviders;
using AppStudio.DataProviders.Core;

#if UWP
using Windows.ApplicationModel.DataTransfer;
using AppStudio.Uwp;
using AppStudio.Uwp.Actions;
using AppStudio.Uwp.Cache;
using AppStudio.Uwp.Navigation;
#else
using AppStudio.Xamarin;
using AppStudio.Xamarin.Actions;
using AppStudio.Xamarin.Cache;
using AppStudio.Xamarin.Navigation;
using Xamarin.Forms;
#endif
namespace WasAppNamespace.ViewModels
{
    public abstract class PageViewModelBase : ObservableBase, INavigation
    {
        protected readonly TimeSpan CacheExpiration = new TimeSpan(2, 0, 0);

        private string _title;
        private bool _hasLoadDataErrors;
        private bool _isBusy;
        private DateTime? _lastUpdated;

        public string SectionName { get; set; }

        protected PageViewModelBase()
        {
            Actions = new List<ActionInfo>();
        }

        public string Title
        {
            get { return _title; }
            protected set { SetProperty(ref _title, value); }
        }

        public bool HasLoadDataErrors
        {
            get { return _hasLoadDataErrors; }
            protected set { SetProperty(ref _hasLoadDataErrors, value); }
        }

        public bool IsBusy
        {
            get { return _isBusy; }
            protected set { SetProperty(ref _isBusy, value); }
        }

        public DateTime? LastUpdated
        {
            get { return _lastUpdated; }
            protected set { SetProperty(ref _lastUpdated, value); }
        }

        public List<ActionInfo> Actions { get; private set; }

        public bool HasActions
        {
            get
            {
                return Actions != null && Actions.Count > 0;
            }
        }

        public bool HasLocalData { get; set; }
#if UWP
        protected void ShareContent(DataRequest dataRequest, ItemViewModel item, bool supportsHtml)
        {
            try
            {
                if (item != null)
                {
                    dataRequest.Data.Properties.Title = string.IsNullOrEmpty(item.Title) ? Title : item.Title;

                    if (!string.IsNullOrEmpty(item.SubTitle))
                    {
                        SetContent(dataRequest, item.SubTitle, false);
                    }

                    if (!string.IsNullOrEmpty(item.Description))
                    {
                        SetContent(dataRequest, item.Description, supportsHtml);
                    }

                    if (!string.IsNullOrEmpty(item.Content))
                    {
                        SetContent(dataRequest, item.Content, supportsHtml);
                    }

                    if (!string.IsNullOrEmpty(item.Source))
                    {
                        dataRequest.Data.SetWebLink(new Uri(item.Source));
                    }

                    var imageUrl = item.ImageUrl;
                    if (!string.IsNullOrEmpty(imageUrl))
                    {
                        if (imageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                        {
                            if (string.IsNullOrEmpty(item.Source))
                            {
                                dataRequest.Data.SetWebLink(new Uri(imageUrl));
                            }
                        }
                        else
                        {
                            imageUrl = string.Format("ms-appx://{0}", imageUrl);
                        }
                        dataRequest.Data.SetBitmap(Windows.Storage.Streams.RandomAccessStreamReference.CreateFromUri(new Uri(imageUrl)));
                    }
                }
            }
            catch (UriFormatException)
            {
            }
        }

        private void SetContent(DataRequest dataRequest, string data, bool supportsHtml)
        {
            if (supportsHtml)
            {
                dataRequest.Data.SetHtmlFormat(HtmlFormatHelper.CreateHtmlFormat(data));
            }
            else
            {
                dataRequest.Data.SetText(data.DecodeHtml());
            }
        }
#else
        private INavigation _Navigation
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

        #region INavigation implementation

        public void RemovePage(Page page)
        {
            _Navigation?.RemovePage(page);
        }

        public void InsertPageBefore(Page page, Page before)
        {
            _Navigation?.InsertPageBefore(page, before);
        }

        public async Task PushAsync(Page page)
        {
            var task = _Navigation?.PushAsync(page);
            if (task != null)
                await task;
        }

        public async Task<Page> PopAsync()
        {
            var task = _Navigation?.PopAsync();
            return task != null ? await task : await Task.FromResult(null as Page);
        }

        public async Task PopToRootAsync()
        {
            var task = _Navigation?.PopToRootAsync();
            if (task != null)
                await task;
        }

        public async Task PushModalAsync(Page page)
        {
            var task = _Navigation?.PushModalAsync(page);
            if (task != null)
                await task;
        }

        public async Task<Page> PopModalAsync()
        {
            var task = _Navigation?.PopModalAsync();
            return task != null ? await task : await Task.FromResult(null as Page);
        }

        public async Task PushAsync(Page page, bool animated)
        {
            var task = _Navigation?.PushAsync(page, animated);
            if (task != null)
                await task;
        }

        public async Task<Page> PopAsync(bool animated)
        {
            var task = _Navigation?.PopAsync(animated);
            return task != null ? await task : await Task.FromResult(null as Page);
        }

        public async Task PopToRootAsync(bool animated)
        {
            var task = _Navigation?.PopToRootAsync(animated);
            if (task != null)
                await task;
        }

        public async Task PushModalAsync(Page page, bool animated)
        {
            var task = _Navigation?.PushModalAsync(page, animated);
            if (task != null)
                await task;
        }

        public async Task<Page> PopModalAsync(bool animated)
        {
            var task = _Navigation?.PopModalAsync(animated);
            return task != null ? await task : await Task.FromResult(null as Page);
        }

        public IReadOnlyList<Page> NavigationStack => _Navigation?.NavigationStack;

        public IReadOnlyList<Page> ModalStack => _Navigation?.ModalStack;

        #endregion
#endif

    }
}