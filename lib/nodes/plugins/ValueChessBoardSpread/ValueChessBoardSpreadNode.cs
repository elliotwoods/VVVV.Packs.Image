#region usings
using System;
using System.ComponentModel.Composition;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using VVVV.Core.Logging;
#endregion usings

namespace VVVV.Nodes
{
	#region PluginInfo
	[PluginInfo(Name = "ChessBoardSpread", Category = "Value", Help = "Basic template with one value in/out", Tags = "")]
	#endregion PluginInfo
	public class ValueChessBoardSpreadNode : IPluginEvaluate
	{
		#region fields & pins
		[Input("Board Size X", IsSingle=true)]
		ISpread<int> FPinInBoardX;
		
		[Input("Board Size Y", IsSingle=true)]
		ISpread<int> FPinInBoardY;

		[Output("Output")]
		ISpread<ISpread<bool>> FOutput;

		[Import()]
		ILogger FLogger;
		#endregion fields & pins

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			FOutput.SliceCount = FPinInBoardY[0]+1;
			
			bool firstOn = true;
			bool high = false;
			
			for (int j=0; j<FPinInBoardY[0]+1; j++)
			{			
				FOutput[j].SliceCount = FPinInBoardX[0]+1;
				
				firstOn = !firstOn;
				for (int i=0; i<FPinInBoardX[0]+1; i++){
					if (i==0)
						high = firstOn;
					else { 
						high = !high;
					}
					FOutput[j][i] = high;		
				}
			}
		}
	}
}
