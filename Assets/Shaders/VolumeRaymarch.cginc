#include "UnityCG.cginc"

Texture3D<float> _VolumeTex;
SamplerState sampler_VolumeTex;
float3 _VolumeTex_Size;
float3 _VolumeTex_InvSize;
float _Threshold;
float _ZOffset;
float _Scale;
float _Power;
float _ScrollOffset;
float _ScrollScale;

struct Ray
{
    float3 origin;
    float3 dir;
};

struct Box
{
    float3 boxMin;
    float3 boxMax;
};

struct Grid
{
    float3 offset;
    float size;
};

struct Intersection
{
    bool intersect;
    float t;
};

bool rayBoxIntersection(Ray r, Box b, out float tNear, out float tFar)
{
    float3 invDir = 1.0 / r.dir;
    float3 tbot = invDir * (b.boxMin - r.origin);
    float3 ttop = invDir * (b.boxMax - r.origin);

    float3 tmin = min(ttop, tbot);
    float3 tmax = max(ttop, tbot);

    float2 t0 = max(tmin.xx, tmin.yz);
    tNear = max(t0.x, t0.y);
    t0 = min(tmax.xx, tmax.yz);
    tFar = min(t0.x, t0.y);

    tNear = max(tNear, 0.0);

    // make sure that tFar sits slightly before the grid boundary
    tFar -= 0.001;
    tNear += 0.00001;
    return tFar > tNear;
}

float map(float3 pos)
{
    float v = _VolumeTex.SampleLevel(sampler_VolumeTex, pos.xzy * float3(1.0, 1.0, 1.0 - _VolumeTex_InvSize.z * _ScrollScale) + float3(0.5, 0.5, 0.5 + _ZOffset - _VolumeTex_InvSize.z * _ScrollOffset), 0).r;
    v = pow(v, _Power) * _Scale;

    return _Threshold - v;
}

Intersection intersect(float3 ro, float3 rd, float maxd)
{
    float precis = 0.005;
    float h = precis*2.0;
    float t = 0.0;

    Intersection s;
    s.intersect = false;

    for( uint i=0; i<75; i++ )
    {
        if (h < precis)
        {
            s.intersect = true;
            break;
        }
        if( t>maxd )
        {
            break;
        }
        t += min(max(h * 0.3, 0.001), 0.05);
        h = map( ro+rd*t );
    }

    s.t = t;
    // s.intersect = (t <= maxd);
    return s;
}

Intersection raymarch(float3 rayOrigin, float3 rayDir)
{
    Box b;
    b.boxMin = (float3)-0.5;
    b.boxMax = (float3)0.5;
    Ray r;
    r.origin = rayOrigin;
    r.dir = rayDir;

    float tNear, tFar;
    rayBoxIntersection(r, b, tNear, tFar);

    Intersection i = intersect(rayOrigin, rayDir, tFar);
    return i;

    // if (!i.intersect)
    //     return float4(0,0,0,0);

    // float a = 0.1 / i.t;
    // return float4(a,a,a,1);
}

float3 getNormal(float3 pos)
{
    const float h = 1e-2;

    if (pos.y > 0.495) return float3(0,1,0);

    float center = map(pos);
    const float2 k = float2(1,-1);
    return normalize( k.xyy*map( pos + k.xyy*h ) + 
                      k.yyx*map( pos + k.yyx*h ) + 
                      k.yxy*map( pos + k.yxy*h ) + 
                      k.xxx*map( pos + k.xxx*h ) );
}

inline float EncodeDepth(float4 pos)
{
    float z = pos.z / pos.w;
#if defined(SHADER_API_GLCORE) || \
    defined(SHADER_API_OPENGL) || \
    defined(SHADER_API_GLES) || \
    defined(SHADER_API_GLES3)
    return z * 0.5 + 0.5;
#else 
    return z;
#endif 
}

inline float EncodeDepth(float3 pos)
{
    float4 vpPos = UnityObjectToClipPos(float4(pos, 1.0));
    return EncodeDepth(vpPos);
}


struct appdata
{
    float4 vertex : POSITION;
};

struct v2f
{
    float4 vertex : SV_POSITION;
    float4 localPos : TEXCOORD0;
    float3 viewDir : TEXCOORD1;
};

struct FragOutput
{
    float4 color : SV_Target;
    float depth : SV_Depth;
};

float4 _Color;
float3 _RimPowers;

v2f vert (appdata v)
{
    v2f o;
    o.vertex = UnityObjectToClipPos(v.vertex);
    o.localPos = v.vertex;
    o.viewDir = ObjSpaceViewDir(v.vertex);
    return o;
}

FragOutput frag (v2f i)
{
    float3 rayOrigin = i.localPos;
    float3 rayDir = -normalize(i.viewDir);
    Intersection intersect = raymarch(rayOrigin, rayDir);

    if (!intersect.intersect) discard;

    float3 localPos = rayOrigin + rayDir * intersect.t;
    float3 normal = getNormal(localPos);
    float rim = 1.0 - saturate(dot(normal, normalize(i.viewDir)));

    FragOutput o;
    UNITY_INITIALIZE_OUTPUT(FragOutput, o);

    o.color = float4(
        pow(rim.xxx, _RimPowers) + _Color.rgb * (localPos.y > 0.495),
        1.0
    );

    float depth = -UnityObjectToViewPos( localPos ).z;
    depth = (1.0 / depth - _ZBufferParams.w) / _ZBufferParams.z;
    o.depth = depth;

#if (defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2))
    depth = Linear01Depth(depth);
    UNITY_CALC_FOG_FACTOR_RAW(depth * _ProjectionParams.z - _ProjectionParams.y);
    UNITY_FOG_LERP_COLOR(o.color, unity_FogColor, unityFogFactor);

#endif

    // UNITY_APPLY_FOG_COLOR(depth * _ProjectionParams.z - _ProjectionParams.y, o.color, unity_FogColor);

    return o;
}
