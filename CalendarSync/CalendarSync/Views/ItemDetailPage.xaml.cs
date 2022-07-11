using CalendarSync.ViewModels;
using System.ComponentModel;
using Xamarin.Forms;

namespace CalendarSync.Views
{
    public partial class ItemDetailPage : ContentPage
    {
        public ItemDetailPage()
        {
            InitializeComponent();
            BindingContext = new ItemDetailViewModel();
        }
    }
}