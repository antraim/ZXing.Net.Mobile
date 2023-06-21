using System;

using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Views;

using AndroidX.Fragment.App;

namespace ZXing.Mobile
{
    [Activity(Label = "Scanner", ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.KeyboardHidden | ConfigChanges.ScreenLayout)]
	public class ZxingActivity : FragmentActivity
	{
		public static readonly string[] RequiredPermissions = new[] {
			Android.Manifest.Permission.Camera
		};

		public static Action<ZXing.Result> ScanCompletedHandler;
		public static Action CanceledHandler;

		public static Action CancelRequestedHandler;
		public static Action AutoFocusRequestedHandler;
		public static Action PauseAnalysisHandler;
		public static Action ResumeAnalysisHandler;

		public static void RequestCancel()
			=> CancelRequestedHandler?.Invoke();

		public static void RequestAutoFocus()
			=> AutoFocusRequestedHandler?.Invoke();

		public static void RequestPauseAnalysis()
			=> PauseAnalysisHandler?.Invoke();

		public static void RequestResumeAnalysis()
			=> ResumeAnalysisHandler?.Invoke();

		public static View CustomOverlayView { get; set; }

		public static bool UseCustomOverlayView { get; set; }

		public static MobileBarcodeScanningOptions ScanningOptions { get; set; }

		public static string TopText { get; set; }

		public static string BottomText { get; set; }

		public static bool ScanContinuously { get; set; }

		ZXingScannerFragment scannerFragment;

		protected override void OnCreate(Bundle bundle)
		{
			base.OnCreate(bundle);

			RequestWindowFeature(WindowFeatures.NoTitle);

			Window.AddFlags(WindowManagerFlags.Fullscreen); //to show
			Window.AddFlags(WindowManagerFlags.KeepScreenOn); //Don't go to sleep while scanning

			if (ScanningOptions.AutoRotate.HasValue && !ScanningOptions.AutoRotate.Value)
				RequestedOrientation = ScreenOrientation.Nosensor;

			SetContentView(ZXing.Net.Mobile.Resource.Layout.zxingscanneractivitylayout);

			scannerFragment = new ZXingScannerFragment();
			scannerFragment.CustomOverlayView = CustomOverlayView;
			scannerFragment.UseCustomOverlayView = UseCustomOverlayView;
			scannerFragment.TopText = TopText;
			scannerFragment.BottomText = BottomText;

			SupportFragmentManager.BeginTransaction()
				.Replace(ZXing.Net.Mobile.Resource.Id.contentFrame, scannerFragment, "ZXINGFRAGMENT")
				.Commit();

			CancelRequestedHandler = CancelScan;
			AutoFocusRequestedHandler = AutoFocus;
			PauseAnalysisHandler = scannerFragment.PauseAnalysis;
			ResumeAnalysisHandler = scannerFragment.ResumeAnalysis;
		}

		protected override async void OnResume()
		{
			base.OnResume();


			StartScanning();
		}

		public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
			=> Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

		void StartScanning()
		{
			scannerFragment.StartScanning(result =>
			{
				ScanCompletedHandler?.Invoke(result);

				if (!ZxingActivity.ScanContinuously)
					Finish();
			}, ScanningOptions);
		}

		public override void OnConfigurationChanged(Android.Content.Res.Configuration newConfig)
		{
			base.OnConfigurationChanged(newConfig);

			Android.Util.Log.Debug(MobileBarcodeScanner.TAG, "Configuration Changed");
		}

		public void AutoFocus()
			=> scannerFragment.AutoFocus();

		public void CancelScan()
		{
			Finish();
			CanceledHandler?.Invoke();
		}

		public override bool OnKeyDown(Keycode keyCode, KeyEvent e)
		{
			switch (keyCode)
			{
				case Keycode.Back:
					CancelScan();
					break;
				case Keycode.Focus:
					return true;
			}

			return base.OnKeyDown(keyCode, e);
		}
	}
}