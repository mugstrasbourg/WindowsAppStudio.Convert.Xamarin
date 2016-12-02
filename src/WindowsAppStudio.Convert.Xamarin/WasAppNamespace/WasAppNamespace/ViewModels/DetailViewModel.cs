using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
#if UWP
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using AppStudio.Uwp;
using AppStudio.Uwp.Actions;
using AppStudio.Uwp.Cache;
using AppStudio.Uwp.Commands;
using AppStudio.Uwp.DataSync;
using AppStudio.Uwp.Navigation;
#else
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Acquaint.Data;
using Acquaint.Abstractions;
using Acquaint.Util;
using FormsToolkit;
using Plugin.ExternalMaps;
using Plugin.Messaging;
using Xamarin.Forms.Maps;
using AppStudio.Xamarin;
using AppStudio.Xamarin.Actions;
using AppStudio.Xamarin.Cache;
using AppStudio.Xamarin.Commands;
using AppStudio.Xamarin.DataSync;
using AppStudio.Xamarin.Navigation;
#endif

using AppStudio.DataProviders;

using WasAppNamespace.Sections;
using WasAppNamespace.Services;
using WasAppNamespace.Navigation;
using WasAppNamespace.Pages;


namespace WasAppNamespace.ViewModels
{
    /// <summary>
    /// From AppStudio AND from Acquaint!
    /// From AppStudio but disabled (so, enabled only for UWP):
    ///  - ZoomMode
    ///  - SlideShowTimer
    ///  - FullScreenCommand
    ///  - ShowPresentationCommand
    ///  - ShareContent
    ///  - OnExitFullScreen
    ///  - OnEnterFullScreen
    /// </summary>
    public abstract class DetailViewModel : PageViewModelBase
    {
        private bool _showInfoLastValue;

        public abstract Task LoadStateAsync(NavDetailParameter detailParameter);

        public DetailViewModel(string title, string sectionName)
        {
            Title = title;
            SectionName = sectionName;

            ShowInfo = true;
            FontSize = GetFontSize();
#if UWP
            ZoomMode = ZoomMode.Disabled;
            if (!Windows.ApplicationModel.DesignMode.DesignModeEnabled)
            {
                ShellPage.Current.ShellControl.OnExitFullScreen += OnExitFullScreen;
                ShellPage.Current.ShellControl.OnEnterFullScreen += OnEnterFullScreen;
            }
#endif
        }

        public ObservableCollection<ComposedItemViewModel> Items { get; protected set; } = new ObservableCollection<ComposedItemViewModel>();

#region SelectedItem
        private ComposedItemViewModel _selectedItem;

        public ComposedItemViewModel SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                SetProperty(ref _selectedItem, value);
                UpdateInformationPanel();
            }
        }
        #endregion

        #region ZoomMode
#if UWP

        private ZoomMode _zoomMode;

        public ZoomMode ZoomMode
        {
            get { return _zoomMode; }
            set { SetProperty(ref _zoomMode, value); }
        }
#endif
        #endregion

        #region FontSize
        private double _fontSize;

        public double FontSize
        {
            get { return _fontSize; }
            set { SetProperty(ref _fontSize, value); }
        }
        #endregion

        #region ShowInfo
        private bool _showInfo;

        public bool ShowInfo
        {
            get { return _showInfo; }
            set { SetProperty(ref _showInfo, value); }
        }
        #endregion


#region SlideShowTimer
#if UWP
        private DispatcherTimer _slideShowTimer;

        public DispatcherTimer SlideShowTimer
        {
            get
            {
                if (_slideShowTimer == null)
                {
                    _slideShowTimer = new DispatcherTimer()
                    {
                        Interval = TimeSpan.FromMilliseconds(1500)
                    };
                    _slideShowTimer.Tick += PresentationTimeEvent;
                }
                return _slideShowTimer;
            }
        }
#endif
#endregion

