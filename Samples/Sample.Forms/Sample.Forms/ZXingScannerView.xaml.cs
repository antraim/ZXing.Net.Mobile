using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using ZXing;
using ZXing.Mobile;

namespace Sample.Forms
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ZXingScannerView : ContentPage
    {
        public ZXingScannerView()
        {
            InitializeComponent();

			toOsSettingsButton.Clicked += (o, e) =>
			{
				Xamarin.Essentials.AppInfo.ShowSettingsUI();
			};

			var options = new MobileBarcodeScanningOptions
			{
				PossibleFormats = new[] { BarcodeFormat.QR_CODE }
			};

			scannerView.Options = options;
			scannerView.IsAnalyzing = false;
			scannerView.OnScanResult += (result) =>
				Device.BeginInvokeOnMainThread(() =>
				{
					scannerView.IsAnalyzing = false;
					scannerView.IsScanning = false;

					scannedValueLabel.Text = result?.Text;
				});
		}

		protected override void OnAppearing()
		{
			base.OnAppearing();

			scannerView.IsScanning = true;
			scannerView.IsAnalyzing = true;
		}

		protected override void OnDisappearing()
		{
			scannerView.IsScanning = false;
			scannerView.IsAnalyzing = false;

			base.OnDisappearing();
		}
	}
}