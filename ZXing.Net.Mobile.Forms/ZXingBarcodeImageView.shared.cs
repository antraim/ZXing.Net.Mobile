using Xamarin.Forms;

using ZXing.Common;

namespace ZXing.Net.Mobile.Forms
{
    public class ZXingBarcodeImageView : Image
    {
        #region BindableProperties

        public static readonly BindableProperty BarcodeFormatProperty =
            BindableProperty.Create(nameof(BarcodeFormat), typeof(BarcodeFormat), typeof(ZXingBarcodeImageView),
                defaultValue: BarcodeFormat.QR_CODE,
                defaultBindingMode: BindingMode.TwoWay);

        public static readonly BindableProperty BarcodeValueProperty =
            BindableProperty.Create(nameof(BarcodeValue), typeof(string), typeof(ZXingBarcodeImageView),
                defaultValue: string.Empty,
                defaultBindingMode: BindingMode.TwoWay);


        public static readonly BindableProperty BarcodeOptionsProperty =
            BindableProperty.Create(nameof(BarcodeOptions), typeof(EncodingOptions), typeof(ZXingBarcodeImageView),
                defaultValue: new EncodingOptions(),
                defaultBindingMode: BindingMode.TwoWay);

        #endregion

        #region Properies

        public BarcodeFormat BarcodeFormat
        {
            get => (BarcodeFormat)GetValue(BarcodeFormatProperty);
            set => SetValue(BarcodeFormatProperty, value);
        }

        public string BarcodeValue
        {
            get => (string)GetValue(BarcodeValueProperty);
            set => SetValue(BarcodeValueProperty, value);
        }

        public EncodingOptions BarcodeOptions
        {
            get => (EncodingOptions)GetValue(BarcodeOptionsProperty);
            set => SetValue(BarcodeOptionsProperty, value);
        }

        #endregion

        public ZXingBarcodeImageView() : base() { }
	}
}