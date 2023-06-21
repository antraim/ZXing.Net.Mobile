using System;
using System.Collections.Generic;

using Xamarin.Forms;

namespace Sample.Forms
{
    public partial class App : Application
	{
		public App()
		{
			InitializeComponent();

			MainPage = new NavigationPage(new HomePage { Title = "ZXing.Net.Mobile" });
		}

		protected override void OnStart()
		{
		}

		protected override void OnSleep()
		{
		}

		protected override void OnResume()
		{
		}

		public void UITestBackdoorScan(string param)
		{
			var expectedFormat = ZXing.BarcodeFormat.QR_CODE;
			Enum.TryParse(param, out expectedFormat);
			var opts = new ZXing.Mobile.MobileBarcodeScanningOptions
			{
				PossibleFormats = new List<ZXing.BarcodeFormat> { expectedFormat }
			};

			System.Diagnostics.Debug.WriteLine("Scanning " + expectedFormat);
		}
	}
}