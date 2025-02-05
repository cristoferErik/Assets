 Shader "Unlit/impostor"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Grid ("Grid Size",int) = 7
        _MaskTex ("Mask Texture (Grayscale)", 2D) = "white" {}
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5
    }
    SubShader
    {
        Tags { "Queue"="AlphaTest" "RenderType"="Cutout" }
        LOD 100
        AlphaTest Greater [_Cutoff]
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            sampler2D _MaskTex;
            float4 _MainTex_ST;
            int _Grid;
            float _Cutoff;

            int _PrevScaleOct;
            float2 _PrevUv;

            float2 convertToSquare(float3 viewDir){
                viewDir.y = max(0.0, viewDir.y);
                float addCoord=abs(viewDir.x)+abs(viewDir.y)+abs(viewDir.z);
                float2 octUv=viewDir.xz /addCoord;
                const float SQRT_2 = 1.41421356237;
                //EL codigo de rotacion del rombo se escribe antes de tranformarlo a uv
                float angle=radians(45);
                float Vx= octUv.x*cos(angle)- octUv.y*sin(angle);
                float Vy= octUv.x*sin(angle) + octUv.y*cos(angle);
                float2 coord= float2 (Vx,Vy)*(SQRT_2);
                //El valor 0.99 sirve para evitar que el valor sea 1 , que generaria el error al tocar el borde del octahedro
                //Esto evita que al momento de que se redonde el valor erroneamente, en las celdas y coordenadas

                float2 uv = (coord*0.99f+1)*0.5f;
                return uv;
            }
            v2f vert (appdata v)
            {
                v2f o;

                // Calcula la posición del centro del billboard en el espacio mundial
                float3 centerWorldPos = mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;

                // Dirección de vista desde el objeto hacia la cámara
                float3 viewDir = normalize(_WorldSpaceCameraPos - centerWorldPos);

                // Aplica la rotación del objeto en el espacio 3D de Unity
                float3x3 rotationMatrix = float3x3(
                    unity_ObjectToWorld[0].xyz, // Primera fila (sin el componente de traslación)
                    unity_ObjectToWorld[1].xyz, // Segunda fila (sin el componente de traslación)
                    unity_ObjectToWorld[2].xyz  // Tercera fila (sin el componente de traslación)
                );
 
                 viewDir.y = max(0.0, viewDir.y);
                 // Ejes del billboard
                 // Eje 'up' artificio, que me permitira hallar los demas vectores
                 float3 up = float3(0, 1, 0);
                 //Estos vectores deben estar normalizados
                /*
                     if(abs(dot(up,viewDir))>0.99f){
                        up=float3(1,0,0);
                    }                   
                */
                 float3 right = normalize(cross(up, viewDir));   // Eje 'right' perpendicular a viewDir
                 float3 newUp= normalize(cross(viewDir,right));// Eje 'up' recalculado perpendicular a viewDir y right, este si es perpendicular
 
 
                 // Reposiciona el vértice en base al centro del billboard, aplicando el offset en los ejes calculados
                 float3 rotatedPosition = centerWorldPos + (v.vertex.x * right + v.vertex.y*newUp);

                // Convierte la posición a espacio de clip
                o.vertex = UnityWorldToClipPos(rotatedPosition);

                viewDir = mul(rotationMatrix, viewDir);
                
                float2 uvOrt = convertToSquare(viewDir);
                
                float2 uvScaled = floor(uvOrt*_Grid);
                float2 octScaled = floor(v.uv*_Grid);
                 
                
                float2 cellDistance = uvScaled - octScaled;
                float2 uv = ((v.uv*_Grid) + cellDistance)/_Grid;
                
                o.uv = uv;
                // Convierte la posición a espacio de clip
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                 // Mostrar la textura principal con blending
                 fixed4 col = tex2D(_MainTex, i.uv);
                // Mostrar la textura principal con blending
                return col;
            }
            ENDCG
        }
    }
}
