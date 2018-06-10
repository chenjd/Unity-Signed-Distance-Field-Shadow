Shader "chenjd/SDFShadow"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
    
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"


            float4x4 _Corners;

            float4 _MainTex_TexelSize;

            float4x4 _CamInvViewMatrix;

            float3 _CamPosition;


            //传入光的方向
            float3 _LightDir;
            float3 _LightPos;


			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
                float3 ray : TEXCOORD1;
				float4 vertex : SV_POSITION;
			};

            float map(float3 p);

            //#定义有向距离场方程
            float sdSphere(float3 rp, float3 c, float r)
            {
                return distance(rp,c)-r;
            }

            float sdCube( float3 p, float3 b, float r )
            {
              return length(max(abs(p)-b,0.0))-r;
            }

            float sdPlane( float3 p )
            {
                return p.y + 1;
            }

            float map(float3 rp)
            {
                float4 d;
                float4 sp = float4( float3(0.68, 0.9, 0.40), sdSphere(rp, float3(1.0,0.0,0.0), 1.0) );
                float4 sp2 = float4( float3(0.68, 0.1, 0.40), sdSphere(rp, float3(1.0,2.0,0.0), 1.0) );
                float4 cb = float4( float3(0.17,0.46,1.0), sdCube(rp+float3(2.1,-1.0,0.0), float3(2.0,2.0, 2.0), 0.0) );
                float4 py = float4( float3(0.7,0.35,0.1), sdPlane(rp.y) );
                d = (sp.a < py.a) ? sp : py;
                d = (d.a < sp2.a) ? d : sp2;
                d = (d.a < cb.a) ? d : cb;
                return d.a;
            }


            //Adapted from:iquilezles
            float calcSoftshadow( float3 ro, float3 rd, in float mint, in float tmax)
            {

                float res = 1.0;
                float t = mint;
                float ph = 1e10;
                
                for( int i=0; i<32; i++ )
                {
                    float h = map( ro + rd*t );
                    float y = h*h/(2.0*ph);
                    float d = sqrt(h*h-y*y);
                    res = min( res, 10.0*d/max(0.0,t-y) );
                    ph = h;
                    
                    t += h;
                    
                    if( res<0.0001 || t>tmax ) 
                        break;
                    
                }
                return clamp( res, 0.0, 1.0 );
            }

            float3 calcNorm(float3 p);

            //实现raymarching的逻辑
            //raymarching的逻辑在fragment shader中来实现，代表沿着射向该像素的射线进行采样。
            //所以输入要指明该射线的其实位置，以及它的方向。
            //返回则是一个颜色值
            fixed4 raymarching(float3 rayOrigin, float3 rayDirection)
            {

                fixed4 ret = fixed4(0, 0, 0, 0);

                int maxStep = 64;

                float rayDistance = 0;

                for(int i = 0; i < maxStep; i++)
                {
                    float3 p = rayOrigin + rayDirection * rayDistance;
                    float surfaceDistance = map(p);
                    if(surfaceDistance < 0.001)
                    {
                        ret = fixed4(1, 0, 0, 1);
                        //增加光照效果

                        float3 norm = calcNorm(p);
                        ret = clamp(dot(-_LightDir, norm), 0, 1) * calcSoftshadow(p, -_LightDir,    0.01, 300.0);
                        ret.a = 1;

                        //
                        break;
                    }

                    rayDistance += surfaceDistance;
                }
                return ret;
            }


            //计算光照
            //计算法线
            float3 calcNorm(float3 p)
            {
                float2 eps = float2(0.001, 0.0);

                float3 norm = float3(
                map(p + eps.xyy).x - map(p - eps.xyy).x,
                map(p + eps.yxy).x - map(p - eps.yxy).x,
                map(p + eps.yyx).x - map(p - eps.yyx).x
                );

                return normalize(norm);
            }


			v2f vert (appdata v)
			{
				v2f o;

                fixed index = v.vertex.z;

                v.vertex.z = 0;

				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
                index = v.uv.x + (2 * o.uv.y);
                o.ray = _Corners[index].xyz;

				return o;
			}
            
			
			sampler2D _MainTex;

			fixed4 frag (v2f i) : SV_Target
			{

                float3 rayDirection = normalize(i.ray.xyz); 

                float3 rayOrigin = _CamPosition;

                fixed4 bgColor =  tex2D(_MainTex, i.uv);

                fixed4 objColor = raymarching(rayOrigin, rayDirection);

                fixed4 finalColor = fixed4(objColor.xyz * objColor.w + bgColor.xyz * (1 - objColor.w), 1);
                return finalColor;
			}
			ENDCG
		}
	}
}
