# Ninja (NIN)

_✅ works · ⚠️ partial · ❌ not simulated · ➖ nothing to simulate (pure damage/heal/mitigation, or a native game button-swap)._

## Actions
| Action | ID | Status | Note |
|--------|----|:------:|------|
| Spinning Edge | 2240 | ✅ | Ninki +5 |
| Shade Shift | 2241 | ➖ | |
| Gust Slash | 2242 | ✅ | combo Ninki +5 |
| Hide | 2245 | ❌ | grants Hidden (614); not set (OOC-only, low impact) |
| Throwing Dagger | 2247 | ✅ | Ninki +5 |
| Mug | 2248 | ➖ | target debuff; → Dokumori at 66 |
| Trick Attack | 2258 | ✅ | consumes Shadow Walker |
| Aeolian Edge | 2255 | ✅ | combo Ninki +15; Kazematoi −1 |
| Ten | 2259 | ❌ | mudra; swap not simulable |
| Ninjutsu | 2260 | ❌ | swap host; not simulable |
| Chi | 2261 | ❌ | mudra; not simulable |
| Death Blossom | 2254 | ✅ | Ninki +5 |
| Assassinate | 2246 | ➖ | |
| Shukuchi | 2262 | ➖ | |
| Jin | 2263 | ❌ | mudra; not simulable |
| Kassatsu | 2264 | ✅ | grant + native Goka/Hyosho swap |
| Hakke Mujinsatsu | 16488 | ✅ | combo Ninki +5 |
| Armor Crush | 3563 | ✅ | combo Ninki +15; Kazematoi +2 |
| Dream Within a Dream | 3566 | ➖ | |
| Hellfrog Medium | 7401 | ✅ | Ninki −50 (native cost); Deathfrog swap via Higi |
| Dokumori | 36957 | ✅ | Ninki +40; grants Higi |
| Bhavacakra | 7402 | ✅ | Ninki −50 (native cost) |
| Ten Chi Jin | 7403 | ✅ | TCJ + Tenri Jindo Ready set; inner mudra mode not simulable |
| Meisui | 16489 | ✅ | Ninki +50; grants Meisui; dispels Shadow Walker |
| Bunshin | 16493 | ✅ | Ninki −50; Bunshin + PK Ready; per-hit +5 trickle not modelled |
| Phantom Kamaitachi | 25774 | ✅ | Ninki +10; consumes PK Ready |
| Hollow Nozuchi | 25776 | ➖ | |
| Forked Raiju | 25777 | ✅ | Ninki +5; consumes Raiju Ready |
| Fleeting Raiju | 25778 | ✅ | Ninki +5; consumes Raiju Ready |
| Kunai's Bane | 36958 | ✅ | consumes Shadow Walker |
| Deathfrog Medium | 36959 | ✅ | Ninki −50; consumes Higi (3850). |
| Zesho Meppo | 36960 | ✅ | Ninki −50; consumes Higi |
| Tenri Jindo | 36961 | ✅ | consumes Tenri Jindo Ready |
| Fuma Shuriken | 2265 | ➖ | requires mudra to cast |
| Katon | 2266 | ➖ | → Goka under Kassatsu (native swap) |
| Raiton | 2267 | ⚠️ | grants Raiju Ready 3 stacks/cast; should be 1 |
| Hyoton | 2268 | ➖ | target Bind |
| Huton | 2269 | ❌ | grants Shadow Walker; no row (missing) |
| Doton | 2270 | ➖ | self status only gates Hollow Nozuchi damage |
| Suiton | 2271 | ✅ | grants Shadow Walker |
| Goka Mekkyaku | 16491 | ➖ | Kassatsu-only |
| Hyosho Ranryu | 16492 | ➖ | Kassatsu-only |

## Traits
| Trait | Status | Note |
|-------|:------:|------|
| Enhanced Raiton | ⚠️ | grants Raiju Ready stacks — per-cast stack count wrong (Raiton bug) |

## Role actions
Second Wind, Leg Sweep, Bloodbath, Feint, Arm's Length, True North — all ➖.

## Not simulated
- **Mudra → Ninjutsu (Ten/Chi/Jin → Ninjutsu) not simulable.** The swap is computed by native `ProcessDeferredReplaceAction` from an internal ordered 3-mudra sequence value FFXIVClientStructs never exposes (the ~0x0C byte is undefined and one byte can't encode the ordered sequence). No rows for Ten/Chi/Jin/Ninjutsu; Mudra status (496) is never written. Consequence: the ninjutsu kit is uncastable in-sim, so Raiton→Raiju Ready and Suiton/Huton→Shadow Walker grants are unreachable through normal play.
- **Raiton (2267) over-grants Raiju Ready.** `Status(2690, 30f, 3)` sets Param to 3 each cast → 3 Forked/Fleeting Raiju per Raiton. Should be 1 per cast, accumulating to max 3.
- **Huton (2269) → Shadow Walker (3848) missing** — no data row (Suiton has one).
- **Hide (2245) → Hidden (614) not set** (OOC-only, low impact).
- **Bunshin per-hit +5 Ninki trickle not modelled** (the shadow's autonomous attacks aren't simulated) — minor.
