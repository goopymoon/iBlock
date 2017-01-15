Shader "OpaqueBrickColor" {
    Properties
    {
        _Glossiness("Smoothness", Range(0, 1)) = 1.0
    }
        
    SubShader
    {
        Tags{ "RenderType" = "Transparent" "Queue" = "Transparent" "IgnoreProjector" = "True" }
        Blend SrcAlpha OneMinusSrcAlpha
        LOD 200
        Cull off

        CGPROGRAM

        #pragma surface surf Standard keepalpha vertex:vert fullforwardshadows
        #pragma target 3.0

        struct Input
        {
            float2 uv_MainTex;
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
            o.Alpha = IN.vertexColor.a;
        }

        ENDCG
    }
    FallBack "Standard"
}
