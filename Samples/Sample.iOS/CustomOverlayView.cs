using CoreGraphics;

using UIKit;

namespace Sample.iOS
{
    public class CustomOverlayView : UIView
	{
		public UIButton ButtonCancel;

		public CustomOverlayView() : base()
		{
			ButtonCancel = UIButton.FromType(UIButtonType.RoundedRect);
			ButtonCancel.Frame = new CGRect(0, Frame.Height - 60, Frame.Width / 2 - 10, 34);
			ButtonCancel.AutoresizingMask = UIViewAutoresizing.FlexibleTopMargin | UIViewAutoresizing.FlexibleRightMargin;
			ButtonCancel.SetTitle("Cancel", UIControlState.Normal);
			AddSubview(ButtonCancel);
		}

		public override void LayoutSubviews()
		{
			ButtonCancel.Frame = new CGRect(0, Frame.Height - 60, Frame.Width / 2 - 10, 34);
		}
	}
}