using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Diagnostics;
using System.Windows.Input;
using AppStudio.DataProviders;
#if UWP
using AppStudio.Uwp.Actions;
using AppStudio.Uwp.Cache;
using AppStudio.Uwp.Commands;
using AppStudio.Uwp.DataSync;
using AppStudio.Uwp;
using AppStudio.Uwp.Navigation;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Xaml.Controls;
#else
using AppStudio.Xamarin.Actions;
using AppStudio.Xamarin.Cache;
using AppStudio.Xamarin.Commands;
using AppStudio.Xamarin.DataSync;
using AppStudio.Xamarin;
using AppStudio.Xamarin.Navigation;
using FormsToolkit;
using MvvmHelpers;
using Plugin.Messaging;
using Xamarin.Forms;
using Acquaint.Abstractions;
using Acquaint.Util;
#endif
using WasAppNamespace.Sections;
using WasAppNamespace.Services;

namespace WasAppNamespace.ViewModels
{
    public abstract class ListViewModel : ListViewModelBase
    {
        public abstract Task SearchDataAsync(string searchTerm);
        public abstract Task FilterDataAsync(List<string> itemsId);

        protected abstract Type ListPage { get; }

        public ListViewModel(string title, string sectionName)
        {
            Title = title;
            SectionName = sectionName;
        }

        private ObservableCollection<ItemViewModel> _items = new ObservableCollection<ItemViewModel>();
        public ObservableCollection<ItemViewModel> Items
        {
            get { return _items; }
            protected set { SetProperty(ref _items, value); }
        }

        private bool _hasMoreItems;
        public bool HasMoreItems
        {
            get { return _hasMoreItems; }
            protected set { SetProperty(ref _hasMoreItems, value); }
        }

        private bool _hasMorePages;

        public bool HasMorePages
        {
            get { return _hasMorePages; }
            set { SetProperty(ref _hasMorePages, value); }
        }

        public RelayCommand LoadNextPageCommand
        {
            get
            {
                return new RelayCommand(
                async () =>
                {
                    if (!IsLoadingNextPage)
                    {
                        await LoadNextPageAsync();
                    }
                });
            }
        }

        public RelayCommand SectionHeaderClickCommand
        {
            get
            {
                return new RelayCommand(async () =>
                {
                    await this.NavigateToPage(ListPage);
                });
            }
        }
#if UWP
        public void ShareContent(DataRequest dataRequest, bool supportsHtml = true)
        {
            if (Items != null && Items.Count > 0)
            {
                ShareContent(dataRequest, Items[0], supportsHtml);
            }
        }
#endif
        internal void CleanItems()
        {
            this.Items.Clear();
            this.HasItems = false;
        }
        #region From Acquaint
        public abstract Task ExecuteLoadAcquaintancesCommand();
        #endregion

    }

    public class ListViewModel<TSchema> : ListViewModel where TSchema : SchemaBase
    {
        private Section<TSchema> _section;
        private int _visibleItems;
        private SchemaBase _connected;

        protected override Type ListPage
        {
            get
            {
                return _section.ListPage.Page;
            }
        }

        public ListViewModel(Section<TSchema> section, int visibleItems = 0) : base(section.ListPage.Title, section.Name)
        {
            _section = section;
            _visibleItems = visibleItems;

            HasLocalData = !section.NeedsNetwork;
            HasMorePages = true;

            if (_section.NeedsNetwork)
            {
                Actions.Add(new ActionInfo
                {
                    Command = Refresh,
                    Style = ActionKnownStyles.Refresh,
                    Name = "RefreshButton",
                    ActionType = ActionType.Primary
                });
            }
        }

        public override async Task LoadDataAsync(bool forceRefresh = false, SchemaBase connected = null)
        {
            try
            {
                _connected = connected;
                HasLoadDataErrors = false;
                IsBusy = true;

                var loaderSettings = LoaderSettings.FromSection(_section, GetCacheKey(connected), forceRefresh);
                var loaderOutcome = await DataLoader.LoadAsync(loaderSettings, () => _section.GetDataAsync(connected), (items) => ParseItems(items));

                LastUpdated = loaderOutcome.Timestamp;
                _isDataProviderInitialized = loaderOutcome.IsFreshData;
                _isFirstPage = true;
            }
            catch (Exception ex)
            {
                HasLoadDataErrors = true;
                Debug.WriteLine(ex.ToString());
            }
            finally
            {
                IsBusy = false;
            }
        }

