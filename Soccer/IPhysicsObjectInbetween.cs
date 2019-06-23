using Windows.UI.Xaml;

namespace Soccer
{
    public interface IInbetween
    {
        UIElement Element { get; }

        void Update();
    }
}