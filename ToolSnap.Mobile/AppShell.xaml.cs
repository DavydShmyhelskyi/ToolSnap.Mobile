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
            Routing.RegisterRoute("profile1", typeof(ProfilePage1));
            Routing.RegisterRoute(nameof(ReturnPage), typeof(ReturnPage));
            Routing.RegisterRoute(nameof(ConfirmOnReturnToolAsignmentPage), typeof(ConfirmOnReturnToolAsignmentPage));
            Routing.RegisterRoute(nameof(MapPage), typeof(MapPage));
        }
    }
}
