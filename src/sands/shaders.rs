use vulkano::{buffer::BufferContents, pipeline::graphics::vertex_input::Vertex};

#[derive(BufferContents, Vertex)]
#[repr(C)]
pub struct ShaderVertex {
	#[format(R32G32_SFLOAT)]
	pub position: [f32; 2],
}

//TODO: Macro to automatically compile shaders
pub mod test_vs {
	vulkano_shaders::shader! {
		ty: "vertex",
		path: "./assets/shaders/vertex/test_vs.vert"
	}
}
pub mod test_fs {
	vulkano_shaders::shader! {
		ty: "fragment",
		path: "./assets/shaders/fragment/test_fs.frag"
	}
}
