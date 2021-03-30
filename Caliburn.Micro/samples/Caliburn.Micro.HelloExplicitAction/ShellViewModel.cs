using System.Windows;

namespace Caliburn.Micro.HelloExplicitAction
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

        public bool CanSayHello => !string.IsNullOrEmpty(Name);

        public void SayHello()
        {
            MessageBox.Show($"Hello {Name}.");
        }
    }
}