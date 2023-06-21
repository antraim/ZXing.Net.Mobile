using System;
using System.ComponentModel;

using Foundation;

using UIKit;

using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

using ZXing.Mobile;
using ZXing.Net.Mobile.Forms;
using ZXing.Net.Mobile.Forms.iOS;

[assembly: ExportRenderer(typeof(ZXingBarcodeImageView), typeof(ZXingBarcodeImageViewRenderer))]
namespace ZXing.Net.Mobile.Forms.iOS
{
    [Preserve(AllMembers = true)]
	public class ZXingBarcodeImageViewRenderer : ViewRenderer<ZXingBarcodeImageView, UIImageView>
	{
		public static void Init()
		{
			var temp = DateTime.Now;
		}

		UIImageView _imageView;

		ZXingBarcodeImageView FormsView => Element;

		protected override void OnElementChanged(ElementChangedEventArgs<ZXingBarcodeImageView> e)
		{
			if (FormsView != null && _imageView == null)
			{
				_imageView = new UIImageView { ContentMode = UIViewContentMode.ScaleAspectFit };

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

					if (_imageView != null)
						_imageView.Image = image;
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Failed to update image: {ex}");
				}
			});
		}
	}
}