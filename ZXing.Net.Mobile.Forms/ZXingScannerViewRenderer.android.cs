using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;

using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

using ZXing.Mobile;
using ZXing.Net.Mobile.Forms;
using ZXing.Net.Mobile.Forms.Android;

using static ZXing.Mobile.MobileBarcodeScanningOptions;

[assembly: ExportRenderer(typeof(ZXingScannerView), typeof(ZXingScannerViewRenderer))]
namespace ZXing.Net.Mobile.Forms.Android
{
    [Preserve(AllMembers = true)]
	public class ZXingScannerViewRenderer : ViewRenderer<ZXingScannerView, ZXing.Mobile.ZXingSurfaceView>
    {
        public static void Init()
        {
            var _ = DateTime.Now;
        }

		ZXingScannerView FormsView => Element;

		public ZXingScannerViewRenderer(Context context) : base(context) { }

		protected override async void OnElementChanged(ElementChangedEventArgs<ZXingScannerView> e)
		{
			base.OnElementChanged(e);

			if (e.NewElement != null)
			{
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

					var nativeView = new ZXing.Mobile.ZXingSurfaceView(Context as Activity, FormsView.Options)
					{
						LayoutParameters = new LayoutParams(LayoutParams.MatchParent, LayoutParams.MatchParent)
					};

					SetNativeControl(nativeView);
				}

				UpdateCameraResolutionSelector();
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

		void UpdateCameraResolutionSelector()
		{
			if (Control == null)
				return;

			if (Control.ScanningOptions == null)
				return;

			Control.ScanningOptions.CameraResolutionSelector = new CameraResolutionSelectorDelegate(SelectLowestResolutionMatchingDisplayAspectRatio);
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

		volatile bool isHandlingTouch = false;

		public override bool OnTouchEvent(MotionEvent e)
		{
			if (Control == null)
			{
				isHandlingTouch = false;

				return base.OnTouchEvent(e);
			}

			if (!isHandlingTouch)
			{
				isHandlingTouch = true;

				try
				{
					var x = e.GetX();
					var y = e.GetY();

					Control.AutoFocus((int)x, (int)y);
				}
				finally
				{
					isHandlingTouch = false;
				}
			}

			return base.OnTouchEvent(e);
		}

		public CameraResolution SelectLowestResolutionMatchingDisplayAspectRatio(List<CameraResolution> availableResolutions)
		{
			CameraResolution result = null;

			//a tolerance of 0.1 should not be visible to the user
			var aspectTolerance = 0.1;
			var displayOrientationHeight = DeviceDisplay.MainDisplayInfo.Orientation == DisplayOrientation.Portrait
				? DeviceDisplay.MainDisplayInfo.Height
				: DeviceDisplay.MainDisplayInfo.Width;
			var displayOrientationWidth = DeviceDisplay.MainDisplayInfo.Orientation == DisplayOrientation.Portrait
				? DeviceDisplay.MainDisplayInfo.Width
				: DeviceDisplay.MainDisplayInfo.Height;

			//calculatiing our targetRatio
			var targetRatio = displayOrientationHeight / displayOrientationWidth;
			var targetHeight = displayOrientationHeight;
			var minDiff = double.MaxValue;

			//camera API lists all available resolutions from highest to lowest, perfect for us
			//making use of this sorting, following code runs some comparisons to select the lowest resolution that matches the screen aspect ratio and lies within tolerance
			//selecting the lowest makes Qr detection actual faster most of the time
			foreach (var r in availableResolutions.Where(r => Math.Abs(((double)r.Width / r.Height) - targetRatio) < aspectTolerance))
			{
				//slowly going down the list to the lowest matching solution with the correct aspect ratio
				if (Math.Abs(r.Height - targetHeight) < minDiff)
					minDiff = Math.Abs(r.Height - targetHeight);

				result = r;
			}

			return result;
		}

		#endregion
	}
}