using Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

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
                }catch (Exception ex){
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
                JoinButton.IsEnabled = false;
                GameName.IsEnabled = false;
                StartButton.IsEnabled = false;
                LoadingText.Text = ex.Message;
                LoadingSpinner.IsActive = false;
            });
        }

        private void Start(object sender, RoutedEventArgs e)
        {
            JoinButton.IsEnabled = false;
            GameName.IsEnabled = false;
            StartButton.IsEnabled = false;
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
                    var handler = await SingleSignalRHandler.GetOrThrow();
                    var res = await handler.Send(new CreateGame(name));
                    if (res.Is1(out var gameCreated))
                    {
                        await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                        CoreDispatcherPriority.Normal,
                        async () =>
                        {
                            (await SingleSignalRHandler.GetOrThrow()).SetOnClosed(null);
                            this.Frame.Navigate(typeof(MainPage), gameCreated.Id);
                        });
                    }
                    else if (res.Is2(out var gameAlreadyExists))
                    {
                        await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                        CoreDispatcherPriority.Normal,
                        () =>
                        {
                            // TODO tell the player the game already exists
                            JoinButton.IsEnabled = true;
                            GameName.IsEnabled = true;
                            StartButton.IsEnabled = true;
                            LoadingText.Text = "Game already exists";
                            LoadingText.Visibility = Visibility.Visible;
                            LoadingSpinner.IsActive = false;
                        });
                    }
                    else if (res.Is3(out var exception)) {
                        await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                        CoreDispatcherPriority.Normal,
                        () =>
                        {
                            // TODO tell the player the game already exists
                            JoinButton.IsEnabled = true;
                            GameName.IsEnabled = true;
                            StartButton.IsEnabled = true;
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
                            JoinButton.IsEnabled = true;
                            GameName.IsEnabled = true;
                            StartButton.IsEnabled = true;
                            LoadingText.Text = ex.Message;
                            LoadingSpinner.IsActive = false;
                        });
                }
            });
        }

        private void Join(object sender, RoutedEventArgs e)
        {
            JoinButton.IsEnabled = false;
            GameName.IsEnabled = false;
            StartButton.IsEnabled = false;
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
                            LoadingText.Text = "Joining Game";
                            LoadingSpinner.IsActive = true;
                        });
                    var res = await (await SingleSignalRHandler.GetOrThrow()).Send(new JoinGame(name));
                    if (res.Is1(out var gameJoined))
                    {
                        await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                        CoreDispatcherPriority.Normal,
                        async () =>
                        {
                            (await SingleSignalRHandler.GetOrThrow()).SetOnClosed(null);
                            this.Frame.Navigate(typeof(MainPage), gameJoined.Id);
                        });
                    }
                    else if (res.Is2(out var gameDoesNotExist))
                    {
                        await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                        CoreDispatcherPriority.Normal,
                        () =>
                        {
                            // TODO tell the player the game already exists
                            JoinButton.IsEnabled = true;
                            GameName.IsEnabled = true;
                            StartButton.IsEnabled = true;
                            LoadingText.Text = "Game does not exist";
                            LoadingText.Visibility = Visibility.Visible;
                            LoadingSpinner.IsActive = false;
                        });
                    }
                    else if (res.Is3(out var exception))
                    {
                        await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                        CoreDispatcherPriority.Normal,
                        () =>
                        {
                            // TODO tell the player the game already exists
                            JoinButton.IsEnabled = true;
                            GameName.IsEnabled = true;
                            StartButton.IsEnabled = true;
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
                            JoinButton.IsEnabled = true;
                            GameName.IsEnabled = true;
                            StartButton.IsEnabled = true;
                            LoadingText.Text = ex.Message;
                            LoadingSpinner.IsActive = false;
                        });
                }
            });
        }
    }
}
