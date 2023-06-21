using System;
using System.Threading.Tasks;

namespace ZXing.Mobile
{
    public interface IZXingScanner<TOverlayViewType> : IScannerView
	{
		TOverlayViewType CustomOverlayView { get; set; }
		bool UseCustomOverlayView { get; set; }
		string TopText { get; set; }
		string BottomText { get; set; }
	}

	public interface IMobileBarcodeScanner
	{
		Task<Result> Scan(MobileBarcodeScanningOptions options);
		Task<Result> Scan();

		void ScanContinuously(MobileBarcodeScanningOptions options, Action<Result> scanHandler);
		void ScanContinuously(Action<Result> scanHandler);

		void Cancel();

		void AutoFocus();

		void PauseAnalysis();
		void ResumeAnalysis();

		bool UseCustomOverlay { get; }
		string TopText { get; set; }
		string BottomText { get; set; }

		string CancelButtonText { get; set; }
		string FlashButtonText { get; set; }
		string CameraUnsupportedMessage { get; set; }
	}

	public abstract class MobileBarcodeScannerBase : IMobileBarcodeScanner
	{
		public MobileBarcodeScannerBase()
		{
			CancelButtonText = "Cancel";
			FlashButtonText = "Flash";
			CameraUnsupportedMessage = "Unable to start Camera for Scanning";
		}

		public bool UseCustomOverlay { get; set; }
		public string TopText { get; set; }
		public string BottomText { get; set; }
		public string CancelButtonText { get; set; }
		public string FlashButtonText { get; set; }
		public string CameraUnsupportedMessage { get; set; }

		public abstract Task<Result> Scan(MobileBarcodeScanningOptions options);

		public Task<Result> Scan()
			=> Scan(MobileBarcodeScanningOptions.Default);

		public void ScanContinuously(Action<Result> scanHandler)
			=> ScanContinuously(MobileBarcodeScanningOptions.Default, scanHandler);

		public abstract void ScanContinuously(MobileBarcodeScanningOptions options, Action<Result> scanHandler);

		public abstract void Cancel();

		public abstract void AutoFocus();

		public abstract void PauseAnalysis();
		public abstract void ResumeAnalysis();
	}

	public class CancelScanRequestEventArgs : EventArgs
	{
		public CancelScanRequestEventArgs()
			=> Cancel = false;

		public bool Cancel { get; set; }
	}
}