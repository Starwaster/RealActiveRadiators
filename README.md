Realistic version of KSP stock ModuleActiveRadiator class.

Has the following features:

* Can always cool parts regardless of ambient temperature. (unlike real radiators, stock KSP radiators will not pull heat if the ambient temperature is hotter than the radiator's temperature)
* If the cooled part is colder than the radiator's temperature then an added refrigeration cost (in ElectricCharge) will be incurred.

Refrigeration cost requires that the radiator config has an ElectricCharge RESOURCE input. The actual cost is static and determined by the maxCryoElectricCost field and a maximum cooling rate will be determined based on the formula: electric cost * coolingTemp / (radiator temp - coolingTemp) * cryo cooler efficiency (for the cool temperature)

cryoCoolerEfficiency uses a FloatCurve to map the efficiency of the cryocooler over a range of temperatures and is configurable via the part cfg (or upgraded via stock upgrade nodes)

Refrigeration cost requires that the radiator config has an ElectricCharge RESOURCE. If there is not enough electricity then refrigeration will be scaled back accordingly. Normal radiator cooling will still occur as long as the configured resource inputs can be satisfied. Refrigeration efficiency is determined according to the Carnot equation which determines the ideal refrigeration cost for a given rate of cooling (or conversely, the amount of cooling for a given input of power). The equation is cold temp / (hot temp - cold temp). This is the ideal efficiency assuming 100% efficient cooling apparatus. Such perfect cooling is not possible however which is where cryoCoolerEfficiency. This is the percent of Carnot so the final equation is cold temp / (hot temp - cold temp) * cryoCoolerEfficiency.
