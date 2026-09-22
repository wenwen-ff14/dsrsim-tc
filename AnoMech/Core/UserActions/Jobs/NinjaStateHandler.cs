namespace AnoMech.Core.UserActions.Jobs;

// BLOCKED: the Mudra (Ten 2259 / Chi 2261 / Jin 2263) -> Ninjutsu button swap has no writable
// state the plugin can set, so it cannot be simulated here.
//
// The swap is computed by native JobGauge::ProcessDeferredReplaceAction (gauge vtable vfunc 9)
// from an internal mudra-combination value. That value is not an exposed gauge field:
// FFXIVClientStructs' NinjaGauge defines only Ninki + Kazematoi (both already handled by the data
// table); the mudra byte at ~0x0C is commented out and unidentified ("NinjutsuStarted?
// FirstMudraUsed?") and has never been a defined field in the struct's history. One byte also
// can't encode the *ordered* 3-mudra sequence that selects among the nine ninjutsu (Ten->Chi and
// Chi->Ten yield different results). Dalamud's NINGauge wrapper exposes no mudra accessor either.
//
// The Mudra status (496) is only the Ninjutsu cost gate (action 2260 has cost=10:496, maxStacks=0),
// not a sequence selector, so writing it can't drive the swap. And both the mudra state (a server
// JobGauge packet) and the Mudra status grant (a server StatusEffectList packet) are firewalled, so
// pressing mudras advances nothing client-side. Writing a raw offset byte would be inventing a
// field the struct doesn't define, with an unknown encoding the native swap wouldn't read correctly.
internal sealed class NinjaStateHandler : IUserActionHandler { }
