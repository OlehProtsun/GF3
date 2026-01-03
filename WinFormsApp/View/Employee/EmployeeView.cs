using DataAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinFormsApp.ViewModel;
using WinFormsApp.View.Shared;

namespace WinFormsApp.View.Employee
{
    public partial class EmployeeView : Form, IEmployeeView
    {
        public EmployeeView()
        {
            InitializeComponent();
            _busyController = new BusyOverlayController(this);
            ConfigureGrid();
            AssociateAndRaiseViewEvents();
        }
    }
}
