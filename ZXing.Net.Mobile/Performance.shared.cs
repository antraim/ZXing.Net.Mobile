using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ZXing.Mobile
{
    public class PerformanceCounter
	{
		static Dictionary<string, Stopwatch> _counters = new Dictionary<string, Stopwatch>();

		public static string Start()
		{
			var guid = Guid.NewGuid().ToString();

			var sw = new Stopwatch();

			_counters.Add(guid, sw);

			sw.Start();

			return guid;
		}

		public static void Stop(string guid, string message)
		{
			var elapsed = Stop(guid);

			if (!message.Contains("{0}"))
				message += " {0}";

			if (Debugger.IsAttached)
				Debug.WriteLine($"***{message}", elapsed.TotalMilliseconds);
		}

		static TimeSpan Stop(string guid)
		{
			if (!_counters.ContainsKey(guid))
				return TimeSpan.Zero;

			var sw = _counters[guid];

			sw.Stop();

			_counters.Remove(guid);

			return sw.Elapsed;
		}
	}
}