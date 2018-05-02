Realistic version of KSP stock ModuleActiveRadiator class.

Has the following features:

* Can always cool parts regardles of ambient temperature. (stock radiators will not function if the ambient temperature is hotter than the radiator's temperature)
* If the cooled part is colder than the radiator's skin temperature then an added refrigeration cost (in ElectricCharge) will be incurred.

Refrigeration cost requires that the radiator config has an ElectricCharge RESOURCE input and is calculated using the following formula.

refrigeration flux / ((part.temperature / (radiator.skinTemperature - part.temperature)) * cryoCoolerEfficiency)

Refrigeration cost requires that the radiator config has an ElectricCharge RESOURCE. If there is not enough electricity then refrigeration will be scaled back accordingly. Normal radiator cooling will still occur as long as the configured resource inputs can be satisfied.
