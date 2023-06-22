using System;
using System.Threading.Tasks;

using Android.Views;

using ApxLabs.FastAndroidCamera;

namespace ZXing.Mobile.CameraAccess
{
	public class CameraAnalyzer
	{
		readonly IScannerSessionHost _scannerHost;
		readonly CameraEventsListener _cameraEventListener;
		readonly CameraController _cameraController;

		Task _processingTask;
		DateTime _lastPreviewAnalysis = DateTime.UtcNow;
		bool _wasScanned;
		BarcodeReaderGeneric _barcodeReader;

		public Action<Result> ScanResultCallback { get; set; }

		public bool IsAnalyzing { get; private set; }

		public CameraAnalyzer(SurfaceView surfaceView, IScannerSessionHost scannerHost)
		{
			_scannerHost = scannerHost;
			_cameraEventListener = new CameraEventsListener();
			_cameraController = new CameraController(surfaceView, _cameraEventListener, scannerHost);
		}

		#region

		public void SetupCamera()
		{
			_cameraEventListener.OnPreviewFrameReady += HandleOnPreviewFrameReady;
			_cameraController.SetupCamera();
			_barcodeReader = _scannerHost.ScanningOptions.BuildBarcodeReader();
		}

		public void ShutdownCamera()
		{
			IsAnalyzing = false;

			_cameraEventListener.OnPreviewFrameReady -= HandleOnPreviewFrameReady;
			_cameraController.ShutdownCamera();
		}

		public void RefreshCamera() => _cameraController.RefreshCamera();

		public void AutoFocus() => _cameraController.AutoFocus();

		public void AutoFocus(int x, int y) => _cameraController.AutoFocus(x, y);

		public void ResumeAnalysis() => IsAnalyzing = true;

		public void PauseAnalysis() => IsAnalyzing = false;

		#endregion

		bool CanAnalyzeFrame()
		{
			if (!IsAnalyzing)
				return false;

			//Check and see if we're still processing a previous frame
			// todo: check if we can run as many as possible or mby run two analyzers at once (Vision + ZXing)
			if (_processingTask != null && !_processingTask.IsCompleted)
				return false;

			var elapsedTimeMs = (DateTime.UtcNow - _lastPreviewAnalysis).TotalMilliseconds;

			if (elapsedTimeMs < _scannerHost.ScanningOptions.DelayBetweenAnalyzingFrames)
				return false;

			// Delay a minimum between scans
			if (_wasScanned && elapsedTimeMs < _scannerHost.ScanningOptions.DelayBetweenContinuousScans)
				return false;

			return true;
		}

		void HandleOnPreviewFrameReady(object sender, FastJavaByteArray fastArray)
		{
			if (!CanAnalyzeFrame())
				return;

			_wasScanned = false;
			_lastPreviewAnalysis = DateTime.UtcNow;

			_processingTask = Task.Run(() =>
			{
				try
				{
					DecodeFrame(fastArray);
				}
				catch (Exception ex)
				{
					Android.Util.Log.Error(PerformanceCounter.TAG, "Decode Frame Failed: {0}", ex);
				}
			}).ContinueWith(task =>
			{
				if (task.IsFaulted)
					Android.Util.Log.Info(PerformanceCounter.TAG, "DecodeFrame exception occurs");
			}, TaskContinuationOptions.OnlyOnFaulted);
		}

		void DecodeFrame(FastJavaByteArray fastArray)
		{
			var resolution = _cameraController.CameraResolution;
			var width = resolution.Width;
			var height = resolution.Height;

			var rotate = false;
			var newWidth = width;
			var newHeight = height;

			// use last value for performance gain
			var cDegrees = _cameraController.LastCameraDisplayOrientationDegree;

			if (cDegrees == 90 || cDegrees == 270)
			{
				rotate = true;
				newWidth = height;
				newHeight = width;
			}

			var start = PerformanceCounter.Start();

			// _area.Left, _area.Top, _area.Width, _area.Height);
			LuminanceSource fast = new FastJavaByteArrayYUVLuminanceSource(fastArray, width, height, 0, 0, width, height);

			if (rotate)
				fast = fast.rotateCounterClockwise();

			var result = _barcodeReader.Decode(fast);

			fastArray.Dispose();
			fastArray = null;

			PerformanceCounter.Stop(start,
				$"Decode Time {0} ms (width: {width}, height: {height}, degrees: {cDegrees}, rotate: {rotate})");

			if (result != null)
			{
				_wasScanned = true;

				ScanResultCallback?.Invoke(result);

				Android.Util.Log.Info(PerformanceCounter.TAG, "Barcode Found");
			}
		}
	}
}