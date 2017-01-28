Shader "Brick/Transparent" {
    SubShader
    {
        Tags{ "RenderType" = "Transparent" "Queue" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        LOD 200

        Cull Back

        CGPROGRAM
        #pragma surface surf_fade Lambert vertex:vert keepalpha
        #include "brickVC.cginc"
        ENDCG
    }
    FallBack "Diffuse"
}