#region Commands
#if UWP
        public ICommand FullScreenCommand
        {
            get
            {
                return new RelayCommand(async () =>
                {
                    await ShellPage.Current.ShellControl.TryEnterFullScreenAsync();
                });
            }
        }

        public ICommand ShowPresentationCommand
        {
            get
            {
                return new RelayCommand(async () =>
                {
                    if (await ShellPage.Current.ShellControl.TryEnterFullScreenAsync())
                    {
                        ZoomMode = ZoomMode.Disabled;
                        SlideShowTimer.Start();
                    }
                });
            }
        }
#endif

        public ICommand ShowInfoCommand
        {
            get
            {
                return new RelayCommand(() =>
                {
                    ShowInfo = !ShowInfo;
                });
            }
        }


        public ICommand ApplyFontSizeCommand
        {
            get
            {
                return new RelayCommand<string>((fontSizeResourceName) =>
                {
                    FontSize = (double)fontSizeResourceName.Resource();
                    SetFontSize(FontSize);
                });
            }
        }
        #endregion
#if UWP
        public void ShareContent(DataRequest dataRequest, bool supportsHtml = true)
        {
            ShareContent(dataRequest, SelectedItem, supportsHtml);
        }
#endif
        public static double GetFontSize()
        {
#if UWP
            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey("DescriptionFontSize"))
            {
                SetFontSize((double)"DescriptionTextSizeNormal".Resource());
            }
            return (double)ApplicationData.Current.LocalSettings.Values["DescriptionFontSize"];
#else
            if (Settings.DescriptionFontSizeDefault == null)
            {
                object objectValue = null;
                XamApp.Current.Resources.TryGetValue("FontSizes", out objectValue);
                ResourceDictionary fontSizes = objectValue as ResourceDictionary;
                if (fontSizes != null)
                {
                    fontSizes.TryGetValue("DescriptionTextSizeNormal", out objectValue);
                    if (objectValue != null)
                        Settings.DescriptionFontSizeDefault = (double)objectValue;
                }
            }
            return Settings.DescriptionFontSize;
#endif
        }

        public static void SetFontSize(double fontsize)
        {
#if UWP
            ApplicationData.Current.LocalSettings.Values["DescriptionFontSize"] = fontsize;
#else
            Settings.DescriptionFontSize = fontsize;
#endif
        }


        private void UpdateInformationPanel()
        {
            if (SelectedItem != null)
            {
                if (string.IsNullOrEmpty(SelectedItem.Title) && string.IsNullOrEmpty(SelectedItem.Description))
                {
                    ShowInfo = false;
                }
            }
        }

        #region Events
#if UWP
        private void OnExitFullScreen(object sender, EventArgs e)
        {
            this.ShowInfo = _showInfoLastValue;
            SlideShowTimer.Stop();
            ZoomMode = ZoomMode.Disabled;
        }

        private void OnEnterFullScreen(object sender, EventArgs e)
        {
            _showInfoLastValue = this.ShowInfo;
            ShowInfo = false;
            ZoomMode = ZoomMode.Enabled;
        }
