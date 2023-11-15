use std::sync::Arc;

use vulkano::VulkanLibrary;
use vulkano::device::physical::{PhysicalDeviceType, PhysicalDevice};
use vulkano::instance::{Instance, InstanceCreateInfo};
use vulkano::swapchain::Surface;
use winit::event_loop::EventLoop;
use once_cell::sync::Lazy;
use winit::window::{WindowBuilder, Icon};
use vulkano::device::{DeviceExtensions, QueueFlags};

use log::*;

static LIBRARY: Lazy<Arc<VulkanLibrary>> = Lazy::new(|| {
	info!(target: "Prelude", "Initialising Vulkan");

	VulkanLibrary::new().expect("No Vulkan library found")
});

pub struct Window {
	vulkan_instance: Arc<Instance>,
	window: Arc<winit::window::Window>,
	surface: Arc<Surface>,
	physical_device: Arc<PhysicalDevice>, 
	queue_family_index: u32, 
	pub event_loop: EventLoop<()>
}

///Represents a window with graphics handled by Heatwave
impl Window {
	///Creates a new window, using winit, binding to Vulkan and finding a physical device to use
	pub fn new(title: &str, icon: Option<Icon>) -> Self {
		let event_loop = EventLoop::new();

		let required_extensions = Surface::required_extensions(&event_loop);
		let instance = Instance::new(
			LIBRARY.clone(),
			InstanceCreateInfo {
				enabled_extensions: required_extensions,
				..Default::default()
			}
		).expect("Failed to create a Vulkan instance");

		debug!(target: "Heatwave", "Initialising window");
		let window = Arc::new(WindowBuilder::new().with_title(title).with_window_icon(icon).build(&event_loop).expect("Failed to instance a new window:\n{:?}"));

		let surface = Surface::from_window(instance.clone(), window.clone()).expect("Failed to create a surface:\n{:?}");

		let device_extensions = DeviceExtensions {
			khr_swapchain: true,
			..DeviceExtensions::empty()
		};

		debug!(target: "Heatwave", "Looking for compatible physical devices");
		let (physical_device, queue_family_index) = instance
			.enumerate_physical_devices()
			.expect("Could not enumerate physical graphics devices")
			.filter(|p| p.supported_extensions().contains(&device_extensions)) //Filter devices that support the operations we're gonna use
			.filter_map(|p| { //Filter out devices that doesn't support graphical operations or presenting to the surface
				p.queue_family_properties()
        			.iter()
        			.enumerate()
					.position(|(i,q)| {
						q.queue_flags.contains(QueueFlags::GRAPHICS) &&
							p.surface_support(i as u32, &surface).unwrap_or(false) //If this failed, assume it doesn't support
					})
				.map(|q| (p, q as u32))
			}) //All devices after this point should work with our app
			.min_by_key(|(p, _)| match p.properties().device_type { //Prioritise real GPUs, only use CPU if absolutely necessary
				PhysicalDeviceType::DiscreteGpu => 0,
				PhysicalDeviceType::IntegratedGpu => 1,
				PhysicalDeviceType::VirtualGpu => 2,
				PhysicalDeviceType::Cpu => 3,

				_ => 4, //Graphics-supporting physical device, but we don't know what it is so we can't guarantee it's ok to use.
			})
			.expect("No compatible graphics devices found.");
		debug!(target: "Heatwave", "Found compatible devices. Selected {} by default", physical_device.properties().device_name);
		Self {
			physical_device,
			queue_family_index,
			vulkan_instance: instance,
			window,
			event_loop,
			surface
		}
	}
}
