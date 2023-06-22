using System;

using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Android.Views;

using ZXing.Mobile.CameraAccess;

namespace ZXing.Mobile
{
    public class ZXingSurfaceView : SurfaceView, ISurfaceHolderCallback, IScannerView, IScannerSessionHost
	{
		CameraAnalyzer _cameraAnalyzer;
		bool _addedHolderCallback;
		bool _surfaceCreated;

		public MobileBarcodeScanningOptions ScanningOptions { get; set; }

		public bool IsAnalyzing => _cameraAnalyzer.IsAnalyzing;

		public ZXingSurfaceView(Context context, MobileBarcodeScanningOptions options)
			: base(context)
		{
			ScanningOptions = options ?? new MobileBarcodeScanningOptions();

			Init();
		}

		protected ZXingSurfaceView(IntPtr javaReference, JniHandleOwnership transfer)
			: base(javaReference, transfer) => Init();

		void Init()
		{
			_cameraAnalyzer ??= new CameraAnalyzer(this, this);
			_cameraAnalyzer.ResumeAnalysis();

			if (!_addedHolderCallback)
			{
				Holder?.AddCallback(this);
				Holder?.SetType(SurfaceType.PushBuffers);

				_addedHolderCallback = true;
			}
		}

		public void SurfaceCreated(ISurfaceHolder holder)
		{
			_cameraAnalyzer.SetupCamera();

			_surfaceCreated = true;
		}

		public void SurfaceChanged(ISurfaceHolder holder, Format format, int wx, int hx)
			=> _cameraAnalyzer.RefreshCamera();

		public void SurfaceDestroyed(ISurfaceHolder holder)
		{
			try
			{
				if (_addedHolderCallback)
				{
					Holder?.RemoveCallback(this);
					_addedHolderCallback = false;
				}
			}
			catch { }

			_cameraAnalyzer.ShutdownCamera();
		}

		#region IScannerView

		public void StartScanning(Action<Result> scanResultCallback, MobileBarcodeScanningOptions options = null)
		{
			_cameraAnalyzer.SetupCamera();

			ScanningOptions = options ?? MobileBarcodeScanningOptions.Default;

			_cameraAnalyzer.BarcodeFound = (result) =>
				scanResultCallback?.Invoke(result);
			_cameraAnalyzer.ResumeAnalysis();
		}

		public void StopScanning() => _cameraAnalyzer.ShutdownCamera();

		public void AutoFocus() => _cameraAnalyzer.AutoFocus();

		public void AutoFocus(int x, int y) => _cameraAnalyzer.AutoFocus(x, y);

		public void ResumeAnalysis() => _cameraAnalyzer.ResumeAnalysis();

		public void PauseAnalysis() => _cameraAnalyzer.PauseAnalysis();

		#endregion

		protected override void OnAttachedToWindow()
		{
			base.OnAttachedToWindow();

			Init();
		}

		protected override void OnWindowVisibilityChanged(ViewStates visibility)
		{
			base.OnWindowVisibilityChanged(visibility);

			if (visibility == ViewStates.Visible)
				Init();
		}

		public override bool OnTouchEvent(MotionEvent e)
		{
			var result = base.OnTouchEvent(e);

			switch (e.Action)
			{
				case MotionEventActions.Down:
					return true;
				case MotionEventActions.Up:
					var touchX = e.GetX();
					var touchY = e.GetY();
					AutoFocus((int)touchX, (int)touchY);
					break;
			}

			return result;
		}

		public override void OnWindowFocusChanged(bool hasWindowFocus)
		{
			base.OnWindowFocusChanged(hasWindowFocus);

			if (!hasWindowFocus)
				return;

			//only refresh the camera if the surface has already been created. Fixed #569
			if (_surfaceCreated)
				_cameraAnalyzer.RefreshCamera();
		}
	}
}