        public override async Task LoadNextPageAsync()
        {
            try
            {
                HasLoadDataErrors = false;
                IsLoadingNextPage = true;

                if (!_isDataProviderInitialized && _isFirstPage)
                {
                    await LoadDataAsync(true, _connected);
                    _isFirstPage = false;
                }

                await DataLoader.LoadAsync(LoaderSettings.NoCache(_section.NeedsNetwork), () => _section.GetNextPageAsync(), (items) => ParseNextPage(items));

                HasMorePages = _section.HasMorePages;
            }
            catch (Exception ex)
            {
                HasLoadDataErrors = true;
                Debug.WriteLine(ex.ToString());
            }
            finally
            {
                IsLoadingNextPage = false;
            }
        }

        public override async Task SearchDataAsync(string searchTerm)
        {
            if (!string.IsNullOrEmpty(searchTerm))
            {
                try
                {
                    HasLoadDataErrors = false;
                    IsBusy = true;

                    var loaderSettings = LoaderSettings.FromSection(_section, _section.Name, true);
                    var loaderOutcome = await DataLoader.LoadAsync(loaderSettings, () => _section.GetDataAsync(), (items) => ParseItems(items, i => i.ContainsString(searchTerm)));
                    LastUpdated = loaderOutcome.Timestamp;
                }
                catch (Exception ex)
                {
                    HasLoadDataErrors = true;
                    Debug.WriteLine(ex.ToString());
                }
                finally
                {
                    IsBusy = false;
                }
            }
        }

        public override async Task FilterDataAsync(List<string> itemsId)
        {
            if (itemsId != null && itemsId.Any())
            {
                try
                {
                    HasLoadDataErrors = false;
                    IsBusy = true;

                    var loaderSettings = LoaderSettings.FromSection(_section, _section.Name, true);
                    var loaderOutcome = await DataLoader.LoadAsync(loaderSettings, () => _section.GetDataAsync(), (items) => ParseItems(items, i => itemsId.Contains(i.Id)));
                    LastUpdated = loaderOutcome.Timestamp;
                }
                catch (Exception ex)
                {
                    HasLoadDataErrors = true;
                    Debug.WriteLine(ex.ToString());
                }
                finally
                {
                    IsBusy = false;
                }
            }
        }

        private void ParseItems(IEnumerable<TSchema> content, Func<ItemViewModel, bool> filterFunc = null)
        {
            SourceItems.Clear();
            SourceItems.AddRange(content);

            var parsedItems = new List<ItemViewModel>();
            foreach (var item in GetVisibleItems(content, _visibleItems))
            {
                var parsedItem = new ItemViewModel
                {
                    Id = item._id,
                    NavInfo = _section.ListPage.DetailNavigation(item)
                };

                _section.ListPage.LayoutBindings(parsedItem, item);

                if (filterFunc == null || filterFunc(parsedItem))
                {
                    parsedItems.Add(parsedItem);
                }
            }
            Items.Sync(parsedItems);

            HasItems = Items.Count > 0;
            HasMoreItems = content.Count() > Items.Count;
        }

        private void ParseNextPage(IEnumerable<TSchema> content)
        {
            SourceItems.AddRange(content);

            foreach (var item in content)
            {
                var parsedItem = new ItemViewModel
                {
                    Id = item._id,
                    NavInfo = _section.ListPage.DetailNavigation(item)
                };
                _section.ListPage.LayoutBindings(parsedItem, item);

                Items.Add(parsedItem);
            }
        }

        private IEnumerable<TSchema> GetVisibleItems(IEnumerable<TSchema> content, int visibleItems)
        {
            if (visibleItems == 0)
            {
                return content;
            }
            else
            {
                return content.Take(visibleItems);
            }
        }

        private string GetCacheKey(SchemaBase connectedItem)
        {
            if (connectedItem == null)
            {
                return _section.Name;
            }
            else
            {
                return $"{_section.Name}_{connectedItem._id}";
            }
        }

#region From Acquaint

        // this is just a utility service that we're using in this demo app to mitigate some limitations of the iOS simulator
        readonly ICapabilityService _CapabilityService;

