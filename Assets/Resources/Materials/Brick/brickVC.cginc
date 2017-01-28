#ifndef BRICK_VC_CGINC
#define BRICK_VC_CGINC

struct Input
{
    float4 color : COLOR; // Vertex color stored here by vert() method
};

void vert(inout appdata_full v, out Input o)
{
    UNITY_INITIALIZE_OUTPUT(Input,o);
    o.color = v.color; // Save the Vertex Color in the Input for the surf() method
}

void vert_back(inout appdata_full v, out Input o) 
{
    UNITY_INITIALIZE_OUTPUT(Input, o);
    o.color = v.color; // Save the Vertex Color in the Input for the surf() method
    v.normal = -v.normal;
}

void surf_opaque(Input IN, inout SurfaceOutput o)
{
    o.Albedo = IN.color.rgb;
}

void surf_fade(Input IN, inout SurfaceOutput o)
{
    o.Albedo = IN.color.rgb;
    o.Alpha = IN.color.a;
}

#endif
