namespace ZXing.Net.Mobile.Forms.iOS
{
    public static class Platform
	{
		public static void Init()
		{
			ZXingScannerViewRenderer.Init();
			ZXingBarcodeImageViewRenderer.Init();
		}
	}
}