        // readonly IDataSource<ItemViewModel> DataSource;

        // ObservableRangeCollection<ItemViewModel> Items;

        Command _LoadAcquaintancesCommand;

        Command _RefreshAcquaintancesCommand;

        Command _NewAcquaintanceCommand;

        Command _ShowSettingsCommand;

        //public ObservableRangeCollection<ItemViewModel> Items
        //{
        //    get { return Items ?? (Items = new ObservableRangeCollection<ItemViewModel>()); }
        //    set
        //    {
        //        Items = value;
        //        OnPropertyChanged("Items");
        //    }
        //}

        /// <summary>
        /// Command to create new acquaintance
        /// </summary>
        public Command NewAcquaintanceCommand
        {
            get
            {
                return _NewAcquaintanceCommand ??
                    (_NewAcquaintanceCommand = new Command(async () => await ExecuteNewAcquaintanceCommand()));
            }
        }

        async Task ExecuteNewAcquaintanceCommand()
        {
//            await PushAsync(new AcquaintanceEditPage() { BindingContext = new AcquaintanceEditViewModel() });
        }

        /// <summary>
        /// Command to show settings
        /// </summary>
        public Command ShowSettingsCommand
        {
            get
            {
                return _ShowSettingsCommand ??
                    (_ShowSettingsCommand = new Command(async () => await ExecuteShowSettingsCommand()));
            }
        }

        async Task ExecuteShowSettingsCommand()
        {
            //var navPage = new NavigationPage(
            //    new SettingsPage() { BindingContext = new SettingsViewModel() })
            //{
            //    BarBackgroundColor = Color.FromHex("547799")
            //};

            //navPage.BarTextColor = Color.White;

            //await PushModalAsync(navPage);
        }

        Command _DialNumberCommand;

        /// <summary>
        /// Command to dial acquaintance phone number
        /// </summary>
        public Command DialNumberCommand
        {
            get
            {
                return _DialNumberCommand ??
                (_DialNumberCommand = new Command((parameter) =>
                        ExecuteDialNumberCommand((string)parameter)));
            }
        }

        void ExecuteDialNumberCommand(string acquaintanceId)
        {
            if (string.IsNullOrWhiteSpace(acquaintanceId))
                return;

            var acquaintance = SourceItems.SingleOrDefault(c => c._id == acquaintanceId);

            IPhoneForSchema acquaintanceWithPhone = acquaintance as IPhoneForSchema;
            if (acquaintanceWithPhone == null)
                return;

            if (_CapabilityService.CanMakeCalls)
            {
                var phoneCallTask = MessagingPlugin.PhoneDialer;
                if (phoneCallTask.CanMakePhoneCall)
                    phoneCallTask.MakePhoneCall(acquaintanceWithPhone.Phone.SanitizePhoneNumber());
            }
            else
            {
                MessagingService.Current.SendMessage<MessagingServiceAlert>(MessageKeys.DisplayAlert, new MessagingServiceAlert()
                {
                    Title = "Simulator Not Supported",
                    Message = "Phone calls are not supported in the iOS simulator.",
                    Cancel = "OK"
                });
            }
        }

        Command _MessageNumberCommand;

        /// <summary>
        /// Command to message acquaintance phone number
        /// </summary>
        public Command MessageNumberCommand
        {
            get
            {
                return _MessageNumberCommand ??
                (_MessageNumberCommand = new Command((parameter) =>
                        ExecuteMessageNumberCommand((string)parameter)));
            }
        }

        void ExecuteMessageNumberCommand(string acquaintanceId)
        {
            if (string.IsNullOrWhiteSpace(acquaintanceId))
                return;

            var acquaintance = SourceItems.SingleOrDefault(c => c._id == acquaintanceId);

            IPhoneForSchema acquaintanceWithPhone = acquaintance as IPhoneForSchema;
            if (acquaintanceWithPhone == null)
                return;

            if (_CapabilityService.CanSendMessages)
            {
                var messageTask = MessagingPlugin.SmsMessenger;
                if (messageTask.CanSendSms)
                    messageTask.SendSms(acquaintanceWithPhone.Phone.SanitizePhoneNumber());
            }
            else
            {
                MessagingService.Current.SendMessage<MessagingServiceAlert>(MessageKeys.DisplayAlert, new MessagingServiceAlert()
                {
                    Title = "Simulator Not Supported",
                    Message = "Messaging is not supported in the iOS simulator.",
                    Cancel = "OK"
                });
            }
        }

