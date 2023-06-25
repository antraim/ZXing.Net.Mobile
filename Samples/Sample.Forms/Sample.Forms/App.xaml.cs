using Xamarin.Forms;

namespace Sample.Forms
{
	public partial class App : Application
	{
		public App()
		{
			InitializeComponent();

			MainPage = new NavigationPage(new ZXingScannerView());
		}
	}
}