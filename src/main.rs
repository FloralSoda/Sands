pub mod logging;
pub mod heatwave;

use std::path::PathBuf;

use clap::Parser;
use heatwave::prelude::Window;
use log::*;
use winit::{event::{Event, WindowEvent}, event_loop::ControlFlow, window::Icon};

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
        let instance = Window::new(game_name, Some(load_icon(&PathBuf::from("./assets/icon.png"))));
        instance.event_loop.run(|event, _, control_flow| {
            match event {
                Event::WindowEvent {
                    event: WindowEvent::CloseRequested,
                    ..
                } => {
                    *control_flow = ControlFlow::Exit;
                },

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
