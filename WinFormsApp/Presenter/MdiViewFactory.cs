using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows.Forms;
using WinFormsApp.View.Availability;
using WinFormsApp.View.Container;
using WinFormsApp.View.Employee;
using WinFormsApp.View.Shop;

namespace WinFormsApp.Presenter
{
    public interface IMdiViewFactory
    {
        Form CreateEmployeeView();
        Form CreateShopView();
        Form CreateAvailabilityView();
        Form CreateContainerView();
    }

    public sealed class MdiViewFactory : IMdiViewFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public MdiViewFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public Form CreateEmployeeView() => _serviceProvider.GetRequiredService<EmployeeView>();

        public Form CreateShopView() => _serviceProvider.GetRequiredService<ShopView>();

        public Form CreateAvailabilityView() => _serviceProvider.GetRequiredService<AvailabilityView>();

        public Form CreateContainerView() => _serviceProvider.GetRequiredService<ContainerView>();
    }
}
