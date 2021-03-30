using System.Windows;

namespace Caliburn.Micro.Hello
{
    public class ShellViewModel : PropertyChangedBase
    {
        string name;

        public string Name
        {
            get => name;
            set
            {
                name = value;
                NotifyOfPropertyChange(() => Name);
                NotifyOfPropertyChange(() => CanSayHello);
            }
        }

        public bool CanSayHello
        {
            get { return !string.IsNullOrEmpty(Name); }
        }

        public void SayHello()
        {
            MessageBox.Show($"Hello {Name}!");
        }
    }
}