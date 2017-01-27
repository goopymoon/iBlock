Shader "OpaqueDoubleSideBrick" {
    SubShader
    {
        Tags{ "RenderType" = "Opaque" "Queue" = "Geometry" }
        Cull off
        LOD 200

        CGPROGRAM
        #pragma surface surf_opaque Lambert vertex:vert fullforwardshadows
        #include "brickVC.cginc"
        ENDCG
    }
    FallBack "Diffuse"
}
