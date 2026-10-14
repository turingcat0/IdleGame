// Made with Amplify Shader Editor v1.9.1.5
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Atsuki_Shader/Particle/Flipbook_001"
{
	Properties
	{
		_RowsColumnsXY("Rows Columns XY", Vector) = (8,8,0,0)
		_MainTex1("Sprite Sheet", 2D) = "white" {}
		_MotionVector("MotionVector", 2D) = "white" {}
		_Influence("Influence", Float) = 0.003
		_Smaoke("Smaoke", Color) = (0,0,0,0)
		[HDR]_Fire("Fire", Color) = (0,0,0,0)
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Transparent"  "Queue" = "Transparent+0" "IgnoreProjector" = "True" "IsEmissive" = "true"  }
		Cull Off
		CGPROGRAM
		#pragma target 3.0
		#pragma surface surf Standard alpha:fade keepalpha noshadow 
		#undef TRANSFORM_TEX
		#define TRANSFORM_TEX(tex,name) float4(tex.xy * name##_ST.xy + name##_ST.zw, tex.z, tex.w)
		struct Input
		{
			float4 vertexColor : COLOR;
			float3 uv_texcoord;
		};

		uniform float4 _Smaoke;
		uniform float4 _Fire;
		uniform sampler2D _MainTex1;
		uniform float2 _RowsColumnsXY;
		uniform sampler2D _MotionVector;
		uniform float _Influence;

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float temp_output_4_0_g5 = _RowsColumnsXY.x;
			float temp_output_5_0_g5 = _RowsColumnsXY.y;
			float2 appendResult7_g5 = (float2(temp_output_4_0_g5 , temp_output_5_0_g5));
			float totalFrames39_g5 = ( temp_output_4_0_g5 * temp_output_5_0_g5 );
			float2 appendResult8_g5 = (float2(totalFrames39_g5 , temp_output_5_0_g5));
			float clampResult42_g5 = clamp( 0.0 , 0.0001 , ( totalFrames39_g5 - 1.0 ) );
			float temp_output_35_0_g5 = frac( ( ( i.uv_texcoord.z + clampResult42_g5 ) / totalFrames39_g5 ) );
			float2 appendResult29_g5 = (float2(temp_output_35_0_g5 , ( 1.0 - temp_output_35_0_g5 )));
			float2 temp_output_15_0_g5 = ( ( i.uv_texcoord.xy / appendResult7_g5 ) + ( floor( ( appendResult8_g5 * appendResult29_g5 ) ) / appendResult7_g5 ) );
			float2 temp_output_1_0 = temp_output_15_0_g5;
			float2 temp_cast_0 = (1.0).xx;
			float temp_output_31_0 = frac( i.uv_texcoord.z );
			float4 tex2DNode3 = tex2D( _MainTex1, ( temp_output_1_0 - ( ( ( ( (tex2D( _MotionVector, temp_output_1_0 )).rg * 2.0 ) - temp_cast_0 ) * temp_output_31_0 ) * _Influence ) ) );
			float temp_output_4_0_g4 = _RowsColumnsXY.x;
			float temp_output_5_0_g4 = _RowsColumnsXY.y;
			float2 appendResult7_g4 = (float2(temp_output_4_0_g4 , temp_output_5_0_g4));
			float totalFrames39_g4 = ( temp_output_4_0_g4 * temp_output_5_0_g4 );
			float2 appendResult8_g4 = (float2(totalFrames39_g4 , temp_output_5_0_g4));
			float clampResult42_g4 = clamp( 0.0 , 0.0001 , ( totalFrames39_g4 - 1.0 ) );
			float temp_output_35_0_g4 = frac( ( ( ( i.uv_texcoord.z + 1.0 ) + clampResult42_g4 ) / totalFrames39_g4 ) );
			float2 appendResult29_g4 = (float2(temp_output_35_0_g4 , ( 1.0 - temp_output_35_0_g4 )));
			float2 temp_output_15_0_g4 = ( ( i.uv_texcoord.xy / appendResult7_g4 ) + ( floor( ( appendResult8_g4 * appendResult29_g4 ) ) / appendResult7_g4 ) );
			float2 temp_output_8_0 = temp_output_15_0_g4;
			float2 temp_cast_1 = (1.0).xx;
			float4 tex2DNode38 = tex2D( _MainTex1, ( ( ( ( ( (tex2D( _MotionVector, temp_output_8_0 )).rg * 2.0 ) - temp_cast_1 ) * ( 1.0 - temp_output_31_0 ) ) * _Influence ) + temp_output_8_0 ) );
			float4 lerpResult40 = lerp( tex2DNode3 , tex2DNode38 , temp_output_31_0);
			float saferPower48 = abs( (lerpResult40).r );
			float4 lerpResult44 = lerp( _Smaoke , ( i.vertexColor * _Fire ) , pow( saferPower48 , 3.5 ));
			o.Albedo = lerpResult44.rgb;
			o.Emission = lerpResult44.rgb;
			float lerpResult43 = lerp( tex2DNode3.a , tex2DNode38.a , temp_output_31_0);
			o.Alpha = ( lerpResult43 * i.vertexColor.a );
		}

		ENDCG
	}
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=19105
Node;AmplifyShaderEditor.SimpleAddOpNode;9;-3697.226,319.8157;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;20;-2660.955,172.8923;Inherit;False;Constant;_2;2;5;0;Create;True;0;0;0;False;0;False;2;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;23;-2345.955,176.8923;Inherit;False;Constant;_1;1;5;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode;16;-2730.824,491.1839;Inherit;False;True;True;False;False;1;0;COLOR;0,0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;19;-2442.104,485.8037;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;22;-2186.558,475.2162;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;30;-1959.898,492.7146;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;34;-1734.697,484.2823;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;39;-1537.146,374.8814;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ComponentMaskNode;15;-2722.179,-248.1213;Inherit;False;True;True;False;False;1;0;COLOR;0,0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;21;-2246.634,-234.8839;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;29;-1966.043,-241.3172;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;33;-1710.673,-246.9893;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;36;-1535.103,-169.3286;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ColorNode;46;-671.3639,-539.0031;Inherit;False;Property;_Smaoke;Smaoke;4;0;Create;True;0;0;0;False;0;False;0,0,0,0;0.4433961,0.1876279,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;51;-832.2981,-340.8909;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.VertexColorNode;52;-498.6537,344.1202;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector2Node;2;-3858.799,-229.2452;Inherit;False;Property;_RowsColumnsXY;Rows Columns XY;0;0;Create;True;0;0;0;False;0;False;8,8;8,8;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.WireNode;41;-1770.066,85.07009;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode;62;-1016.55,241.1351;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode;61;-1634.537,256.394;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;32;-2032.499,236.564;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WireNode;63;-2456.613,69.47164;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;35;-1811.954,141.7975;Inherit;False;Property;_Influence;Influence;3;0;Create;True;0;0;0;False;0;False;0.003;0.0005;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;53;-268.6537,228.1202;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;60;18.94008,-115.8687;Float;False;True;-1;2;ASEMaterialInspector;0;0;Standard;Atsuki_Shader/Particle/Flipbook_001;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;False;Off;0;False;;0;False;;False;0;False;;0;False;;False;0;Transparent;0.5;True;False;0;False;Transparent;;Transparent;All;12;all;True;True;True;True;0;False;;False;0;False;;255;False;;255;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;2;15;10;25;False;0.5;False;2;5;False;;10;False;;0;0;False;;0;False;;0;False;;0;False;;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;;-1;0;False;;0;0;0;False;0.1;False;;0;False;;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
Node;AmplifyShaderEditor.LerpOp;44;-302.7046,-375.0031;Inherit;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.VertexColorNode;50;-1113.298,-494.8909;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ComponentMaskNode;42;-647.3088,-120.7163;Inherit;False;True;False;False;False;1;0;COLOR;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;48;-444.7756,-198.0031;Inherit;False;True;2;0;FLOAT;0;False;1;FLOAT;3.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;18;-2464.444,-246.3729;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.LerpOp;40;-808.1233,14.26367;Inherit;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;43;-793.9771,268.5099;Inherit;True;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;11;-3065.058,-292.1895;Inherit;True;Property;_MotionVector;MotionVector;2;0;Create;True;0;0;0;False;0;False;-1;None;63cea16fd316a774793678225716a8f3;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;12;-3042.113,448.9257;Inherit;True;Property;_MotionVector1;MotionVector;2;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Instance;11;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;38;-1207.63,321.2896;Inherit;True;Property;_SpriteSheet1;Sprite Sheet;1;0;Create;True;0;0;0;False;0;False;-1;None;10e4b9444fb224e4d91b47775e1fd928;True;0;False;white;Auto;False;Instance;3;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;3;-1204.099,-82.37952;Inherit;True;Property;_MainTex1;Sprite Sheet;1;0;Create;False;0;0;0;False;0;False;-1;None;b99e573eb02119a4394fbb1d8ea463a4;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;10;-4006.626,421.2158;Inherit;False;Constant;_Offsset;Offsset;4;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;47;-1197.364,-309.0031;Inherit;False;Property;_Fire;Fire;5;1;[HDR];Create;True;0;0;0;False;0;False;0,0,0,0;1,0.3256541,0.2584905,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.FunctionNode;8;-3477.827,357.5158;Inherit;True;Flipbook;-1;;4;53c2488c220f6564ca6c90721ee16673;2,71,0,68,0;8;51;SAMPLER2D;0.0;False;13;FLOAT2;0,0;False;4;FLOAT;3;False;5;FLOAT;3;False;24;FLOAT;0;False;2;FLOAT;0;False;55;FLOAT;0;False;70;FLOAT;0;False;5;COLOR;53;FLOAT2;0;FLOAT;47;FLOAT;48;FLOAT;62
Node;AmplifyShaderEditor.FractNode;31;-2108.917,137.9789;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TexCoordVertexDataNode;49;-3905.719,-7.195435;Inherit;False;0;3;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.FunctionNode;1;-3508.251,-155.1997;Inherit;True;Flipbook;-1;;5;53c2488c220f6564ca6c90721ee16673;2,71,0,68,0;8;51;SAMPLER2D;0.0;False;13;FLOAT2;0,0;False;4;FLOAT;3;False;5;FLOAT;3;False;24;FLOAT;0;False;2;FLOAT;0;False;55;FLOAT;0;False;70;FLOAT;0;False;5;COLOR;53;FLOAT2;0;FLOAT;47;FLOAT;48;FLOAT;62
WireConnection;9;0;49;3
WireConnection;9;1;10;0
WireConnection;16;0;12;0
WireConnection;19;0;16;0
WireConnection;19;1;20;0
WireConnection;22;0;19;0
WireConnection;22;1;23;0
WireConnection;30;0;22;0
WireConnection;30;1;32;0
WireConnection;34;0;30;0
WireConnection;34;1;35;0
WireConnection;39;0;34;0
WireConnection;39;1;8;0
WireConnection;15;0;11;0
WireConnection;21;0;18;0
WireConnection;21;1;23;0
WireConnection;29;0;21;0
WireConnection;29;1;31;0
WireConnection;33;0;29;0
WireConnection;33;1;35;0
WireConnection;36;0;1;0
WireConnection;36;1;33;0
WireConnection;51;0;50;0
WireConnection;51;1;47;0
WireConnection;41;0;31;0
WireConnection;62;0;61;0
WireConnection;61;0;31;0
WireConnection;32;0;31;0
WireConnection;63;0;49;3
WireConnection;53;0;43;0
WireConnection;53;1;52;4
WireConnection;60;0;44;0
WireConnection;60;2;44;0
WireConnection;60;9;53;0
WireConnection;44;0;46;0
WireConnection;44;1;51;0
WireConnection;44;2;48;0
WireConnection;42;0;40;0
WireConnection;48;0;42;0
WireConnection;18;0;15;0
WireConnection;18;1;20;0
WireConnection;40;0;3;0
WireConnection;40;1;38;0
WireConnection;40;2;41;0
WireConnection;43;0;3;4
WireConnection;43;1;38;4
WireConnection;43;2;62;0
WireConnection;11;1;1;0
WireConnection;12;1;8;0
WireConnection;38;1;39;0
WireConnection;3;1;36;0
WireConnection;8;4;2;1
WireConnection;8;5;2;2
WireConnection;8;2;9;0
WireConnection;31;0;63;0
WireConnection;1;4;2;1
WireConnection;1;5;2;2
WireConnection;1;2;49;3
ASEEND*/
//CHKSM=8944E3272B6DA7A78AD4B30B1FCD884BDF486FCF