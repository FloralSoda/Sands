use std::{fs::{File, self}, path::{PathBuf, Path}, ffi::OsStr, io::Write, sync::{atomic::AtomicBool, Arc}};
use log::*;
use colored::Colorize;

pub struct Logger {
	level: LogLevel,
	output: Option<File>,
	write_fail: Arc<AtomicBool>,
}

impl Logger {
	pub fn new(output: &PathBuf, level: LogLevel) -> Self {
		if let Ok(exists) = Path::try_exists(output) {
			if exists {
				let mut counter: usize = 1;
				let extension = output.extension().and_then(OsStr::to_str).unwrap_or("");
				let stem = output.with_extension("").to_string_lossy().to_string();
				loop {
					let mut path_str = stem.clone().to_string();
					path_str.push_str(".old.");
					path_str.push_str(counter.to_string().as_str());
					path_str.push('.');
					path_str.push_str(extension);
					let new_path = PathBuf::from(path_str);
					if Path::try_exists(&new_path).unwrap_or(false) {
						counter += 1;
						if counter == usize::MAX {
							print!("{}","[ERROR] Log Init: Too many logs, aborting relocation. Will append to most recent output.log".red())
						}
					} else {
						if let Err(e) = fs::rename(output, &new_path) {
							print!("{}",format!("[ERROR] Log Init: Failed to relocate log file ({} to {}). Will append to it\n{:?}", output.to_string_lossy(), new_path.to_string_lossy(), e).red());
						}
						break;
					}
				}
			}
		}
		let file = match fs::OpenOptions::new().append(true).create(true).open(output) {
			Ok(file) => Some(file),
			Err(err) => {println!("{}",format!("[ERROR] Log Init: Failed to create a new log file at \"{}\"\n{:?}", output.to_string_lossy(), err).red()); None}
		};
		Self {
			level,
			output: file,
			write_fail: Arc::new(AtomicBool::new(false))
		}
	}
}
impl log::Log for Logger {
    fn enabled(&self, metadata: &log::Metadata) -> bool {
        metadata.level() <= match self.level {
			LogLevel::Important => log::Level::Warn,
			LogLevel::Info => log::Level::Info,
			LogLevel::Debug => log::Level::Debug,
			LogLevel::Trace => log::Level::Trace
		}
    }

    fn log(&self, record: &log::Record) {
        if self.enabled(record.metadata()) {
			let line = format!("[{}] {} | {}: {}\n", match record.level() {
				Level::Error => record.level().to_string().red(),
				Level::Warn => record.level().to_string().yellow(),
				Level::Info => record.level().to_string().green(),
				Level::Debug => record.level().to_string().bright_blue(),
				Level::Trace => record.level().to_string().white().italic()
			}, chrono::Local::now().format("%H:%M:%S%.3f").to_string().blue().italic(), record.target(), record.args());
			print!("{}", line);
			if !self.write_fail.load(std::sync::atomic::Ordering::Acquire) {
				if let Some(file) = self.output.as_ref().as_mut() {
					if let Err(e) = file.write_all(line.as_bytes()) {
						error!(target: "Logging", "Failed to write to logging file! Aborting future write operations, using terminal only mode.\n{:?}", e);
						Arc::clone(&self.write_fail).store(true, std::sync::atomic::Ordering::SeqCst)
					}
				}
			}
		}
    }

    fn flush(&self) {
        if !self.write_fail.load(std::sync::atomic::Ordering::SeqCst) {
			if let Some(file) = self.output.as_ref().as_mut() {
				if let Err(e) = file.flush() {
					println!("{}", format!("[Error] Log Flush: Failed to flush the logger file\n{:?}",e).red());
				}
			}
		}
    }
}

#[derive(clap::ValueEnum, Debug, Clone, Copy)]
pub enum LogLevel {
	Important,
	Info,
	Debug,
	Trace
}
