using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KSP;
using Radiators;

namespace RealActiveRadiator
{
	public class ModuleRealActiveRadiator : ModuleActiveRadiator
	{
        [KSPField()]
        public FloatCurve cryoCoolerEfficiency;

        [KSPField]
        public double maxCryoEnergyTransfer = 500.0;

        [KSPField]
        public double maxCryoElectricCost = 10;

        [KSPField(guiName = "AR:RefrCost", guiActive = false)]
        public string D_RefrCost = "???";

        protected int electricResourceIndex = -1;
        protected double baseElectricRate = 0;
        protected double refrigerationThrottle = 1;
        protected BaseField Dfld_RefrCost;

		public ModuleRealActiveRadiator () : base()
		{
		}

		public override void OnAwake()
		{
			base.OnAwake();
            electricResourceIndex = this.resHandler.inputResources.FindIndex(x => x.name == "ElectricCharge");
            if (electricResourceIndex >= 0)
                baseElectricRate = this.resHandler.inputResources[electricResourceIndex].rate;
            if (cryoCoolerEfficiency == null)
                cryoCoolerEfficiency = new FloatCurve();
            if (cryoCoolerEfficiency.Curve.keys.Length == 0)
                cryoCoolerEfficiency.Add(0f, 0.7f);
		}

		public override void OnStart(StartState state)
		{
			base.OnStart (state);
            this.Dfld_RefrCost = base.Fields ["D_RefrCost"];
		}

		public new void FixedUpdate()
		{
			base.FixedUpdate();

            if (this.Dfld_RefrCost != null)
                this.Dfld_RefrCost.guiActive = PhysicsGlobals.ThermalDataDisplay;
		}

        public override string GetInfo()
        {
            string info = base.GetInfo();
            string cryoInfo = "";

            if (maxCryoElectricCost > 0)
            {
                cryoInfo = "\n<b><color=#99ff00ff>Max Cryogenic Cooling</color></b>\n";
                cryoInfo += " <b><color=#00C3FFff>@20K = " + (CoolingEfficiency(20, 300) * maxCryoElectricCost).ToString("F2") + "</color>\n";
                cryoInfo += " <b><color=#00C3FFff>@90K = " + (CoolingEfficiency(90, 300) * maxCryoElectricCost).ToString("F2") + "</color>\n";
            }
            else
            {
                cryoInfo = "No cryogenic cooling capacity.\n";
            }

            return info + cryoInfo;

        }


        protected void AdjustResourceRates()
        {
            // How to handle this... walk through reshandler resources and adjust EC if present?
            // maybe later do something more robust such as having a list of resources to adjust here along with rates...

        }

        //<summary>
        // Returns the % of Carnot Efficiency of the cryocooler.
        //</summary>
        public double CoolingEfficiency(double coolingTemp, double hotTemp)
        {
            return coolingTemp / (hotTemp - coolingTemp) * cryoCoolerEfficiency.Evaluate((float)coolingTemp);
        }



