namespace ZXing.Net.Mobile.Forms.Android
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