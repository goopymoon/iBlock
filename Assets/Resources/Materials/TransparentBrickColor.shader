Shader "TransparentBrickColor" {
    SubShader
    {
        Tags{ "RenderType" = "Transparent" "Queue" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull off
        LOD 200

        CGPROGRAM
        #pragma surface surf Lambert vertex:vert alpha:fade

        struct Input
        {
            float4 color : COLOR; // Vertex color stored here by vert() method
        };

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input,o);
            o.color = v.color; // Save the Vertex Color in the Input for the surf() method
        }

        void surf(Input IN, inout SurfaceOutput o)
        {
            o.Albedo = IN.color.rgb;
            o.Alpha = IN.color.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
