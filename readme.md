# ZXing.Net.Mobile
# This fork contains only ZXingSurfaceView for Xanarin.Android, ZXingScannerView for Xanarin.iOS and modified renderers of these views for Xamarin.Forms + ZXingBarcodeImageView. A lot of unnecessary garbage has been removed and classes have been refactored.

[![Join the chat at https://gitter.im/Redth/ZXing.Net.Mobile](https://badges.gitter.im/Redth/ZXing.Net.Mobile.svg)](https://gitter.im/Redth/ZXing.Net.Mobile?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

![ZXing.Net.Mobile Logo](https://raw.github.com/Redth/ZXing.Net.Mobile/master/zxing.net.mobile_128x128.png)

ZXing.Net.Mobile is a C#/.NET library based on the open source Barcode Library: [ZXing (Zebra Crossing)](https://github.com/zxing/zxing), using the [ZXing.Net Port](https://github.com/micjahn/ZXing.Net).  It works with Xamarin.iOS, Xamarin.Android and Xamarin.Forms.  The goal of ZXing.Net.Mobile is to make scanning barcodes as effortless and painless as possible in your own applications.

#### Xamarin Forms

For Xamarin Forms there is a bit more setup needed.  You will need to initialize the library on each platform in your platform specific app project.

##### Android 

On Android, in your main `Activity`'s `OnCreate (..)` implementation, call:

```csharp
Xamarin.Essentials.Platform.Init(Application);
ZXing.Net.Mobile.Forms.Android.Platform.Init();
```

ZXing.Net.Mobile for Xamarin.Forms also handles the new Android permission request model for you via Xamarin.Essentials, but you will need to add the following override implementation to your main `Activity` as well:

```csharp
public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
{
    Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
    base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
}
```

##### iOS

In your `AppDelegate`'s `FinishedLaunching (..)` implementation, call:

```csharp
ZXing.Net.Mobile.Forms.iOS.Platform.Init();
```

### Features
- Xamarin.iOS
- Xamarin.Android
- Xamarin.Forms

### Barcode Formats
By default, all barcode formats are monitored while scanning.  You can change which formats to check for by passing a ZxingScanningOptions instance into the StartScanning method:

```csharp
var options = new ZXing.Mobile.MobileBarcodeScanningOptions();
options.PossibleFormats = new List<ZXing.BarcodeFormat>() { 
    ZXing.BarcodeFormat.Ean8, ZXing.BarcodeFormat.Ean13 
};
```

- Aztec
- Code 128
- Code 39
- Code 93
- EAN13
- EAN8
- PDF417
- QR
- UPC-E

### Thanks
ZXing.Net.Mobile is a combination of a lot of peoples' work that I've put together (including my own).  So naturally, I'd like to thank everyone who's helped out in any way.  Those of you I know have helped I'm listing here, but anyone else that was involved, please let me know!

- ZXing Project and those responsible for porting it to C#
- John Carruthers - https://github.com/JohnACarruthers/zxing.MonoTouch
- Martin Bowling - https://github.com/martinbowling
- Alex Corrado - https://github.com/chkn/zxing.MonoTouch
- ZXing.Net Project - https://github.com/micjahn/ZXing.Net - HUGE effort here to port ZXing to .NET

### License
Apache ZXing.Net.Mobile Copyright 2012 The Apache Software Foundation
This product includes software developed at The Apache Software Foundation (http://www.apache.org/).

### ZXing.Net
ZXing.Net is released under the Apache 2.0 license.
ZXing.Net can be found here: https://github.com/micjahn/ZXing.Net
A copy of the Apache 2.0 license can be found here: https://github.com/micjahn/ZXing.Net/blob/master/COPYING

### ZXing
ZXing is released under the Apache 2.0 license.
ZXing can be found here: http://code.google.com/p/zxing/
A copy of the Apache 2.0 license can be found here: https://github.com/zxing/zxing/blob/master/LICENSE

### System.Drawing
The System.Drawing classes included are from the mono source code which is property of Novell.
Copyright notice is intact in source code files.
