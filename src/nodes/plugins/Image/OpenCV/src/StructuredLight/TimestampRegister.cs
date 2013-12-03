using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VVVV.CV.Nodes.StructuredLight
{

	public class TimestampRegister : IComparable
	{
		/// <summary>
		/// Dictionary of <Timestamp, Frame>
		/// </summary>
		SortedDictionary<long, ulong> FRegister = new SortedDictionary<long, ulong>();
		ulong FFrameCount;

		/// <summary>
		/// Initialise the register
		/// </summary>
		/// <param name="FrameCount">The register will only store this many frames</param>
		public void Initialise(ulong FrameCount)
		{
			lock (this)
			{
				FFrameCount = FrameCount;
				FRegister.Clear();
			}
		}

		public void Add(ulong Frame)
		{
			Add(Frame, DateTime.UtcNow.Ticks);
		}

		public void Add(ulong Frame, long Timestamp)
		{
			if (FFrameCount > 0) {
				lock (this)
				{
					while ((ulong)FRegister.Count >= FFrameCount)
						FRegister.Remove(FRegister.Keys.ElementAt<long>(0));

					FRegister.Add(Timestamp, Frame);
				}
			}
		}

		/// <summary>
		/// Find closest frame index (or 1 frame earlier) to timestamp in register.
		/// </summary>
		/// <param name="Timestamp">Timestamp to lookup</param>
		/// <param name="Frame">The returned index of the frame</param>
		/// <returns>false if no entry found</returns>
		public bool Lookup(long Timestamp, out ulong Frame)
		{
			lock (this)
			{
				if (FRegister.ContainsKey(Timestamp))
				{
					Frame = FRegister[Timestamp];
					return true;
				}

				int idx = FRegister.Keys.ToList().BinarySearch(Timestamp);
				if (idx < 0)
					idx = (~idx) - 1;

				if (idx < 0 || idx >= (int)FFrameCount)
				{
					Frame = 0;
					return false;
				} else {
					Frame = FRegister.Values.ToList()[idx];
					return true;
				}
			}
		}

		public int CompareTo(object obj)
		{
			throw new NotImplementedException();
		}
	}
}
