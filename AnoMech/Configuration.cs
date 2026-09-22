using Dalamud.Configuration;
using System;

namespace AnoMech;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public bool OpenSimMenuOnInn { get; set; } = true;
    public bool OpenSimMenuOnSupportedInstanceSolo { get; set; } = false;
    public bool EnableEventLogging { get; set; } = false;
    public bool SuppressBgm { get; set; } = false;
    public bool DsrMusicInitialized { get; set; }

    // Resolve the player's own actions client-side, since the sim firewall blocks
    // the server responses that normally grant them. Sprint is always resolved;
    // this gates everything else (shared actions + per-job kits).
    public bool EnableUserActions { get; set; } = true;

    // Seconds before a player cast finishes during which it can no longer be
    // interrupted by move/jump/cancel — the slidecast window the server's
    // ActionEffect ack opens (measured ~0.5s from replay data). Gated by
    // EnableUserActions.
    public float CastInterruptThreshold { get; set; } = 0.5f;

    // Firewall opcode config — updated automatically by OpcodeUpdater on game version change.
    public uint[] ZoneDownOpcodes { get; set; } = [];
    public string ZoneFirewallGameVersion { get; set; } = "";

    // Safe mode (incoming packet firewall):
    //   true  — only ZoneDownOpcodes pass; cuts you off from server traffic
    //           (no party join/leave updates, no ready checks, no duty pops).
    //   false — all incoming packets pass to the engine. You'll see popups
    //           and party updates, but it's easier to break the sim zone.
    // The send-side firewall stays on either way: nothing the client does in
    // the sim zone leaks back to the server.
    public bool SafeMode { get; set; } = true;

    // The below exists just to make saving less cumbersome
    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
