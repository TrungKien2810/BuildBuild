# Frame rebuild QC â€” redesign B

Mechanical/export checks; visual review is recorded separately.

- idle: 4 frames, canvas 144x128, max opaque 100x64; decoded sheet matches individual PNGs.
- attack: 6 frames, canvas 144x128, max opaque 102x64; decoded sheet matches individual PNGs.
- bullet: 4 frames, canvas 128x128, max opaque 14x6; decoded sheet matches individual PNGs.
- bullet-impact: 4 frames, canvas 128x128, max opaque 26x22; decoded sheet matches individual PNGs.
- icon: 1 frames, canvas 48x48, max opaque 42x27; decoded sheet matches individual PNGs.

Total: 19 individual frames outside identity. Alpha 0/255; no pure black; no canvas-edge clipping.
Base/pedestal y>=72 identical in every body frame; bottom y=96. Idle 1/4 and attack 1/6 equal current B identity. Fixed receiver/collar/cradle also pixel-checked outside moving region.
Body muzzle origin: (31,60). VFX use (64,64); projectiles preserve exact alpha silhouette; impact and smoke use a single fixed source transform per sequence.
Gatling: generated axial muzzle phases inside original silhouette; no rectangular bitmap rotation; receiver/collar/base fixed.
GIF preview: 19 decoded frames, 768x480. Attack-slow GIF: 6 decoded frames, 720x640.
All decoded GIF frames equal their source scene PNG pixels exactly; global palette, no dithering or palette drift.
Animation timing illustrates art; Unity integration is outside this export.
