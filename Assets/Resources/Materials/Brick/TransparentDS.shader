Shader "Brick/Transparent Double Side" {
    SubShader
    {
        Tags{ "RenderType" = "Transparent" "Queue" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        LOD 200

        /////////////////////////////////////////////////////
        // Draw front faces
        Cull Back

        CGPROGRAM
        #pragma surface surf_fade Lambert vertex:vert keepalpha
        #include "brickVC.cginc"
        ENDCG

        /////////////////////////////////////////////////////
        // Draw back faces
        Cull Front

        CGPROGRAM
        #pragma surface surf_fade Lambert vertex:vert_back alpha:fade
        #include "brickVC.cginc"
        ENDCG
    }
    FallBack "Diffuse"
}
