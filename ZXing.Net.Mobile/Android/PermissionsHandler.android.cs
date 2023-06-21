using System;
using System.Threading.Tasks;

using Android.App;
using Android.Content.PM;

using Xamarin.Essentials;

namespace ZXing.Net.Mobile.Android
{
    public static class PermissionsHandler
	{
		[Obsolete("Use Xamarin.Essentials.Platform.OnRequestPermissionsResult instead.")]
		public static void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
			=> Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

		public static async Task<bool> RequestPermissionsAsync()
		{
			var camera = await Permissions.RequestAsync<Permissions.Camera>();

			if (camera != PermissionStatus.Granted)
				return false;

			return true;
		}

		[Obsolete("Use Xamarin.Essentials.Permissions instead.")]
		public static bool NeedsPermissionRequest(Activity activity = null)
			=> true;
	}
}