using System;

using UIKit;

namespace ZXing.Mobile
{
    public interface IScannerViewController
	{
		void Cancel();

		bool ContinuousScanning { get; set; }

		void PauseAnalysis();
		void ResumeAnalysis();

		event Action<ZXing.Result> OnScannedResult;

		MobileBarcodeScanningOptions ScanningOptions { get; set; }
		MobileBarcodeScanner Scanner { get; set; }

		UIViewController AsViewController();
	}
}