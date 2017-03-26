Shader "Brick/Opaque Double Side" {
    SubShader
    {
        Tags{ "RenderType" = "Opaque" "Queue" = "Geometry" }
        LOD 200

        /////////////////////////////////////////////////////
        // Draw front faces of mesh
        Cull Back

        CGPROGRAM
        #pragma surface surf_opaque Lambert vertex:vert
        #include "brickVC.cginc"
        ENDCG

        /////////////////////////////////////////////////////
        // Draw front faces of mesh
        Cull Front

        CGPROGRAM
        #pragma surface surf_opaque Lambert vertex:vert_back
        #include "brickVC.cginc"
        ENDCG
    }
    FallBack "Diffuse"
}
