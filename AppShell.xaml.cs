using TrackPointV.View;
using TrackPointV.View.DBView;
using TrackPointV.View.DBView.CrudView;
namespace TrackPointV
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            
            // Register routes without the // prefix
            Routing.RegisterRoute(nameof(MainPage), typeof(MainPage));
            Routing.RegisterRoute(nameof(RegisterPage), typeof(RegisterPage));


            //DashboardPage
            Routing.RegisterRoute(nameof(DashboardPage), typeof(DashboardPage));
            Routing.RegisterRoute(nameof(UserPage),typeof(UserPage));
            Routing.RegisterRoute(nameof(SalePage), typeof(SalePage));
            Routing.RegisterRoute(nameof(ProductPage), typeof(ProductPage));
            Routing.RegisterRoute(nameof(NewSalePage), typeof(NewSalePage));


            //Crud
            Routing.RegisterRoute(nameof(ProductDetailPage), typeof(ProductDetailPage));
            Routing.RegisterRoute(nameof(UserDetailPage), typeof(UserDetailPage));
            Routing.RegisterRoute(nameof(NewSaleDetailPage), typeof(NewSaleDetailPage));
        }
    }
}
