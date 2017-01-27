Shader "Low Poly Transparent" 
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _Glossiness("Smoothness", Range(0,1)) = 0.5
    }

    SubShader
    {
        Tags{ "RenderType" = "Transparent" "Queue" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Back
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard vertex:vert fullforwardshadows alpha:fade
        #pragma target 3.0

        struct Input 
        {
            float4 color : COLOR; // Vertex color stored here by vert() method
        };

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input,o);
            o.color = v.color; // Save the Vertex Color in the Input for the surf() method
        }

        half _Glossiness;
        fixed4 _Color;

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = _Color;
            o.Albedo = c.rgb * IN.color.rgb; // Combine normal color with the vertex color
            o.Smoothness = _Glossiness;
            float alpha = c.a * IN.color.a;
            o.Alpha = alpha;
        }
        ENDCG
    }
    FallBack "Diffuse"
}