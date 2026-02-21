using ToolSnap.Mobile.Pages;

namespace ToolSnap.Mobile
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(TakePage), typeof(TakePage));
            Routing.RegisterRoute(
            nameof(ConfirmOnTakeToolAsignmentPage),
            typeof(ConfirmOnTakeToolAsignmentPage));
        }
    }
}
