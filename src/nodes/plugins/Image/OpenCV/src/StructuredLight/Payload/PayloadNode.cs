using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VVVV.PluginInterfaces.V2;

namespace VVVV.CV.Nodes.StructuredLight.Payload
{
	public enum PayloadMode { Graycode };

	#region PluginInfo
	[PluginInfo(Name = "Payload", Category = "CV.StructuredLight", Help = "Setup a payload", Author = "", Credits = "", Tags = "")]
	#endregion PluginInfo
	public class PayloadNode : IPluginEvaluate
	{
		#region pins
		[Input("Width", IsSingle = true, MinValue=4, DefaultValue=1024)]
		IDiffSpread<int> FWidth;

		[Input("Height", IsSingle = true, MinValue=1, DefaultValue=768)]
		IDiffSpread<int> FHeight;

		[Input("Mode", IsSingle = true)]
		IDiffSpread<PayloadMode> FMode;

		[Input("Balanced", IsSingle = true, DefaultValue=1)]
		IDiffSpread<bool> FBalanced;

		[Output("Payload")]
		ISpread<IPayload> FOutput;

		[Output("Frame Count")]
		ISpread<int> FCount;

		IPayload FPayload;
		#endregion
		public void Evaluate(int SpreadMax)
		{
			if (FWidth.IsChanged || FHeight.IsChanged || FMode.IsChanged || FBalanced.IsChanged)
			{
				switch (FMode[0])
				{
					case PayloadMode.Graycode:
						{
							FPayload = new PayloadGraycode(FWidth[0], FHeight[0], FBalanced[0]);
						}
						break;
				}

				FOutput[0] = FPayload;
				FCount[0] = (int)FPayload.FrameCount;
			}
		}
	}
}
