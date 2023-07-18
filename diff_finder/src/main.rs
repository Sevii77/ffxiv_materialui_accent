use std::{path::Path, fs::File, io::{Read, Write, BufReader}, collections::HashMap};
use colored::*;

fn main() -> Result<(), Box<dyn std::error::Error>> {
	let args: Vec<_> = std::env::args().collect();
	
	if args.len() != 4 {
		println!("Usage:\n\t--create <ttmp, penumbra, materialui> <game directory>\n\t--check <old path> <new path>");
		return Ok(());
	}
	
	if args[1] == "--create" {
		let path = Path::new(&args[2]);
		if !path.exists() {
			println!("Invalid path provided");
			return Ok(());
		}
		
		let game_path = Path::new(&args[3]);
		if !game_path.exists() {
			println!("Invalid game directory provided");
			return Ok(());
		}
		
		let game = ironworks::Ironworks::new()
			.with_resource(ironworks::sqpack::SqPack::new(ironworks::ffxiv::FsResource::at(&game_path)));
		
		if game.file::<Vec<u8>>("common/font/font1.tex").is_err() {
			println!("Invalid game directory provided");
			return Ok(());
		}
		
		let name = path.file_name().unwrap().to_string_lossy();
		
		let mut paths = Vec::new();
		if path.extension().map(|v| v.to_str().unwrap()) == Some("ttmp2") {
			// ttmp
			let mut ttmp = zip::ZipArchive::new(File::open(&path)?).expect("ttmp2 file should be zip file");
			let mut mpl = ttmp.by_name("TTMPL.mpl").expect("ttmp2 file should contain TTMPL.mpl file");
			let mpl: serde_json::Value = serde_json::from_reader(&mut mpl)?;
			
			for file in mpl["SimpleModsList"].as_array().expect("Should be simple modpack, others are currently unsupported") {
				// let dat = file["DatFile"].as_str().ok_or("invalid str")?;
				// let cate = dat[0..2].parse::<u8>()?;
				// let repo = dat[2..4].parse::<u8>()?;
				// let chunk = dat[4..6].parse::<u8>()?;
				let path = file["FullPath"].as_str().ok_or("invalid str")?;
				
				println!("Adding {path}");
				paths.push(path.to_owned());
			}
		} else if path.join("meta.json").exists() {
			// penumbra
			println!("TODO");
			return Ok(());
		} else if path.join("ui").exists() {
			// materialui
			for sub in std::fs::read_dir(path.join("ui/uld"))? {
				if let Ok(sub) = sub {
					let path = format!("ui/uld/{}_hr1.tex", sub.path().file_name().unwrap().to_string_lossy());
					println!("Adding {path}");
					paths.push(path);
					
				}
			}
			
			for sub in std::fs::read_dir(path.join("ui/icon"))? {
				if let Ok(sub) = sub {
					let icon = sub.path().file_name().unwrap().to_string_lossy().to_string();
					let icon_group = format!("{}000", &icon[0..3]);
					let path = format!("ui/icon/{icon_group}/{icon}_hr1.tex");
					println!("Adding {path}");
					paths.push(path);
				}
			}
		}
		
		let mut version = String::with_capacity(20);
		File::open(game_path.join("game").join("ffxivgame.ver"))?.read_to_string(&mut version)?;
		let mut f = File::create(&format!("./{name} [{version}]")).unwrap();
		f.write_all("FMDC".as_bytes())?; // ffxiv mod difference checker magic
		f.write_all(&(paths.len() as u32).to_le_bytes())?;
		for path in paths {
			// let segs = path.rsplitn(2, "/").map(|v| (!crc32fast::hash(v.as_bytes())) as u64).collect::<Vec<_>>();
			// let path_hash = segs[1] << 32 | segs[0];
			// f.write_all(&path_hash.to_le_bytes())?;
			f.write_all(&(path.len() as u8).to_le_bytes())?;
			f.write_all(&path.as_bytes())?;
			f.write_all(blake3::hash(&game.file::<Vec<u8>>(&path)?).as_bytes())?;
		}
	} else if args[1] == "--check" {
		let mut buf = [0u8; 4];
		let mut path_buf = [0u8; 256];
		let mut hash_buf = [0u8; 32];
		
		let mut old_file = BufReader::new(File::open(&args[2])?);
		old_file.read_exact(&mut buf)?;
		if &buf != b"FMDC" {
			println!("Invalid old file provided");
			return Ok(());
		}
		
		let mut new_file = BufReader::new(File::open(&args[3])?);
		new_file.read_exact(&mut buf)?;
		if &buf != b"FMDC" {
			println!("Invalid new file provided");
			return Ok(());
		}
		
		let mut old = HashMap::new();
		old_file.read_exact(&mut buf)?;
		for _ in 0..(u32::from_le_bytes(buf.clone())) {
			let mut len_buf = [0u8; 1];
			old_file.read_exact(&mut len_buf)?;
			let len = u8::from_le_bytes(len_buf) as usize;
			old_file.read_exact(&mut path_buf[0..len])?;
			let path = String::from_utf8_lossy(&path_buf[0..len]);
			old_file.read_exact(&mut hash_buf)?;
			old.insert(path.to_string(), hash_buf.clone());
		}
		
		let mut new = HashMap::new();
		new_file.read_exact(&mut buf)?;
		for _ in 0..(u32::from_le_bytes(buf.clone())) {
			let mut len_buf = [0u8; 1];
			new_file.read_exact(&mut len_buf)?;
			let len = u8::from_le_bytes(len_buf) as usize;
			new_file.read_exact(&mut path_buf[0..len])?;
			let path = String::from_utf8_lossy(&path_buf[0..len]);
			new_file.read_exact(&mut hash_buf)?;
			new.insert(path.to_string(), hash_buf.clone());
		}
		
		let mut changed = Vec::new();
		for (path, old_hash) in old {
			if let Some(new_hash) = new.get(&path) {
				if old_hash == *new_hash {
					println!("[{}] {path} {}", "✓".bright_green(), "is the same".bright_green());
				} else {
					// println!("[{}] {path} {}", "✗".bright_red(), "has changed".bright_red());
					changed.push(path);
				}
			}
		}
		
		changed.sort();
		for path in changed {
			println!("[{}] {path} {}", "✗".bright_red(), "has changed".bright_red());
		}
	} else {
		println!("Unknown mode {}", args[1]);
	}
	
	Ok(())
}
