Shader "OpaqueBrickColor" {
    Properties
    {
        _Glossiness("Smoothness", Range(0, 1)) = 0.5
    }
        
    SubShader
    {
        Tags{ "RenderType" = "Opaque" "Queue" = "Geometry" }
        Cull off
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard vertex:vert fullforwardshadows
        #pragma target 3.0

        struct Input
        {
            float4 vertexColor;
        };

        struct v2f
        {
            float4 pos : SV_POSITION;
            fixed4 color : COLOR;
        };

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input,o);
            o.vertexColor = v.color;
        }

        half _Glossiness;

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            o.Albedo = IN.vertexColor.rgb;
            o.Smoothness = _Glossiness;
            o.Specular = IN.vertexColor.rgb;
        }

        ENDCG
    }
    FallBack "Standard"
}
