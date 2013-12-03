using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using VVVV.PluginInterfaces.V2;

namespace VVVV.CV.Core
{
	public class CVImageOutputSpread : IDisposable
	{
		ISpread<CVImageLink> FOutputPin;
		Spread<CVImageOutput> FOutput = new Spread<CVImageOutput>(0);

		public CVImageOutputSpread(ISpread<CVImageLink> outputPin)
		{
			FOutputPin = outputPin;
		}

		public void Dispose()
		{
			foreach (var output in FOutput)
				output.Dispose();
		}

		public void AlignOutputPins()
		{
			FOutputPin.SliceCount = FOutput.SliceCount;

			for (int i = 0; i < FOutput.SliceCount; i++)
				FOutputPin[i] = FOutput[i].Link;
		}

		public int SliceCount
		{
			get
			{
				return FOutput.SliceCount;
			}
			set
			{
				while (FOutput.SliceCount < value)
					FOutput.Add(new CVImageOutput());

				FOutput.SliceCount = value;
			}
		}

		public void Add(CVImageOutput output)
		{
			FOutput.Add(output);
		}

		public CVImageOutput this[int index]
		{
			get
			{
				return FOutput[index];
			}
			set
			{
				FOutput[index] = value;
			}
		}
	}
}
