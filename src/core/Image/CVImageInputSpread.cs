using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VVVV.PluginInterfaces.V2;
using System.Collections;

namespace VVVV.CV.Core
{
	public class CVImageInputSpread : IEnumerable, IDisposable
	{
		ISpread<CVImageLink> FInputPin;
		Spread<CVImageInput> FInput = new Spread<CVImageInput>(0);

		public CVImageInputSpread(ISpread<CVImageLink> inputPin)
		{
			FInputPin = inputPin;
			CheckInputSize();
		}

		public void Dispose()
		{
			foreach (var input in FInput)
				input.Dispose();

			FInput.SliceCount = 0;
		}

		public bool CheckInputSize()
		{
			bool noInput = false;
			if (FInputPin.SliceCount == 0)
				noInput = true;
			else if (FInputPin[0] == null)
				noInput = true;

			if (noInput)
			{
				if (FInput.SliceCount == 0)
					return false;
				else
				{
					FInput.SliceCount = 0;
					return true;
				}
			}

			bool changed = false;
			if (FInput.SliceCount != FInputPin.SliceCount)
			{
				changed = true;

				//add
				for (int iAdd = FInput.SliceCount; iAdd < FInputPin.SliceCount; iAdd++)
				{
					CVImageInput add = new CVImageInput();
					if (FInputPin[iAdd] != null)
						add.Connect(FInputPin[iAdd]);
					FInput.Add(add);
				}

				//remove
				if (FInput.SliceCount != FInputPin.SliceCount)
				{
					for (int iDispose = FInputPin.SliceCount; iDispose < FInput.SliceCount; iDispose++)
					{
						FInput[iDispose].Dispose();
					}

					FInput.SliceCount = FInputPin.SliceCount;
				}
			}

			for (int i = 0; i < FInput.SliceCount; i++)
				if (!FInput[i].ConnectedTo(FInputPin[i]))
				{
					changed = true;
					FInput[i].Connect(FInputPin[i]);
				}

			return changed;
		}

		public Spread<CVImageInput> Spread
		{
			get
			{
				return FInput;
			}
		}

		public CVImageInput this[int index]
		{
			get
			{
				return FInput[index];
			}
		}

		public int SliceCount
		{
			get
			{
				return FInput.SliceCount;
			}
		}

		public bool Connected
		{
			get
			{
				if (FInputPin.SliceCount > 0)
					return !(FInputPin[0] == null && FInputPin.SliceCount == 1);
				else
					return false;
			}
		}

		#region IEnumerable
		public IEnumerator GetEnumerator()
		{
			return new CVImageInputSpreadEnumerator(this);
		}

		class CVImageInputSpreadEnumerator : IEnumerator
		{
			private int FSlice = -1;
			private CVImageInputSpread FSpread;

			public CVImageInputSpreadEnumerator(CVImageInputSpread spread)
			{
				FSpread = spread;
			}

			public object Current
			{
				get
				{
					return FSpread.Spread[FSlice];
				}
			}

			public bool MoveNext()
			{
				if ((FSlice + 1) < FSpread.SliceCount)
				{
					FSlice++;
					return true;
				}
				else
				{
					return false;
				}
			}

			public void Reset()
			{
				FSlice = -1;
			}
		}
		#endregion
	}
}
