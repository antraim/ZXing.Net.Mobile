using System;

using UIKit;

namespace ZXing.Mobile
{
	public class UIImageBarcodeReader : BarcodeReader<UIImage>, IBarcodeReader
	{
		static readonly Func<UIImage, LuminanceSource> defaultCreateLuminanceSource =
			(image) => new RGBLuminanceSourceiOS(image);

		public UIImageBarcodeReader()
			: this(null, defaultCreateLuminanceSource, null)
		{

		}

		public UIImageBarcodeReader(Reader reader,
			Func<UIImage, LuminanceSource> createLuminanceSource,
			Func<LuminanceSource, Binarizer> createBinarizer)
			: base(reader, createLuminanceSource ?? defaultCreateLuminanceSource, createBinarizer)
		{

		}

		public UIImageBarcodeReader(Reader reader,
			Func<UIImage, LuminanceSource> createLuminanceSource,
			Func<LuminanceSource, Binarizer> createBinarizer,
			Func<byte[], int, int, RGBLuminanceSource.BitmapFormat, LuminanceSource> createRGBLuminanceSource)
			: base(reader, createLuminanceSource ?? defaultCreateLuminanceSource, createBinarizer, createRGBLuminanceSource)
		{

		}
	}
}