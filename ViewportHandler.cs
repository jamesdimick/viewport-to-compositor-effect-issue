using Godot;

namespace ViewportToCompositorEffectTest;

[Tool,GlobalClass]
public partial class ViewportHandler : Node3D
{
	[Export(PropertyHint.Range,"0,100000,1")]
	public int Amount = 3;

	[Export(PropertyHint.Range,"0,100,0.01")]
	public float Radius { get { return _Radius; } set { _Radius = value; _CustomMaterial?.SetShaderParameter( "radius", value ); } }

	[Export]
	public WorldEnvironment Environment;

	[Export(PropertyHint.Layers3DRender)]
	public uint Layers = 524288u;

	private float _Radius = 0.2f;
	private readonly System.Collections.Generic.List<Rid> _Instances = new();
	private Rid _WorldScenario;
	private Viewport _MainViewport;
	private Camera3D _MainCamera;
	private Rid _CustomViewport;
	private Rid _CustomCamera;
	private PointMesh _Mesh;
	private Rid _MeshRID;
	private ShaderMaterial _CustomMaterial;
	private Rid _CustomMaterialRID;

	public override void _Ready()
	{
		_WorldScenario = GetWorld3D().Scenario;
		_MainViewport = Engine.IsEditorHint() ? EditorInterface.Singleton.GetEditorViewport3D() : GetViewport();
		Vector2 mainViewportSize = _MainViewport.GetVisibleRect().Size;
		_MainCamera = _MainViewport.GetCamera3D();
		_CustomViewport = RenderingServer.ViewportCreate();
		_CustomCamera = RenderingServer.CameraCreate();
		_Mesh = new();
		_MeshRID = _Mesh.GetRid();
		_CustomMaterial = ResourceLoader.Load<ShaderMaterial>( "res://custom_material.tres" );
		_CustomMaterial.SetShaderParameter( "radius", Radius );
		_CustomMaterialRID = _CustomMaterial.GetRid();

		RenderingServer.CameraSetCullMask( _CustomCamera, Layers );
		RenderingServer.CameraSetPerspective( _CustomCamera, _MainCamera.Fov, _MainCamera.Near, _MainCamera.Far );

		RenderingServer.ViewportSetSize( _CustomViewport, (int) mainViewportSize.X, (int) mainViewportSize.Y );
		RenderingServer.ViewportSetUpdateMode( _CustomViewport, RenderingServer.ViewportUpdateMode.Always );
		RenderingServer.ViewportSetScenario( _CustomViewport, _WorldScenario );
		RenderingServer.ViewportAttachCamera( _CustomViewport, _CustomCamera );
		RenderingServer.ViewportSetDebugDraw( _CustomViewport, RenderingServer.ViewportDebugDraw.Unshaded );
		RenderingServer.ViewportSetActive( _CustomViewport, true );

		Setup();
	}

	public override void _ExitTree()
	{
		RenderingServer.FreeRid( _WorldScenario );
		RenderingServer.FreeRid( _CustomViewport );
		RenderingServer.FreeRid( _CustomCamera );
		RenderingServer.FreeRid( _MeshRID );
		RenderingServer.FreeRid( _CustomMaterialRID );

		Cleanup();
	}

	public override void _Process( double delta )
	{
		Capture();
	}

	public void Setup()
	{
		Cleanup();

		_Mesh = new();
		_MeshRID = _Mesh.GetRid();
		_CustomMaterial = ResourceLoader.Load<ShaderMaterial>( "res://custom_material.tres" );
		_CustomMaterial.SetShaderParameter( "radius", Radius );
		_CustomMaterialRID = _CustomMaterial.GetRid();

		for ( int i = 0; i < Amount; i++ )
		{
			Rid instance = RenderingServer.InstanceCreate();

			_Instances.Add( instance );

			RenderingServer.InstanceSetScenario( instance, _WorldScenario );
			RenderingServer.InstanceSetBase( instance, _MeshRID );
			RenderingServer.InstanceSetSurfaceOverrideMaterial( instance, 0, _CustomMaterialRID );
			RenderingServer.InstanceSetTransform( instance, new Transform3D( Basis.Identity, new Vector3( (float) GD.RandRange( -10f, 10f ), (float) GD.RandRange( 0f, 10f ), (float) GD.RandRange( -10f, 10f ) ) ) );
			RenderingServer.InstanceSetExtraVisibilityMargin( instance, Radius * 2f );
			RenderingServer.InstanceSetLayerMask( instance, Layers );
			RenderingServer.InstanceSetVisible( instance, true );
		}
	}

	public void Cleanup()
	{
		if ( _Instances.Count > 0 )
		{
			_Instances.ForEach( RenderingServer.FreeRid );
			_Instances.Clear();
		}
	}

	public async void Capture()
	{
		RenderingServer.CameraSetTransform( _CustomCamera, _MainCamera.GlobalTransform );

		await ToSignal( RenderingServer.Singleton, RenderingServerInstance.SignalName.FramePostDraw );

		if ( Environment?.Compositor?.CompositorEffects?[0] is ViewportToCompositorEffect effect )
		{
			effect._CustomImage = RenderingServer.TextureGetRdTexture( RenderingServer.ViewportGetTexture( _CustomViewport ) );
		}
	}
}