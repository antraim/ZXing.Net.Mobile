using System;
using System.Collections.Generic;
using System.Linq;

using Android.Content;
using Android.Graphics;
using Android.Hardware;
using Android.OS;
using Android.Runtime;
using Android.Views;

using ApxLabs.FastAndroidCamera;

using Camera = Android.Hardware.Camera;

namespace ZXing.Mobile.CameraAccess
{
	public class CameraController
	{
		readonly Context _context;
		readonly ISurfaceHolder _holder;
		readonly SurfaceView _surfaceView;
		readonly CameraEventsListener _cameraEventListener;
		IScannerSessionHost _scannerHost;
		int _cameraId;

		public Camera Camera { get; private set; }

		public CameraResolution CameraResolution { get; private set; }

		public int LastCameraDisplayOrientationDegree { get; private set; }

		public CameraController(SurfaceView surfaceView, CameraEventsListener cameraEventListener, IScannerSessionHost scannerHost)
		{
			_context = surfaceView.Context;
			_holder = surfaceView.Holder;
			_surfaceView = surfaceView;
			_cameraEventListener = cameraEventListener;
			_scannerHost = scannerHost;
		}

		#region

		public void SetupCamera()
		{
			if (Camera != null)
				return;

			var start = PerformanceCounter.Start();

			OpenCamera();

			PerformanceCounter.Stop(start, "Setup Camera took {0} ms");

			if (Camera == null)
				return;

			start = PerformanceCounter.Start();

			ApplyCameraSettings();

			try
			{
				Camera.SetPreviewDisplay(_holder);

				var previewParameters = Camera.GetParameters();
				var previewSize = previewParameters.PreviewSize;
				var bitsPerPixel = ImageFormat.GetBitsPerPixel(previewParameters.PreviewFormat);

				var bufferSize = (previewSize.Width * previewSize.Height * bitsPerPixel) / 8;

				const int NUM_PREVIEW_BUFFERS = 5;

				for (uint i = 0; i < NUM_PREVIEW_BUFFERS; ++i)
					using (var buffer = new FastJavaByteArray(bufferSize))
						Camera.AddCallbackBuffer(buffer);

				Camera.StartPreview();
				Camera.SetNonMarshalingPreviewCallback(_cameraEventListener);
			}
			catch (Exception ex)
			{
				Android.Util.Log.Error(PerformanceCounter.TAG, "Setup Camera Failed: {0}", ex);

				return;
			}
			finally
			{
				PerformanceCounter.Stop(start, "Setup Camera Parameters took {0} ms");
			}

			// Docs suggest if Auto or Macro modes, we should invoke AutoFocus at least once
			var currentFocusMode = Camera.GetParameters().FocusMode;
			if (currentFocusMode == Camera.Parameters.FocusModeAuto
				|| currentFocusMode == Camera.Parameters.FocusModeMacro)
				AutoFocus();
		}

		public void ShutdownCamera()
		{
			if (Camera == null)
				return;

			// camera release logic takes about 0.005 sec so there is no need in async releasing
			var start = PerformanceCounter.Start();

			try
			{
				Camera.StopPreview();
				Camera.SetNonMarshalingPreviewCallback(null);
				Camera.SetPreviewDisplay(null);
				Camera.Release();
				Camera = null;
			}
			catch (Exception ex)
			{
				Android.Util.Log.Error(PerformanceCounter.TAG, "Shutdown Camera Failed: {0}", ex);
			}
			finally
			{
				PerformanceCounter.Stop(start, "Shutdown Camera took {0} ms");
			}
		}

		public void RefreshCamera()
		{
			if (_holder == null)
				return;

			var start = PerformanceCounter.Start();

			ApplyCameraSettings();

			try
			{
				Camera.SetPreviewDisplay(_holder);
				Camera.StartPreview();
			}
			catch (Exception ex)
			{
				Android.Util.Log.Error(PerformanceCounter.TAG, "Refresh Camera Failed: {0}", ex);
			}
			finally
			{
				PerformanceCounter.Stop(start, "Refresh Camera took {0} ms");
			}
		}

		public void AutoFocus()
		{
			AutoFocus(0, 0, false);
		}

		public void AutoFocus(int x, int y)
		{
			// The bounds for focus areas are actually -1000 to 1000
			// So we need to translate the touch coordinates to this scale
			var focusX = x / _surfaceView.Width * 2000 - 1000;
			var focusY = y / _surfaceView.Height * 2000 - 1000;

			// Call the autofocus with our coords
			AutoFocus(focusX, focusY, true);
		}

