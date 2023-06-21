using System;
using System.ComponentModel;

using Android.Content;
using Android.Runtime;
using Android.Widget;

using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

using ZXing.Mobile;
using ZXing.Net.Mobile.Forms;
using ZXing.Net.Mobile.Forms.Android;

[assembly: ExportRenderer(typeof(ZXingBarcodeImageView), typeof(ZXingBarcodeImageViewRenderer))]
namespace ZXing.Net.Mobile.Forms.Android
{
    [Preserve(AllMembers = true)]
	public class ZXingBarcodeImageViewRenderer : ViewRenderer<ZXingBarcodeImageView, ImageView>
	{
        public static void Init()
        {
            var _ = DateTime.Now;
        }

        ImageView _imageView;

        ZXingBarcodeImageView FormsView => Element;

        public ZXingBarcodeImageViewRenderer(Context context) : base(context) { }

        protected override void OnElementChanged(ElementChangedEventArgs<ZXingBarcodeImageView> e)
        {
            if (FormsView != null && _imageView == null)
            {
                _imageView = new ImageView(Context);

                SetNativeControl(_imageView);
            }

            Regenerate();

            base.OnElementChanged(e);
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(ZXingBarcodeImageView.BarcodeValue)
				|| e.PropertyName == nameof(ZXingBarcodeImageView.BarcodeOptions)
				|| e.PropertyName == nameof(ZXingBarcodeImageView.BarcodeFormat))
				Regenerate();

			base.OnElementPropertyChanged(sender, e);
		}

		void Regenerate()
		{
			BarcodeWriter writer = null;
			string barcodeValue = null;

			if (FormsView != null
				&& !string.IsNullOrWhiteSpace(FormsView.BarcodeValue)
				&& FormsView.BarcodeFormat != BarcodeFormat.All_1D)
			{
				barcodeValue = FormsView.BarcodeValue;
				writer = new BarcodeWriter { Format = FormsView.BarcodeFormat };

				if (FormsView != null && FormsView.BarcodeOptions != null)
					writer.Options = FormsView.BarcodeOptions;
			}

			// Update or clear out the image depending if we had enough info
			// to instantiate the barcode writer, otherwise null the image
			Device.BeginInvokeOnMainThread(() =>
			{
				try
				{
					var image = writer?.Write(barcodeValue);

					_imageView?.SetImageBitmap(image);
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Failed to update image: {ex}");
				}
			});
		}
	}
}