// Made with Amplify Shader Editor v1.9.1.5
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Atsuki_Shader/Particle/Distortion_001"
{
	Properties
	{
		_DistortionStrength("Distortion Strength", Range( 0 , 1)) = 0
		[NoScaleOffset][Normal]_NormalMap("Normal Map", 2D) = "bump" {}
		_TextureTiling("Texture Tiling", Vector) = (1,1,0,0)
		_TextureScroll("Texture Scroll", Vector) = (0,-0.1,0,0)
		_RoundtripSpeed("Round trip Speed", Float) = 0
		_Roundtripdistance("Round trip distance", Float) = 0
		_TextureMask("Texture Mask", 2D) = "white" {}
		[Toggle(_SWIRL_ON)] _Swirl("Swirl", Float) = 0
		_SwirlUVRotation("Swirl UV Rotation", Vector) = (0,-0.2,0,0)
		_SwirlUVShearX("Swirl UV Shear X", Range( -10 , 10)) = 0
		_SwirlUVShearY("Swirl UV Shear Y", Range( -10 , 10)) = 5
		[Enum(UnityEngine.Rendering.CullMode)]_CullMode("CullMode", Range( 0 , 2)) = 0
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Transparent"  "Queue" = "Transparent+0" "IgnoreProjector" = "True" "IsEmissive" = "true"  }
		Cull [_CullMode]
		GrabPass{ }
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#pragma target 3.0
		#pragma shader_feature_local _SWIRL_ON
		#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
		#define ASE_DECLARE_SCREENSPACE_TEXTURE(tex) UNITY_DECLARE_SCREENSPACE_TEXTURE(tex);
		#else
		#define ASE_DECLARE_SCREENSPACE_TEXTURE(tex) UNITY_DECLARE_SCREENSPACE_TEXTURE(tex)
		#endif
		#pragma surface surf Unlit alpha:fade keepalpha noshadow 
		struct Input
		{
			float4 screenPos;
			float2 uv_texcoord;
			float4 vertexColor : COLOR;
		};

		uniform float _CullMode;
		ASE_DECLARE_SCREENSPACE_TEXTURE( _GrabTexture )
		uniform sampler2D _NormalMap;
		uniform float2 _TextureTiling;
		uniform float2 _TextureScroll;
		uniform float _Roundtripdistance;
		uniform float _RoundtripSpeed;
		uniform float _SwirlUVShearX;
		uniform float _SwirlUVShearY;
		uniform float2 _SwirlUVRotation;
		uniform float _DistortionStrength;
		uniform sampler2D _TextureMask;
		uniform float4 _TextureMask_ST;


		inline float4 ASE_ComputeGrabScreenPos( float4 pos )
		{
			#if UNITY_UV_STARTS_AT_TOP
			float scale = -1.0;
			#else
			float scale = 1.0;
			#endif
			float4 o = pos;
			o.y = pos.w * 0.5f;
			o.y = ( pos.y - o.y ) * _ProjectionParams.x * scale + o.y;
			return o;
		}


		inline float2 UVxyUVyxShearxy57( float2 UV, float2 Shear )
		{
			return UV.xy + UV.yx * Shear.xy;
		}


		inline half4 LightingUnlit( SurfaceOutput s, half3 lightDir, half atten )
		{
			return half4 ( 0, 0, 0, s.Alpha );
		}

		void surf( Input i , inout SurfaceOutput o )
		{
			float4 ase_screenPos = float4( i.screenPos.xyz , i.screenPos.w + 0.00000000001 );
			float4 ase_grabScreenPos = ASE_ComputeGrabScreenPos( ase_screenPos );
			float4 ase_grabScreenPosNorm = ase_grabScreenPos / ase_grabScreenPos.w;
			float2 TextureTilingLocal66 = _TextureTiling;
			float2 uv_TexCoord97 = i.uv_texcoord * TextureTilingLocal66;
			float3 tex2DNode62 = UnpackNormal( tex2D( _NormalMap, uv_TexCoord97 ) );
			float mulTime38 = _Time.y * _RoundtripSpeed;
			float4 appendResult41 = (float4(( _Roundtripdistance * cos( mulTime38 ) ) , 0.0 , 0.0 , 0.0));
			float2 uv_TexCoord33 = i.uv_texcoord * TextureTilingLocal66 + appendResult41.xy;
			float2 panner34 = ( 1.0 * _Time.y * _TextureScroll + uv_TexCoord33);
			float3 tex2DNode10 = UnpackNormal( tex2D( _NormalMap, panner34 ) );
			float2 uv_TexCoord56 = i.uv_texcoord * TextureTilingLocal66;
			float2 CenteredUV15_g2 = ( uv_TexCoord56 - ( TextureTilingLocal66 / 2.0 ) );
			float2 break17_g2 = CenteredUV15_g2;
			float2 appendResult23_g2 = (float2(( length( CenteredUV15_g2 ) * 1.0 * 2.0 ) , ( atan2( break17_g2.x , break17_g2.y ) * ( 1.0 / 6.28318548202515 ) * 1.0 )));
			float2 UV57 = appendResult23_g2;
			float2 appendResult81 = (float2(_SwirlUVShearX , _SwirlUVShearY));
			float2 Shear57 = appendResult81;
			float2 localUVxyUVyxShearxy57 = UVxyUVyxShearxy57( UV57 , Shear57 );
			float mulTime70 = _Time.y * 0.1;
			#ifdef _SWIRL_ON
				float3 staticSwitch67 = UnpackNormal( tex2D( _NormalMap, frac( (localUVxyUVyxShearxy57*1.0 + ( mulTime70 * _SwirlUVRotation )) ) ) );
			#else
				float3 staticSwitch67 = ( tex2DNode62 + tex2DNode10 );
			#endif
			float4 screenColor7 = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_GrabTexture,( float3( (ase_grabScreenPosNorm).xy ,  0.0 ) + ( staticSwitch67 * _DistortionStrength ) ).xy);
			o.Emission = saturate( screenColor7 ).rgb;
			float2 uv_TextureMask = i.uv_texcoord * _TextureMask_ST.xy + _TextureMask_ST.zw;
			o.Alpha = ( tex2D( _TextureMask, uv_TextureMask ).r * i.vertexColor.a );
		}

		ENDCG
	}
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=19105
Node;AmplifyShaderEditor.CommentaryNode;95;-3313.578,624.2137;Inherit;False;1827.551;839.2221;渦巻き;17;56;70;55;78;81;80;68;71;77;65;57;82;83;89;88;75;73;;0.4307111,1,0.3716981,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;42;-3781.365,-445.6648;Inherit;False;1454.628;712.1213;スクロール;11;34;35;38;39;40;33;37;41;36;61;66;;0.4643757,0.409434,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;32;-2105.312,-419.865;Inherit;False;1896.724;650.7266;ディストーション;15;96;97;62;12;63;67;1;10;2;6;3;7;8;99;100;;1,0.3716981,0.7843262,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;31;-965.3694,348.1599;Inherit;False;775.0031;385.9885;アルファマスク;3;26;25;30;;1,0.9628727,0.390566,1;0;0
Node;AmplifyShaderEditor.SaturateNode;8;-373.389,-100.5298;Inherit;False;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.PannerNode;34;-2531.137,-72.15016;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleTimeNode;38;-3545.313,-183.7018;Inherit;True;1;0;FLOAT;10;False;1;FLOAT;0
Node;AmplifyShaderEditor.CosOpNode;39;-3370.071,-203.839;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;40;-3188.489,-323.6235;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;41;-3069.019,-59.13366;Inherit;False;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RangedFloatNode;36;-3506.931,-426.3996;Float;True;Property;_Roundtripdistance;Round trip distance;5;0;Create;True;0;0;0;False;0;False;0;0.05;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;37;-3752.365,-217.2885;Float;True;Property;_RoundtripSpeed;Round trip Speed;4;0;Create;True;0;0;0;False;0;False;0;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;43;-106.3927,-154.71;Inherit;False;Property;_CullMode;CullMode;11;1;[Enum];Create;True;0;0;1;UnityEngine.Rendering.CullMode;True;0;False;0;0;0;2;0;1;FLOAT;0
Node;AmplifyShaderEditor.VertexColorNode;25;-622.1306,530.1484;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;26;-425.7664,398.1599;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;33;-2852.759,-103.7431;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;30;-915.3694,429.3376;Inherit;True;Property;_TextureMask;Texture Mask;6;0;Create;True;0;0;0;False;0;False;-1;None;6402b2bee15b621458eec128609efbbf;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ScreenColorNode;7;-563.0713,-122.3754;Inherit;False;Global;_GrabScreen0;Grab Screen 0;0;0;Create;True;0;0;0;False;0;False;Object;-1;False;False;False;False;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;3;-706.1876,-112.5639;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ComponentMaskNode;2;-873.5013,-222.7331;Inherit;False;True;True;False;False;1;0;FLOAT4;0,0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GrabScreenPosition;1;-1183.985,-345.5978;Inherit;False;0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.StaticSwitch;67;-1172.633,-120.4634;Inherit;False;Property;_Swirl;Swirl;7;0;Create;True;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;True;All;9;1;FLOAT3;0,0,0;False;0;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT3;0,0,0;False;5;FLOAT3;0,0,0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;87;65.06071,25.5262;Float;False;True;-1;2;ASEMaterialInspector;0;0;Unlit;Atsuki_Shader/Particle/Distortion_001;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;False;Back;0;False;;0;False;;False;0;False;;0;False;;False;0;Transparent;0.5;True;False;0;False;Transparent;;Transparent;All;12;all;True;True;True;True;0;False;;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;2;15;10;25;False;0.5;False;2;5;False;;10;False;;0;0;False;;0;False;;0;False;;0;False;;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;-1;-1;-1;-1;0;False;0;0;True;_CullMode;-1;0;False;;0;0;0;False;0.1;False;;0;False;;False;15;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
Node;AmplifyShaderEditor.Vector2Node;35;-2835.614,76.94802;Inherit;False;Property;_TextureScroll;Texture Scroll;3;0;Create;True;0;0;0;False;0;False;0,-0.1;0,-0.1;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.RangedFloatNode;12;-1145.67,54.51218;Inherit;False;Property;_DistortionStrength;Distortion Strength;0;0;Create;True;0;0;0;False;0;False;0;0.3;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;56;-2986.894,674.2137;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleTimeNode;70;-2562.085,1211.734;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;55;-2645.278,732.4438;Inherit;False;Polar Coordinates;-1;;2;7dab8e02884cf104ebefaa2e788e4162;0;4;1;FLOAT2;0,0;False;2;FLOAT2;0.5,0.5;False;3;FLOAT;1;False;4;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;78;-2839.511,896.8975;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;81;-2435.609,872.3273;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;80;-3066.511,1012.897;Inherit;False;Constant;_Float1;Float 1;10;0;Create;True;0;0;0;False;0;False;2;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;68;-2731.997,1207.439;Inherit;False;Constant;_Float0;Float 0;9;0;Create;True;0;0;0;False;0;False;0.1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;71;-2315.386,1203.799;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;77;-3111.511,900.8975;Inherit;False;66;TextureTilingLocal;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.FractNode;89;-1784.117,1069.518;Inherit;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ScaleAndOffsetNode;88;-2024.117,974.5176;Inherit;True;3;0;FLOAT2;0,0;False;1;FLOAT;1;False;2;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;75;-1806.828,779.0668;Inherit;True;Property;_NormalMap2;Normal Map;1;2;[NoScaleOffset];[Normal];Create;True;0;0;0;False;0;False;-1;None;None;True;0;True;bump;Auto;True;Instance;10;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector2Node;73;-2687.531,1301.236;Inherit;False;Property;_SwirlUVRotation;Swirl UV Rotation;8;0;Create;True;0;0;0;False;0;False;0,-0.2;0,-0.2;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.RangedFloatNode;82;-2712.175,932.8483;Inherit;False;Property;_SwirlUVShearX;Swirl UV Shear X;9;0;Create;True;0;0;0;False;0;False;0;0;-10;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;83;-2703.376,1029.688;Inherit;False;Property;_SwirlUVShearY;Swirl UV Shear Y;10;0;Create;True;0;0;0;False;0;False;5;5;-10;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node;61;-2967.873,-311.3515;Inherit;False;Property;_TextureTiling;Texture Tiling;2;0;Create;True;0;0;0;False;0;False;1,1;0.2,0.2;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.RegisterLocalVarNode;66;-2738.254,-291.0723;Inherit;False;TextureTilingLocal;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;65;-3231.757,695.1011;Inherit;False;66;TextureTilingLocal;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;97;-1887.934,-371.9059;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GetLocalVarNode;96;-2096.882,-358.2491;Inherit;False;66;TextureTilingLocal;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SamplerNode;10;-1702.158,-123.737;Inherit;True;Property;_NormalMap;Normal Map;1;2;[NoScaleOffset];[Normal];Create;True;0;0;0;False;0;False;-1;None;e599ad883e034264fb0dac0a21f0f8ac;True;0;True;bump;Auto;True;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.CustomExpressionNode;57;-2290.146,799.5576;Inherit;False;UV.xy + UV.yx * Shear.xy;2;Create;2;True;UV;FLOAT2;0,0;In;;Inherit;False;True;Shear;FLOAT2;0,0;In;;Inherit;False;UV.xy + UV.yx * Shear.xy;True;False;0;;False;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;100;-1638.389,136.5047;Inherit;False;Constant;_Float4;Float 4;12;0;Create;True;0;0;0;False;0;False;0.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;99;-1519.52,123.749;Inherit;True;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SamplerNode;62;-1689.321,-366.9874;Inherit;True;Property;_NormalMap1;Normal Map;1;2;[NoScaleOffset];[Normal];Create;True;0;0;0;False;0;False;-1;None;None;True;0;True;bump;Auto;True;Instance;10;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;63;-1421.693,-221.8813;Inherit;True;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;6;-858.44,-107.2291;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
WireConnection;8;0;7;0
WireConnection;34;0;33;0
WireConnection;34;2;35;0
WireConnection;38;0;37;0
WireConnection;39;0;38;0
WireConnection;40;0;36;0
WireConnection;40;1;39;0
WireConnection;41;0;40;0
WireConnection;26;0;30;1
WireConnection;26;1;25;4
WireConnection;33;0;66;0
WireConnection;33;1;41;0
WireConnection;7;0;3;0
WireConnection;3;0;2;0
WireConnection;3;1;6;0
WireConnection;2;0;1;0
WireConnection;67;1;63;0
WireConnection;67;0;75;0
WireConnection;87;2;8;0
WireConnection;87;9;26;0
WireConnection;56;0;65;0
WireConnection;70;0;68;0
WireConnection;55;1;56;0
WireConnection;55;2;78;0
WireConnection;78;0;77;0
WireConnection;78;1;80;0
WireConnection;81;0;82;0
WireConnection;81;1;83;0
WireConnection;71;0;70;0
WireConnection;71;1;73;0
WireConnection;89;0;88;0
WireConnection;88;0;57;0
WireConnection;88;2;71;0
WireConnection;75;1;89;0
WireConnection;66;0;61;0
WireConnection;97;0;96;0
WireConnection;10;1;34;0
WireConnection;57;0;55;0
WireConnection;57;1;81;0
WireConnection;99;0;62;0
WireConnection;99;1;10;0
WireConnection;99;2;100;0
WireConnection;62;1;97;0
WireConnection;63;0;62;0
WireConnection;63;1;10;0
WireConnection;6;0;67;0
WireConnection;6;1;12;0
ASEEND*/
//CHKSM=6AF9E439446265E801F798003EDE34B89A6AD5D9