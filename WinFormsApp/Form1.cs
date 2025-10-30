using DataAccessLayer.Models.DataBaseContext;
using Microsoft.EntityFrameworkCore;

namespace WinFormsApp
{
    public partial class Form1 : Form
    {
        public Form1(AppDbContext db)
        {
            InitializeComponent();

        }
    }
}