        protected override void InternalCooling(RadiatorData rad, int radCount)
        {
            // TODO hard coding this for ElectricCharge but I may do something like this for all resource inputs IF there are likely to be other resource inputs
            // and IF those other inputs are likely to need scaling...
            double coolingEfficiency = 1;
            double refrigerationCost = 0;
            if (this.Dfld_RadCount.guiActive)
            {
                this.D_RadCount = radCount.ToString();
            }
            this.coolParts.Clear ();
            this.hotParts.Clear ();
            int count = this.nonRadiatorParts.Count;
            while (count-- > 0)
            {
                Part part = this.nonRadiatorParts[count];
                if (part.temperature > part.maxTemp * part.radiatorMax)
                {
                    this.hotParts.Add(part);
                }
            }
            int count2 = this.hotParts.Count;
            for (int i = 0; i < count2; i++)
            {
                Part part2 = this.hotParts[i];
                bool flag = true;
                if (this.parentCoolingOnly)
                {
                    flag = this.IsSibling(part2);
                }
                if (flag)
                {
                    double num = base.part.temperature * this.overcoolFactor;
                    if (part2.temperature > num)
                    {
                        RadiatorData thermalData = RadiatorUtilities.GetThermalData(part2);
                        double num2 = thermalData.Energy - thermalData.MaxEnergy;
                        if (num2 > 0.0)
                        {
                            this.coolParts.Add(thermalData);
                        }
                    }
                }
            }

            int cooledParts = this.coolParts.Count;
            // Would have liked to do this part as coolParts was being built so the list doesn't have to be walked through twice
            // but I don't know the amount of flux until after it's done being built - so if I want to throttle refrigeration I have to do this.
            for (int i = 0; i < cooledParts; i++)
            {
                if (coolParts[i].Part.temperature < base.part.skinTemperature)
                {
                    RadiatorData radiatorData = this.coolParts[i];
                    coolingEfficiency = CoolingEfficiency(coolParts[i].Part.temperature, base.part.skinTemperature);
                    double _maxCryoEnergyTransfer = maxCryoElectricCost * coolingEfficiency;
                    double excessHeat = (radiatorData.Energy - radiatorData.MaxEnergy) / (double)TimeWarp.fixedDeltaTime;
                    excessHeat /= (double)(radCount + cooledParts);
                    double val = Math.Min(rad.EnergyCap - rad.Energy, _maxCryoEnergyTransfer) / (double)TimeWarp.fixedDeltaTime;
                    double liftedHeatFlux = Math.Min(val, excessHeat) * Math.Min(1.0, this.energyTransferScale);

                    refrigerationCost += Math.Min(maxCryoElectricCost, liftedHeatFlux / coolingEfficiency);
                }
            }
            if (electricResourceIndex >= 0)
            {
                this.resHandler.inputResources[electricResourceIndex].rate = baseElectricRate + refrigerationCost;

                refrigerationThrottle = this.resHandler.UpdateModuleResourceInputs(ref this.status, 1.0, 0.9, false, true, true);
                if (refrigerationThrottle < 1.0)
                {
                    refrigerationThrottle *= 0.90;
                    refrigerationCost *= refrigerationThrottle;
                    this.resHandler.inputResources[electricResourceIndex].rate = baseElectricRate + (refrigerationCost);
                }
                if (this.Dfld_RefrCost.guiActive)
                    this.D_RefrCost = refrigerationCost.ToString("F4");
            }


            if (this.Dfld_CoolParts.guiActive)
            {
                this.D_CoolParts = StringBuilderCache.Format("{0}/{1}", new object[]
                {
                    cooledParts,
                    this.hotParts.Count
                });
            }
            for (int j = 0; j < cooledParts; j++)
            {
                RadiatorData radiatorData = this.coolParts[j];
                coolingEfficiency = CoolingEfficiency(coolParts[j].Part.temperature, base.part.skinTemperature);
                double _maxCryoEnergyTransfer = maxCryoElectricCost * coolingEfficiency;
                double excessHeat = (radiatorData.Energy - radiatorData.MaxEnergy) / (double)TimeWarp.fixedDeltaTime;
                excessHeat /= (double)(radCount + cooledParts);
                double _maxEnergyTransfer = radiatorData.Part.temperature >= base.part.skinTemperature ? this.maxEnergyTransfer : _maxCryoEnergyTransfer;
                double val = Math.Min(rad.EnergyCap - rad.Energy, _maxEnergyTransfer) / (double)TimeWarp.fixedDeltaTime;
                double liftedHeatFlux = Math.Min(val, excessHeat);
                if (this.Dfld_XferBase.guiActive)
                {
                    this.D_XferBase = liftedHeatFlux.ToString();
                }
                liftedHeatFlux *= Math.Min(1.0, this.energyTransferScale);

                if (radiatorData.Part.temperature < base.part.skinTemperature)
                    liftedHeatFlux *= refrigerationThrottle;
                
                if (liftedHeatFlux > 0.0 && !base.vessel.IsFirstFrame())
                {
                    radiatorData.Part.AddThermalFlux(-liftedHeatFlux);
                    base.part.AddThermalFlux(liftedHeatFlux);
                }
                if (this.Dfld_Excess.guiActive)
                {
                    this.D_Excess = excessHeat.ToString();
                }
                if (this.Dfld_HeadRoom.guiActive)
                {
                    this.D_HeadRoom = val.ToString();
                }
                if (this.Dfld_XferFin.guiActive)
                {
                    this.D_XferFin = liftedHeatFlux.ToString();
                }
            }
        }

		public void print(string msg)
		{
			MonoBehaviour.print ("[ModuleRealActiveRadiator] " + msg);
		}
	}
}

