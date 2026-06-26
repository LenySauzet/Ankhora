#ifndef ANKHORA_REVEAL_INCLUDED
#define ANKHORA_REVEAL_INCLUDED

// Global published by the passthrough transition (OvrPassthroughSurface): 0 = VR, 1 = MR.
float _AnkhoraMrAmount;

// Unified VR<->MR reveal shared by the gradient sky and the floor grid so they transition as one
// seamless effect. Returns VR-visibility in [0,1] for a world-space view direction
// (camera -> fragment, or the sky pixel direction): 1 = show the VR environment, 0 = revealed to
// passthrough. A feathered cone centred on the view forward opens as _AnkhoraMrAmount rises, so
// passthrough grows from the centre of vision outward (and the VR environment closes back to it).
// Requires UNITY_MATRIX_V — include after the URP Core library.
float AnkhoraVrVisibility(float3 viewDirWS, float feather)
{
    float3 fwd = -UNITY_MATRIX_V[2].xyz;                 // camera forward in world space
    float centerness = dot(normalize(viewDirWS), fwd);   // 1 at the centre of vision
    float threshold = lerp(1.0 + feather, -1.0 - feather, saturate(_AnkhoraMrAmount));
    float reveal = smoothstep(threshold - feather, threshold + feather, centerness);
    return 1.0 - reveal;
}

#endif