        Command _EmailCommand;

        /// <summary>
        /// Command to email acquaintance
        /// </summary>
        public Command EmailCommand
        {
            get
            {
                return _EmailCommand ??
                (_EmailCommand = new Command((parameter) =>
                        ExecuteEmailCommand((string)parameter)));
            }
        }

        void ExecuteEmailCommand(string acquaintanceId)
        {
            if (string.IsNullOrWhiteSpace(acquaintanceId))
                return;

            var acquaintance = SourceItems.SingleOrDefault(c => c._id == acquaintanceId);
            IEmailForSchema acquaintanceWithEmail = acquaintance as IEmailForSchema;

            if (acquaintanceWithEmail == null)
                return;

            if (_CapabilityService.CanSendEmail)
            {
                var emailTask = MessagingPlugin.EmailMessenger;
                if (emailTask.CanSendEmail)
                    emailTask.SendEmail(acquaintanceWithEmail.Email);
            }
            else
            {
                MessagingService.Current.SendMessage<MessagingServiceAlert>(MessageKeys.DisplayAlert, new MessagingServiceAlert()
                {
                    Title = "Simulator Not Supported",
                    Message = "Email composition is not supported in the iOS simulator.",
                    Cancel = "OK"
                });
            }
        }
        /// <summary>
        /// Command to load Items
        /// </summary>
        public Command LoadAcquaintancesCommand
        {
            get { return _LoadAcquaintancesCommand ?? (_LoadAcquaintancesCommand = new Command(async () => await ExecuteLoadAcquaintancesCommand())); }
        }

        public override async Task ExecuteLoadAcquaintancesCommand()
        {
            LoadAcquaintancesCommand.ChangeCanExecute();

            //if (Settings.LocalDataResetIsRequested)
            //    Items.Clear();

            //if (Items.Count < 1 || !Settings.DataIsSeeded || Settings.ClearImageCacheIsRequested)
            await FetchAcquaintances();

            LoadAcquaintancesCommand.ChangeCanExecute();
        }

        public Command RefreshAcquaintancesCommand
        {
            get
            {
                return _RefreshAcquaintancesCommand ?? (_RefreshAcquaintancesCommand = new Command(async () => await ExecuteRefreshAcquaintancesCommandCommand()));
            }
        }

        async Task ExecuteRefreshAcquaintancesCommandCommand()
        {
            RefreshAcquaintancesCommand.ChangeCanExecute();

            await FetchAcquaintances();

            RefreshAcquaintancesCommand.ChangeCanExecute();
        }

        protected async Task FetchAcquaintances()
        {
            IsBusy = true;
            await LoadDataAsync();
            //Items = new ObservableRangeCollection<ItemViewModel>(await DataSource.GetItems());

            // ensuring that this flag is reset
            Settings.ClearImageCacheIsRequested = false;

            IsBusy = false;
        }



        /// <summary>
        /// Subscribes to "AddAcquaintance" messages
        /// </summary>
        void SubscribeToAddAcquaintanceMessages()
        {
            MessagingService.Current.Subscribe<TSchema>(MessageKeys.AddAcquaintance, async (service, acquaintance) =>
            {
                IsBusy = true;

                //await DataSource.AddItem(acquaintance);

                await FetchAcquaintances();

                IsBusy = false;
            });
        }

        /// <summary>
        /// Subscribes to "UpdateAcquaintance" messages
        /// </summary>
        void SubscribeToUpdateAcquaintanceMessages()
        {
            MessagingService.Current.Subscribe<TSchema>(MessageKeys.UpdateAcquaintance, async (service, acquaintance) =>
            {
                IsBusy = true;

                //await DataSource.UpdateItem(acquaintance);

                await FetchAcquaintances();

                IsBusy = false;
            });
        }

        /// <summary>
        /// Subscribes to "DeleteAcquaintance" messages
        /// </summary>
        void SubscribeToDeleteAcquaintanceMessages()
        {
            MessagingService.Current.Subscribe<TSchema>(MessageKeys.DeleteAcquaintance, async (service, acquaintance) =>
            {
                IsBusy = true;

                //await DataSource.RemoveItem(acquaintance);

                await FetchAcquaintances();

                IsBusy = false;
            });
        }
#endregion



    }
}

