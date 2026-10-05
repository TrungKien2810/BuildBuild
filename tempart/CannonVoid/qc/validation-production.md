# Form B full-set validation

- idle: 4 frames; canvas 144x128; maximum opaque bounds 87x70; 29 colors.
- attack: 6 frames; canvas 144x128; maximum opaque bounds 87x70; 30 colors.
- bullet: 4 frames; canvas 128x128; maximum opaque bounds 20x10; 12 colors.
- vortex-open: 4 frames; canvas 128x128; maximum opaque bounds 57x32; 16 colors.
- vortex-loop-0: 4 frames; canvas 128x128; maximum opaque bounds 56x27; 13 colors.
- vortex-loop-1: 4 frames; canvas 128x128; maximum opaque bounds 57x27; 14 colors.
- vortex-loop-2: 4 frames; canvas 128x128; maximum opaque bounds 58x29; 13 colors.
- absorb: 4 frames; canvas 128x128; maximum opaque bounds 26x25; 10 colors.
- collapse: 4 frames; canvas 128x128; maximum opaque bounds 55x29; 15 colors.
- bullet-impact: 6 frames; canvas 128x128; maximum opaque bounds 64x46; 21 colors.
- bullet-impact-1: 6 frames; canvas 128x128; maximum opaque bounds 64x43; 26 colors.
- bullet-impact-2: 6 frames; canvas 128x128; maximum opaque bounds 64x47; 26 colors.
- icon: 1 frames; canvas 48x48; maximum opaque bounds 42x36; 34 colors.

57 individual non-identity frames. 13 per-group sheets. 1 approved identity.
Binary alpha throughout, no pure black. Fixed base/neck pixels at y>=71 exactly match approved B. Body bottom y96.
Idle1/4 and attack1/6 match identity exactly; recoil +2/+1 logical px RIGHT.
Core/muzzle anchor (43,49); VFX anchor (64,64). Loop area58x32 at all charge levels. Impact union64x48 at all levels.
Regular-grid VFX use a shared crop/scale. Irregular collapse/impact sheets use explicit nonuniform source regions, a shared sequence scale and centered registration; no pieces cross exported frame boundaries. Absorb ends at64,64.

Manifest check: all57 frame paths and13 group sheets exist. No cooldown directories. Max-charge composite preview41frames; zero33frames; medium37frames. GIFs use explicit per-frame indexed palettes to preserve pixel colors without automatic dithering.
Body core/flash patches sampled from image_gen sheets over approved fixed identity. No gameplay changes.
Decoded all frames of three composite previews; additional per-clip GIFs saved under qc/clips/.
No cooldown assets. Timing in previews is illustrative, game timing in mechanics.md remains a proposal.
