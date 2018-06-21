using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using KSP.Localization;
using Radiators;

namespace AdvancedActiveRadiators
{
    public class ModuleFixedCoreHeat : ModuleCoreHeat
    {
        public ModuleFixedCoreHeat()
        {
        }


        protected override void MoveCoreEnergyToRadiators(double excess, double deltaTime)
        {
            this.vRadList.Clear();
            this.pRadList.Clear();
            double num = 0.0;
            int count = this.activeRadiatorParts.Count;
            for (int i = 0; i < count; i++)
            {
                Part part = this.activeRadiatorParts[i];
                ModuleActiveRadiator moduleActiveRadiator = part.FindModuleImplementing<ModuleActiveRadiator>();
                ModuleDeployableRadiator moduleDeployableRadiator = part.FindModuleImplementing<ModuleDeployableRadiator>();
                if (!(moduleDeployableRadiator != null) || moduleDeployableRadiator.deployState == ModuleDeployablePart.DeployState.EXTENDED)
                {
                    if (moduleActiveRadiator.IsCooling)
                    {
                        if (!moduleActiveRadiator.parentCoolingOnly || moduleActiveRadiator.IsSibling(base.part))
                        {
                            num += moduleActiveRadiator.maxEnergyTransfer;
                            if (moduleActiveRadiator.parentCoolingOnly)
                            {
                                this.pRadList.Add(part);
                            }
                            else
                            {
                                this.vRadList.Add(part);
                            }
                        }
                    }
                }
            }
            int num2 = this.pRadList.Count + this.vRadList.Count;
            if (num2 == 0)
            {
                return;
            }
            double num3 = 0.0;
            count = this.coreHeatParts.Count;
            for (int j = 0; j < count; j++)
            {
                Part part2 = this.coreHeatParts[j];
                ModuleCoreHeat moduleCoreHeat = part2.FindModuleImplementing<ModuleCoreHeat>();
                double num4 = moduleCoreHeat.MaxCoolant * moduleCoreHeat.radiatorHeatingFactor;
                if (moduleCoreHeat.CoreTemperature > part2.temperature)
                {
                    int num5 = this.vRadList.Count;
                    int count2 = this.pRadList.Count;
                    for (int k = 0; k < count2; k++)
                    {
                        Part part3 = this.pRadList[k];
                        ModuleActiveRadiator moduleActiveRadiator2 = part3.FindModuleImplementing<ModuleActiveRadiator>();
                        if (moduleActiveRadiator2.IsSibling(part2))
                        {
                            num5++;
                        }
                    }
                    num4 *= (double)(num5 / num2);
                    if (moduleCoreHeat.CoreTemperature < moduleCoreHeat.CoreTempGoal + moduleCoreHeat.CoreTempGoalAdjustment)
                    {
                        num4 = 0.0;
                    }
                    num3 += num4;
                }
            }
            if (num3 < 1E-09)
            {
                return;
            }
            double num6 = this.MaxCoolant * this.radiatorHeatingFactor;
            if (this.CoreTemperature < this.CoreTempGoal + this.CoreTempGoalAdjustment)
            {
                num6 = 0.0;
            }
            double num7 = num6 / num3;
            double val = num * num7 * deltaTime;
            val = Math.Min(val, this.MaxCoolant * deltaTime);
            double num8 = Math.Min(val, excess);
            double heatFromCore = num8 * this.radiatorCoolingFactor;
            double heatToRadiator = num8 * this.radiatorHeatingFactor;
            this.AddEnergyToCore(-heatFromCore);
            this.vRadList.AddRange(this.pRadList);
            count = this.vRadList.Count;
            for (int l = 0; l < count; l++)
            {
                Part part4 = this.vRadList[l];
                ModuleActiveRadiator moduleActiveRadiator3 = part4.FindModuleImplementing<ModuleActiveRadiator>();
                double num11 = moduleActiveRadiator3.maxEnergyTransfer / num;
                if (this.Dfld_POT.guiActive)
                {
                    this.D_POT = num11.ToString();
                }
                double xferToPart = heatToRadiator * num11 / deltaTime;
                if (this.Dfld_XTP.guiActive)
                {
                    this.D_XTP = xferToPart.ToString();
                }
                this.AddFluxToRadiator(part4, xferToPart, deltaTime);
            }
            if (this.Dfld_Excess.guiActive)
            {
                this.D_Excess = excess.ToString();
            }
            if (this.Dfld_RadCap.guiActive)
            {
                this.D_RadCap = num.ToString();
            }
            if (this.Dfld_RadSat.guiActive)
            {
                this.D_RadSat = num3.ToString();
            }
            if (this.Dfld_TRU.guiActive)
            {
                this.D_TRU = val.ToString();
            }
            if (this.Dfld_CoolPercent.guiActive)
            {
                this.D_CoolPercent = num7.ToString();
            }
            if (this.Dfld_RCA.guiActive)
            {
                this.D_RCA = val.ToString();
            }
            if (this.Dfld_CoolAmt.guiActive)
            {
                this.D_CoolAmt = num8.ToString();
            }
        }
    }
}
