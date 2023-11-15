mod heatwave;
use clap::Parser;
use heatwave::prelude::Window;
use winit::{event::{Event, WindowEvent}, event_loop::ControlFlow};

#[derive(Parser, Debug)]
#[command(author, version, about, long_about = None)]
struct Args {
    /// Starts up the graphical toolbox
    #[arg(short, long)]
    toolbox: bool
}
fn main() {
    let instance = Window::new("Sands");
    instance.event_loop.run(|event, _, control_flow| {
        match event {
            Event::WindowEvent {
                event: WindowEvent::CloseRequested,
                ..
            } => {
                *control_flow = ControlFlow::Exit;
            }

            _ => ()
        }
    });
}
