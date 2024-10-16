using Godot;

namespace ViewportToCompositorEffectTest;

[Tool,GlobalClass]
public partial class ViewportToCompositorEffect : CompositorEffect
{
	private RenderingDevice _RD;
	private RenderSceneBuffersRD _RenderSceneBuffers;
	private RenderSceneDataRD _RenderSceneData;
	private Rid _CustomShader;
	private Rid _CustomPipeline;
	public Rid _CustomImage;
	private Rid _ScreenColorImage;
	private float[] _CustomPushConstants = new float[4] { 0f, 0f, 0f, 0f };
	private Vector2I _EffectSize;
	private Transform3D _CameraTransform;
	private Projection _ViewProjection;

	public ViewportToCompositorEffect()
	{
		EffectCallbackType = EffectCallbackTypeEnum.PostTransparent;
		RenderingServer.CallOnRenderThread( new Callable( this, MethodName._InitializeCompute ) );
	}

	public override void _Notification( int what )
	{
		if ( what == NotificationPredelete )
		{
			Rid[] rids = new Rid[] {
				_CustomShader,
				_CustomPipeline,
				_CustomImage,
				_ScreenColorImage
			};

			foreach ( Rid rid in rids )
			{
				if ( rid.IsValid )
				{
					_RD.FreeRid( rid );
				}
			}
		}
	}

	private RDUniform _GetImageUniform( Rid id, int binding = 0 )
	{
		RDUniform uniform = new() { UniformType = RenderingDevice.UniformType.Image, Binding = binding };
		uniform.AddId( id );
		return uniform;
	}

	private (Rid,Rid) _CreatePass( string file )
	{
		Rid shader = _RD.ShaderCreateFromSpirV( GD.Load<RDShaderFile>( file ).GetSpirV() );
		Rid pipeline = _RD.ComputePipelineCreate( shader );
		return ( shader, pipeline );
	}

	private void _ExecutePass( Rid shader, Rid pipeline, Vector2I size, byte[] pushConstant, params RDUniform[] uniforms )
	{
		_RD.DrawCommandBeginLabel( $"Execute pass [shader: {shader.Id}, pipeline: {pipeline.Id}]", Colors.White );

		long computeList = _RD.ComputeListBegin();
		_RD.ComputeListBindComputePipeline( computeList, pipeline );
		_RD.ComputeListSetPushConstant( computeList, pushConstant, (uint) pushConstant.Length );
		_RD.ComputeListBindUniformSet( computeList, UniformSetCacheRD.GetCache( shader, 0, new Godot.Collections.Array<RDUniform>( uniforms ) ), 0 );
		_RD.ComputeListDispatch( computeList, (uint)( ( size.X - 1 ) / 8 + 1 ), (uint)( ( size.Y - 1 ) / 8 + 1 ), 1 );
		_RD.ComputeListEnd();

		_RD.DrawCommandEndLabel();
	}

	private void _InitializeCompute()
	{
		_RD = RenderingServer.GetRenderingDevice();

		if ( _RD is null ) return;

		( _CustomShader, _CustomPipeline ) = _CreatePass( "res://custom_compositor_effect.glsl" );
	}

	public override void _RenderCallback( int effectCallbackType, RenderData renderData )
	{
		if ( _RD is not null && effectCallbackType == (int) EffectCallbackTypeEnum.PostTransparent )
		{
			_RenderSceneBuffers = (RenderSceneBuffersRD) renderData.GetRenderSceneBuffers();
			_RenderSceneData = (RenderSceneDataRD) renderData.GetRenderSceneData();

			if ( _RenderSceneBuffers is not null && _RenderSceneData is not null )
			{
				_EffectSize = _RenderSceneBuffers.GetInternalSize();

				if ( _EffectSize.X == 0 && _EffectSize.Y == 0 ) return;

				_CustomPushConstants[0] = _EffectSize.X;
				_CustomPushConstants[1] = _EffectSize.Y;

				_CameraTransform = _RenderSceneData.GetCamTransform();

				_RD.DrawCommandBeginLabel( "Weapon Particles", Colors.White );

				for ( uint view = 0; view < _RenderSceneBuffers.GetViewCount(); view++ )
				{
					_ViewProjection = _RenderSceneData.GetViewProjection( view );

					_ScreenColorImage = _RenderSceneBuffers.GetColorLayer( view );

					if ( _CustomImage.IsValid )
					{
						_ExecutePass(
							_CustomShader,
							_CustomPipeline,
							_EffectSize,
							_FloatArrayToByteArray( _CustomPushConstants ),
							_GetImageUniform( _CustomImage, 0 ),
							_GetImageUniform( _ScreenColorImage, 1 )
						);
					}
				}

				_RD.DrawCommandEndLabel();
			}
		}
	}

	private byte[] _FloatArrayToByteArray( float[] floats )
	{
		byte[] bytes = new byte[ floats.Length * sizeof( float ) ];
		System.Buffer.BlockCopy( floats, 0, bytes, 0, bytes.Length );
		return bytes;
	}
}