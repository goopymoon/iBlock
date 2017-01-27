Shader "TransparentDoubleSideBrick" {
    SubShader
    {
        Tags{ "RenderType" = "Transparent" "Queue" = "Transparent" }
        LOD 200

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        CGPROGRAM
        #pragma surface surf_fade Lambert vertex:vert alpha:fade
        #include "brickVC.cginc"
        ENDCG
    }
    FallBack "Diffuse"
}
