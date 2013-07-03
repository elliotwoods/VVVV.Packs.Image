using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using DeckLinkAPI;

namespace VVVV.Nodes.DeckLink
{
	class DeviceRegister
	{
		static public DeviceRegister Singleton = new DeviceRegister();

		public class Device
		{
			public IDeckLink DeviceHandle;
			public string ModelName;
			public string DisplayName;
		}

		List<Device> FDevices = new List<Device>();
		public List<Device> Devices
		{
			get
			{
				return FDevices;
			}
		}

		public int Count
		{
			get
			{
				int result = 0;
				WorkerThread.Singleton.PerformBlocking(() =>
					{
						result = FDevices.Count;
					});
				return result;
			}
		}

		public string GetModelName(int index)
		{
			string result = "";
			WorkerThread.Singleton.PerformBlocking(() =>
			{
				result = FDevices[index].ModelName;
			});
			return result;
		}

		public string GetDisplayName(int index)
		{
			string result = "";
			WorkerThread.Singleton.PerformBlocking(() =>
			{
				result = FDevices[index].DisplayName;
			});
			return result;
		}

		public IDeckLink GetDeviceHandle(int index)
		{
			if (this.Count == 0)
			{
				Refresh();
			}
			if (this.Count == 0)
				throw (new Exception("No DeckLink device available"));
			else
				return FDevices[index % FDevices.Count].DeviceHandle;
		}

		public void Refresh()
		{
			WorkerThread.Singleton.PerformBlocking(() => {
				foreach (var oldDevice in FDevices)
				{
					Marshal.ReleaseComObject(oldDevice.DeviceHandle);
				}
				FDevices.Clear();

				var iterator = new CDeckLinkIterator();
				IDeckLink device;
				string modelName, displayName;
				while (true)
				{
					iterator.Next(out device);
					if (device == null)
						break;

					device.GetModelName(out modelName);
					device.GetDisplayName(out displayName);

					FDevices.Add(new Device()
					{
						DeviceHandle = device,
						ModelName = modelName,
						DisplayName = displayName
					});
				}
			});
		}
	}
}
