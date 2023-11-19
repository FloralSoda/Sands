use std::sync::Arc;

use vulkano::VulkanLibrary;
use vulkano::buffer::Subbuffer;
use vulkano::command_buffer::{PrimaryAutoCommandBuffer, AutoCommandBufferBuilder, RenderPassBeginInfo, SubpassBeginInfo};
use vulkano::command_buffer::allocator::StandardCommandBufferAllocator;
use vulkano::device::{Device, Queue, QueueFlags, DeviceExtensions, DeviceCreateInfo, QueueCreateInfo};
use vulkano::device::physical::{PhysicalDevice, PhysicalDeviceType};
use vulkano::image::{Image, ImageUsage};
use vulkano::image::view::ImageView;
use vulkano::instance::{Instance, InstanceCreateInfo};
use vulkano::pipeline::graphics::GraphicsPipelineCreateInfo;
use vulkano::pipeline::graphics::color_blend::{ColorBlendState, ColorBlendAttachmentState};
use vulkano::pipeline::graphics::input_assembly::InputAssemblyState;
use vulkano::pipeline::graphics::multisample::MultisampleState;
use vulkano::pipeline::graphics::rasterization::RasterizationState;
use vulkano::pipeline::layout::PipelineDescriptorSetLayoutCreateInfo;
use vulkano::pipeline::{GraphicsPipeline, PipelineShaderStageCreateInfo, PipelineLayout};
use vulkano::pipeline::graphics::vertex_input::{Vertex, VertexDefinition};
use vulkano::pipeline::graphics::viewport::{Viewport, ViewportState};
use vulkano::render_pass::{RenderPass, Framebuffer, FramebufferCreateInfo, Subpass};
use vulkano::shader::ShaderModule;
use vulkano::swapchain::{Surface, Swapchain, SwapchainCreateInfo};
use winit::dpi::PhysicalSize;
use winit::event::Event;
use winit::event_loop::{EventLoop, EventLoopWindowTarget, ControlFlow};
use winit::window::Icon;

use once_cell::sync::Lazy;

use log::*;

use crate::sands::shaders::ShaderVertex;


pub struct Window {
	vulkan_instance: Arc<Instance>,
	window: Arc<winit::window::Window>,
	surface: Arc<Surface>,
	vulkan_device: Arc<Device>,
	swapchain: Arc<Swapchain>,
	queue_family_index: u32, 
	event_loop: Option<EventLoop<()>>,
	images: Vec<Arc<Image>>,
	queue: Arc<Queue>
}

///Represents a window with graphics handled by Heatwave
impl Window {
	//TODO: Change unwrap to Result error
	pub fn get_render_pass(&self) -> Arc<RenderPass> {
		vulkano::single_pass_renderpass!(
			self.vulkan_device.clone(),
			attachments: {
				color: {
					format: (self.swapchain).image_format(),
					samples: 1,
					load_op: Clear,
					store_op: Store,
				}
			},
			pass: {
				color: [color],
				depth_stencil: {}
			}
		).unwrap()
	}

	//TODO: Change unwraps to return as Result errors
	pub fn get_framebuffers(&self, render_pass: &Arc<RenderPass>) -> Vec<Arc<Framebuffer>>{
		self.images.iter().map(|image| {
			let view = ImageView::new_default(image.clone()).unwrap();
			Framebuffer::new(
				render_pass.clone(),
				FramebufferCreateInfo {
					attachments: vec![view],
					..Default::default()
				},
			).unwrap()
		}).collect::<Vec<_>>()
	}

	//TODO: Generalise this function to allow any number of shaders to be loaded
	//TODO: Change unwraps and expects to an error type here
	pub fn get_pipeline(&self, vs: Arc<ShaderModule>, fs: Arc<ShaderModule>, render_pass: Arc<RenderPass>, viewport: Viewport) -> Arc<GraphicsPipeline> {
		let vs = vs.entry_point("main").expect("Vector shader did not have an entrypoint by the name of \"main\"");
		let fs = fs.entry_point("main").expect("Fragment shader did not have an entrypoint by the name of \"main\"");

		let vertex_input_state = ShaderVertex::per_vertex()
			.definition(&vs.info().input_interface)
			.expect("Failed to create the input state\n{:?}");

		let stages = [
			PipelineShaderStageCreateInfo::new(vs),
			PipelineShaderStageCreateInfo::new(fs)
		];

		let layout = PipelineLayout::new(
			self.vulkan_device(),
			PipelineDescriptorSetLayoutCreateInfo::from_stages(&stages)
				.into_pipeline_layout_create_info(self.vulkan_device())
				.unwrap(), //TODO: Learn how this can fail, and create a descriptive error struct
		)
		.unwrap();

		//TODO: What does the id do? How do I control this id?
		let subpass = Subpass::from(render_pass.clone(), 0).expect("Failed to create Subpass\n{:?}");

		GraphicsPipeline::new(
			self.vulkan_device(), 
			None, //TODO: Allow usage of caches 
			GraphicsPipelineCreateInfo {
				stages: stages.into_iter().collect(),
				vertex_input_state: Some(vertex_input_state),
				input_assembly_state: Some(InputAssemblyState::default()), //Todo: What else can go here?
				viewport_state: Some(ViewportState {
					viewports: [viewport].into_iter().collect(), //TODO: Allow multiple viewports
					..Default::default()
				}),//TODO: Research how to use viewport_dynamic_scissor_irrelevant
				rasterization_state: Some(RasterizationState::default()), //Todo: What else can go here?
				multisample_state: Some(MultisampleState::default()), //Todo: ^
				color_blend_state: Some(ColorBlendState::with_attachment_states(
					subpass.num_color_attachments(),
					ColorBlendAttachmentState::default() //Todo: What is this and how can it vary?
				)),
				subpass: Some(subpass.into()),
				..GraphicsPipelineCreateInfo::layout(layout)
			}
		).unwrap()
	}

