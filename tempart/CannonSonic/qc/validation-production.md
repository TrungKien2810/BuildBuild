# Form B full production validation

- idle: 4 frames; canvas128x128; max bounds67x70; 31 colors.
- attack: 6 frames; canvas128x128; max bounds67x70; 35 colors.
- attack-shock: 6 frames; canvas128x128; max bounds67x70; 31 colors.
- rhythm: 4 frames; canvas128x128; max bounds67x70; 31 colors.
- lamp-overlay: 4 frames; canvas128x128; max bounds16x8; 18 colors.
- wave-normal: 4 frames; canvas128x128; max bounds88x48; 15 colors.
- wave-shock: 6 frames; canvas128x128; max bounds88x48; 11 colors.
- hit-normal: 4 frames; canvas128x128; max bounds16x14; 10 colors.
- hit-shock: 4 frames; canvas128x128; max bounds21x18; 11 colors.
- icon: 1 frames; canvas48x48; max bounds36x40; 28 colors.

43 individual frames excluding identity; 10 transparent group sheets.
Binary alpha; no pure black. Fixed body base/neck y>=78 matches approved B exactly. Bottom y96.
Idle1/4, attack1/6, attack-shock1/6 and rhythm0 exactly equal approved B.
Lamps0..3 differ cumulatively; ON/OFF pixels sampled from existing image_gen B sources. Rhythm full bodies equal identity+lamp overlays.
Membrane patches sampled from image_gen body sheets; fixed approved silhouette. Normal recoil1pxRIGHT, shock2/1pxRIGHT; overlay follows same shift.
Normal/shock waves share max88x48 and right emission anchor(112,64); body mouth pair anchor(32,50). Logical per-frame common sequence scales preserve expansion/dissipation.
49 decoded composite GIF frames: 2idlecycles, 3normalshots and1shock; one-second shot cadence; lamps0->1->2->3->0. Indexed palettes preserve pixel colors.
Enemy RatRunner read-only reference in QC only. Preview push10 logical px (30px at3x), illustrating0.8cell for normal enemies; heavy proposal0.4cell. Scene is not game simulation; mechanics.md defines the proposal.
No solid projectile, cooldown, form A, Unity import or gameplay changes. Raw sources and prompt set retained local.