		#endregion

		void OpenCamera()
		{
			try
			{
				var version = Build.VERSION.SdkInt;

				if (version >= BuildVersionCodes.Gingerbread)
				{
					Android.Util.Log.Info(PerformanceCounter.TAG, "Checking Number Of Cameras...");

					var numberOfCameras = Camera.NumberOfCameras;
					var cameraInfo = new Camera.CameraInfo();
					var found = false;

					Android.Util.Log.Debug(PerformanceCounter.TAG, $"Found {numberOfCameras} Cameras...");

					var whichCamera = CameraFacing.Back;

					if (_scannerHost.ScanningOptions.UseFrontCameraIfAvailable.HasValue &&
						_scannerHost.ScanningOptions.UseFrontCameraIfAvailable.Value)
						whichCamera = CameraFacing.Front;

					for (var i = 0; i < numberOfCameras; i++)
					{
						Camera.GetCameraInfo(i, cameraInfo);

						if (cameraInfo.Facing == whichCamera)
						{
							Android.Util.Log.Info(PerformanceCounter.TAG, $"Found {whichCamera} Camera, Opening...");

							Camera = Camera.Open(i);

							_cameraId = i;

							found = true;

							break;
						}
					}

					if (!found)
					{
						Android.Util.Log.Info(PerformanceCounter.TAG, $"Finding {whichCamera} Camera Failed, Opening Camera 0...");

						Camera = Camera.Open(0);

						_cameraId = 0;
					}
				}
				else
				{
					Camera = Camera.Open();
				}
			}
			catch (Exception ex)
			{
				ShutdownCamera();

				Android.Util.Log.Error(PerformanceCounter.TAG, "Open Camera Failed: {0}", ex);
			}
		}

		void ApplyCameraSettings()
		{
			if (Camera == null)
				OpenCamera();

			// do nothing if something wrong with camera
			if (Camera == null)
				return;

			var parameters = Camera.GetParameters();
			parameters.PreviewFormat = ImageFormatType.Nv21;

			var supportedFocusModes = parameters.SupportedFocusModes;

			if (_scannerHost.ScanningOptions.DisableAutofocus)
				parameters.FocusMode = Camera.Parameters.FocusModeFixed;
			else if (Build.VERSION.SdkInt >= BuildVersionCodes.IceCreamSandwich &&
				supportedFocusModes.Contains(Camera.Parameters.FocusModeContinuousPicture))
				parameters.FocusMode = Camera.Parameters.FocusModeContinuousPicture;
			else if (supportedFocusModes.Contains(Camera.Parameters.FocusModeContinuousVideo))
				parameters.FocusMode = Camera.Parameters.FocusModeContinuousVideo;
			else if (supportedFocusModes.Contains(Camera.Parameters.FocusModeAuto))
				parameters.FocusMode = Camera.Parameters.FocusModeAuto;
			else if (supportedFocusModes.Contains(Camera.Parameters.FocusModeFixed))
				parameters.FocusMode = Camera.Parameters.FocusModeFixed;

			var selectedFps = parameters.SupportedPreviewFpsRange.FirstOrDefault();

			if (selectedFps != null)
			{
				// This will make sure we select a range with the highest maximum fps
				// which still has the lowest minimum fps (Widest Range)
				foreach (var fpsRange in parameters.SupportedPreviewFpsRange)
					if (fpsRange[1] > selectedFps[1] || fpsRange[1] == selectedFps[1] && fpsRange[0] < selectedFps[0])
						selectedFps = fpsRange;

				parameters.SetPreviewFpsRange(selectedFps[0], selectedFps[1]);
			}

			CameraResolution resolution = null;

			var supportedPreviewSizes = parameters.SupportedPreviewSizes;

			if (supportedPreviewSizes != null)
			{
				var availableResolutions = supportedPreviewSizes.Select(sps => new CameraResolution
				{
					Width = sps.Width,
					Height = sps.Height
				});

				// Try and get a desired resolution from the options selector
				resolution = _scannerHost.ScanningOptions.GetResolution(availableResolutions.ToList());

				// If the user did not specify a resolution, let's try and find a suitable one
				if (resolution == null)
					foreach (var sps in supportedPreviewSizes)
						if (sps.Width >= 640 && sps.Width <= 1000 && sps.Height >= 360 && sps.Height <= 1000)
						{
							resolution = new CameraResolution
							{
								Width = sps.Width,
								Height = sps.Height
							};

							break;
						}
			}

			// Google Glass requires this fix to display the camera output correctly
			if (Build.Model.Contains("Glass"))
			{
				resolution = new CameraResolution
				{
					Width = 640,
					Height = 360
				};

				// Glass requires 30fps
				parameters.SetPreviewFpsRange(30000, 30000);
			}

			// Hopefully a resolution was selected at some point
			if (resolution != null)
			{
				CameraResolution = resolution;

				parameters.SetPreviewSize(resolution.Width, resolution.Height);

				Android.Util.Log.Info(PerformanceCounter.TAG, $"Selected Resolution: {resolution.Width} x {resolution.Height}");
			}

			Camera.SetParameters(parameters);

			SetCameraDisplayOrientation();
		}