	pub fn get_command_buffers(&self, command_buffer_allocator: &StandardCommandBufferAllocator, pipeline: &Arc<GraphicsPipeline>, framebuffers: &[Arc<Framebuffer>], vertex_buffer: &Subbuffer<[ShaderVertex]>) -> Vec<Arc<PrimaryAutoCommandBuffer>> {
		framebuffers.iter()
			.map(|framebuffer| {
				let mut builder = AutoCommandBufferBuilder::primary(
					command_buffer_allocator,
					self.queue.queue_family_index(),
					vulkano::command_buffer::CommandBufferUsage::MultipleSubmit
				).unwrap(); //Todo: Handle this error

				builder.begin_render_pass(RenderPassBeginInfo {
					clear_values: vec![Some([0.0,0.0,1.0,1.0].into())],
					..RenderPassBeginInfo::framebuffer(framebuffer.clone())
				}, SubpassBeginInfo {
					contents: vulkano::command_buffer::SubpassContents::Inline,
					..Default::default()
				}).unwrap()
				.bind_pipeline_graphics(pipeline.clone())
				.unwrap()
				.bind_vertex_buffers(0, vertex_buffer.clone())
				.unwrap()
				.draw(vertex_buffer.len() as u32, 1,0,0)
				.unwrap()
				.end_render_pass(Default::default())
				.unwrap();

			builder.build().unwrap()
			})
		.collect()
	}

	pub fn vulkan_device(&self) -> Arc<Device> {
		self.vulkan_device.clone()
	}
	pub fn swapchain(&self) -> Arc<Swapchain> {
		self.swapchain.clone()
	}
	pub fn render_region_size(&self) -> PhysicalSize<u32> {
		self.window.inner_size()
	}
	pub fn queue(&self) -> Arc<Queue> {
		self.queue.clone()
	}
	pub fn image_count(&self) -> usize {
		self.images.len()
	}
	pub fn resize(&mut self, new_size: PhysicalSize<u32>) {
		let (new_swapchain, new_buffers) = self.swapchain.recreate(SwapchainCreateInfo {
			image_extent: new_size.into(),
			..self.swapchain.create_info()
		}).expect("Failed to resize swapchain\n{:?}");

		self.swapchain = new_swapchain;
		self.images = new_buffers;
	}

	pub fn get_runner(&mut self) -> WindowRunner {
		WindowRunner { event_loop:  self.event_loop.take().expect("Expect an event loop, but received none. Is there already a runner for this?") }
	}

}

static LIBRARY: Lazy<Arc<VulkanLibrary>> = Lazy::new(|| {
	info!(target: "Prelude", "Initialising Vulkan");

	VulkanLibrary::new().expect("No Vulkan library found")
});

pub struct WindowRunner {
	event_loop: EventLoop<()>
}
impl WindowRunner {
	///Occupies thread
	pub fn start<F>(self, event_handler: F) where
		F: 'static + FnMut(Event<'_, ()>, &EventLoopWindowTarget<()>, &mut ControlFlow) {
		self.event_loop.run(event_handler);
	}
}

#[derive(Default)]
pub struct WindowBuilder {
	title: String,
	buffer_count: u8,

