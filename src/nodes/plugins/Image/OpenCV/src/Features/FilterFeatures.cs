using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VVVV.PluginInterfaces.V2;
using VVVV.Nodes.OpenCV.Features.Criteria;

namespace VVVV.CV.Nodes.Features
{
    #region PluginInfo
    [PluginInfo(Name = "FilterFeatures", Category = "CV.Features", Help = "Filter features given criteria.", Tags = "")]
    #endregion PluginInfo
    public class FilterFeatures : IPluginEvaluate
    {
        [Input("Input")]
        IDiffSpread<FeatureSet> FInput;

        [Input("Criteria")]
        IDiffSpread<ICriteria> FCriteria;

        [Output("Output")]
        ISpread<FeatureSet> FOutput;

        bool FPerform = false;

        List<FeatureSet> sets = new List<FeatureSet>();
        List<ICriteria> criterias = new List<ICriteria>();

        public void Evaluate(int SpreadMax)
        {
            if (FInput.IsChanged)
            {
                foreach(var set in sets)
                {
                    if (set != null)
                    {
                        set.Update -= new EventHandler(update);
                    }
                }

                this.sets.Clear();

                foreach (var set in FInput)
                {
                    if (set != null)
                    {
                        set.Update += new EventHandler(update);
                        this.sets.Add(set);
                    }
                }

                FPerform = true;
            }

            if (FCriteria.IsChanged)
            {
                foreach (var criteria in criterias)
                {
                    if (criteria != null)
                        criteria.Update -= new EventHandler(update);
                }

                this.criterias.Clear();

                foreach (var criteria in FCriteria)
                {
                    if (criteria != null)
                    {
                        criteria.Update += new EventHandler(update);
                        this.criterias.Add(criteria);
                    }
                }

                FPerform = true;
            }

            if (FPerform)
            {
                FPerform = false;

                FOutput.SliceCount = SpreadMax;

                for (int i = 0; i < SpreadMax; i++)
                {
                    if (FInput[i] == null)
                        FOutput[i] = null;
                    else
                        FOutput[i] = new FeatureSet(FInput[i], FCriteria[i]);
                }
            }
        }

        void update(object sender, EventArgs e)
        {
            FPerform = true;
        }
    }
}
