using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using KSP.Localization;
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

        [KSPField]
        public double cryoEnergyTransferScale = 1;

        [KSPField]
        protected bool cooledByOtherRadiators = false;

        //[KSPField]
        //protected double coolTemperatureThreshold = 297.6;

        protected static string cacheAutoLOC_232071;
        protected static string cacheAutoLOC_232067;
        protected static string cacheAutoLOC_232036;

        [KSPField(guiName = "AR:RefrCost", guiActive = false)]
        public string D_RefrCost = "???";

        protected int electricResourceIndex = -1;
        protected int ECID = -1;
        protected double baseElectricRate = 0;
        protected double refrigerationThrottle = 1;
        protected BaseField Dfld_RefrCost;
        protected List<Part> compensatedParts = new List<Part> ();

		public ModuleRealActiveRadiator () : base()
		{
		}

        internal static void CacheLocalStrings()
        {
            ModuleRealActiveRadiator.cacheAutoLOC_232036 = Localizer.Format("#autoLOC_232036");
            ModuleRealActiveRadiator.cacheAutoLOC_232067 = Localizer.Format("#autoLOC_232067");
            ModuleRealActiveRadiator.cacheAutoLOC_232071 = Localizer.Format("#autoLOC_232071");
        }
		public override void OnAwake()
		{
			base.OnAwake();
            electricResourceIndex = this.resHandler.inputResources.FindIndex(x => x.name == "ElectricCharge");
            if (electricResourceIndex >= 0)
            {
                baseElectricRate = this.resHandler.inputResources[electricResourceIndex].rate;
                ECID = this.resHandler.inputResources[electricResourceIndex].id;
            }
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
            StringBuilder stringBuilder = new StringBuilder();
            if (this.resHandler.inputResources.Count > 0)
            {
                stringBuilder.Append(this.resHandler.PrintModuleResources(1.0));
            }
            stringBuilder.Append("\n<b><color=#99ff00ff>" + ModuleRealActiveRadiator.cacheAutoLOC_232036 + "</color></b>\n");
            double num = this.maxEnergyTransfer;
            stringBuilder.AppendFormat(Localizer.Format("#autoLOC_232038", new object[] {
                num
            }), new object[0]);
            double num2 = 0.0;
            int count = base.part.DragCubes.Cubes.Count;
            while (count-- > 0)
            {
                DragCube dragCube = base.part.DragCubes.Cubes[count];
                double num3 = (double)(dragCube.Area[0] + dragCube.Area[1] + dragCube.Area[2] + dragCube.Area[3] + dragCube.Area[4] + dragCube.Area[5]);
                if (num3 > num2)
                {
                    num2 = num3;
                }
            }
            if (num2 != 0.0)
            {
                double num4 = base.part.skinMaxTemp * base.part.radiatorHeadroom;
                num4 *= num4;
                num4 *= num4;
                stringBuilder.Append(Localizer.Format("#autoLOC_232057", new string[] {
                    (num2 * base.part.emissiveConstant * num4 * PhysicsGlobals.StefanBoltzmanConstant * 0.001).ToString ("F0")
                }));
            }
            StringBuilder stringBuilder2 = new StringBuilder();
            stringBuilder2.AppendFormat("{0:0.00}", this.energyTransferScale * 100.0);
            stringBuilder.Append(Localizer.Format("#autoLOC_232060", new string[] {
                stringBuilder2.ToString ()
            }));
            {
                stringBuilder.Append(ModuleRealActiveRadiator.cacheAutoLOC_232067);
            }
            if (this.parentCoolingOnly)
            {
                stringBuilder.Append(ModuleRealActiveRadiator.cacheAutoLOC_232071);
            }

            if (maxCryoElectricCost > 0)
            {
                stringBuilder.Append("\n<b><color=#99ff00ff>Max Cryogenic Cooling</color></b>\n");
                stringBuilder.Append(" <b><color=#00C3FFff>@20K = " + (CoolingEfficiency(20, 300) * maxCryoElectricCost).ToString("F2") + "kW" + "</color>\n");
                                     stringBuilder.Append(" <b><color=#00C3FFff>@90K = " + (CoolingEfficiency(90, 300) * maxCryoElectricCost).ToString("F2") + "kW" + "</color>\n");
            }
            else
            {
                stringBuilder.Append("No cryogenic cooling capacity.\n");
            }

            return stringBuilder.ToString();
        }

        protected override void CheckPartCaches()
        {
            if (!this.partCachesDirty)
            {
                return;
            }
            this.activeRadiatorParts.Clear();
            this.nonRadiatorParts.Clear();
            this.compensatedParts.Clear();
            int count = base.vessel.parts.Count;
            ModuleRealActiveRadiator radPart;
            while (count-- > 0)
            {
                Part part = base.vessel.parts[count];
                if (radPart = (ModuleRealActiveRadiator)part.FindModuleImplementing<ModuleActiveRadiator>())
                {
                    if (!radPart.cooledByOtherRadiators)
                        this.activeRadiatorParts.Add(part);
                }
                else
                {
                    this.nonRadiatorParts.Add(part);
                    if (part.Modules.Contains("ModuleFuelTanks"))
                    {
                        this.compensatedParts.Add(part);
                    }
                }
            }
            this.partCachesDirty = false;
        }

        //<summary>
        // Returns the cooling efficiency taking into account both Carnot and efficiency % of Carnot of the cooler.
        // This ONLY is valid for conditions where refrigeration is happening. Otherwise stock radiator conditions apply.
        //</summary>
        public double CoolingEfficiency(double coolingTemp, double hotTemp)
        {
            return Math.Max(coolingTemp / (hotTemp - coolingTemp) * cryoCoolerEfficiency.Evaluate((float)coolingTemp), 0);
        }



        protected override void InternalCooling(RadiatorData thisRadiator, int radCount)
        {
            if (!base.vessel.IsFirstFrame())
            {
                // TODO hard coding this for ElectricCharge but I may do something like this for all resource inputs IF there are likely to be other resource inputs
                // and IF those other inputs are likely to need scaling...
                double coolingEfficiency = 1;
                double refrigerationCost = 0;
                if (this.Dfld_RadCount.guiActive)
                {
                    this.D_RadCount = radCount.ToString();
                }
                this.coolParts.Clear();
                this.hotParts.Clear();
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
                        RadiatorData thermalData = RadiatorUtilities.GetThermalData(part2);
                        double energyDifference = thermalData.Energy - thermalData.MaxEnergy;
                        if (energyDifference > 0.0)
                        {
                            this.coolParts.Add(thermalData);
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
                        RadiatorData partThermalData = this.coolParts[i];
                        coolingEfficiency = CoolingEfficiency(coolParts[i].Part.temperature, base.part.skinTemperature);
                        double _maxCryoEnergyTransfer = maxCryoElectricCost * coolingEfficiency;
                        double excessHeat = (partThermalData.Energy - partThermalData.MaxEnergy);
                        excessHeat /= (double)(radCount + cooledParts);
                        double val = Math.Min(Math.Max(0, thisRadiator.EnergyCap - thisRadiator.Energy), _maxCryoEnergyTransfer);
                        double liftedHeatFlux = Math.Min(val, excessHeat) * Math.Min(1.0, cryoEnergyTransferScale) * refrigerationThrottle;

                        refrigerationCost += Math.Min(maxCryoElectricCost, liftedHeatFlux / coolingEfficiency);
                    }
                }
                for (int i = 0; i < this.compensatedParts.Count; i++)
                {
                    Part part = compensatedParts[i];
                    if (!(part.temperature < part.maxTemp * part.radiatorMax))
                    {
                        double conductionFlux = part.thermalConductionFlux + part.skinToInternalFlux;

                        if (conductionFlux > 0 && part.temperature < base.part.skinTemperature)
                        {
                            coolingEfficiency = CoolingEfficiency(part.temperature, base.part.skinTemperature);
                            conductionFlux = Math.Min(conductionFlux, maxCryoElectricCost * coolingEfficiency);
                            conductionFlux *= refrigerationThrottle;
                            refrigerationCost += conductionFlux / coolingEfficiency;
                        }
                    }
                }

                if (electricResourceIndex >= 0)
                {
                    this.resHandler.inputResources[electricResourceIndex].rate = baseElectricRate + refrigerationCost; 

                    //double powerAvailability = this.resHandler.UpdateModuleResourceInputs(ref this.status, radCount, 0.9, false, false, true);
                    double ECAmount, ECMaxAmount;
                    this.part.GetConnectedResourceTotals(ECID, PartResourceLibrary.GetDefaultFlowMode(ECID), out ECAmount, out ECMaxAmount, true);
                    // if the craft has mixed radiator types then availability calculation may be wrong
                    double powerAvailability = ECAmount / (refrigerationCost * radCount);
                    if (powerAvailability < 1.0)
                    {
                        // Looks like radiator count can include inactive radiators so this could throttle cooling down more than intended.
                        refrigerationThrottle = powerAvailability * 0.75 / radCount;
                        refrigerationCost *= refrigerationThrottle;
                        this.resHandler.inputResources[electricResourceIndex].rate = baseElectricRate + (refrigerationCost);
                    }
                    else if (refrigerationThrottle < 1.0)
                    {
                        // Try to increase the throttle if power reserves have increased to more than 10x what would be required.
                        if (powerAvailability >= 10 * refrigerationCost)
                        {
                            refrigerationThrottle += 0.001;
                            refrigerationCost *= refrigerationThrottle;
                            this.resHandler.inputResources[electricResourceIndex].rate = baseElectricRate + (refrigerationCost);
                        }
                    }
                    //this.resHandler.UpdateModuleResourceInputs(ref this.status, 1, 0.9, false, false, true);

                    refrigerationThrottle = Math.Min(refrigerationThrottle, 1);

                    if (this.Dfld_RefrCost.guiActive)
                        this.D_RefrCost = refrigerationCost.ToString("F4") + " (throttle = " + refrigerationThrottle.ToString("P0") + ")";
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
                    bool useHeatPump = radiatorData.Part.temperature < base.part.skinTemperature;
                    coolingEfficiency = CoolingEfficiency(coolParts[j].Part.temperature, base.part.skinTemperature);
                    double _maxCryoEnergyTransfer = maxCryoElectricCost * coolingEfficiency;
                    double excessHeat = (radiatorData.Energy - radiatorData.MaxEnergy);
                    excessHeat /= (double)(radCount + cooledParts);
                    double _maxEnergyTransfer = radiatorData.Part.temperature >= base.part.skinTemperature ? this.maxEnergyTransfer : _maxCryoEnergyTransfer;
                    double val = Math.Min(Math.Max(0, thisRadiator.EnergyCap - thisRadiator.Energy), _maxEnergyTransfer);
                    double liftedHeatFlux = Math.Min(val, excessHeat);
                    if (this.Dfld_XferBase.guiActive)
                    {
                        this.D_XferBase = liftedHeatFlux.ToString();
                    }
                    liftedHeatFlux *= Math.Min(1.0, useHeatPump ? cryoEnergyTransferScale : this.energyTransferScale);

                    if (useHeatPump)
                        liftedHeatFlux *= refrigerationThrottle;

                    if (liftedHeatFlux > 0.0)
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
                for (int i = 0; i < this.compensatedParts.Count; i++)
                {
                    Part part = this.compensatedParts[i];
                    if (!(part.temperature < Math.Round(part.maxTemp * part.radiatorMax * 0.99, 4)))
                    {
                        double conductionFlux = part.thermalConductionFlux + part.skinToInternalFlux;
                        if (conductionFlux > 0)
                        {
                            double compensatedFlux = Math.Min(conductionFlux, CoolingEfficiency(part.temperature, this.part.skinTemperature) * maxCryoElectricCost) * refrigerationThrottle / radCount;

                            part.AddThermalFlux(-compensatedFlux);
                            base.part.AddThermalFlux(compensatedFlux);
                        }
                    }
                }
            }
        }

		public void print(string msg)
		{
			MonoBehaviour.print ("[ModuleRealActiveRadiator] " + msg);
		}
	}
}

