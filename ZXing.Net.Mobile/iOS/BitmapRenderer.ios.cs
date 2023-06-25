using CoreGraphics;

using UIKit;

using ZXing.Common;
using ZXing.Rendering;

namespace ZXing.Mobile
{
	public class BitmapRenderer : IBarcodeRenderer<UIImage>
	{
		public UIImage Render(BitMatrix matrix, BarcodeFormat format, string content)
			=> Render(matrix, format, content, new EncodingOptions());

		public UIImage Render(BitMatrix matrix, BarcodeFormat format, string content, EncodingOptions options)
		{
			UIGraphics.BeginImageContext(new CGSize(matrix.Width, matrix.Height));

			var context = UIGraphics.GetCurrentContext();

			var black = new CGColor(0f, 0f, 0f);
			var white = new CGColor(1.0f, 1.0f, 1.0f);

			for (var x = 0; x < matrix.Width; x++)
				for (var y = 0; y < matrix.Height; y++)
				{
					context.SetFillColor(matrix[x, y] ? black : white);
					context.FillRect(new CGRect(x, y, 1, 1));
				}

			var image = UIGraphics.GetImageFromCurrentImageContext();

			UIGraphics.EndImageContext();

			return image;
		}
	}
}