##Introduction

The Automated Guidance Computer (AGC) is a part that you can attach to your vessel. The AGC lets you run flight control programs that you code in the LISP programming language. For example, you could write a program to automatically lower the landing gear when you get below a set altitude, or a program to damp manual control inputs as speed increases, or even a program that implements a three-axis PID controller to follow a specific ascent path.

##Installation

Copy these folders to your KSP folder, merging them.

##How to use

- Place the AGC (Automated Guidance Computer) on your ship. In career mode, you need to unlock the Flight Control research node. The AGC lets you run programs that you code in the LISP programming language. Put your programs in the Plugins/PluginData/AGC folder with a txt extension. Look at autoabort.txt for an example. The stdlib.txt (stdlib is short for Standard Library) code gets executed when the computer boots up and allows you to implement common library functions to share functionality between your programs.
- To run a program, put the AGC on a ship in KSP, right click on the AGC and Toggle UI. Put autoabort in for the program name, then click Toggle Computer. The computer will start executing the autoabort program, which activates the Abort action group when the vertical speed is significantly negative and the altitude is still low. Take a look at the autoabort.txt file and examine the source code. Note that it uses the library function makeDebounceOneshot to set up an interlock that will fire when the condition is consecutively true for a given number of seconds.
- I also provided a speedlimit.txt example, which will reduce the maximum throttle allowed to try and keep the speed from exceeding the limit set in the input field. If you run this program on a spaceplane, for example, and set the limit to 150, the throttle will automatically reduce to try and keep your speed from going too much over 150 m/s.
- Multiple programs can be run on the same AGC part at the same time. These programs share the same environment, except for the AGC.Input and AGC.Status variables.
- Press the Debug button to dump out the computer's current state (LISP environment) to Plugins/PluginData/AGC/debug.txt.

##LISP Dialect Notes

