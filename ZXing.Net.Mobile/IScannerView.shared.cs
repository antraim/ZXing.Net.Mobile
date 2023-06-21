using System;

namespace ZXing.Mobile
{
    public interface IScannerView
	{
		void StartScanning(Action<Result> scanResultHandler, MobileBarcodeScanningOptions options = null);
		void StopScanning();

		void PauseAnalysis();
		void ResumeAnalysis();

		void AutoFocus();
		void AutoFocus(int x, int y);
		bool IsAnalyzing { get; }
	}
}