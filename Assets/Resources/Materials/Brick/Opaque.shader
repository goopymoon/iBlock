Shader "Brick/Opaque" {
    SubShader
    {
        Tags{ "RenderType" = "Opaque" "Queue" = "Geometry" }
        Cull Back
        LOD 200

        CGPROGRAM
        #pragma surface surf_opaque Lambert vertex:vert 
        #include "brickVC.cginc"
        ENDCG
    }
    FallBack "Diffuse"
}