	icon: Option<Icon>,
	preferred_device: Option<u32>
}
impl WindowBuilder {
	pub fn new() -> WindowBuilder {
		WindowBuilder {
			title: "Heatwave App".to_owned(),
			buffer_count: 2,
			..Default::default()
		}
	}
	pub fn title(&mut self, new_title: String) -> &mut Self {
		self.title = new_title;
		self
	}
	pub fn icon(&mut self, new_icon: Icon) -> &mut Self {
		self.icon = Some(new_icon);
		self
	}
	pub fn buffer_count(&mut self, new_count: u8) -> &mut Self {
		self.buffer_count = new_count;
		self
	}
	pub fn with_physical_device(&mut self, device_id: u32) -> &mut Self {
		trace!(target: "Heatwave Window Builder", "Using custom physical device {}", device_id);
		self.preferred_device = Some(device_id);
		self
	}
	fn scan_for_device(compatible_devices: impl Iterator<Item =(Arc<PhysicalDevice>, u32)>) -> (Arc<PhysicalDevice>, u32) {
		let (physical_device, index) = compatible_devices //All devices after this point should work with our app
			.min_by_key(|(p, _)| match p.properties().device_type { //Prioritise real GPUs, only use CPU if absolutely necessary
				PhysicalDeviceType::DiscreteGpu => 0,
				PhysicalDeviceType::IntegratedGpu => 1,
				PhysicalDeviceType::VirtualGpu => 2,
				PhysicalDeviceType::Cpu => 3,

				_ => 4, //Graphics-supporting physical device, but we don't know what it is so we can't guarantee it's ok to use.
			})
			.expect("No compatible graphics devices found."); //Todo: Return a Result error
		debug!(target: "Heatwave Init", "Found compatible devices. Selected {} by default (id: {})", physical_device.properties().device_name, physical_device.properties().device_id);
		(physical_device, index)
	}
	
	pub fn build(&self) -> Window { //Todo: Return a Result, as there are numerous ways this could go wrong, and we want to give the user control over whether or not this stops the app from loading
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
		let mut builder = winit::window::WindowBuilder::new()
			.with_title(&self.title);
		if let Some(icon) = &self.icon {
			builder = builder.with_window_icon(Some(icon.clone()));
		}
			
		let window = Arc::new(builder.build(&event_loop)
			.expect("Failed to instance a new window:\n{:?}"));

		let surface = Surface::from_window(instance.clone(), window.clone()).expect("Failed to create a surface:\n{:?}");

		let device_extensions = DeviceExtensions {
			khr_swapchain: true,
			..DeviceExtensions::empty()
		};

		debug!(target: "Heatwave Init", "Looking for compatible physical devices");
		let mut compatible_devices = instance
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
		});//All devices after this point should work with our app
	
		let device_extensions = DeviceExtensions {
			khr_swapchain: true,
			..DeviceExtensions::empty()
		};

		let (physical_device, queue_family_index) = match self.preferred_device {
			Some(device_name) => {
				match compatible_devices.find(|(device, _)| {
					device.properties().device_id == device_name
				}) {
					Some((device, index)) => (device, index),
					None => {
						warn!(target:"Heatwave Init", "Failed to find configured device. Using default");
						WindowBuilder::scan_for_device(compatible_devices)
					}
				}
			},
			None => {
				WindowBuilder::scan_for_device(compatible_devices)
			}
		};
		
		debug!(target: "Heatwave Init", "Creating logical device");
		let (device, mut queues) = Device::new(
			physical_device.clone(),
			DeviceCreateInfo {
				queue_create_infos: vec![QueueCreateInfo {
					queue_family_index,
					..Default::default()
				}],
				enabled_extensions: device_extensions,
				..Default::default()
			}
		).expect("Failed to create logical device");

		let queue = queues.next().expect("Should have found a vulkan queue on logical device.");

		debug!(target: "Heatwave Init", "Creating swapchain");
		let capabilities = physical_device
			.surface_capabilities(&surface, Default::default())
			.expect("Failed to get capabilities of generated surface");

		let dimensions = window.inner_size(); //Get size of the window
		let composite_alpha = capabilities.supported_composite_alpha.into_iter().next().expect("Could not get the transparency behavior");
		let image_format = physical_device
			.surface_formats(&surface, Default::default())
			.expect("Couldn't get the surface format for the bound physical device")[0]
			.0;

		debug!(target: "Heatwave Init", "Initialising swapchain with following details:\n\tBuffer Count: \t{}\n\tDimensions: \tX: {},Y: {} ", 
				(capabilities.min_image_count + 1).max(self.buffer_count.into()).min(capabilities.max_image_count.unwrap_or(3)), 
				dimensions.width, 
				dimensions.height);

		
		let (swapchain, images) = Swapchain::new(
			device.clone(),
			surface.clone(),
			SwapchainCreateInfo {
				min_image_count: (capabilities.min_image_count + 1)//How many buffers to use in the swapchain
										.max(self.buffer_count.into())
										.min(capabilities.max_image_count.unwrap_or(3)), //Limited to the max image count. Assumes triple buffer if can't find a max count
				image_format,
				image_extent: dimensions.into(),
				image_usage: ImageUsage::COLOR_ATTACHMENT, //What images are going to be used for 
				composite_alpha,
				..Default::default()
			}
		).expect("Failed to create Swapchain");

		Window {
    		vulkan_instance: instance,
    		window,
    		surface,
    		vulkan_device: device,
    		queue_family_index,
    		event_loop: Some(event_loop),
			swapchain,
			images,
			queue
		}
	}
}
