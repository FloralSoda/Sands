pub mod logging;
pub mod heatwave;
pub mod sands;

use std::{path::PathBuf, sync::{Arc, Mutex}, rc::Rc};

use clap::Parser;
use log::*;
use vulkano::{buffer::{Buffer, BufferCreateInfo, BufferUsage}, memory::allocator::{StandardMemoryAllocator, AllocationCreateInfo, MemoryTypeFilter}, pipeline::graphics::viewport::Viewport, sync::{future::FenceSignalFuture, self, GpuFuture}, command_buffer::{self, allocator::StandardCommandBufferAllocator}, swapchain::{self, SwapchainPresentInfo}, Validated, VulkanError};
use winit::{event::{Event, WindowEvent}, event_loop::ControlFlow, window::Icon};

use crate::{heatwave::prelude::WindowBuilder, sands::shaders::{ShaderVertex, test_vs, test_fs}};

#[derive(Parser, Debug)]
#[command(author, version, about, long_about = None)]
struct Args {
    /// Starts up the graphical toolbox
    #[arg(short, long)]
    toolbox: bool,

    ///Defines how much information the game should output to the logs. Defaults to Important
    #[arg(short, long)]
    verbosity: Option<logging::LogLevel>
}
fn main() {
    let game_name = "Sands";
    let args = Args::parse();

    if let Err(e) = std::fs::create_dir_all("./logs") {
        panic!("Failed to create directory for storing logs! Aborting program.\n{:?}", e)
    }
    if let Err(e) = init_logger(&PathBuf::from("./logs/output.log"), args.verbosity.unwrap_or(logging::LogLevel::Important)) {
        panic!("Failed to initialise logger! Aborting program.\n{:?}", e);
    }
    info!(target: "", "----===== New Sands Log @ {} =====----", chrono::Local::now().format("%d/%m/%Y %H:%M:%S%.3f"));

    if args.toolbox {
        //Start toolbox
        info!(target: "Toolbox", "Starting toolbox app");
    } else {
        info!(target: "Heatwave", "Launching game \"{}\"", game_name);

        let mut window = WindowBuilder::new().title(game_name.to_owned()).icon(load_icon(&PathBuf::from("./assets/icon.png"))).buffer_count(3).build();

        //TODO: Streamline below code for more generalised operation
        let render_pass = window.get_render_pass();
        let framebuffers = window.get_framebuffers(&render_pass);

        let memory_allocator = Arc::new(StandardMemoryAllocator::new_default(window.vulkan_device()));

        let vertex1 = ShaderVertex {
            position: [-0.5, -0.5]
        };
        let vertex2 = ShaderVertex {
            position: [0.0, 0.5]
        };
        let vertex3 = ShaderVertex {
            position: [0.5, -0.25]
        };

        let vertex_buffer = Buffer::from_iter(
            memory_allocator,
            BufferCreateInfo {
                usage: BufferUsage::VERTEX_BUFFER,
                ..Default::default()
            },
            AllocationCreateInfo {
                memory_type_filter: MemoryTypeFilter::PREFER_DEVICE |
                    MemoryTypeFilter::HOST_SEQUENTIAL_WRITE,
                ..Default::default()
            },
            vec![vertex1, vertex2, vertex3]
        ).expect("Failed to create buffer\n{:?}");

        let vs = test_vs::load(window.vulkan_device()).expect("Failed to create vector shader module");
        let fs = test_fs::load(window.vulkan_device()).expect("Failed to create fragment shader module");

        let mut viewport = Viewport {
            offset: [0.0, 0.0],
            extent: window.render_region_size().into(),
            depth_range: 0.0..=1.0,
        };

        let pipeline = window.get_pipeline(vs.clone(), fs.clone(), render_pass.clone(), viewport.clone());

        let command_buffer_allocator= StandardCommandBufferAllocator::new(window.vulkan_device(), Default::default());
        let mut command_buffers = window.get_command_buffers(&command_buffer_allocator, &pipeline, &framebuffers, &vertex_buffer);

        let mut window_resized = false;
        let mut recreate_swapchain = false;

        let frames_in_flight = window.image_count();
        let mut fences:Vec<Option<Arc<FenceSignalFuture<_>>>> = vec![None; frames_in_flight];
        let mut previous_fence_i = 0;

        let runner = window.get_runner();

        let window_rc = Rc::new(Mutex::new(window));

        trace!("Starting event loop");
        runner.start(move |event, _, control_flow| {
            match event {
                Event::WindowEvent {
                    event: WindowEvent::CloseRequested,
                    ..
                } => {
                    *control_flow = ControlFlow::Exit;
                },
                Event::WindowEvent { event: WindowEvent::Resized(_), .. } => {
                    window_resized = true;
                },
                Event::MainEventsCleared => {
                    let window_local_rc = Rc::clone(&window_rc);
                    let mut window_local = window_local_rc.lock().unwrap();

                    if window_resized || recreate_swapchain {
                        recreate_swapchain = false;

                        let new_dimensions = window_local.render_region_size();

                        window_local.resize(new_dimensions);
                        if window_resized {
                            trace!(target: "Heatwave Window Handler", "Request was a resize. Rebuilding framebuffers");
                            let new_framebuffers = window_local.get_framebuffers(&render_pass);
                            window_resized = false;

                            trace!(target: "Heatwave Window Handler", "Rebuilding pipeline");
                            viewport.extent = new_dimensions.into();
                            let new_pipeline = window_local.get_pipeline(vs.clone(), fs.clone(), render_pass.clone(), viewport.clone());
                            trace!(target: "Heatwave Window Handler", "Rebuilding command buffers");
                            command_buffers = window_local.get_command_buffers(&command_buffer_allocator, &new_pipeline, &new_framebuffers, &vertex_buffer);
                        }
                    }

                    let (image_i, suboptimal, acquire_future) = match swapchain::acquire_next_image(window_local.swapchain(), None).map_err(Validated::unwrap) {
                        Ok(r) => r,
                        Err(VulkanError::OutOfDate) => {
                            recreate_swapchain = true;
                            return;
                        }
                        Err(e) => panic!("Failed to get next image: {e}")
                    };

                    if suboptimal {
                        recreate_swapchain = true;
                    }

                    if let Some(image_fence) = &fences[previous_fence_i as usize].clone() {
                        trace!(target: "Heatwave Window Handler", "Awaiting image fence toggle");
                        image_fence.wait(None).unwrap();
                    }
                    let previous_future = match fences[previous_fence_i as usize].clone() {
                        None => { //Create a new Future
                            let mut now = sync::now(window_local.vulkan_device());
                            now.cleanup_finished();

                            now.boxed()
                        }
                        //Use the pre-existing one
                        Some(fence) => fence.boxed(),
                    };

                    let future = previous_future
                        .join(acquire_future)
                        .then_execute(window_local.queue(), command_buffers[image_i as usize].clone())
                        .unwrap()
                        .then_swapchain_present(
                            window_local.queue(), 
                            SwapchainPresentInfo::swapchain_image_index(window_local.swapchain(), image_i)
                        )
                        .then_signal_fence_and_flush();
                    
                    fences[image_i as usize] = match future.map_err(Validated::unwrap) {
                        Ok(value) => Some(Arc::new(value)),
                        Err(VulkanError::OutOfDate) => {
                            recreate_swapchain = true;
                            None
                        }
                        Err(e) => {
                            warn!("Failed to flush future: {e}");
                            None
                        }
                    };

                    previous_fence_i = image_i;
                }
                _ => ()
            }
        });
    }
}

pub fn init_logger(output: &PathBuf, level: logging::LogLevel) -> Result<(), SetLoggerError> {
    log::set_boxed_logger(Box::new(logging::Logger::new(output, level)))
        .map(|()| log::set_max_level(match level {
            logging::LogLevel::Important => log::LevelFilter::Warn,
            logging::LogLevel::Info => log::LevelFilter::Info,
            logging::LogLevel::Debug => log::LevelFilter::Debug,
            logging::LogLevel::Trace => log::LevelFilter::Trace,
        }))
}

fn load_icon(path: &PathBuf) -> Icon {
    let (icon_rgba, icon_width, icon_height) = {
        let img_data = image::open(path).expect("Couldn't open icon file").into_rgba8();

        let (width, height) = img_data.dimensions();
        let rgba = img_data.into_raw();
        (rgba, width, height)
    };
    match Icon::from_rgba(icon_rgba, icon_width, icon_height) {
        Ok(icon) => icon,
        Err(err) => panic!("Failed to read icon file's contents as image data: {:?}", err)
    }
}
