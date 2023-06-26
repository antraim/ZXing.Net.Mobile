using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;

using AVFoundation;

using CoreFoundation;

using CoreGraphics;

using CoreMedia;

using CoreVideo;

using Foundation;

using ObjCRuntime;

using UIKit;

namespace ZXing.Mobile
{
	public class ZXingScannerView : UIView, IScannerView, IScannerSessionHost
	{
		AVCaptureSession _session;
		AVCaptureDevice _captureDevice = null;
		AVCaptureVideoPreviewLayer _previewLayer;
		AVCaptureVideoDataOutput _output;
		OutputRecorder _outputRecorder;
		DispatchQueue _queue;
		Action<Result> _scanResultCallback;
		volatile bool _stopped = true;
		UIView _layerView;
		bool _shouldRotatePreviewBuffer = false;
		AVConfigs _captureDeviceOriginalConfig;

		public delegate void ScannerSetupCompleteDelegate();
		public event ScannerSetupCompleteDelegate OnScannerSetupComplete;

		public MobileBarcodeScanningOptions ScanningOptions { get; set; }

		public bool IsAnalyzing { get; private set; }

		public ZXingScannerView() { }

		public ZXingScannerView(IntPtr handle) : base(handle) { }

		public ZXingScannerView(CGRect frame) : base(frame) { }