/*

namespace WasAppNamespace.ViewModels
{
    /// <summary>
    /// From AppStudio AND from Acquaint!
    /// From AppStudio but disabled (so, enabled only for UWP):
    ///  - ShareContent
    /// </summary>
    public abstract class ListViewModel : ListViewModelBase
    {
        public abstract Task SearchDataAsync(string searchTerm);
        public abstract Task FilterDataAsync(List<string> itemsId);

        protected abstract Type ListPage { get; }

        public ListViewModel(string title, string sectionName)
        {
            Title = title;
            SectionName = sectionName;
        }

        private ObservableCollection<ItemViewModel> _items = new ObservableCollection<ItemViewModel>();
        public ObservableCollection<ItemViewModel> Items
        {
            get { return _items; }
            protected set { SetProperty(ref _items, value); }
        }



        private bool _hasMoreItems;
        public bool HasMoreItems
        {
            get { return _hasMoreItems; }
            protected set { SetProperty(ref _hasMoreItems, value); }
        }

        private bool _hasMorePages;

        public bool HasMorePages
        {
            get { return _hasMorePages; }
            set { SetProperty(ref _hasMorePages, value); }
        }

        public RelayCommand LoadNextPageCommand
        {
            get
            {
                return new RelayCommand(
                async () =>
                {
                    if (!IsLoadingNextPage)
                    {
                        await LoadNextPageAsync();
                    }
                });
            }
        }
        public RelayCommand SectionHeaderClickCommand
        {
            get
            {
                return new RelayCommand(() =>
                {
                    NavigationService.NavigateToPage(ListPage);
                });
            }
        }
#if UWP

        public void ShareContent(DataRequest dataRequest, bool supportsHtml = true)
        {
            if (Items != null && Items.Count > 0)
            {
                ShareContent(dataRequest, Items[0], supportsHtml);
            }
        }
#endif
        internal void CleanItems()
        {           
            this.Items.Clear();
            this.HasItems = false;
        }

        public abstract Task ExecuteLoadAcquaintancesCommand();

    }

    public class ListViewModel<TSchema> : ListViewModel where TSchema : SchemaBase
    {
        private Section<TSchema> _section;
        private int _visibleItems;
        private SchemaBase _connected;

        protected override Type ListPage
        {
            get
            {
                return _section.ListPage.Page;
            }
        }
            // new ObservableCollection<SchemaBase>(SourceItems);

        public ListViewModel(Section<TSchema> section, int visibleItems = 0) : base(section.ListPage.Title, section.Name)
        {
            _section = section;
            _visibleItems = visibleItems;
            HasLocalData = !section.NeedsNetwork;
            HasMorePages = true;

            if (_section.NeedsNetwork)
            {
                Actions.Add(new ActionInfo
                {
                    Command = Refresh,
                    Style = ActionKnownStyles.Refresh,
                    Name = "RefreshButton",
                    ActionType = ActionType.Primary
                });
            }

            //From Acquaint
            _CapabilityService = DependencyService.Get<ICapabilityService>();
            //DataSource = new AzureAcquaintanceSource();
            SubscribeToAddAcquaintanceMessages();

            SubscribeToUpdateAcquaintanceMessages();

            SubscribeToDeleteAcquaintanceMessages();

        }
#region From Acquaint

        // this is just a utility service that we're using in this demo app to mitigate some limitations of the iOS simulator
        readonly ICapabilityService _CapabilityService;

        // readonly IDataSource<ItemViewModel> DataSource;

        // ObservableRangeCollection<ItemViewModel> Items;

        Command _LoadAcquaintancesCommand;

        Command _RefreshAcquaintancesCommand;

        Command _NewAcquaintanceCommand;

        Command _ShowSettingsCommand;

        //public ObservableRangeCollection<ItemViewModel> Items
        //{
        //    get { return Items ?? (Items = new ObservableRangeCollection<ItemViewModel>()); }
        //    set
        //    {
        //        Items = value;
        //        OnPropertyChanged("Items");
        //    }
        //}

        /// <summary>
        /// Command to create new acquaintance
        /// </summary>
        public Command NewAcquaintanceCommand
        {
            get
            {
                return _NewAcquaintanceCommand ??
                    (_NewAcquaintanceCommand = new Command(async () => await ExecuteNewAcquaintanceCommand()));
            }
        }

        async Task ExecuteNewAcquaintanceCommand()
        {
            await NavigationService.PushAsync(new AcquaintanceEditPage() { BindingContext = new AcquaintanceEditViewModel() });
        }

        /// <summary>
        /// Command to show settings
        /// </summary>
        public Command ShowSettingsCommand
        {
            get
            {
                return _ShowSettingsCommand ??
                    (_ShowSettingsCommand = new Command(async () => await ExecuteShowSettingsCommand()));
            }
        }

        async Task ExecuteShowSettingsCommand()
        {
            var navPage = new NavigationPage(
                new SettingsPage() { BindingContext = new SettingsViewModel() })
            {
                BarBackgroundColor = Color.FromHex("547799")
            };

            navPage.BarTextColor = Color.White;

            await NavigationService.PushModalAsync(navPage);
        }

        Command _DialNumberCommand;

        /// <summary>
        /// Command to dial acquaintance phone number
        /// </summary>
        public Command DialNumberCommand
        {
            get
            {
                return _DialNumberCommand ??
                (_DialNumberCommand = new Command((parameter) =>
                        ExecuteDialNumberCommand((string)parameter)));
            }
        }

        void ExecuteDialNumberCommand(string acquaintanceId)
        {
            if (string.IsNullOrWhiteSpace(acquaintanceId))
                return;

            var acquaintance = SourceItems.SingleOrDefault(c => c._id == acquaintanceId);

            IPhoneForSchema acquaintanceWithPhone = acquaintance as IPhoneForSchema;
            if (acquaintanceWithPhone == null)
                return;

            if (_CapabilityService.CanMakeCalls)
            {
                var phoneCallTask = MessagingPlugin.PhoneDialer;
                if (phoneCallTask.CanMakePhoneCall)
                    phoneCallTask.MakePhoneCall(acquaintanceWithPhone.Phone.SanitizePhoneNumber());
            }
            else
            {
                MessagingService.Current.SendMessage<MessagingServiceAlert>(MessageKeys.DisplayAlert, new MessagingServiceAlert()
                {
                    Title = "Simulator Not Supported",
                    Message = "Phone calls are not supported in the iOS simulator.",
                    Cancel = "OK"
                });
            }
        }

        Command _MessageNumberCommand;

        /// <summary>
        /// Command to message acquaintance phone number
        /// </summary>
        public Command MessageNumberCommand
        {
            get
            {
                return _MessageNumberCommand ??
                (_MessageNumberCommand = new Command((parameter) =>
                        ExecuteMessageNumberCommand((string)parameter)));
            }
        }

        void ExecuteMessageNumberCommand(string acquaintanceId)
        {
            if (string.IsNullOrWhiteSpace(acquaintanceId))
                return;

            var acquaintance = SourceItems.SingleOrDefault(c => c._id == acquaintanceId);

            IPhoneForSchema acquaintanceWithPhone = acquaintance as IPhoneForSchema;
            if (acquaintanceWithPhone == null)
                return;

            if (_CapabilityService.CanSendMessages)
            {
                var messageTask = MessagingPlugin.SmsMessenger;
                if (messageTask.CanSendSms)
                    messageTask.SendSms(acquaintanceWithPhone.Phone.SanitizePhoneNumber());
            }
            else
            {
                MessagingService.Current.SendMessage<MessagingServiceAlert>(MessageKeys.DisplayAlert, new MessagingServiceAlert()
                {
                    Title = "Simulator Not Supported",
                    Message = "Messaging is not supported in the iOS simulator.",
                    Cancel = "OK"
                });
            }
        }

        Command _EmailCommand;

        /// <summary>
        /// Command to email acquaintance
        /// </summary>
        public Command EmailCommand
        {
            get
            {
                return _EmailCommand ??
                (_EmailCommand = new Command((parameter) =>
                        ExecuteEmailCommand((string)parameter)));
            }
        }

        void ExecuteEmailCommand(string acquaintanceId)
        {
            if (string.IsNullOrWhiteSpace(acquaintanceId))
                return;

            var acquaintance = SourceItems.SingleOrDefault(c => c._id == acquaintanceId);
            IEmailForSchema acquaintanceWithEmail = acquaintance as IEmailForSchema;

            if (acquaintanceWithEmail == null)
                return;

            if (_CapabilityService.CanSendEmail)
            {
                var emailTask = MessagingPlugin.EmailMessenger;
                if (emailTask.CanSendEmail)
                    emailTask.SendEmail(acquaintanceWithEmail.Email);
            }
            else
            {
                MessagingService.Current.SendMessage<MessagingServiceAlert>(MessageKeys.DisplayAlert, new MessagingServiceAlert()
                {
                    Title = "Simulator Not Supported",
                    Message = "Email composition is not supported in the iOS simulator.",
                    Cancel = "OK"
                });
            }
        }
        /// <summary>
        /// Command to load Items
        /// </summary>
        public Command LoadAcquaintancesCommand
        {
            get { return _LoadAcquaintancesCommand ?? (_LoadAcquaintancesCommand = new Command(async () => await ExecuteLoadAcquaintancesCommand())); }
        }

        public override async Task ExecuteLoadAcquaintancesCommand()
        {
            LoadAcquaintancesCommand.ChangeCanExecute();

            //if (Settings.LocalDataResetIsRequested)
            //    Items.Clear();

            //if (Items.Count < 1 || !Settings.DataIsSeeded || Settings.ClearImageCacheIsRequested)
            await FetchAcquaintances();

            LoadAcquaintancesCommand.ChangeCanExecute();
        }

        public Command RefreshAcquaintancesCommand
        {
            get
            {
                return _RefreshAcquaintancesCommand ?? (_RefreshAcquaintancesCommand = new Command(async () => await ExecuteRefreshAcquaintancesCommandCommand()));
            }
        }

        async Task ExecuteRefreshAcquaintancesCommandCommand()
        {
            RefreshAcquaintancesCommand.ChangeCanExecute();

            await FetchAcquaintances();

            RefreshAcquaintancesCommand.ChangeCanExecute();
        }

        protected async Task FetchAcquaintances()
        {
            IsBusy = true;
            await LoadDataAsync();
            //Items = new ObservableRangeCollection<ItemViewModel>(await DataSource.GetItems());

            // ensuring that this flag is reset
            Settings.ClearImageCacheIsRequested = false;

            IsBusy = false;
        }



        /// <summary>
        /// Subscribes to "AddAcquaintance" messages
        /// </summary>
        void SubscribeToAddAcquaintanceMessages()
        {
            MessagingService.Current.Subscribe<TSchema>(MessageKeys.AddAcquaintance, async (service, acquaintance) =>
            {
                IsBusy = true;

                //await DataSource.AddItem(acquaintance);

                await FetchAcquaintances();

                IsBusy = false;
            });
        }

        /// <summary>
        /// Subscribes to "UpdateAcquaintance" messages
        /// </summary>
        void SubscribeToUpdateAcquaintanceMessages()
        {
            MessagingService.Current.Subscribe<TSchema>(MessageKeys.UpdateAcquaintance, async (service, acquaintance) =>
            {
                IsBusy = true;

                //await DataSource.UpdateItem(acquaintance);

                await FetchAcquaintances();

                IsBusy = false;
            });
        }

        /// <summary>
        /// Subscribes to "DeleteAcquaintance" messages
        /// </summary>
        void SubscribeToDeleteAcquaintanceMessages()
        {
            MessagingService.Current.Subscribe<TSchema>(MessageKeys.DeleteAcquaintance, async (service, acquaintance) =>
            {
                IsBusy = true;

                //await DataSource.RemoveItem(acquaintance);

                await FetchAcquaintances();

                IsBusy = false;
            });
        }
#endregion
        public override async Task LoadDataAsync(bool forceRefresh = false, SchemaBase connected = null)
        {
            try
            {
                _connected = connected;
                HasLoadDataErrors = false;
                IsBusy = true;

                var loaderSettings = LoaderSettings.FromSection(_section, GetCacheKey(connected), forceRefresh);
                var loaderOutcome = await DataLoader.LoadAsync(loaderSettings, () => _section.GetDataAsync(connected), (items) => ParseItems(items));

                LastUpdated = loaderOutcome.Timestamp;
                _isDataProviderInitialized = loaderOutcome.IsFreshData;
                _isFirstPage = true;
            }
            catch (Exception ex)
            {
                HasLoadDataErrors = true;
                Debug.WriteLine(ex.ToString());
            }
            finally
            {
                IsBusy = false;
            }
        }

        public override async Task LoadNextPageAsync()
        {
            try
            {
                HasLoadDataErrors = false;
                IsLoadingNextPage = true;

                if (!_isDataProviderInitialized && _isFirstPage)
                {
                    await LoadDataAsync(true, _connected);
                    _isFirstPage = false;
                }

                await DataLoader.LoadAsync(LoaderSettings.NoCache(_section.NeedsNetwork), () => _section.GetNextPageAsync(), (items) => ParseNextPage(items));

                HasMorePages = _section.HasMorePages;
            }
            catch (Exception ex)
            {
                HasLoadDataErrors = true;
                Debug.WriteLine(ex.ToString());
            }
            finally
            {
                IsLoadingNextPage = false;
            }
        }

        public override async Task SearchDataAsync(string searchTerm)
        {
            if (!string.IsNullOrEmpty(searchTerm))
            {
                try
                {
                    HasLoadDataErrors = false;
                    IsBusy = true;

                    var loaderSettings = LoaderSettings.FromSection(_section, _section.Name, true);
                    var loaderOutcome = await DataLoader.LoadAsync(loaderSettings, () => _section.GetDataAsync(), (items) => ParseItems(items, i => i.ContainsString(searchTerm)));
                    LastUpdated = loaderOutcome.Timestamp;
                }
                catch (Exception ex)
                {
                    HasLoadDataErrors = true;
                    Debug.WriteLine(ex.ToString());
                }
                finally
                {
                    IsBusy = false;
                }
            }
        }

        public override async Task FilterDataAsync(List<string> itemsId)
        {
            if (itemsId != null && itemsId.Any())
            {
                try
                {
                    HasLoadDataErrors = false;
                    IsBusy = true;

                    var loaderSettings = LoaderSettings.FromSection(_section, _section.Name, true);
                    var loaderOutcome = await DataLoader.LoadAsync(loaderSettings, () => _section.GetDataAsync(), (items) => ParseItems(items, i => itemsId.Contains(i.Id)));
                    LastUpdated = loaderOutcome.Timestamp;
                }
                catch (Exception ex)
                {
                    HasLoadDataErrors = true;
                    Debug.WriteLine(ex.ToString());
                }
                finally
                {
                    IsBusy = false;
                }
            }
        }

        private void ParseItems(IEnumerable<TSchema> content, Func<ItemViewModel, bool> filterFunc = null)
        { 
            SourceItems.Clear();
            SourceItems.AddRange(content);

            var parsedItems = new List<ItemViewModel>();
            foreach (var item in GetVisibleItems(content, _visibleItems))
            {
                var parsedItem = new ItemViewModel
                {
                    Id = item._id,
                    NavInfo = _section.ListPage.DetailNavigation(item)
                };

                _section.ListPage.LayoutBindings(parsedItem, item);

                if (filterFunc == null || filterFunc(parsedItem))
                {
                    parsedItems.Add(parsedItem);
                }
            }
            Items.Sync(parsedItems);

            HasItems = Items.Count > 0;
            HasMoreItems = content.Count() > Items.Count;
        }

        private void ParseNextPage(IEnumerable<TSchema> content)
        {
            SourceItems.AddRange(content);

            foreach (var item in content)
            {
                var parsedItem = new ItemViewModel
                {
                    Id = item._id,
                    NavInfo = _section.ListPage.DetailNavigation(item)
                };
                _section.ListPage.LayoutBindings(parsedItem, item);

                Items.Add(parsedItem);
            }
        }

        private IEnumerable<TSchema> GetVisibleItems(IEnumerable<TSchema> content, int visibleItems)
        {
            if (visibleItems == 0)
            {
                return content;
            }
            else
            {
                return content.Take(visibleItems);
            }
        }

        private string GetCacheKey(SchemaBase connectedItem)
        {
            if (connectedItem == null)
            {
                return _section.Name;
            }
            else
            {
                return $"{_section.Name}_{connectedItem._id}";
            }
        }
    }
}
*/