		void AutoFocus(int x, int y, bool useCoordinates)
		{
			if (Camera == null)
				return;

			if (_scannerHost.ScanningOptions.DisableAutofocus)
			{
				Android.Util.Log.Info(PerformanceCounter.TAG, "AutoFocus Disabled");

				return;
			}

			var cameraParams = Camera.GetParameters();

			Android.Util.Log.Info(PerformanceCounter.TAG, "AutoFocus Requested");

			// Cancel any previous requests
			Camera.CancelAutoFocus();

			try
			{
				// If we want to use coordinates
				// Also only if our camera supports Auto focus mode
				// Since FocusAreas only really work with FocusModeAuto set
				if (useCoordinates
					&& cameraParams.SupportedFocusModes.Contains(Camera.Parameters.FocusModeAuto))
				{
					// Let's give the touched area a 20 x 20 minimum size rect to focus on
					// So we'll offset -10 from the center of the touch and then 
					// make a rect of 20 to give an area to focus on based on the center of the touch
					x = x - 10;
					y = y - 10;

					// Ensure we don't go over the -1000 to 1000 limit of focus area
					if (x >= 1000)
						x = 980;
					if (x < -1000)
						x = -1000;
					if (y >= 1000)
						y = 980;
					if (y < -1000)
						y = -1000;

					// Explicitly set FocusModeAuto since Focus areas only work with this setting
					cameraParams.FocusMode = Camera.Parameters.FocusModeAuto;

					// Add our focus area
					cameraParams.FocusAreas = new List<Camera.Area>
					{
						new Camera.Area(new Rect(x, y, x + 20, y + 20), 1000)
					};

					Camera.SetParameters(cameraParams);
				}

				// Finally autofocus (weather we used focus areas or not)
				Camera.AutoFocus(_cameraEventListener);
			}
			catch (Exception ex)
			{
				Android.Util.Log.Error(PerformanceCounter.TAG, "AutoFocus Failed: {0}", ex);
			}
		}

		void SetCameraDisplayOrientation()
		{
			var degrees = GetCameraDisplayOrientation();

			LastCameraDisplayOrientationDegree = degrees;

			Android.Util.Log.Info(PerformanceCounter.TAG, $"Changing Camera Orientation to {degrees}");

			try
			{
				Camera.SetDisplayOrientation(degrees);
			}
			catch (Exception ex)
			{
				Android.Util.Log.Error(PerformanceCounter.TAG, "Set Camera Display Orientation Failed: {0}", ex);
			}
		}

		int GetCameraDisplayOrientation()
		{
			var windowManager = _context.GetSystemService(Context.WindowService).JavaCast<IWindowManager>();
			var display = windowManager.DefaultDisplay;
			var rotation = display.Rotation;

			var degrees = rotation switch
			{
				SurfaceOrientation.Rotation0 => 0,
				SurfaceOrientation.Rotation90 => 90,
				SurfaceOrientation.Rotation180 => 180,
				SurfaceOrientation.Rotation270 => 270,
				_ => throw new ArgumentOutOfRangeException(),
			};

			var info = new Camera.CameraInfo();

			Camera.GetCameraInfo(_cameraId, info);

			int correctedDegrees;

			if (info.Facing == CameraFacing.Front)
			{
				correctedDegrees = (info.Orientation + degrees) % 360;
				correctedDegrees = (360 - correctedDegrees) % 360; // compensate the mirror
			}
			else
			{
				// Back-facing
				correctedDegrees = (info.Orientation - degrees + 360) % 360;
			}

			return correctedDegrees;
		}
	}
}