		bool SetupCaptureSession()
		{
			var started = DateTime.UtcNow;

			var availableResolutions = new List<CameraResolution>();

			var consideredResolutions = new Dictionary<NSString, CameraResolution> {
				{ AVCaptureSession.Preset352x288, new CameraResolution   { Width = 352,  Height = 288 } },
				{ AVCaptureSession.PresetMedium, new CameraResolution    { Width = 480,  Height = 360 } },	//480x360
				{ AVCaptureSession.Preset640x480, new CameraResolution   { Width = 640,  Height = 480 } },
				{ AVCaptureSession.Preset1280x720, new CameraResolution  { Width = 1280, Height = 720 } },
				{ AVCaptureSession.Preset1920x1080, new CameraResolution { Width = 1920, Height = 1080 } }
			};

			// configure the capture session for low resolution, change this if your code
			// can cope with more data or volume
			_session = new AVCaptureSession()
			{
				SessionPreset = AVCaptureSession.Preset640x480
			};

			// create a device input and attach it to the session
			var devices = AVCaptureDevice.DevicesWithMediaType(AVMediaType.Video);
			
			foreach (var device in devices)
			{
				_captureDevice = device;

				if (ScanningOptions.UseFrontCameraIfAvailable.HasValue &&
					ScanningOptions.UseFrontCameraIfAvailable.Value &&
					device.Position == AVCaptureDevicePosition.Front)

					break; //Front camera successfully set
				else if (device.Position == AVCaptureDevicePosition.Back && (!ScanningOptions.UseFrontCameraIfAvailable.HasValue || !ScanningOptions.UseFrontCameraIfAvailable.Value))
					break; //Back camera succesfully set
			}
			
			if (_captureDevice == null)
			{
				Console.WriteLine("No captureDevice - this won't work on the simulator, try a physical device");

				return false;
			}

			CameraResolution resolution = null;

			// Find resolution
			// Go through the resolutions we can even consider
			foreach (var cr in consideredResolutions)
			{
				// Now check to make sure our selected device supports the resolution
				// so we can add it to the list to pick from
				if (_captureDevice.SupportsAVCaptureSessionPreset(cr.Key))
					availableResolutions.Add(cr.Value);
			}

			resolution = ScanningOptions.GetResolution(availableResolutions);

			// See if the user selected a resolution
			if (resolution != null)
			{
				// Now get the preset string from the resolution chosen
				var preset = (from c in consideredResolutions
							  where c.Value.Width == resolution.Width
								&& c.Value.Height == resolution.Height
							  select c.Key).FirstOrDefault();

				// If we found a matching preset, let's set it on the session
				if (!string.IsNullOrEmpty(preset))
					_session.SessionPreset = preset;
			}

			var input = AVCaptureDeviceInput.FromDevice(_captureDevice);

			if (input == null)
			{
				Console.WriteLine("No input - this won't work on the simulator, try a physical device");

				return false;
			}
			else
				_session.AddInput(input);

			var start1 = PerformanceCounter.Start();

			_previewLayer = new AVCaptureVideoPreviewLayer(_session);

			PerformanceCounter.Stop(start1, "Alloc AVCaptureVideoPreviewLayer took {0} ms");

			var start2 = PerformanceCounter.Start();

			_previewLayer.VideoGravity = AVLayerVideoGravity.ResizeAspectFill;
			_previewLayer.Frame = new CGRect(0, 0, Frame.Width, Frame.Height);
			_previewLayer.Position = new CGPoint(Layer.Bounds.Width / 2, (Layer.Bounds.Height / 2));

			_layerView = new UIView(new CGRect(0, 0, Frame.Width, Frame.Height));
			_layerView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
			_layerView.Layer.AddSublayer(_previewLayer);

			AddSubview(_layerView);

			ResizePreview(UIApplication.SharedApplication.StatusBarOrientation);

			PerformanceCounter.Stop(start2, "Setup Layers took {0} ms");

			var start3 = PerformanceCounter.Start();

			_session.StartRunning();

			PerformanceCounter.Stop(start3, "session.StartRunning() took {0} ms");

			var start4 = PerformanceCounter.Start();

			var videoSettings = NSDictionary.FromObjectAndKey(new NSNumber((int)CVPixelFormatType.CV32BGRA),
				CVPixelBuffer.PixelFormatTypeKey);

			// create a VideoDataOutput and add it to the sesion
			_output = new AVCaptureVideoDataOutput
			{
				WeakVideoSettings = videoSettings
			};

			// configure the output
			_queue = new DispatchQueue("ZxingScannerView"); // (Guid.NewGuid().ToString());

			var barcodeReader = ScanningOptions.BuildBarcodeReader();

			_outputRecorder = new OutputRecorder(this, img =>
			{
				var ls = img;

				if (!IsAnalyzing)
					return false;

				try
				{
					var start5 = PerformanceCounter.Start();

					if (_shouldRotatePreviewBuffer)
						ls = ls.rotateCounterClockwise();

					var result = barcodeReader.Decode(ls);

					PerformanceCounter.Stop(start5, "Decode Time {0} ms");

					if (result != null)
					{
						_scanResultCallback(result);

						return true;
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine("DECODE FAILED: " + ex);
				}

				return false;
			});

			_output.AlwaysDiscardsLateVideoFrames = true;
			_output.SetSampleBufferDelegate(_outputRecorder, _queue);

			PerformanceCounter.Stop(start4, "Setup Camera Finished took {0} ms");

			_session.AddOutput(_output);

			var start6 = PerformanceCounter.Start();

			if (_captureDevice.LockForConfiguration(out var err))
			{
				_captureDeviceOriginalConfig = new AVConfigs
				{
					FocusMode = _captureDevice.FocusMode,
					ExposureMode = _captureDevice.ExposureMode,
					WhiteBalanceMode = _captureDevice.WhiteBalanceMode,
					AutoFocusRangeRestriction = _captureDevice.AutoFocusRangeRestriction,
				};

				if (_captureDevice.HasFlash)
					_captureDeviceOriginalConfig.FlashMode = _captureDevice.FlashMode;
				if (_captureDevice.FocusPointOfInterestSupported)
					_captureDeviceOriginalConfig.FocusPointOfInterest = _captureDevice.FocusPointOfInterest;
				if (_captureDevice.ExposurePointOfInterestSupported)
					_captureDeviceOriginalConfig.ExposurePointOfInterest = _captureDevice.ExposurePointOfInterest;

				if (ScanningOptions.DisableAutofocus)
					_captureDevice.FocusMode = AVCaptureFocusMode.Locked;
				else
				{
					if (_captureDevice.IsFocusModeSupported(AVCaptureFocusMode.ContinuousAutoFocus))
						_captureDevice.FocusMode = AVCaptureFocusMode.ContinuousAutoFocus;
					else if (_captureDevice.IsFocusModeSupported(AVCaptureFocusMode.AutoFocus))
						_captureDevice.FocusMode = AVCaptureFocusMode.AutoFocus;
				}

				if (_captureDevice.IsExposureModeSupported(AVCaptureExposureMode.ContinuousAutoExposure))
					_captureDevice.ExposureMode = AVCaptureExposureMode.ContinuousAutoExposure;
				else if (_captureDevice.IsExposureModeSupported(AVCaptureExposureMode.AutoExpose))
					_captureDevice.ExposureMode = AVCaptureExposureMode.AutoExpose;

				if (_captureDevice.IsWhiteBalanceModeSupported(AVCaptureWhiteBalanceMode.ContinuousAutoWhiteBalance))
					_captureDevice.WhiteBalanceMode = AVCaptureWhiteBalanceMode.ContinuousAutoWhiteBalance;
				else if (_captureDevice.IsWhiteBalanceModeSupported(AVCaptureWhiteBalanceMode.AutoWhiteBalance))
					_captureDevice.WhiteBalanceMode = AVCaptureWhiteBalanceMode.AutoWhiteBalance;

				if (UIDevice.CurrentDevice.CheckSystemVersion(7, 0) && _captureDevice.AutoFocusRangeRestrictionSupported)
					_captureDevice.AutoFocusRangeRestriction = AVCaptureAutoFocusRangeRestriction.Near;

				if (_captureDevice.FocusPointOfInterestSupported)
					_captureDevice.FocusPointOfInterest = new PointF(0.5f, 0.5f);

				if (_captureDevice.ExposurePointOfInterestSupported)
					_captureDevice.ExposurePointOfInterest = new PointF(0.5f, 0.5f);

				_captureDevice.UnlockForConfiguration();
			}
			else
				Console.WriteLine("Failed to Lock for Config: " + err.Description);

			PerformanceCounter.Stop(start6, "Setup Focus in {0} ms");

			return true;
		}

		public void DidRotate(UIInterfaceOrientation orientation)
		{
			ResizePreview(orientation);

			LayoutSubviews();
		}

		public void ResizePreview(UIInterfaceOrientation orientation)
		{
			_shouldRotatePreviewBuffer = orientation == UIInterfaceOrientation.Portrait || orientation == UIInterfaceOrientation.PortraitUpsideDown;

			if (_previewLayer == null)
				return;

			_previewLayer.Frame = new CGRect(0, 0, Frame.Width, Frame.Height);

			if (_previewLayer.RespondsToSelector(new Selector("connection")) && _previewLayer.Connection != null)
			{
				switch (orientation)
				{
					case UIInterfaceOrientation.LandscapeLeft:
						_previewLayer.Connection.VideoOrientation = AVCaptureVideoOrientation.LandscapeLeft;
						break;
					case UIInterfaceOrientation.LandscapeRight:
						_previewLayer.Connection.VideoOrientation = AVCaptureVideoOrientation.LandscapeRight;
						break;
					case UIInterfaceOrientation.Portrait:
						_previewLayer.Connection.VideoOrientation = AVCaptureVideoOrientation.Portrait;
						break;
					case UIInterfaceOrientation.PortraitUpsideDown:
						_previewLayer.Connection.VideoOrientation = AVCaptureVideoOrientation.PortraitUpsideDown;
						break;
				}
			}
		}

		public void Focus(PointF pointOfInterest)
		{
			//Get the device
			if (AVMediaType.Video == null)
				return;

			var device = AVCaptureDevice.DefaultDeviceWithMediaType(AVMediaType.Video);

			if (device == null)
				return;

			//See if it supports focusing on a point
			if (device.FocusPointOfInterestSupported && !device.AdjustingFocus)
			{
				NSError err = null;

				//Lock device to config
				if (device.LockForConfiguration(out err))
				{
					Console.WriteLine("Focusing at point: " + pointOfInterest.X + ", " + pointOfInterest.Y);

					//Focus at the point touched
					device.FocusPointOfInterest = pointOfInterest;
					device.FocusMode = AVCaptureFocusMode.ContinuousAutoFocus;
					device.UnlockForConfiguration();
				}
			}
		}

		public class OutputRecorder : AVCaptureVideoDataOutputSampleBufferDelegate
		{
			public OutputRecorder(IScannerSessionHost scannerHost, Func<LuminanceSource, bool> handleImage) : base()
			{
				this.handleImage = handleImage;
				this.scannerHost = scannerHost;
			}

			IScannerSessionHost scannerHost;
			Func<LuminanceSource, bool> handleImage;

			DateTime lastAnalysis = DateTime.MinValue;
			volatile bool working = false;
			volatile bool wasScanned = false;

			[Export("captureOutput:didDropSampleBuffer:fromConnection:")]
			public override void DidDropSampleBuffer(AVCaptureOutput captureOutput, CMSampleBuffer sampleBuffer, AVCaptureConnection connection) { }

			public CancellationTokenSource CancelTokenSource = new CancellationTokenSource();

			public override void DidOutputSampleBuffer(AVCaptureOutput captureOutput, CMSampleBuffer sampleBuffer, AVCaptureConnection connection)
			{
				var msSinceLastPreview = (DateTime.UtcNow - lastAnalysis).TotalMilliseconds;

				if (msSinceLastPreview < scannerHost.ScanningOptions.DelayBetweenAnalyzingFrames
					|| (wasScanned && msSinceLastPreview < scannerHost.ScanningOptions.DelayBetweenContinuousScans)
					|| working
					|| CancelTokenSource.IsCancellationRequested)
				{

					if (msSinceLastPreview < scannerHost.ScanningOptions.DelayBetweenAnalyzingFrames)
						Console.WriteLine("Too soon between frames");
					if (wasScanned && msSinceLastPreview < scannerHost.ScanningOptions.DelayBetweenContinuousScans)
						Console.WriteLine("Too soon since last scan");

					if (sampleBuffer != null)
					{
						sampleBuffer.Dispose();
						sampleBuffer = null;
					}

					return;
				}

				wasScanned = false;
				working = true;
				lastAnalysis = DateTime.UtcNow;

				try
				{
					// Get the CoreVideo image
					using (var pixelBuffer = sampleBuffer.GetImageBuffer() as CVPixelBuffer)
					{
						// Lock the base address
						pixelBuffer.Lock(CVPixelBufferLock.ReadOnly); // MAYBE NEEDS READ/WRITE

						LuminanceSource luminanceSource;

						// Let's access the raw underlying data and create a luminance source from it
						unsafe
						{
							var rawData = (byte*)pixelBuffer.BaseAddress.ToPointer();
							var rawDatalen = (int)(pixelBuffer.Height * pixelBuffer.Width * 4); //This drops 8 bytes from the original length to give us the expected length

							luminanceSource = new CVPixelBufferBGRA32LuminanceSource(rawData, rawDatalen, (int)pixelBuffer.Width, (int)pixelBuffer.Height);
						}

						if (handleImage(luminanceSource))
							wasScanned = true;

						pixelBuffer.Unlock(CVPixelBufferLock.ReadOnly);
					}

					//
					// Although this looks innocent "Oh, he is just optimizing this case away"
					// this is incredibly important to call on this callback, because the AVFoundation
					// has a fixed number of buffers and if it runs out of free buffers, it will stop
					// delivering frames. 
					//	
					sampleBuffer.Dispose();
					sampleBuffer = null;

				}
				catch (Exception e)
				{
					Console.WriteLine(e);
				}
				finally
				{
					working = false;
				}
			}
		}

		#region IScannerView

		public void StartScanning(Action<Result> scanResultCallback, MobileBarcodeScanningOptions options = null)
		{
			if (!_stopped)
				return;

			_stopped = false;
			_scanResultCallback = scanResultCallback;

			ScanningOptions = options ?? MobileBarcodeScanningOptions.Default;

			var start = PerformanceCounter.Start();

			InvokeOnMainThread(() =>
			{
				if (!SetupCaptureSession())
				{

				}

				if (Runtime.Arch == Arch.SIMULATOR)
				{
					InsertSubview(new UIView(new CGRect(0, 0, this.Frame.Width, this.Frame.Height))
					{
						BackgroundColor = UIColor.LightGray,
						AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight
					}, 0);
				}
			});

			if (!IsAnalyzing)
				IsAnalyzing = true;

			PerformanceCounter.Stop(start, "Start Scanning took {0} ms");

			OnScannerSetupComplete?.Invoke();
		}

		public void StopScanning()
		{
			if (_stopped)
				return;

			IsAnalyzing = false;

			_outputRecorder?.CancelTokenSource.Cancel();

			// Revert camera settings to original
			if (_captureDevice != null && _captureDevice.LockForConfiguration(out var err))
			{
				_captureDevice.FocusMode = _captureDeviceOriginalConfig.FocusMode;
				_captureDevice.ExposureMode = _captureDeviceOriginalConfig.ExposureMode;
				_captureDevice.WhiteBalanceMode = _captureDeviceOriginalConfig.WhiteBalanceMode;

				if (UIDevice.CurrentDevice.CheckSystemVersion(7, 0) && _captureDevice.AutoFocusRangeRestrictionSupported)
					_captureDevice.AutoFocusRangeRestriction = _captureDeviceOriginalConfig.AutoFocusRangeRestriction;

				if (_captureDevice.FocusPointOfInterestSupported)
					_captureDevice.FocusPointOfInterest = _captureDeviceOriginalConfig.FocusPointOfInterest;

				if (_captureDevice.ExposurePointOfInterestSupported)
					_captureDevice.ExposurePointOfInterest = _captureDeviceOriginalConfig.ExposurePointOfInterest;

				if (_captureDevice.HasFlash)
					_captureDevice.FlashMode = _captureDeviceOriginalConfig.FlashMode;

				_captureDevice.UnlockForConfiguration();
			}

			//Try removing all existing outputs prior to closing the session
			try
			{
				while (_session.Outputs.Length > 0)
					_session.RemoveOutput(_session.Outputs[0]);
			}
			catch { }

			//Try to remove all existing inputs prior to closing the session
			try
			{
				while (_session.Inputs.Length > 0)
					_session.RemoveInput(_session.Inputs[0]);
			}
			catch { }

			if (_session.Running)
				_session.StopRunning();

			_stopped = true;
		}

		public void AutoFocus() { }

		public void AutoFocus(int x, int y) { }

		public void ResumeAnalysis() => IsAnalyzing = true;

		public void PauseAnalysis() => IsAnalyzing = false;

		#endregion
	}

	struct AVConfigs
	{
		public AVCaptureFocusMode FocusMode;
		public AVCaptureExposureMode ExposureMode;
		public AVCaptureWhiteBalanceMode WhiteBalanceMode;
		public AVCaptureAutoFocusRangeRestriction AutoFocusRangeRestriction;
		public CGPoint FocusPointOfInterest;
		public CGPoint ExposurePointOfInterest;
		public AVCaptureFlashMode FlashMode;
	}
}