- The quotation notation `'` is not supported, but you can use the built-in quote function to do the same thing, though not as succinctly.
- Lisp dot notation for dotted lists is not supported.
- The `cond` function is not supported because if no predicates match, the result is undefined.
- The empty list `()` is equivalent to `(quote ())`.
- If you pass `car` a non-list argument, it will simply return it. So, `(car 5)` evaluates to 5.
- If you pass `cdr` a non-list argument, it will return the empty list `()`.
- Your program gets evaluated each computer tick. To carry state between ticks, define variables. To make this easier, the `initialize` built-in function is the same as `define`, except it does nothing if the symbol is already defined.
- In Lisp, function arguments are evaluated prior to applying them to the function, except for some special cases like `if` and `lambda`. If you are new to Lisp, I recommend [*The Structure and Interpretation of Computer Programs*](http://mitpress.mit.edu/sicp/), a free ebook published by MIT.

##Built-in Functions

- `if`
- `quote`
- `set!`
- `define`
- `initialize`
- `lambda`
- `begin`
- `+`
- `-`
- `*`
- `/`
- `not`
- `and`
- `or`
- `>`
- `<`
- `>=`
- `<=`
- `=`
- `equal?`
- `eq?`
- `length`
- `cons`
- `car`
- `cdr`
- `append`
- `list`
- `list?`
- `null?`
- `symbol?`
- `defined?`
- `modulo`
- `abs`
- `floor`
- `ceiling`
- `min`
- `max`
- `apply`
- `id`
- `sqrt`
- `let`

##Interface functions

Inside your programs you can use special functions to interface with your vessel. Note that the Staging functions only work when the vessel is active.

- `Staging.ActivateNextStage`
- `Staging.ActivateStage` - expects a single numeric argument

Functions expecting a single boolean argument:

- `ActionGroups.SetGroupStage`
- `ActionGroups.SetGroupGear`
- `ActionGroups.SetGroupLight`
- `ActionGroups.SetGroupRCS`
- `ActionGroups.SetGroupSAS`
- `ActionGroups.SetGroupBrakes`
- `ActionGroups.SetGroupAbort`
- `ActionGroups.SetGroupCustom01`
- `ActionGroups.SetGroupCustom02`
- `ActionGroups.SetGroupCustom03`
- `ActionGroups.SetGroupCustom04`
- `ActionGroups.SetGroupCustom05`
- `ActionGroups.SetGroupCustom06`
- `ActionGroups.SetGroupCustom07`
- `ActionGroups.SetGroupCustom08`
- `ActionGroups.SetGroupCustom09`
- `ActionGroups.SetGroupCustom10`

For the functions starting with `vessel` below, you can use `target` instead for the corresponding data about your target. Use `vessel.hasTarget` to check if you have a target.

Functions taking no arguments, returning number:

- `vessel.verticalSpeed`
- `vessel.staticPressure`
- `vessel.geeForce`
- `vessel.currentStage`
- `vessel.specificAcceleration`
- `vessel.heightFromTerrain`
- `vessel.pqsAltitude`
- `vessel.terrainAltitude`
- `vessel.heightFromSurface`
- `vessel.Landed`
- `vessel.missionTime`
- `vessel.longitude`
- `vessel.latitude`
- `vessel.altitude`
- `vessel.GetTotalMass`
- `vessel.obt_speed`
- `vessel.srfSpeed`
- `vessel.horizontalSrfSpeed`

Functions taking no arguments, returning 3-dimensional lists of numbers:

- `vessel.acceleration`
- `vessel.angularMomentum`
- `vessel.angularVelocity`
- `vessel.CoM`
- `vessel.MOI`
- `vessel.obt_velocity`
- `vessel.srf_velocity`
- `vessel.upAxis`
- `vessel.vesselTransform.eulerAngles`
- `vessel.vesselTransform.forward`

Functions taking 3-element list, returning number:

- `vessel.orbit.getOrbitalSpeedAtPos`
- `vessel.orbit.getOrbitalSpeedAtRelativePos`
- `vessel.orbit.GetTrueAnomalyOfZupVector`

Functions taking a number, returning number:
- `vessel.orbit.getObtAtUT`
- `vessel.orbit.getObTAtMeanAnomaly`
- `vessel.orbit.GetEccentricAnomaly`
- `vessel.orbit.RadiusAtTrueAnomaly`
- `vessel.orbit.TrueAnomalyAtRadius`
- `vessel.orbit.TrueAnomalyAtT`
- `vessel.orbit.getOrbitalSpeedAt`
- `vessel.orbit.getOrbitalSpeedAtDistance`
- `vessel.orbit.getTrueAnomaly`

Functions taking a number, returning 3-element list:

- `vessel.orbit.getPositionAtT`
- `vessel.orbit.getPositionFromEccAnomaly`
- `vessel.orbit.getPositionFromMeanAnomaly`
- `vessel.orbit.getPositionFromTrueAnomaly`
- `vessel.orbit.getRelativePositionAtT`
- `vessel.orbit.GetFrameVelAtUT`
- `vessel.orbit.getOrbitalVelocityAtObT`
- `vessel.orbit.getTruePositionAtUT`

Functions taking a number, returning number:

- `vessel.orbit.GetDTforTrueAnomaly`
- `vessel.orbit.GetUTforTrueAnomaly`
- `vessel.orbit.GetMeanAnomaly`

Functions returning a number:

- `vessel.orbit.ApA`
- `vessel.orbit.ApR`
- `vessel.orbit.PeA`
- `vessel.orbit.PeR`
- `vessel.orbit.semiLatusRectum`
- `vessel.orbit.semiMinorAxis`
- `vessel.orbit.altitude`
- `vessel.orbit.argumentOfPeriapsis`
- `vessel.orbit.ClAppr`
- `vessel.orbit.ClEctr1`
- `vessel.orbit.ClEctr2`
- `vessel.orbit.closestTgtApprUT`
- `vessel.orbit.CrAppr`
- `vessel.orbit.E`
- `vessel.orbit.eccentricAnomaly`
- `vessel.orbit.eccentricity`
- `vessel.orbit.EndUT`
- `vessel.orbit.epoch`
- `vessel.orbit.FEVp`
- `vessel.orbit.FEVs`
- `vessel.orbit.fromE`
- `vessel.orbit.fromV`
- `vessel.orbit.inclination`
- `vessel.orbit.LAN`
- `vessel.orbit.mag`
- `vessel.orbit.meanAnomaly`
- `vessel.orbit.meanAnomalyAtEpoch`
- `vessel.orbit.nearestTT`
- `vessel.orbit.nextTT`
- `vessel.orbit.ObT`
- `vessel.orbit.ObTAtEpoch`
- `vessel.orbit.orbitalEnergy`
- `vessel.orbit.orbitalSpeed`
- `vessel.orbit.orbitPercent`
- `vessel.orbit.period`
- `vessel.orbit.radius`
- `vessel.orbit.sampleInterval`
- `vessel.orbit.semiMajorAxis`
- `vessel.orbit.SEVp`
- `vessel.orbit.SEVs`
- `vessel.orbit.StartUT`
- `vessel.orbit.timeToAp`
- `vessel.orbit.timeToPe`
- `vessel.orbit.timeToTransition1`
- `vessel.orbit.timeToTransition2`
- `vessel.orbit.toE`
- `vessel.orbit.toV`
- `vessel.orbit.trueAnomaly`
- `vessel.orbit.UTappr`
- `vessel.orbit.UTsoi`
- `vessel.orbit.V`

Functions returning a 3-element list:

- `vessel.orbit.an`
- `vessel.orbit.eccVec`
- `vessel.orbit.h`
- `vessel.orbit.pos`
- `vessel.orbit.secondaryPosAtTransition1`
- `vessel.orbit.secondaryPosAtTransition2`
- `vessel.orbit.vel`
- `vessel.orbit.GetANVector`
- `vessel.orbit.GetEccVector`
- `vessel.orbit.GetFrameVel`
- `vessel.orbit.GetOrbitNormal`
- `vessel.orbit.GetRelativeVel`
- `vessel.orbit.GetVel`
- `vessel.orbit.GetWorldSpaceVel`

These functions can be supplied with an argument to set the value, or used with no argument to simply return the currently set value:

- `ctrlState.mainThrottle (0..1)`
- `ctrlState.pitch (-1..1)`
- `ctrlState.roll`
- `ctrlState.yaw`
- `ctrlState.X (RCS translation)`
- `ctrlState.Y`
- `ctrlState.Z`
- `ctrlState.killRot (SAS enable)`

##AGC Variables

Unlike above, these AGC variables are not functions, they are variables.

Input variables:

- `AGC.Input` - the user-entered AGC UI value for your program, as a number
- `AGC.dt` - tick delta-time in seconds
- `AGC.TickCount` - number of AGC computer ticks since bootup
- `AGC.Runtime` - the number of seconds since bootup

Output variables:

- `AGC.Status` - define this variable to output values in the AGC UI
