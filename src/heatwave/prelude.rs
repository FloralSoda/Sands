use std::sync::Arc;

use vulkano::VulkanLibrary;
use vulkano::instance::{Instance, InstanceCreateInfo};
use vulkano::swapchain::Surface;
use winit::event::{Event, WindowEvent};
use winit::event_loop::{EventLoop, ControlFlow};
use once_cell::sync::Lazy;
use winit::window::WindowBuilder;

static LIBRARY: Lazy<Arc<VulkanLibrary>> = Lazy::new(|| {
	println!("Initialising Vulkan");

	VulkanLibrary::new().expect("No Vulkan library found")
});

pub struct Window {
	window: Arc<winit::window::Window>,
	surface: Arc<Surface>,
	pub event_loop: EventLoop<()>
}

impl Window {
	pub fn new(title: &str) -> Self {
		let event_loop = EventLoop::new();

		let required_extensions = Surface::required_extensions(&event_loop);
		let instance = Instance::new(
			LIBRARY.clone(),
			InstanceCreateInfo {
				enabled_extensions: required_extensions,
				..Default::default()
			}
		).expect("Failed to create a Vulkan instance");

		let window = Arc::new(WindowBuilder::new().with_title(title).build(&event_loop).expect("Failed to instance a new window:\n{:?}"));

		let surface = Surface::from_window(instance.clone(), window.clone()).expect("Failed to create a surface:\n{:?}");

		Self {
			window,
			event_loop,
			surface
		}
	}
}
