using System;
using System.ComponentModel;

using Foundation;

using UIKit;

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

		protected ZXingScannerView formsView;
		protected ZXing.Mobile.ZXingScannerView zxingView;

		protected override async void OnElementChanged(ElementChangedEventArgs<ZXingScannerView> e)
		{
			AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;

			formsView = Element;

			if (zxingView == null)
			{
				// Process requests for autofocus
				formsView.AutoFocusRequested += (x, y) =>
				{
					if (zxingView != null)
					{
						if (x < 0 && y < 0)
							zxingView.AutoFocus();
						else
							zxingView.AutoFocus(x, y);
					}
				};

				var cameraPermission = await Xamarin.Essentials.Permissions.RequestAsync<Xamarin.Essentials.Permissions.Camera>();
				if (cameraPermission != Xamarin.Essentials.PermissionStatus.Granted)
				{
					Console.WriteLine("Missing Camera Permission");
					return;
				}

				zxingView = new ZXing.Mobile.ZXingScannerView();
				zxingView.UseCustomOverlayView = true;
				zxingView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;

				base.SetNativeControl(zxingView);

				if (formsView.IsScanning)
					zxingView.StartScanning(formsView.RaiseScanResult, formsView.Options);

				if (!formsView.IsAnalyzing)
					zxingView.PauseAnalysis();
			}

			base.OnElementChanged(e);
		}

		protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			base.OnElementPropertyChanged(sender, e);

			if (zxingView == null)
				return;

			switch (e.PropertyName)
			{
				case nameof(ZXingScannerView.IsScanning):
					if (formsView.IsScanning)
						zxingView.StartScanning(formsView.RaiseScanResult, formsView.Options);
					else
						zxingView.StopScanning();
					break;
				case nameof(ZXingScannerView.IsAnalyzing):
					if (formsView.IsAnalyzing)
						zxingView.ResumeAnalysis();
					else
						zxingView.PauseAnalysis();
					break;
			}
		}

		public override void TouchesEnded(NSSet touches, UIKit.UIEvent evt)
		{
			base.TouchesEnded(touches, evt);

			zxingView?.AutoFocus();
		}

		public override void LayoutSubviews()
		{
			base.LayoutSubviews();

			// Find the best guess at current orientation
			var orientation = UIApplication.SharedApplication.StatusBarOrientation;
			
			if (ViewController != null)
				orientation = ViewController.InterfaceOrientation;

			// Tell the native view to rotate
			zxingView?.DidRotate(orientation);
		}
	}
}