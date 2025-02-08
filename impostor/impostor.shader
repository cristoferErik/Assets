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
            float3 convertToHemioct(float3 uvGrid){
                const float SQRT_2 = 1.41421356237;
                //EL codigo de rotacion del rombo se escribe antes de tranformarlo a uv
                float angle=radians(-45);

                float2 coord= (float2 (uvGrid.x,uvGrid.z)/(SQRT_2));
                float2 uv = (coord*2 - 1);
                float Vx=uv.x*cos(angle) - uv.y.sin(angle);
                float Vy=uv.x*sin(angle) + uv.y*cos(angle);
                float y = 1 - (Vx+ Vy);
                float3 viewDir = normalize(float3(Vx,y,Vy));
                return viewDir;
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
 
                //Aqui usamos la rotacion del espacio 3d , y la vista para obtener la nueva direccion hacia el octahedro
                viewDir = mul(rotationMatrix, viewDir);
                
                float2 uvOrt = convertToSquare(viewDir);
                float2 uvRaw = v.uv;

                float2 quantizedUvOrt = floor(uvOrt * _Grid);
                float2 quantizedUv = floor(uvRaw * _Grid);
                
                
                float2 cellDistance = quantizedUvOrt-quantizedUv;
                float2 uv = (v.uv*_Grid + cellDistance)/_Grid;
                float3 rotatedPosition;
                //Redondeamos y luego lo convertimos nuevamente en coordenadas de vista
                if(length(cellDistance)<=0.001f){
                    float3 viewRounded = convertToHemioct(float3(quantizedUvOrt.x,0,quantizedUvOrt.y)/_Grid);
                    float3 up = float3(0, 1, 0);

                    float3 right = normalize(cross(up, viewRounded));   // Eje 'right' perpendicular a viewDir
                    float3 newUp= normalize(cross(viewRounded,right));// Eje 'up' recalculado perpendicular a viewDir y right, este si es perpendicular
                    // Reposiciona el vértice en base al centro del billboard, aplicando el offset en los ejes calculados
                    rotatedPosition = centerWorldPos + (v.vertex.x * right + v.vertex.y*newUp);
                    
                }
                
               
                o.uv = uv;
                // Convierte la posición a espacio de clip
                o.vertex = UnityWorldToClipPos(rotatedPosition);
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
