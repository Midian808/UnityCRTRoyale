Shader "Hidden/CrtTestUnlit"
{
    Properties { _MainTex ("Texture", 2D) = "white" {} }
    SubShader
    {
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            float4x4 unity_ObjectToWorld;
            float4x4 unity_MatrixVP;
            sampler2D _MainTex;
            struct Attributes { float4 position : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 position : SV_Position; float2 uv : TEXCOORD0; };
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.position = mul(unity_MatrixVP, mul(unity_ObjectToWorld, float4(input.position.xyz, 1.0)));
                output.uv = input.uv;
                return output;
            }
            float4 frag(Varyings input) : SV_Target { return float4(tex2D(_MainTex, input.uv).rgb, 1.0); }
            ENDHLSL
        }
    }
}
