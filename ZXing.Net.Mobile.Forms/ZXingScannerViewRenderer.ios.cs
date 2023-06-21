using System;
using System.ComponentModel;

using Foundation;

using UIKit;

using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

using ZXing.Net.Mobile.Forms;
using ZXing.Net.Mobile.Forms.iOS;

[assembly: ExportRenderer(typeof(ZXingScannerView), typeof(ZXingScannerViewRenderer))]
namespace ZXing.Net.Mobile.Forms.iOS
{
    [Preserve(AllMembers = true)]
	public class ZXingScannerViewRenderer : ViewRenderer<ZXingScannerView, ZXing.Mobile.ZXingScannerView>
	{  
		public static void Init()
		{
			var _ = DateTime.Now;
		}

		ZXingScannerView FormsView => Element;

		protected override async void OnElementChanged(ElementChangedEventArgs<ZXingScannerView> e)
		{
			base.OnElementChanged(e);

			if (e.NewElement != null)
			{
				AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;

				if (Control == null)
				{
					// Process requests for autofocus
					FormsView.AutoFocusRequested += (x, y) =>
					{
						if (Control == null)
							return;

						if (x < 0 && y < 0)
							Control.AutoFocus();
						else
							Control.AutoFocus(x, y);
					};

					var cameraPermission = await Permissions.RequestAsync<Permissions.Camera>();
					if (cameraPermission != PermissionStatus.Granted)
					{
						Console.WriteLine("Missing Camera Permission");
						return;
					}

					var nativeView = new ZXing.Mobile.ZXingScannerView
					{
						UseCustomOverlayView = true,
						AutoresizingMask = AutoresizingMask
					};

					SetNativeControl(nativeView);
				}

				UpdateScanning();
				UpdateAnalysis();
			}
		}

		protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			base.OnElementPropertyChanged(sender, e);

			if (e.PropertyName.Equals(nameof(ZXingScannerView.IsScanning)))
				UpdateScanning();
			else if (e.PropertyName.Equals(nameof(ZXingScannerView.IsAnalyzing)))
				UpdateAnalysis();
		}

		void UpdateScanning()
		{
			if (Control == null)
				return;

			if (FormsView.IsScanning)
				Control.StartScanning(FormsView.RaiseScanResult, FormsView.Options);
			else
				Control.StopScanning();
		}

		void UpdateAnalysis()
		{
			if (Control == null)
				return;

			if (FormsView.IsAnalyzing)
				Control.ResumeAnalysis();
			else
				Control.PauseAnalysis();
		}

		#region

		public override void TouchesEnded(NSSet touches, UIEvent evt)
		{
			base.TouchesEnded(touches, evt);

			Control?.AutoFocus();
		}

		public override void LayoutSubviews()
		{
			base.LayoutSubviews();

			if (Control == null)
				return;

			// Find the best guess at current orientation
			var orientation = UIApplication.SharedApplication.StatusBarOrientation;
			
			if (ViewController != null)
				orientation = ViewController.InterfaceOrientation;

			// Tell the native view to rotate
			Control.DidRotate(orientation);
		}

		#endregion
	}
}