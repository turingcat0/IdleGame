// Made with Amplify Shader Editor v1.9.1.5
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Atsuki_Shader/Particle/Dissolve_Cutout_001"
{
	Properties
	{
		[HDR]_Main_Texture("Main_Texture", 2D) = "white" {}
		[HDR]_Main_Color("Main_Color", Color) = (1,1,1,1)
		_Value("Value", Range( -1 , 1)) = 0
		_Hue("Hue", Range( -1 , 1)) = 0
		_Saturation("Saturation", Range( -1 , 1)) = 0
		_Intensity("Intensity", Float) = 1
		[NoScaleOffset]_DissolveNoiseTexture("Dissolve Noise Texture", 2D) = "white" {}
		_DissolveNoiseTextureTiling("Dissolve Noise Texture Tiling", Vector) = (1,1,0,0)
		_DissolveNoiseTextureScroll1("Dissolve Noise Texture Scroll 1", Vector) = (0.1,0,0,0)
		_DissolveNoiseTextureScroll2("Dissolve Noise Texture Scroll 2", Vector) = (0,0.1,0,0)
		_DissolveNoiseAlpha("Dissolve Noise Alpha", Range( 0 , 1)) = 1
		[KeywordEnum(VertexColor,DissolveEdgeColor)] _DissolveEdgeColorSelect("Dissolve Edge Color Select", Float) = 1
		_DissolveEdgeColorVertexColorStength("Dissolve Edge Color Vertex Color Stength", Float) = 5
		[HDR]_DissolveEdgeColor("Dissolve Edge Color", Color) = (0,0.5589409,1,0)
		_Dissolve_Edge_Stength("Dissolve_Edge_Stength", Float) = 0
		[Enum(UnityEngine.Rendering.CullMode)]_CullMode("CullMode", Range( 0 , 2)) = 0
		[HideInInspector] _texcoord2( "", 2D ) = "white" {}
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "TreeTransparentCutout"  "Queue" = "AlphaTest+0" "IgnoreProjector" = "True" "IsEmissive" = "true"  }
		Cull [_CullMode]
		AlphaToMask On
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#pragma target 3.5
		#pragma shader_feature_local _DISSOLVEEDGECOLORSELECT_VERTEXCOLOR _DISSOLVEEDGECOLORSELECT_DISSOLVEEDGECOLOR
		#pragma surface surf Unlit keepalpha noshadow 
		#undef TRANSFORM_TEX
		#define TRANSFORM_TEX(tex,name) float4(tex.xy * name##_ST.xy + name##_ST.zw, tex.z, tex.w)
		struct Input
		{
			float2 uv_texcoord;
			float4 vertexColor : COLOR;
			float4 uv2_texcoord2;
		};

		uniform float _CullMode;
		uniform float4 _Main_Color;
		uniform sampler2D _Main_Texture;
		uniform float4 _Main_Texture_ST;
		uniform float _Intensity;
		uniform float _Hue;
		uniform float _Saturation;
		uniform float _Value;
		uniform float _Dissolve_Edge_Stength;
		uniform sampler2D _DissolveNoiseTexture;
		uniform float2 _DissolveNoiseTextureScroll1;
		uniform float2 _DissolveNoiseTextureTiling;
		uniform float2 _DissolveNoiseTextureScroll2;
		uniform float _DissolveNoiseAlpha;
		uniform float4 _DissolveEdgeColor;
		uniform float _DissolveEdgeColorVertexColorStength;


		float3 HSVToRGB( float3 c )
		{
			float4 K = float4( 1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0 );
			float3 p = abs( frac( c.xxx + K.xyz ) * 6.0 - K.www );
			return c.z * lerp( K.xxx, saturate( p - K.xxx ), c.y );
		}


		float3 RGBToHSV(float3 c)
		{
			float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
			float4 p = lerp( float4( c.bg, K.wz ), float4( c.gb, K.xy ), step( c.b, c.g ) );
			float4 q = lerp( float4( p.xyw, c.r ), float4( c.r, p.yzx ), step( p.x, c.r ) );
			float d = q.x - min( q.w, q.y );
			float e = 1.0e-10;
			return float3( abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
		}

		inline half4 LightingUnlit( SurfaceOutput s, half3 lightDir, half atten )
		{
			return half4 ( 0, 0, 0, s.Alpha );
		}

		void surf( Input i , inout SurfaceOutput o )
		{
			float2 uv_Main_Texture = i.uv_texcoord * _Main_Texture_ST.xy + _Main_Texture_ST.zw;
			float4 tex2DNode3 = tex2D( _Main_Texture, uv_Main_Texture );
			float3 hsvTorgb36 = RGBToHSV( ( ( _Main_Color * tex2DNode3 ) * i.vertexColor * _Intensity ).rgb );
			float3 hsvTorgb43 = HSVToRGB( float3(( hsvTorgb36.x + _Hue ),( hsvTorgb36.y + _Saturation ),( hsvTorgb36.z + _Value )) );
			float2 uv_TexCoord195 = i.uv_texcoord * _DissolveNoiseTextureTiling;
			float2 panner194 = ( 1.0 * _Time.y * _DissolveNoiseTextureScroll1 + uv_TexCoord195);
			float2 panner206 = ( 1.0 * _Time.y * _DissolveNoiseTextureScroll2 + uv_TexCoord195);
			float lerpResult211 = lerp( tex2D( _DissolveNoiseTexture, panner194 ).r , tex2D( _DissolveNoiseTexture, panner206 ).r , 0.5);
			float2 temp_cast_1 = (( lerpResult211 * _DissolveNoiseAlpha )).xx;
			float4 uv2s_TexCoord22 = i.uv2_texcoord2;
			uv2s_TexCoord22.xy = i.uv2_texcoord2.xy + temp_cast_1;
			float temp_output_106_0 = ( 1.0 - uv2s_TexCoord22.x );
			float temp_output_161_0 = step( ( _Dissolve_Edge_Stength + 0.0 ) , temp_output_106_0 );
			#if defined(_DISSOLVEEDGECOLORSELECT_VERTEXCOLOR)
				float4 staticSwitch165 = ( i.vertexColor * _DissolveEdgeColorVertexColorStength );
			#elif defined(_DISSOLVEEDGECOLORSELECT_DISSOLVEEDGECOLOR)
				float4 staticSwitch165 = _DissolveEdgeColor;
			#else
				float4 staticSwitch165 = _DissolveEdgeColor;
			#endif
			float temp_output_187_0 = step( 0.0 , temp_output_106_0 );
			o.Emission = ( float4( ( hsvTorgb43 * temp_output_161_0 ) , 0.0 ) + ( staticSwitch165 * ( 1.0 - temp_output_161_0 ) * temp_output_187_0 ) ).rgb;
			o.Alpha = ( ( tex2DNode3.a * i.vertexColor.a ) * temp_output_187_0 );
		}

		ENDCG
	}
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=19105
Node;AmplifyShaderEditor.CommentaryNode;170;-2002.756,260.6342;Inherit;False;1235.646;532.8329;エミッション;8;162;139;161;163;160;158;165;187;;1,0,0.6545444,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;169;-2359.145,993.4814;Inherit;False;1701.296;778.5616;ディゾルブ;14;209;210;211;206;205;196;195;194;21;106;22;215;216;217;;0.434944,0.3811321,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;33;-948.2017,-140.0826;Inherit;False;317.4;275;アルファ;1;27;;1,0.5320755,0.5320755,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;30;-1931.776,-907.9413;Inherit;False;954.7888;796.8849;テクスチャ;6;45;44;3;23;29;24;;1,0.7655129,0.4849057,1;0;0
Node;AmplifyShaderEditor.BreakToComponentsNode;34;-551.2433,-583.3645;Inherit;False;FLOAT;1;0;FLOAT;0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.CommentaryNode;35;-757.5356,-782.0889;Inherit;False;766.6093;569.5361;色相、彩度、明度;9;43;42;41;40;39;38;37;36;34;;0.9394951,1,0.4377358,1;0;0
Node;AmplifyShaderEditor.RGBToHSVNode;36;-707.5356,-465.9529;Inherit;True;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RangedFloatNode;37;-698.4117,-642.3629;Inherit;False;Property;_Saturation;Saturation;5;0;Create;True;0;0;0;False;0;False;0;0;-1;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;39;-703.5887,-557.6793;Inherit;False;Property;_Value;Value;3;0;Create;True;0;0;0;False;0;False;0;0;-1;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;40;-386.6491,-516.1587;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;41;-380.5659,-616.5298;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.HSVToRGBNode;43;-247.9261,-518.5906;Inherit;True;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;44;-1432.38,-672.9172;Inherit;True;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;42;-383.6075,-418.8289;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;24;-1206.143,-424.4745;Inherit;True;3;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;38;-695.3698,-732.0889;Inherit;False;Property;_Hue;Hue;4;0;Create;True;0;0;0;False;0;False;0;0;-1;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;22;-1160.665,1043.481;Inherit;True;1;-1;4;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.OneMinusNode;106;-855.8495,1076.157;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StepOpNode;161;-1520.939,522.0839;Inherit;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;29;-1537.342,-262.5938;Inherit;False;Property;_Intensity;Intensity;6;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;171;228.759,-191.1463;Inherit;False;Property;_CullMode;CullMode;16;1;[Enum];Create;True;0;0;1;UnityEngine.Rendering.CullMode;True;0;False;0;2;0;2;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;162;-956.8583,387.6754;Inherit;True;3;3;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.VertexColorNode;27;-898.2017,-90.08255;Inherit;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;49;-444.2014,-96.88419;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;139;-1270.368,452.647;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;190;-217.458,75.56622;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;179;-622.0247,143.2102;Inherit;True;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleAddOpNode;188;-322.7984,338.1296;Inherit;True;2;2;0;FLOAT3;0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.VertexColorNode;23;-1861.821,-382.5765;Inherit;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;3;-1776.983,-612.4587;Inherit;True;Property;_Main_Texture;Main_Texture;1;1;[HDR];Create;True;0;0;0;False;0;False;-1;044e5444d744bf6448dbe5af02d16917;044e5444d744bf6448dbe5af02d16917;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;45;-1728.806,-793.1199;Inherit;False;Property;_Main_Color;Main_Color;2;1;[HDR];Create;True;0;0;0;False;0;False;1,1,1,1;2,2,2,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.StepOpNode;187;-1115.449,562.9769;Inherit;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;160;-1745.654,514.1167;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;192;-1237.806,22.2127;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;211;-1328.825,1369.914;Inherit;True;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;209;-1708.803,1381.451;Inherit;True;Property;_Disolve_Noise_Texture1;Disolve_Noise_Texture;7;0;Create;True;0;0;0;False;0;False;-1;2a588e30498964c459fb1c018db82238;08fad22e841b6434b869b64b0545342d;True;0;False;white;Auto;False;Instance;21;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;210;-1565.984,1599.322;Inherit;False;Constant;_Float0;Float 0;14;0;Create;True;0;0;0;False;0;False;0.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode;206;-1849.703,1238.74;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.PannerNode;194;-1857.03,1111.835;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;195;-2092.333,1057.097;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;216;-1050.762,1408.096;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;2;208.9746,-17.37146;Float;False;True;-1;3;ASEMaterialInspector;0;0;Unlit;Atsuki_Shader/Particle/Dissolve_Cutout_001;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;False;Off;0;False;;0;False;;False;0;False;;0;False;;False;0;Custom;0;True;False;0;False;TreeTransparentCutout;;AlphaTest;All;12;all;True;True;True;True;0;False;;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;2;15;10;25;False;0.5;False;0;5;False;;10;False;;0;5;False;;10;False;;0;False;;0;False;;0;False;0;0,0,0,0;VertexOffset;False;False;Cylindrical;False;True;Relative;0;;0;-1;-1;-1;0;True;0;0;True;_CullMode;-1;0;False;;0;0;0;False;0.1;False;;0;False;;False;15;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
Node;AmplifyShaderEditor.Vector2Node;215;-2352.749,1045.836;Inherit;False;Property;_DissolveNoiseTextureTiling;Dissolve Noise Texture Tiling;8;0;Create;True;0;0;0;False;0;False;1,1;1,1;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.Vector2Node;196;-2136.528,1205.759;Inherit;False;Property;_DissolveNoiseTextureScroll1;Dissolve Noise Texture Scroll 1;9;0;Create;True;0;0;0;False;0;False;0.1,0;0.1,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.Vector2Node;205;-2128.476,1350.664;Inherit;False;Property;_DissolveNoiseTextureScroll2;Dissolve Noise Texture Scroll 2;10;0;Create;True;0;0;0;False;0;False;0,0.1;0,0.1;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.RangedFloatNode;217;-1282.762,1634.096;Inherit;False;Property;_DissolveNoiseAlpha;Dissolve Noise Alpha;11;0;Create;True;0;0;0;False;0;False;1;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;158;-1966.956,506.729;Inherit;False;Property;_Dissolve_Edge_Stength;Dissolve_Edge_Stength;15;0;Create;True;0;0;0;False;0;False;0;0.015;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;193;-1618.682,37.00127;Inherit;False;Property;_DissolveEdgeColorVertexColorStength;Dissolve Edge Color Vertex Color Stength;13;0;Create;True;0;0;0;False;0;False;5;5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;163;-1670.759,304.1342;Inherit;False;Property;_DissolveEdgeColor;Dissolve Edge Color;14;1;[HDR];Create;True;0;0;0;False;0;False;0,0.5589409,1,0;2,0,0.06392288,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.StaticSwitch;165;-1381.566,320.3763;Inherit;False;Property;_DissolveEdgeColorSelect;Dissolve Edge Color Select;12;0;Create;True;0;0;0;False;0;False;0;1;1;True;;KeywordEnum;2;VertexColor;DissolveEdgeColor;Create;True;True;All;9;1;COLOR;0,0,0,0;False;0;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;4;COLOR;0,0,0,0;False;5;COLOR;0,0,0,0;False;6;COLOR;0,0,0,0;False;7;COLOR;0,0,0,0;False;8;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;21;-1504.178,1071.746;Inherit;True;Property;_DissolveNoiseTexture;Dissolve Noise Texture;7;1;[NoScaleOffset];Create;True;0;0;0;False;0;False;-1;2a588e30498964c459fb1c018db82238;08fad22e841b6434b869b64b0545342d;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
WireConnection;36;0;24;0
WireConnection;40;0;36;2
WireConnection;40;1;37;0
WireConnection;41;0;36;1
WireConnection;41;1;38;0
WireConnection;43;0;41;0
WireConnection;43;1;40;0
WireConnection;43;2;42;0
WireConnection;44;0;45;0
WireConnection;44;1;3;0
WireConnection;42;0;36;3
WireConnection;42;1;39;0
WireConnection;24;0;44;0
WireConnection;24;1;23;0
WireConnection;24;2;29;0
WireConnection;22;1;216;0
WireConnection;106;0;22;1
WireConnection;161;0;160;0
WireConnection;161;1;106;0
WireConnection;162;0;165;0
WireConnection;162;1;139;0
WireConnection;162;2;187;0
WireConnection;49;0;3;4
WireConnection;49;1;27;4
WireConnection;139;0;161;0
WireConnection;190;0;49;0
WireConnection;190;1;187;0
WireConnection;179;0;43;0
WireConnection;179;1;161;0
WireConnection;188;0;179;0
WireConnection;188;1;162;0
WireConnection;187;1;106;0
WireConnection;160;0;158;0
WireConnection;192;0;23;0
WireConnection;192;1;193;0
WireConnection;211;0;21;1
WireConnection;211;1;209;1
WireConnection;211;2;210;0
WireConnection;209;1;206;0
WireConnection;206;0;195;0
WireConnection;206;2;205;0
WireConnection;194;0;195;0
WireConnection;194;2;196;0
WireConnection;195;0;215;0
WireConnection;216;0;211;0
WireConnection;216;1;217;0
WireConnection;2;2;188;0
WireConnection;2;9;190;0
WireConnection;165;1;192;0
WireConnection;165;0;163;0
WireConnection;21;1;194;0
ASEEND*/
//CHKSM=1D1B0D9990A0DAA592E18956AC5E7E5E6F7FDD5F