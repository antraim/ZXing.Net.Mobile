using System;
using System.Threading.Tasks;

namespace ZXing.Mobile
{
    public partial class MobileBarcodeScanner : MobileBarcodeScannerBase
	{
		public override Task<Result> Scan(MobileBarcodeScanningOptions options)
			=> PlatformScan(options);

		public override void ScanContinuously(MobileBarcodeScanningOptions options, Action<Result> scanHandler)
			=> PlatformScanContinuously(options, scanHandler);

		public override void Cancel()
			=> PlatformCancel();

		public override void AutoFocus()
			=> PlatformAutoFocus();

		public override void PauseAnalysis()
			=> PlatformPauseAnalysis();

		public override void ResumeAnalysis()
			=> PlatformResumeAnalysis();
	}
}