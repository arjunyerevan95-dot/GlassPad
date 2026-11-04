# Native binding observations

Queen's Domain 1.01.03. Controls page inspected directly on this Windows installation.
Gamepad enabled; style Xbox; analog controls ON; swap sticks OFF; gamepad horizontal
and vertical inversion OFF. Look sensitivity 1.00, look acceleration 1.0. Stick
Deadzone 0.16. Tap To Run - Gamepad ON. Hold Interact To Run OFF.

Actual binding pages match all researched Xbox mappings: movement LS, Run L3,
look RS, Primary RT, Secondary LT, Special Y, Wind Dash RB, Throw LB, Jump A,
Interact X, pause Menu/Start, status View/Back, next throw D-pad Left, next weapon
D-pad Right, Use Item B, next item Up, previous item Down. Previous throwing
weapon and previous weapon are unbound. Direct weapon-slot entries are unbound.
No direct weapon slot actions were invented in this profile.

Approved profile sprint: deliberate double tap in floating movement zone sends
ONE 100 ms L3 pulse. It does not claim to detect the game's current sprint state.
Game-deadzone compensation changed from provisional 0.18 to observed 0.16.
Sensitivity/response/maximum turn rate and attack charge/release gameplay tests
remain separate from these menu observations.

No game option/binding was changed during inspection. System0.sav hash before
and after inspection remained 496E64354DB951742835CAB11027C53DF3D218BE6E40E40D7DB5C7CFE363F95F.
Private local backup of saves and config created before starting gameplay tests;
not included in the repository or downloadable artifacts.

Performance attempt: existing PresentMon2.6.0 refuses ETW start (access denied).
Needs administrator or Performance Log Users membership. No privilege/membership
or graphics/TDP/frame-cap settings changed. No performance claim from this attempt.