#endif
        private void PresentationTimeEvent(object sender, object e)
        {
            if (Items != null && Items.Count > 1 && SelectedItem != null)
            {
                var index = Items.IndexOf(SelectedItem);
                if (index < Items.Count - 1)
                {
                    index++;
                }
                else
                {
                    index = 0;
                }
                SelectedItem = Items[index];
            }
        }
        #endregion

        #region From Acquaint
        public SchemaBase SourceItem { set; get; }
        public bool HasEmailAddress => !string.IsNullOrWhiteSpace((SourceItem as IEmailForSchema)?.Email);

        public bool HasPhoneNumber => !string.IsNullOrWhiteSpace((SourceItem as IPhoneForSchema)?.Phone);

        public bool HasAddress => !string.IsNullOrWhiteSpace((SourceItem as IAddressForSchema)?.AddressString);

        // this is just a utility service that we're using in this demo app to mitigate some limitations of the iOS simulator
        readonly ICapabilityService _CapabilityService;

        readonly Geocoder _Geocoder;

        /*
        Command _EditAcquaintanceCommand;

        public Command EditAcquaintanceCommand
        {
            get
            {
                return _EditAcquaintanceCommand ??
                    (_EditAcquaintanceCommand = new Command(async () => await ExecuteEditAcquaintanceCommand()));
            }
        }

        async Task ExecuteEditAcquaintanceCommand()
        {
            await NavigationService.PushAsync(new AcquaintanceEditPage() { BindingContext = new AcquaintanceEditViewModel(SourceItem) });
        }

        Command _DeleteAcquaintanceCommand;

        public Command DeleteAcquaintanceCommand => _DeleteAcquaintanceCommand ?? (_DeleteAcquaintanceCommand = new Command(ExecuteDeleteAcquaintanceCommand));

        void ExecuteDeleteAcquaintanceCommand()
        {
            MessagingService.Current.SendMessage<MessagingServiceQuestion>(MessageKeys.DisplayQuestion, new MessagingServiceQuestion()
            {
                Title = $"Delete {(SourceItem as IDisplayNameForSchema)?.DisplayName}?",
                Question = null,
                Positive = "Delete",
                Negative = "Cancel",
                OnCompleted = new Action<bool>(async result => {
                    if (!result) return;

                    // send a message that we want the given acquaintance to be deleted
                    MessagingService.Current.SendMessage<TSchema>(MessageKeys.DeleteAcquaintance, SourceItem);

                    await NavigationService.PopAsync();
                })
            });
        }
        */
                Command _DialNumberCommand;

                public Command DialNumberCommand => _DialNumberCommand ??
                                                    (_DialNumberCommand = new Command(ExecuteDialNumberCommand));

                void ExecuteDialNumberCommand()
                {
                    if (string.IsNullOrWhiteSpace((SourceItem as IPhoneForSchema)?.Phone))
                        return;

                    if (CapabilityService.CanMakeCalls)
                    {
                        var phoneCallTask = MessagingPlugin.PhoneDialer;
                        if (phoneCallTask.CanMakePhoneCall)
                            phoneCallTask.MakePhoneCall(((IPhoneForSchema)SourceItem).Phone.SanitizePhoneNumber());
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

                public Command MessageNumberCommand => _MessageNumberCommand ??
                                                       (_MessageNumberCommand = new Command(ExecuteMessageNumberCommand));

                void ExecuteMessageNumberCommand()
                {
                    if (string.IsNullOrWhiteSpace((SourceItem as IPhoneForSchema)?.Phone))
                        return;

                    if (CapabilityService.CanSendMessages)
                    {
                        var messageTask = MessagingPlugin.SmsMessenger;
                        if (messageTask.CanSendSms)
                            messageTask.SendSms(((IPhoneForSchema)SourceItem).Phone.SanitizePhoneNumber());
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

                public Command EmailCommand => _EmailCommand ??
                                               (_EmailCommand = new Command(ExecuteEmailCommandCommand));

                void ExecuteEmailCommandCommand()
                {
                    if (string.IsNullOrWhiteSpace((SourceItem as IEmailForSchema)?.Email))
                        return;

                    if (CapabilityService.CanSendEmail)
                    {
                        var emailTask = MessagingPlugin.EmailMessenger;
                        if (emailTask.CanSendEmail)
                            emailTask.SendEmail(((IEmailForSchema)SourceItem).Email);
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

                Command _GetDirectionsCommand;

                public Command GetDirectionsCommand
                {
                    get
                    {
                        return _GetDirectionsCommand ??
                        (_GetDirectionsCommand = new Command(async () =>
                                await ExecuteGetDirectionsCommand()));
                    }
                }

                public ICapabilityService CapabilityService => _CapabilityService;

                async Task ExecuteGetDirectionsCommand()
                {
                    var position = await GetPosition();

                    var pin = new Pin() { Position = position };

                    await CrossExternalMaps.Current.NavigateTo(pin.Label, pin.Position.Latitude, pin.Position.Longitude, Plugin.ExternalMaps.Abstractions.NavigationType.Driving);
                }

                public void SetupMap()
                {
                    if (HasAddress)
                    {
                        MessagingService.Current.SendMessage(MessageKeys.SetupMap);
                    }
                }

                public void DisplayGeocodingError()
                {
                    //MessagingService.Current.SendMessage<MessagingServiceAlert>(MessageKeys.DisplayAlert, new MessagingServiceAlert()
                    //    {
                    //        Title = "Geocoding Error", 
                    //        Message = "Please make sure the address is valid, or that you have a network connection.",
                    //        Cancel = "OK"
                    //    });

                    IsBusy = false;
                }

                public async Task<Position> GetPosition()
                {
                    if (!HasAddress)
                        return new Position(0, 0);

                    IsBusy = true;

                    Position p;

                    p = (await _Geocoder.GetPositionsForAddressAsync((SourceItem as IAddressForSchema)?.AddressString)).FirstOrDefault();

                    // The Android geocoder (the underlying implementation in Android itself) fails with some addresses unless they're rounded to the hundreds.
                    // So, this deals with that edge case.
                    if (p.Latitude == 0 && p.Longitude == 0 && AddressBeginsWithNumber((SourceItem as IAddressForSchema)?.AddressString) && Device.OS == TargetPlatform.Android)
                    {
                        var roundedAddress = GetAddressWithRoundedStreetNumber((SourceItem as IAddressForSchema)?.AddressString);

                        p = (await _Geocoder.GetPositionsForAddressAsync(roundedAddress)).FirstOrDefault();
                    }

                    IsBusy = false;

                    return p;
                }

                void SubscribeToSaveAcquaintanceMessages()
                {
                    // This subscribes to the "SaveAcquaintance" message
                    MessagingService.Current.Subscribe<SchemaBase>(MessageKeys.UpdateAcquaintance, (service, sourceItem) =>
                    {
                        SourceItem = sourceItem;
                        OnPropertyChanged("Acquaintance");

                        MessagingService.Current.SendMessage<SchemaBase>(MessageKeys.AcquaintanceLocationUpdated, SourceItem);
                    });
                }

                static bool AddressBeginsWithNumber(string address)
                {
                    return !string.IsNullOrWhiteSpace(address) && char.IsDigit(address.ToCharArray().First());
                }

                static string GetAddressWithRoundedStreetNumber(string address)
                {
                    var endingIndex = GetEndingIndexOfNumericPortionOfAddress(address);

                    if (endingIndex == 0)
                        return address;

                    int originalNumber = 0;
                    int roundedNumber = 0;

                    int.TryParse(address.Substring(0, endingIndex + 1), out originalNumber);

                    if (originalNumber == 0)
                        return address;

                    roundedNumber = originalNumber.RoundToLowestHundreds();

                    return address.Replace(originalNumber.ToString(), roundedNumber.ToString());
                }

                static int GetEndingIndexOfNumericPortionOfAddress(string address)
                {
                    int endingIndex = 0;

                    for (int i = 0; i < address.Length; i++)
                    {
                        if (char.IsDigit(address[i]))
                            endingIndex = i;
                        else
                            break;
                    }

                    return endingIndex;
                }
        #endregion

            }

            public class DetailViewModel<TSchema> : DetailViewModel where TSchema : SchemaBase
            {
                private Section<TSchema> _section;

                public DetailViewModel(Section<TSchema> section) : base(section.DetailPage.Title, section.Name)
                {
                    _section = section;
                }

                public override async Task LoadStateAsync(NavDetailParameter detailParameter)
                {
                    try
                    {
                        HasLoadDataErrors = false;
                        IsBusy = true;

                        if (detailParameter != null)
                        {
                            //avoid warning
                            await Task.Run(() => { });

                            ParseItems(detailParameter.Items.OfType<TSchema>(), detailParameter.SelectedId);
                        }
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

                private void ParseItems(IEnumerable<TSchema> items, string selectedId)
                {
                    foreach (var item in items)
                    {
                        var composedItem = new ComposedItemViewModel
                        {
                            Id = item._id
                        };

                        foreach (var binding in _section.DetailPage.LayoutBindings)
                        {
                            var parsedItem = new ItemViewModel
                            {
                                Id = item._id
                            };
                            binding(parsedItem, item);

                            composedItem.Add(parsedItem);
                        }

                        composedItem.Actions = _section.DetailPage.Actions
                                                                    .Select(a => new ActionInfo
                                                                    {
                                                                        Command = a.Command,
                                                                        CommandParameter = a.CommandParameter(item),
                                                                        Style = a.Style,
                                                                        Text = a.Text,
                                                                        ActionType = ActionType.Primary
                                                                    })
                                                                    .ToList();

                        Items.Add(composedItem);
                    }
                    if (!string.IsNullOrEmpty(selectedId))
                    {
                        SelectedItem = Items.FirstOrDefault(i => i.Id == selectedId);
                    }
                }
            }
        }


        /*
        namespace WasAppNamespace.ViewModels
        {
            /// <summary>
            /// From AppStudio AND from Acquaint!
            /// From AppStudio but disabled (so, enabled only for UWP):
            ///  - ZoomMode
            ///  - SlideShowTimer
            ///  - FullScreenCommand
            ///  - ShowPresentationCommand
            ///  - ShareContent
            ///  - OnExitFullScreen
            ///  - OnEnterFullScreen
            /// </summary>
            public abstract class DetailViewModel : PageViewModelBase
            {
                private bool _showInfoLastValue;

                public abstract Task LoadStateAsync(NavDetailParameter detailParameter);

                public DetailViewModel(string title, string sectionName)
                {
                    Title = title;
                    SectionName = sectionName;

                    ShowInfo = true;
                    FontSize = GetFontSize();
        #if UWP
                    ZoomMode = ZoomMode.Disabled;
                    if (!Windows.ApplicationModel.DesignMode.DesignModeEnabled)
                    {
                        ShellPage.Current.ShellControl.OnExitFullScreen += OnExitFullScreen;
                        ShellPage.Current.ShellControl.OnEnterFullScreen += OnEnterFullScreen;
                    }
        #endif
                    _CapabilityService = DependencyService.Get<ICapabilityService>();
                    _Geocoder = new Geocoder();

                    SubscribeToSaveAcquaintanceMessages();
                }

        #region From Acquaint
                public SchemaBase SourceItem { set; get; }
                public bool HasEmailAddress => !string.IsNullOrWhiteSpace((SourceItem as IEmailForSchema)?.Email);

                public bool HasPhoneNumber => !string.IsNullOrWhiteSpace((SourceItem as IPhoneForSchema)?.Phone);

                public bool HasAddress => !string.IsNullOrWhiteSpace((SourceItem as IAddressForSchema)?.AddressString);

                // this is just a utility service that we're using in this demo app to mitigate some limitations of the iOS simulator
                readonly ICapabilityService _CapabilityService;

                readonly Geocoder _Geocoder;

                //Command _EditAcquaintanceCommand;

                //public Command EditAcquaintanceCommand
                //{
                //    get
                //    {
                //        return _EditAcquaintanceCommand ??
                //            (_EditAcquaintanceCommand = new Command(async () => await ExecuteEditAcquaintanceCommand()));
                //    }
                //}

                //async Task ExecuteEditAcquaintanceCommand()
                //{
                //    await NavigationService.PushAsync(new AcquaintanceEditPage() { BindingContext = new AcquaintanceEditViewModel(SourceItem) });
                //}

                //Command _DeleteAcquaintanceCommand;

                //public Command DeleteAcquaintanceCommand => _DeleteAcquaintanceCommand ?? (_DeleteAcquaintanceCommand = new Command(ExecuteDeleteAcquaintanceCommand));

                //void ExecuteDeleteAcquaintanceCommand()
                //{
                //    MessagingService.Current.SendMessage<MessagingServiceQuestion>(MessageKeys.DisplayQuestion, new MessagingServiceQuestion()
                //    {
                //        Title = $"Delete {(SourceItem as IDisplayNameForSchema)?.DisplayName}?",
                //        Question = null,
                //        Positive = "Delete",
                //        Negative = "Cancel",
                //        OnCompleted = new Action<bool>(async result => {
                //            if (!result) return;

                //            // send a message that we want the given acquaintance to be deleted
                //            MessagingService.Current.SendMessage<TSchema>(MessageKeys.DeleteAcquaintance, SourceItem);

                //            await NavigationService.PopAsync();
                //        })
                //    });
                //}
                Command _DialNumberCommand;

                public Command DialNumberCommand => _DialNumberCommand ??
                                                    (_DialNumberCommand = new Command(ExecuteDialNumberCommand));

                void ExecuteDialNumberCommand()
                {
                    if (string.IsNullOrWhiteSpace((SourceItem as IPhoneForSchema)?.Phone))
                        return;

                    if (CapabilityService.CanMakeCalls)
                    {
                        var phoneCallTask = MessagingPlugin.PhoneDialer;
                        if (phoneCallTask.CanMakePhoneCall)
                            phoneCallTask.MakePhoneCall(((IPhoneForSchema)SourceItem).Phone.SanitizePhoneNumber());
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

                public Command MessageNumberCommand => _MessageNumberCommand ??
                                                       (_MessageNumberCommand = new Command(ExecuteMessageNumberCommand));

                void ExecuteMessageNumberCommand()
                {
                    if (string.IsNullOrWhiteSpace((SourceItem as IPhoneForSchema)?.Phone))
                        return;

                    if (CapabilityService.CanSendMessages)
                    {
                        var messageTask = MessagingPlugin.SmsMessenger;
                        if (messageTask.CanSendSms)
                            messageTask.SendSms(((IPhoneForSchema)SourceItem).Phone.SanitizePhoneNumber());
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

                public Command EmailCommand => _EmailCommand ??
                                               (_EmailCommand = new Command(ExecuteEmailCommandCommand));

                void ExecuteEmailCommandCommand()
                {
                    if (string.IsNullOrWhiteSpace((SourceItem as IEmailForSchema)?.Email))
                        return;

                    if (CapabilityService.CanSendEmail)
                    {
                        var emailTask = MessagingPlugin.EmailMessenger;
                        if (emailTask.CanSendEmail)
                            emailTask.SendEmail(((IEmailForSchema)SourceItem).Email);
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

                Command _GetDirectionsCommand;

                public Command GetDirectionsCommand
                {
                    get
                    {
                        return _GetDirectionsCommand ??
                        (_GetDirectionsCommand = new Command(async () =>
                                await ExecuteGetDirectionsCommand()));
                    }
                }

                public ICapabilityService CapabilityService => _CapabilityService;

                async Task ExecuteGetDirectionsCommand()
                {
                    var position = await GetPosition();

                    var pin = new Pin() { Position = position };

                    await CrossExternalMaps.Current.NavigateTo(pin.Label, pin.Position.Latitude, pin.Position.Longitude, Plugin.ExternalMaps.Abstractions.NavigationType.Driving);
                }

                public void SetupMap()
                {
                    if (HasAddress)
                    {
                        MessagingService.Current.SendMessage(MessageKeys.SetupMap);
                    }
                }

                public void DisplayGeocodingError()
                {
                    //MessagingService.Current.SendMessage<MessagingServiceAlert>(MessageKeys.DisplayAlert, new MessagingServiceAlert()
                    //    {
                    //        Title = "Geocoding Error", 
                    //        Message = "Please make sure the address is valid, or that you have a network connection.",
                    //        Cancel = "OK"
                    //    });

                    IsBusy = false;
                }

                public async Task<Position> GetPosition()
                {
                    if (!HasAddress)
                        return new Position(0, 0);

                    IsBusy = true;

                    Position p;

                    p = (await _Geocoder.GetPositionsForAddressAsync((SourceItem as IAddressForSchema)?.AddressString)).FirstOrDefault();

                    // The Android geocoder (the underlying implementation in Android itself) fails with some addresses unless they're rounded to the hundreds.
                    // So, this deals with that edge case.
                    if (p.Latitude == 0 && p.Longitude == 0 && AddressBeginsWithNumber((SourceItem as IAddressForSchema)?.AddressString) && Device.OS == TargetPlatform.Android)
                    {
                        var roundedAddress = GetAddressWithRoundedStreetNumber((SourceItem as IAddressForSchema)?.AddressString);

                        p = (await _Geocoder.GetPositionsForAddressAsync(roundedAddress)).FirstOrDefault();
                    }

                    IsBusy = false;

                    return p;
                }

                void SubscribeToSaveAcquaintanceMessages()
                {
                    // This subscribes to the "SaveAcquaintance" message
                    MessagingService.Current.Subscribe<SchemaBase>(MessageKeys.UpdateAcquaintance, (service, sourceItem) =>
                    {
                        SourceItem = sourceItem;
                        OnPropertyChanged("Acquaintance");

                        MessagingService.Current.SendMessage<SchemaBase>(MessageKeys.AcquaintanceLocationUpdated, SourceItem);
                    });
                }

                static bool AddressBeginsWithNumber(string address)
                {
                    return !string.IsNullOrWhiteSpace(address) && char.IsDigit(address.ToCharArray().First());
                }

                static string GetAddressWithRoundedStreetNumber(string address)
                {
                    var endingIndex = GetEndingIndexOfNumericPortionOfAddress(address);

                    if (endingIndex == 0)
                        return address;

                    int originalNumber = 0;
                    int roundedNumber = 0;

                    int.TryParse(address.Substring(0, endingIndex + 1), out originalNumber);

                    if (originalNumber == 0)
                        return address;

                    roundedNumber = originalNumber.RoundToLowestHundreds();

                    return address.Replace(originalNumber.ToString(), roundedNumber.ToString());
                }

                static int GetEndingIndexOfNumericPortionOfAddress(string address)
                {
                    int endingIndex = 0;

                    for (int i = 0; i < address.Length; i++)
                    {
                        if (char.IsDigit(address[i]))
                            endingIndex = i;
                        else
                            break;
                    }

                    return endingIndex;
                }
        #endregion
                public ObservableCollection<ComposedItemViewModel> Items { get; protected set; } = new ObservableCollection<ComposedItemViewModel>();  

        #region SelectedItem
                private ComposedItemViewModel _selectedItem;

                public ComposedItemViewModel SelectedItem
                {
                    get { return _selectedItem; }
                    set
                    {
                        SetProperty(ref _selectedItem, value);
                        UpdateInformationPanel();
                    }
                }
        #endregion
        #if UWP
        #region ZoomMode
                private ZoomMode _zoomMode;

                public ZoomMode ZoomMode
                {
                    get { return _zoomMode; }
                    set { SetProperty(ref _zoomMode, value); }
                }
        #endregion
        #endif

        #region FontSize
                private double _fontSize;

                public double FontSize
                {
                    get { return _fontSize; }
                    set { SetProperty(ref _fontSize, value); }
                }
        #endregion

        #region ShowInfo
                private bool _showInfo;

                public bool ShowInfo
                {
                    get { return _showInfo; }
                    set { SetProperty(ref _showInfo, value); }
                }
        #endregion


        #if UWP
        #region SlideShowTimer
                private DispatcherTimer _slideShowTimer;

                public DispatcherTimer SlideShowTimer
                {
                    get
                    {
                        if (_slideShowTimer == null)
                        {
                            _slideShowTimer = new DispatcherTimer()
                            {
                                Interval = TimeSpan.FromMilliseconds(1500)
                            };
                            _slideShowTimer.Tick += PresentationTimeEvent;
                        }
                        return _slideShowTimer;
                    }
                }
        #endregion
        #endif

        #region Commands
        #if UWP
                public ICommand FullScreenCommand
                {
                    get
                    {
                        return new RelayCommand(async () =>
                        {
                            await ShellPage.Current.ShellControl.TryEnterFullScreenAsync();
                        });
                    }
                }

                public ICommand ShowPresentationCommand
                {
                    get
                    {
                        return new RelayCommand(async () =>
                        {
                            if (await ShellPage.Current.ShellControl.TryEnterFullScreenAsync())
                            {
                                ZoomMode = ZoomMode.Disabled;
                                SlideShowTimer.Start();
                            }
                        });
                    }
                }
        #endif
                public ICommand ShowInfoCommand
                {
                    get
                    {
                        return new RelayCommand(() =>
                        {
                            ShowInfo = !ShowInfo;
                        });
                    }
                }

                public ICommand ApplyFontSizeCommand
                {
                    get
                    {
                        return new RelayCommand<string>((fontSizeResourceName) =>
                        {
                            FontSize = (double)fontSizeResourceName.Resource();
                            SetFontSize(FontSize);
                        });
                    }
                }
        #endregion
        #if UWP
                public void ShareContent(DataRequest dataRequest, bool supportsHtml = true)
                {
                    ShareContent(dataRequest, SelectedItem, supportsHtml);
                }
        #endif
                public static double GetFontSize()
                {
        #if UWP
                    if (!ApplicationData.Current.LocalSettings.Values.ContainsKey("DescriptionFontSize"))
                    {
                        SetFontSize((double)"DescriptionTextSizeNormal".Resource());
                    }
                    return (double)ApplicationData.Current.LocalSettings.Values["DescriptionFontSize"];
        #else
                    if (Settings.DescriptionFontSizeDefault == null)
                    {
                        object defaultValue = null;
                        XamApp.Current.Resources.TryGetValue("DescriptionTextSizeNormal", out defaultValue);
                        Settings.DescriptionFontSizeDefault = (double)defaultValue;
                    }
                    return Settings.DescriptionFontSize;
        #endif
                }

                public static void SetFontSize(double fontsize)
                {
        #if UWP
                    ApplicationData.Current.LocalSettings.Values["DescriptionFontSize"] = fontsize;
        #else
                    Settings.DescriptionFontSize = fontsize;
        #endif
                }

                private void UpdateInformationPanel()
                {
                    if (SelectedItem != null)
                    {
                        if (string.IsNullOrEmpty(SelectedItem.Title) && string.IsNullOrEmpty(SelectedItem.Description))
                        {
                            ShowInfo = false;
                        }
                    }
                }

        #region Events
        #if UWP
                private void OnExitFullScreen(object sender, EventArgs e)
                {
                    this.ShowInfo = _showInfoLastValue;
                    SlideShowTimer.Stop();
                    ZoomMode = ZoomMode.Disabled;
                }

                private void OnEnterFullScreen(object sender, EventArgs e)
                {
                    _showInfoLastValue = this.ShowInfo;
                    ShowInfo = false;
                    ZoomMode = ZoomMode.Enabled;
                }
        #endif
                private void PresentationTimeEvent(object sender, object e)
                {
                    if (Items != null && Items.Count > 1 && SelectedItem != null)
                    {
                        var index = Items.IndexOf(SelectedItem);
                        if (index < Items.Count - 1)
                        {
                            index++;
                        }
                        else
                        {
                            index = 0;
                        }
                        SelectedItem = Items[index];
                    }
                }
        #endregion
            }

            public class DetailViewModel<TSchema> : DetailViewModel where TSchema : SchemaBase
            {
                private Section<TSchema> _section;

                public DetailViewModel(Section<TSchema> section)
                    : base(section.DetailPage.Title, section.Name)
                {
                    _section = section;
                }

            public override async Task LoadStateAsync(NavDetailParameter detailParameter)
                {
                    try
                    {
                        HasLoadDataErrors = false;
                        IsBusy = true;

                        if (detailParameter != null)
                        {
                            //avoid warning
                            await Task.Run(() => { });

                            ParseItems(detailParameter.Items.OfType<TSchema>(), detailParameter.SelectedId);
                        }
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

                private void ParseItems(IEnumerable<TSchema> items, string selectedId)
                {
                    foreach (var item in items)
                    {
                        var composedItem = new ComposedItemViewModel
                        {
                            Id = item._id
                        };

                        foreach (var binding in _section.DetailPage.LayoutBindings)
                        {
                            var parsedItem = new ItemViewModel
                            {
                                Id = item._id
                            };
                            binding(parsedItem, item);

                            composedItem.Add(parsedItem);
                        }

                        composedItem.Actions = _section.DetailPage.Actions
                                                                    .Select(a => new ActionInfo
                                                                    {
                                                                        Command = a.Command,
                                                                        CommandParameter = a.CommandParameter(item),
                                                                        Style = a.Style,
                                                                        Text = a.Text,
                                                                        ActionType = ActionType.Primary
                                                                    })
                                                                    .ToList();

                        Items.Add(composedItem);
                    }
                    if (!string.IsNullOrEmpty(selectedId))
                    {
                        SelectedItem = Items.FirstOrDefault(i => i.Id == selectedId);
                    }
                }
            }
        }
        */
