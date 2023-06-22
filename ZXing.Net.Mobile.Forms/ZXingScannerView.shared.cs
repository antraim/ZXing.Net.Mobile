using System;
using System.Windows.Input;

using Xamarin.Forms;

using ZXing.Mobile;

namespace ZXing.Net.Mobile.Forms
{
    public class ZXingScannerView : View
	{
        #region BindableProperties

        public static readonly BindableProperty OptionsProperty =
            BindableProperty.Create(nameof(Options), typeof(MobileBarcodeScanningOptions), typeof(ZXingScannerView), MobileBarcodeScanningOptions.Default);

        public static readonly BindableProperty IsScanningProperty =
            BindableProperty.Create(nameof(IsScanning), typeof(bool), typeof(ZXingScannerView), false);

        public static readonly BindableProperty IsAnalyzingProperty =
            BindableProperty.Create(nameof(IsAnalyzing), typeof(bool), typeof(ZXingScannerView), true);

        public static readonly BindableProperty ResultProperty =
            BindableProperty.Create(nameof(Result), typeof(Result), typeof(ZXingScannerView), default(Result));

        public static readonly BindableProperty ScanResultCommandProperty =
            BindableProperty.Create(nameof(ScanResultCommand), typeof(ICommand), typeof(ZXingScannerView), default(ICommand));

        #endregion

        #region Properties

        public MobileBarcodeScanningOptions Options
        {
            get => (MobileBarcodeScanningOptions)GetValue(OptionsProperty);
            set => SetValue(OptionsProperty, value);
        }

        public bool IsScanning
        {
            get => (bool)GetValue(IsScanningProperty);
            set => SetValue(IsScanningProperty, value);
        }

        public bool IsAnalyzing
        {
            get => (bool)GetValue(IsAnalyzingProperty);
            set => SetValue(IsAnalyzingProperty, value);
        }

        public Result Result
        {
            get => (Result)GetValue(ResultProperty);
            set => SetValue(ResultProperty, value);
        }

        public ICommand ScanResultCommand
        {
            get => (ICommand)GetValue(ScanResultCommandProperty);
            set => SetValue(ScanResultCommandProperty, value);
        }

        #endregion

        public delegate void ScanResultDelegate(Result result);
		public event ScanResultDelegate OnScanResult;

		public event Action<int, int> AutoFocusRequested;

		public ZXingScannerView()
		{
			VerticalOptions = LayoutOptions.FillAndExpand;
			HorizontalOptions = LayoutOptions.FillAndExpand;
		}

		public void RaiseScanResult(Result result)
		{
			Result = result;
			OnScanResult?.Invoke(Result);
			ScanResultCommand?.Execute(Result);
		}

		public void AutoFocus() => AutoFocusRequested?.Invoke(-1, -1);

		public void AutoFocus(int x, int y) => AutoFocusRequested?.Invoke(x, y);
	}
}