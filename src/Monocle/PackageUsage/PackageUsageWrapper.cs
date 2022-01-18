using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MonocleViewExtension.PackageUsage
{
    public class PackageUsageWrapper : INotifyPropertyChanged
    {
        public string PackageName { get; set; }
        public string PackageVersion { get; set; }
        public int NodeCount { get; set; }


        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
