using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace RemoteSoccer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LocalLanding : Page
    {
        public LocalLanding()
        {
            this.InitializeComponent();
        }

        private void StartOrJoin(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(MainPage), "local");
        }
    }
}
