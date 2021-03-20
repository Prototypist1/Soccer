using Common;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace RemoteSoccer
{

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LandingPage : Page
    {
        private readonly Task connecting;

        public LandingPage()
        {
            this.InitializeComponent();
            UpdateEnabled();

            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            if (localSettings.Values.TryGetValue(LocalSettingsKeys.GameName, out var gameName))
            {
                GameName.Text = (string)gameName;
                UpdateEnabled();
            }

            connecting = Task.Run(async () =>
            {
                try
                {
                    var handler = await SingleSignalRHandler.GetCreateOrThrow();
                    handler.SetOnClosed(ConnectionLost);
                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                        CoreDispatcherPriority.Normal,
                        () =>
                        {
                            LoadingText.Visibility = Visibility.Collapsed;
                            LoadingSpinner.IsActive = false;
                        });
                }
                catch (Exception ex)
                {
                    await ConnectionLost(ex);
                }
            });
        }

        private async Task ConnectionLost(Exception ex)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
            CoreDispatcherPriority.Normal,
            () =>
            {
                StartOrJoinButton.IsEnabled = false;
                GameName.IsEnabled = false;
                LoadingText.Text = ex.Message;
                LoadingSpinner.IsActive = false;
            });
        }

        private void StartOrJoin(object sender, RoutedEventArgs e)
        {
            StartOrJoinInner();
        }

        private void StartOrJoinInner()
        {
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            localSettings.Values[LocalSettingsKeys.GameName] = GameName.Text;
            StartOrJoinButton.IsEnabled = false;
            GameName.IsEnabled = false;
            var name = GameName.Text;
            Task.Run(async () =>
            {
                try
                {
                    await connecting;
                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                        CoreDispatcherPriority.Normal,
                        () =>
                        {
                            LoadingText.Visibility = Visibility.Visible;
                            LoadingText.Text = "Starting Game";
                            LoadingSpinner.IsActive = true;
                        });
                    var handler = await SingleSignalRHandler.GetOrThrowAsync();
                    var res = await handler.Send(new CreateOrJoinGame(name, FieldDimensions.Default));
                    if (res.Is1(out var gameCreated))
                    {
                        await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                        CoreDispatcherPriority.Normal,
                        async () =>
                        {
                            var handler = (await SingleSignalRHandler.GetOrThrowAsync());
                            handler.SetOnClosed(null);
                            handler.ClearCallBacks();
                            this.Frame.Navigate(typeof(OnlineGame), new GameInfo(gameCreated.Id, Mouse.IsChecked.Value ? ControlScheme.SipmleMouse : ControlScheme.MouseAndKeyboard));
                        });
                    }
                    else if (res.Is2(out var joined))
                    {
                        await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                        CoreDispatcherPriority.Normal,
                        async () =>
                        {
                            var handler = (await SingleSignalRHandler.GetOrThrowAsync());
                            handler.SetOnClosed(null);
                            handler.ClearCallBacks();
                            this.Frame.Navigate(typeof(OnlineGame), new GameInfo(joined.Id, Mouse.IsChecked.Value ? ControlScheme.SipmleMouse : ControlScheme.MouseAndKeyboard));
                        });
                    }
                    else if (res.Is3(out var exception))
                    {
                        await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                        CoreDispatcherPriority.Normal,
                        () =>
                        {
                            StartOrJoinButton.IsEnabled = true;
                            GameName.IsEnabled = true;
                            LoadingText.Text = exception.Message;
                            LoadingText.Visibility = Visibility.Visible;
                            LoadingSpinner.IsActive = false;
                        });
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
                catch (Exception ex)
                {
                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                        CoreDispatcherPriority.Normal,
                        () =>
                        {
                            StartOrJoinButton.IsEnabled = true;
                            GameName.IsEnabled = true;
                            LoadingText.Text = ex.Message;
                            LoadingSpinner.IsActive = false;
                        });
                }
            });
        }

        private void GameName_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateEnabled();
        }

        private void UpdateEnabled()
        {
            StartOrJoinButton.IsEnabled = !string.IsNullOrEmpty(GameName.Text);
        }

        private void GameName_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter && StartOrJoinButton.IsEnabled)
            {
                StartOrJoinInner();
            }
        }
    }
}
