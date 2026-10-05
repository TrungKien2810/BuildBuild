# Frame rebuild QC â€” redesign B

Mechanical/export checks; visual review is recorded separately.

- idle: 4 frames, canvas 144x128, max opaque 124x60; decoded sheet matches individual PNGs.
- attack: 6 frames, canvas 144x128, max opaque 131x60; decoded sheet matches individual PNGs.
- bullet: 4 frames, canvas 128x128, max opaque 21x6; decoded sheet matches individual PNGs.
- bullet-impact: 4 frames, canvas 128x128, max opaque 25x29; decoded sheet matches individual PNGs.
- target-mark: 2 frames, canvas 128x128, max opaque 19x19; decoded sheet matches individual PNGs.
- muzzle-smoke: 4 frames, canvas 128x128, max opaque 29x12; decoded sheet matches individual PNGs.
- icon: 1 frames, canvas 48x48, max opaque 42x20; decoded sheet matches individual PNGs.

Total: 25 individual frames outside identity. Alpha 0/255; no pure black; no canvas-edge clipping.
Base/pedestal y>=72 identical in every body frame; bottom y=96. Idle 1/4 and attack 1/6 equal current B identity. Fixed receiver/collar/cradle also pixel-checked outside moving region.
Body muzzle origin: (11,55). VFX use (64,64); projectiles preserve exact alpha silhouette; impact and smoke use a single fixed source transform per sequence.
Longgun: only barrel region slides +1/+3/+1px into fixed housing; cradle, scope, receiver and cloth remain attached; marker silhouette locked.
GIF preview: 19 decoded frames, 768x480. Attack-slow GIF: 6 decoded frames, 720x640.
All decoded GIF frames equal their source scene PNG pixels exactly; global palette, no dithering or palette drift.
Animation timing illustrates art; Unity integration is outside this export.
