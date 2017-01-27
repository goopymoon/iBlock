﻿Shader "TransparentBrickColor" {
    Properties
    {
        _Glossiness("Smoothness", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags{ "RenderType" = "Transparent" "Queue" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull off
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

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            o.Albedo = IN.color.rgb;
            o.Smoothness = _Glossiness;
            o.Alpha = IN.color.a;
            o.Specular = IN.vertexColor.rgb;
        }
        ENDCG
    }
    FallBack "Standard"
}
