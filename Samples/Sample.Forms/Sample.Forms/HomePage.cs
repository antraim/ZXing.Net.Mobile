using Xamarin.Forms;

namespace Sample.Forms
{
    public class HomePage : ContentPage
	{
		Button buttonGenerateBarcode;

		public HomePage() : base()
		{
			buttonGenerateBarcode = new Button
			{
				Text = "Barcode Generator",
				AutomationId = "barcodeGenerator",
			};
			buttonGenerateBarcode.Clicked += async delegate
			{
				await Navigation.PushAsync(new BarcodePage());
			};

			var stack = new StackLayout();

			stack.Children.Add(buttonGenerateBarcode);

			Content = stack;
		}
	}
}