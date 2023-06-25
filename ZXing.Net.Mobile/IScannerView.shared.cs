using System;

namespace ZXing.Mobile
{
	public interface IScannerView
	{
		bool IsAnalyzing { get; }

		void StartScanning(Action<Result> scanResultHandler, MobileBarcodeScanningOptions options = null);
		void StopScanning();

		void AutoFocus();
		void AutoFocus(int x, int y);

		void ResumeAnalysis();
		void PauseAnalysis();
	}
}